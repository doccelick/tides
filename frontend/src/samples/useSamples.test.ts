import { renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { SampleResponse, SamplesResponse } from "@/api/generated";
import type { UtcRange } from "@/imports/importPresets";
import type { SampleInterval } from "./fetchSamples";
import { useSamples } from "./useSamples";

function samplesResponse(samples: SampleResponse[]): Response {
  const body: SamplesResponse = {
    locationId: 3,
    timeZone: "UTC",
    unit: "cm",
    reference: "CD",
    intervalMinutes: 10,
    samples,
  };
  return Response.json(body);
}

const nextWeek: UtcRange = { from: "2026-11-01", to: "2026-11-08" };
const midnight: SampleResponse = { time: "2026-11-01T00:00:00+00:00", levelCm: 248 };
const tenPast: SampleResponse = { time: "2026-11-01T00:10:00+00:00", levelCm: 245 };

describe("useSamples", () => {
  it("loads the samples for the range", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(samplesResponse([midnight, tenPast])));

    const { result } = renderHook(() => useSamples(3, nextWeek, 10));

    expect(result.current.status).toBe("loading");
    await waitFor(() =>
      expect(result.current).toEqual({ status: "loaded", samples: [midnight, tenPast] }),
    );
  });

  it("reports no data when the range isn't imported", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 404 })));

    const { result } = renderHook(() => useSamples(3, nextWeek, 10));

    await waitFor(() => expect(result.current.status).toBe("no-data"));
  });

  it("reports a failure when the request fails", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 500 })));

    const { result } = renderHook(() => useSamples(3, nextWeek, 10));

    await waitFor(() => expect(result.current.status).toBe("failed"));
  });

  it("fetches again when the interval changes", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(samplesResponse([midnight, tenPast]))
      .mockResolvedValueOnce(samplesResponse([midnight]));
    vi.stubGlobal("fetch", fetch);

    const { result, rerender } = renderHook(
      ({ interval }: { interval: SampleInterval }) => useSamples(3, nextWeek, interval),
      { initialProps: { interval: 10 } },
    );
    await waitFor(() => expect(result.current.status).toBe("loaded"));

    rerender({ interval: 60 });

    expect(fetch).toHaveBeenLastCalledWith(
      "/v1/locations/3/samples?from=2026-11-01&days=7&interval=60",
      { signal: expect.any(AbortSignal) },
    );
    await waitFor(() => expect(result.current).toEqual({ status: "loaded", samples: [midnight] }));
  });

  it("doesn't fetch again when the parent passes an equal range", async () => {
    const fetch = vi.fn().mockResolvedValue(samplesResponse([midnight]));
    vi.stubGlobal("fetch", fetch);

    const { result, rerender } = renderHook(
      ({ range }: { range: UtcRange }) => useSamples(3, range, 10),
      { initialProps: { range: nextWeek } },
    );
    await waitFor(() => expect(result.current.status).toBe("loaded"));

    rerender({ range: { ...nextWeek } });

    expect(fetch).toHaveBeenCalledTimes(1);
  });
});
