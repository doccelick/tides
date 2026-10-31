import { describe, expect, it } from "vitest";
import { dayTicks } from "./dayTicks";

function ticksAsIso(start: string, end: string): string[] {
  return dayTicks(Date.parse(start), Date.parse(end)).map((tick) => new Date(tick).toISOString());
}

describe("dayTicks", () => {
  it("returns each midnight in Oslo within the range", () => {
    expect(ticksAsIso("2026-11-01T00:00:00Z", "2026-11-03T22:50:00Z")).toEqual([
      "2026-11-01T23:00:00.000Z",
      "2026-11-02T23:00:00.000Z",
    ]);
  });

  it("includes a start that falls on midnight", () => {
    expect(ticksAsIso("2026-11-01T23:00:00Z", "2026-11-02T22:00:00Z")).toEqual([
      "2026-11-01T23:00:00.000Z",
    ]);
  });

  it("keeps ticks on midnight across the end of summer time", () => {
    expect(ticksAsIso("2026-10-24T00:00:00Z", "2026-10-26T23:50:00Z")).toEqual([
      "2026-10-24T22:00:00.000Z",
      "2026-10-25T23:00:00.000Z",
      "2026-10-26T23:00:00.000Z",
    ]);
  });
});
