// A UTC day is a date string in the form YYYY-MM-DD, as the tide endpoint's `from` parameter takes it.

export function toUtcDay(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function addDays(day: string, days: number): string {
  const date = new Date(`${day}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return toUtcDay(date);
}
