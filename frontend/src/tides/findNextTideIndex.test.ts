import { describe, expect, it } from "vitest";
import type { TideResponse } from "@/api/generated";
import { findNextTideIndex } from "./findNextTideIndex";

describe("findNextTideIndex", () => {
  const tides: TideResponse[] = [
    { type: "High", time: "2026-10-19T03:48:00+00:00", levelCm: 54 },
    { type: "Low", time: "2026-10-19T09:45:00+00:00", levelCm: -26 },
    { type: "High", time: "2026-10-19T16:02:00+00:00", levelCm: 61 },
    { type: "Low", time: "2026-10-19T22:10:00+00:00", levelCm: -31 },
  ];

  it("returns the first tide after now", () => {
    const now = new Date("2026-10-19T10:00:00Z");

    expect(findNextTideIndex(tides, now)).toBe(2);
  });

  it("returns the tide happening exactly now", () => {
    const now = new Date(tides[1].time);

    expect(findNextTideIndex(tides, now)).toBe(1);
  });

  it("returns -1 when every tide is past", () => {
    const now = new Date("2026-10-20T10:00:00Z");

    expect(findNextTideIndex(tides, now)).toBe(-1);
  });

  it("returns -1 for no tides", () => {
    const now = new Date("2026-10-19T03:48:00+00:00");

    expect(findNextTideIndex([], now)).toBe(-1);
  });
});
