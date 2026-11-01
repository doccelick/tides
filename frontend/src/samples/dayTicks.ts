// Returns each local midnight from start to end, both included, as milliseconds since the epoch.
// Days are stepped in local time, so a day with a clock change still starts at midnight.
export function dayTicks(start: number, end: number): number[] {
  const day = new Date(start);
  day.setHours(0, 0, 0, 0);
  if (day.getTime() < start) day.setDate(day.getDate() + 1);

  const ticks: number[] = [];
  while (day.getTime() <= end) {
    ticks.push(day.getTime());
    day.setDate(day.getDate() + 1);
  }
  return ticks;
}
