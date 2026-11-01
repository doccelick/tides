import { describe, expect, it, vi } from "vitest";
import type { SamplesResponse } from "@/api/generated";
import { fetchSamples } from "./fetchSamples";

const samples: SamplesResponse = {
  locationId: 3,
  timeZone: "UTC",
  unit: "cm",
  reference: "CD",
  intervalMinutes: 60,
  samples: [{ time: "2026-11-01T00:00:00+00:00", levelCm: 248 }],
};

describe("fetchSamples", () => {
  it("asks for the range as a first day and a number of days", async () => {
    const fetch = vi.fn().mockResolvedValue(Response.json(samples));
    vi.stubGlobal("fetch", fetch);

    const result = await fetchSamples(3, { from: "2026-11-01", to: "2026-12-01" }, 60);

    expect(fetch).toHaveBeenCalledWith(
      "/v1/locations/3/samples?from=2026-11-01&days=30&interval=60",
      { signal: undefined },
    );
    expect(result).toEqual(samples);
  });

  it("returns null when the range isn't imported", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 404 })));

    const result = await fetchSamples(3, { from: "2026-11-01", to: "2026-11-08" }, 10);

    expect(result).toBeNull();
  });

  it("throws when the request fails", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 500 })));

    await expect(fetchSamples(3, { from: "2026-11-01", to: "2026-11-08" }, 10)).rejects.toThrow(
      "Samples request failed with 500",
    );
  });
});
