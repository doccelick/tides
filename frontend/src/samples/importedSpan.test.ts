import { describe, expect, it } from "vitest";
import type { ImportResponse } from "@/api/generated";
import { importedSpan } from "./importedSpan";

function stored(from: string, to: string): ImportResponse {
  return {
    id: 1,
    locationId: 3,
    from: `${from}T00:00:00+00:00`,
    to: `${to}T00:00:00+00:00`,
    importedAt: "2026-10-30T12:00:00+00:00",
    entryCount: 1008,
  };
}

describe("importedSpan", () => {
  it("returns null without imports", () => {
    expect(importedSpan([], "2026-11-01")).toBeNull();
  });

  it("returns the import that includes the day", () => {
    const imports = [stored("2026-10-26", "2026-11-02")];

    expect(importedSpan(imports, "2026-11-01")).toEqual({ from: "2026-10-26", to: "2026-11-02" });
  });

  it("returns null for the day an import ends on", () => {
    const imports = [stored("2026-10-26", "2026-11-01")];

    expect(importedSpan(imports, "2026-11-01")).toBeNull();
  });

  it("joins imports that touch or overlap, in any order", () => {
    const imports = [
      stored("2026-11-08", "2026-12-01"),
      stored("2026-10-26", "2026-11-02"),
      stored("2026-11-01", "2026-11-08"),
    ];

    expect(importedSpan(imports, "2026-11-01")).toEqual({ from: "2026-10-26", to: "2026-12-01" });
  });

  it("stops at a gap between imports", () => {
    const imports = [stored("2026-10-01", "2026-10-08"), stored("2026-10-26", "2026-11-02")];

    expect(importedSpan(imports, "2026-10-03")).toEqual({ from: "2026-10-01", to: "2026-10-08" });
    expect(importedSpan(imports, "2026-11-01")).toEqual({ from: "2026-10-26", to: "2026-11-02" });
  });

  it("returns null for a day in a gap", () => {
    const imports = [stored("2026-10-01", "2026-10-08"), stored("2026-10-26", "2026-11-02")];

    expect(importedSpan(imports, "2026-10-15")).toBeNull();
  });
});
