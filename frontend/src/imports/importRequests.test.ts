import { describe, expect, it, vi } from "vitest";
import { createImport } from "./importRequests";

describe("createImport", () => {
  it("sends the range as UTC midnights", async () => {
    const fetch = vi.fn().mockResolvedValue(Response.json({ imports: [] }, { status: 201 }));
    vi.stubGlobal("fetch", fetch);

    await createImport(3, { from: "2026-11-01", to: "2026-12-01" });

    expect(fetch).toHaveBeenCalledWith(
      "/v1/locations/3/imports",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ from: "2026-11-01T00:00:00Z", to: "2026-12-01T00:00:00Z" }),
      }),
    );
  });
});
