import { act, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ImportResponse, SampleResponse, SamplesResponse } from "@/api/generated";
import { useSampleStrip } from "./useSampleStrip";

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

// One sample at midnight on the first day of the request, so tests can tell responses apart.
function sampleOn(day: string): SampleResponse {
  return { time: `${day}T00:00:00+00:00`, levelCm: 200 };
}

// Answers the imports request with the imports, and each samples request with one sample on its first day.
function stubApi(imports: ImportResponse[]) {
  const fetch = vi.fn((url: string) => {
    if (url.endsWith("/imports")) return Promise.resolve(Response.json({ imports }));

    const from = new URL(url, "http://localhost").searchParams.get("from") ?? "";
    const body: SamplesResponse = {
      locationId: 3,
      timeZone: "UTC",
      unit: "cm",
      reference: "CD",
      intervalMinutes: 10,
      samples: [sampleOn(from)],
    };
    return Promise.resolve(Response.json(body));
  });
  vi.stubGlobal("fetch", fetch);
  return fetch;
}

async function renderLoaded() {
  const { result } = renderHook(() => useSampleStrip(3));
  await waitFor(() => expect(result.current.state.status).toBe("loaded"));
  return result;
}

describe("useSampleStrip", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date("2026-11-01T10:00:00Z"));
  });

  afterEach(() => vi.useRealTimers());

  it("loads the first 4 days from today", async () => {
    const fetch = stubApi([stored("2026-10-20", "2026-11-30")]);

    const result = await renderLoaded();

    expect(fetch).toHaveBeenLastCalledWith(
      "/v1/locations/3/samples?from=2026-11-01&days=4&interval=10",
      { signal: expect.any(AbortSignal) },
    );
    expect(result.current.state).toMatchObject({ samples: [sampleOn("2026-11-01")] });
    expect(result.current.earlier).toBe("idle");
    expect(result.current.later).toBe("idle");
  });

  it("stops at the imported days", async () => {
    const fetch = stubApi([stored("2026-11-01", "2026-11-03")]);

    const result = await renderLoaded();

    expect(fetch).toHaveBeenLastCalledWith(
      "/v1/locations/3/samples?from=2026-11-01&days=2&interval=10",
      { signal: expect.any(AbortSignal) },
    );
    expect(result.current.earlier).toBe("end");
    expect(result.current.later).toBe("end");
  });

  it("reports no data when today isn't imported", async () => {
    const fetch = stubApi([stored("2026-10-01", "2026-10-08")]);

    const { result } = renderHook(() => useSampleStrip(3));

    await waitFor(() => expect(result.current.state.status).toBe("no-data"));
    expect(fetch).toHaveBeenCalledTimes(1);
  });

  it("reports a failure when the imports can't be loaded", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 500 })));

    const { result } = renderHook(() => useSampleStrip(3));

    await waitFor(() => expect(result.current.state.status).toBe("failed"));
  });

  it("adds the 7 days before the first day in front", async () => {
    const fetch = stubApi([stored("2026-10-20", "2026-11-30")]);
    const result = await renderLoaded();

    await act(() => result.current.loadEarlier());

    expect(fetch).toHaveBeenLastCalledWith(
      "/v1/locations/3/samples?from=2026-10-25&days=7&interval=10",
      { signal: expect.any(AbortSignal) },
    );
    expect(result.current.state).toMatchObject({
      samples: [sampleOn("2026-10-25"), sampleOn("2026-11-01")],
      from: "2026-10-25",
    });
    expect(result.current.earlier).toBe("idle");
  });

  it("adds the days up to the end of the imports and stops there", async () => {
    const fetch = stubApi([stored("2026-10-20", "2026-11-10")]);
    const result = await renderLoaded();

    await act(() => result.current.loadLater());

    expect(fetch).toHaveBeenLastCalledWith(
      "/v1/locations/3/samples?from=2026-11-05&days=5&interval=10",
      { signal: expect.any(AbortSignal) },
    );
    expect(result.current.state).toMatchObject({
      samples: [sampleOn("2026-11-01"), sampleOn("2026-11-05")],
      end: "2026-11-10",
    });
    expect(result.current.later).toBe("end");
  });
});
