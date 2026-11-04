import type { ImportResponse } from "@/api/generated";
import { toUtcDay, type UtcRange } from "@/tides/utcDay";

// Matches the API's import window, which is the previous, current and next UTC month.
export function importWindow(now: Date): UtcRange {
  const year = now.getUTCFullYear();
  const month = now.getUTCMonth();

  return {
    from: toUtcDay(new Date(Date.UTC(year, month - 1, 1))),
    to: toUtcDay(new Date(Date.UTC(year, month + 2, 1))),
  };
}

function toTime(day: string): number {
  return Date.parse(`${day}T00:00:00Z`);
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
