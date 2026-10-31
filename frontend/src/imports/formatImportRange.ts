import { addDays, toUtcDay } from "@/tides/utcDay";

const dayFormat = new Intl.DateTimeFormat("en-GB", {
  day: "numeric",
  month: "short",
  timeZone: "UTC",
});

const dayWithYearFormat = new Intl.DateTimeFormat("en-GB", {
  day: "numeric",
  month: "short",
  year: "numeric",
  timeZone: "UTC",
});

// Imports cover whole UTC days, and `to` is the start of the first day after the range.
export function formatImportRange(from: string, to: string): string {
  const firstDay = toUtcDay(new Date(from));
  const lastDay = addDays(toUtcDay(new Date(to)), -1);
  const first = new Date(`${firstDay}T00:00:00Z`);
  const last = new Date(`${lastDay}T00:00:00Z`);

  if (firstDay === lastDay) return dayWithYearFormat.format(first);

  const sameYear = first.getUTCFullYear() === last.getUTCFullYear();
  const start = sameYear ? dayFormat.format(first) : dayWithYearFormat.format(first);
  return `${start} – ${dayWithYearFormat.format(last)}`;
}
