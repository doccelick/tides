import { describe, expect, it } from "vitest";
import type { ImportResponse } from "@/api/generated";
import { importWindow, isImported } from "./importWindow";

function storedImport(from: string, to: string): ImportResponse {
  return {
    id: 1,
    locationId: 3,
    from: `${from}T00:00:00+00:00`,
    to: `${to}T00:00:00+00:00`,
    importedAt: "2026-10-30T18:00:00+00:00",
    entryCount: 0,
  };
}

describe("importWindow", () => {
  it("runs from the previous month through the next month", () => {
    expect(importWindow(new Date("2026-11-15T13:45:00Z"))).toEqual({
      from: "2026-10-01",
      to: "2027-01-01",
    });
  });

  it("crosses into the previous year in January", () => {
    expect(importWindow(new Date("2027-01-10T00:00:00Z"))).toEqual({
      from: "2026-12-01",
      to: "2027-03-01",
    });
  });

  it("uses the UTC month when local time is already in the next month", () => {
    // 00:30 on 1 December at UTC+1 is still 30 November in UTC.
    expect(importWindow(new Date("2026-12-01T00:30:00+01:00"))).toEqual({
      from: "2026-10-01",
      to: "2027-01-01",
    });
  });
});

describe("isImported", () => {
  const range = { from: "2026-11-01", to: "2026-11-08" };

  it("is false without imports", () => {
    expect(isImported(range, [])).toBe(false);
  });

  it("is true when one import covers the range", () => {
    expect(isImported(range, [storedImport("2026-10-25", "2026-11-10")])).toBe(true);
  });

  it("is true when adjacent imports cover the range", () => {
    const imports = [
      storedImport("2026-11-04", "2026-11-08"),
      storedImport("2026-11-01", "2026-11-04"),
    ];

    expect(isImported(range, imports)).toBe(true);
  });

  it("is false when imports leave a gap", () => {
    const imports = [
      storedImport("2026-11-01", "2026-11-03"),
      storedImport("2026-11-04", "2026-11-08"),
    ];

    expect(isImported(range, imports)).toBe(false);
  });

  it("is false when an import covers only part of the range", () => {
    expect(isImported(range, [storedImport("2026-11-01", "2026-11-05")])).toBe(false);
  });
});
