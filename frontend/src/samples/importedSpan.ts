import type { ImportResponse } from "@/api/generated";
import type { UtcRange } from "@/imports/importPresets";
import { addDays, toUtcDay } from "@/tides/utcDay";

// Returns the longest run of imported days without gaps that includes the day,
// or null when the day isn't imported. Imports that overlap or touch join the same run.
export function importedSpan(imports: ImportResponse[], day: string): UtcRange | null {
  const dayStart = Date.parse(`${day}T00:00:00Z`);
  const dayEnd = Date.parse(`${addDays(day, 1)}T00:00:00Z`);
  const sorted = [...imports].sort((a, b) => Date.parse(a.from) - Date.parse(b.from));

  let spanFrom: number | null = null;
  let spanTo = 0;

  for (const stored of sorted) {
    const from = Date.parse(stored.from);
    const to = Date.parse(stored.to);

    if (spanFrom === null || from > spanTo) {
      if (spanFrom !== null && spanFrom <= dayStart && spanTo >= dayEnd) break;
      spanFrom = from;
      spanTo = to;
    } else {
      spanTo = Math.max(spanTo, to);
    }
  }

  if (spanFrom === null || spanFrom > dayStart || spanTo < dayEnd) return null;

  return { from: toUtcDay(new Date(spanFrom)), to: toUtcDay(new Date(spanTo)) };
}
