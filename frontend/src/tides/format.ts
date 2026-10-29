const timeFormat = new Intl.DateTimeFormat("en-GB", {
  hour: "2-digit",
  minute: "2-digit",
});

const relativeDayFormat = new Intl.RelativeTimeFormat("en", {
  numeric: "auto",
});

const dateFormat = new Intl.DateTimeFormat("en-GB", {
  weekday: "short",
  day: "numeric",
  month: "short",
});

const levelFormat = new Intl.NumberFormat("en-GB", {
  style: "unit",
  unit: "centimeter",
  unitDisplay: "short",
  signDisplay: "negative",
  maximumFractionDigits: 0,
});

const millisecondsPerDay = 24 * 60 * 60 * 1000;

export function formatTideTime(time: string): string {
  return timeFormat.format(new Date(time));
}

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

// Names the day relative to today when it is within a day of it, and by weekday and date otherwise.
export function formatTideDay(tide: string, now: Date): string {
  const tideDay = startOfDay(new Date(tide));
  const today = startOfDay(now);
  const duration = tideDay.getTime() - today.getTime();
  const days = Math.round(duration / millisecondsPerDay);
  if (Math.abs(days) <= 1) {
    return relativeDayFormat.format(days, "day");
  }
  return dateFormat.format(tideDay);
}

export function formatTideLevel(levelCm: number): string {
  return levelFormat.format(levelCm);
}
