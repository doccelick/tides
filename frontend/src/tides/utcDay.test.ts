import { describe, expect, it } from "vitest";
import { addDays, toUtcDay } from "./utcDay";

describe("toUtcDay", () => {
  it("follows the UTC day, not the local day", () => {
    expect(toUtcDay(new Date("2026-10-19T23:30:00Z"))).toBe("2026-10-19");
  });
});

describe("addDays", () => {
  it("moves forward across a month end", () => {
    expect(addDays("2026-10-30", 7)).toBe("2026-11-06");
  });

  it("moves back across a year start", () => {
    expect(addDays("2027-01-03", -7)).toBe("2026-12-27");
  });

  it("keeps whole days across the change from summer time", () => {
    expect(addDays("2026-10-24", 2)).toBe("2026-10-26");
  });
});
