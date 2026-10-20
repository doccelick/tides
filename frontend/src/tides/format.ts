const timeFormat = new Intl.DateTimeFormat("en-GB", {
  hour: "2-digit",
  minute: "2-digit",
});

const dayFormat = new Intl.RelativeTimeFormat("en", {
  numeric: "auto",
});

const levelFormat = new Intl.NumberFormat("en-GB", {
  style: "unit",
  unit: "centimeter",
  unitDisplay: "short",
  signDisplay: "exceptZero",
  maximumFractionDigits: 0,
});

const millisecondsPerDay = 24 * 60 * 60 * 1000;

export function formatTideTime(time: string): string {
  return timeFormat.format(new Date(time));
}

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

export function formatTideDay(tide: string, now: Date): string {
  const tideDay = startOfDay(new Date(tide));
  const today = startOfDay(now);
  const duration = tideDay.getTime() - today.getTime();
  const days = Math.round(duration / millisecondsPerDay);
  return dayFormat.format(days, "day");
}

export function formatTideLevel(levelCm: number): string {
  return levelFormat.format(levelCm);
}
