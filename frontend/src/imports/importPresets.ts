import type { ImportResponse } from "@/api/generated";
import { addDays, toUtcDay } from "@/tides/utcDay";

export type PresetUnit = "day" | "week" | "month";

// A preset is plain data, so the list can be stored and edited outside the code.
export interface ImportPreset {
  label: string;
  group: string;
  // Counted from the start of the current unit: today, this week's Monday or the 1st of this month.
  start: { unit: PresetUnit; offset: number };
  length: { unit: PresetUnit; count: number };
}

// UTC days in the form YYYY-MM-DD. `from` is included and `to` is not.
export interface UtcRange {
  from: string;
  to: string;
}

// The API imports at most this many days in one request.
const maxImportDays = 31;

const millisecondsPerDay = 24 * 60 * 60 * 1000;

export const importPresets: ImportPreset[] = [
  {
    label: "Next 7 days",
    group: "Coming days",
    start: { unit: "day", offset: 0 },
    length: { unit: "day", count: 7 },
  },
  {
    label: "Next 31 days",
    group: "Coming days",
    start: { unit: "day", offset: 0 },
    length: { unit: "day", count: 31 },
  },
  {
    label: "Last week",
    group: "Weeks",
    start: { unit: "week", offset: -1 },
    length: { unit: "week", count: 1 },
  },
  {
    label: "This week",
    group: "Weeks",
    start: { unit: "week", offset: 0 },
    length: { unit: "week", count: 1 },
  },
  {
    label: "Next week",
    group: "Weeks",
    start: { unit: "week", offset: 1 },
    length: { unit: "week", count: 1 },
  },
  {
    label: "Last month",
    group: "Months",
    start: { unit: "month", offset: -1 },
    length: { unit: "month", count: 1 },
  },
  {
    label: "This month",
    group: "Months",
    start: { unit: "month", offset: 0 },
    length: { unit: "month", count: 1 },
  },
  {
    label: "Next month",
    group: "Months",
    start: { unit: "month", offset: 1 },
    length: { unit: "month", count: 1 },
  },
];

function startOfUnit(day: string, unit: PresetUnit): string {
  if (unit === "day") return day;

  if (unit === "week") {
    // getUTCDay counts from Sunday, and weeks start on Monday.
    const daysSinceMonday = (new Date(`${day}T00:00:00Z`).getUTCDay() + 6) % 7;
    return addDays(day, -daysSinceMonday);
  }

  return `${day.slice(0, 8)}01`;
}

function addUnits(day: string, unit: PresetUnit, count: number): string {
  if (unit === "day") return addDays(day, count);
  if (unit === "week") return addDays(day, count * 7);

  const date = new Date(`${day}T00:00:00Z`);
  date.setUTCMonth(date.getUTCMonth() + count);
  return toUtcDay(date);
}

function toTime(day: string): number {
  return Date.parse(`${day}T00:00:00Z`);
}

// Returns null when the range is longer than the API imports in one request.
export function resolvePreset(preset: ImportPreset, now: Date): UtcRange | null {
  const unitStart = startOfUnit(toUtcDay(now), preset.start.unit);
  const from = addUnits(unitStart, preset.start.unit, preset.start.offset);
  const to = addUnits(from, preset.length.unit, preset.length.count);

  const days = (toTime(to) - toTime(from)) / millisecondsPerDay;
  if (days > maxImportDays) return null;

  return { from, to };
}

// True when the imports cover the whole range, so importing it would add nothing.
export function isImported(range: UtcRange, imports: ImportResponse[]): boolean {
  const sorted = [...imports].sort((a, b) => Date.parse(a.from) - Date.parse(b.from));
  let coveredUntil = toTime(range.from);

  for (const stored of sorted) {
    if (Date.parse(stored.from) > coveredUntil) break;
    coveredUntil = Math.max(coveredUntil, Date.parse(stored.to));
  }

  return coveredUntil >= toTime(range.to);
}
