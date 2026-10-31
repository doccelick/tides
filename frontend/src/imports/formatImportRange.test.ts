import { describe, expect, it } from "vitest";
import { formatImportRange } from "./formatImportRange";

describe("formatImportRange", () => {
  it("shows the last day included, not the exclusive end", () => {
    expect(formatImportRange("2026-11-01T00:00:00+00:00", "2026-11-08T00:00:00+00:00")).toBe(
      "1 Nov – 7 Nov 2026",
    );
  });

  it("shows both years when the range crosses a year end", () => {
    expect(formatImportRange("2026-12-28T00:00:00+00:00", "2027-01-04T00:00:00+00:00")).toBe(
      "28 Dec 2026 – 3 Jan 2027",
    );
  });

  it("shows a single day once", () => {
    expect(formatImportRange("2026-11-01T00:00:00+00:00", "2026-11-02T00:00:00+00:00")).toBe(
      "1 Nov 2026",
    );
  });
});
