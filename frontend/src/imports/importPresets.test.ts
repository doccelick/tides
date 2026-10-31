import { describe, expect, it } from "vitest";
import type { ImportResponse } from "@/api/generated";
import { importPresets, isImported, resolvePreset, type ImportPreset } from "./importPresets";

function preset(label: string): ImportPreset {
  const found = importPresets.find((p) => p.label === label);
  if (found === undefined) throw new Error(`No preset named ${label}`);
  return found;
}

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

describe("resolvePreset", () => {
  // Saturday 31 October 2026.
  const now = new Date("2026-10-31T10:00:00Z");

  it.each([
    ["Next 7 days", "2026-10-31", "2026-11-07"],
    ["Next 31 days", "2026-10-31", "2026-12-01"],
    ["Last week", "2026-10-19", "2026-10-26"],
    ["This week", "2026-10-26", "2026-11-02"],
    ["Next week", "2026-11-02", "2026-11-09"],
    ["Last month", "2026-09-01", "2026-10-01"],
    ["This month", "2026-10-01", "2026-11-01"],
    ["Next month", "2026-11-01", "2026-12-01"],
  ])("resolves %s to %s up to %s", (label, from, to) => {
    expect(resolvePreset(preset(label), now)).toEqual({ from, to });
  });

  it("starts the week on Monday when today is Sunday", () => {
    const sunday = new Date("2026-11-01T10:00:00Z");

    expect(resolvePreset(preset("This week"), sunday)).toEqual({
      from: "2026-10-26",
      to: "2026-11-02",
    });
  });

  it("moves into the next year from December", () => {
    const december = new Date("2026-12-15T10:00:00Z");

    expect(resolvePreset(preset("Next month"), december)).toEqual({
      from: "2027-01-01",
      to: "2027-02-01",
    });
  });

  it("returns null for a range longer than 31 days", () => {
    const tooLong: ImportPreset = {
      label: "Next 32 days",
      group: "Coming days",
      start: { unit: "day", offset: 0 },
      length: { unit: "day", count: 32 },
    };

    expect(resolvePreset(tooLong, now)).toBeNull();
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
