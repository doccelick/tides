import { describe, expect, it } from "vitest";
import { formatTideDay, formatTideLevel, formatTideTime } from "./format";

describe("formatTideTime", () => {
  it("shows summer time in Oslo", () => {
    expect(formatTideTime("2026-10-19T12:32:00+00:00")).toBe("14:32");
  });

  it("shows winter time in Oslo", () => {
    expect(formatTideTime("2026-12-19T12:32:00+00:00")).toBe("13:32");
  });
});

describe("formatTideDay", () => {
  it("returns 'today' for a tide later the same day", () => {
    const tide = "2026-10-19T18:00:00+00:00";
    const now = new Date("2026-10-19T10:00:00Z");
    expect(formatTideDay(tide, now)).toBe("today");
  });

  it("returns 'tomorrow' for a tide the next day", () => {
    const tide = "2026-10-20T16:00:00+00:00";
    const now = new Date("2026-10-19T16:00:00Z");
    expect(formatTideDay(tide, now)).toBe("tomorrow");
  });

  it("returns 'yesterday' for a tide the day before", () => {
    const tide = "2026-10-17T16:00:00+00:00";
    const now = new Date("2026-10-18T16:00:00Z");
    expect(formatTideDay(tide, now)).toBe("yesterday");
  });

  it("follows the local day, not the UTC day", () => {
    const tide = "2026-10-19T23:30:00+00:00";
    const now = new Date("2026-10-19T12:00:00Z");
    expect(formatTideDay(tide, now)).toBe("tomorrow");
  });
});

describe("formatTideLevel", () => {
  it("shows a positive level without a sign", () => {
    expect(formatTideLevel(54.1)).toBe("54 cm");
  });

  it("adds a minus sign to a negative level", () => {
    expect(formatTideLevel(-26.2)).toBe("-26 cm");
  });

  it("shows zero without a sign", () => {
    expect(formatTideLevel(0)).toBe("0 cm");
  });
});
