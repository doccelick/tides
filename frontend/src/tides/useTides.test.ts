import { act, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { TideResponse, TidesResponse } from "@/api/generated";
import { useTides } from "./useTides";

function tidesResponse(tides: TideResponse[]): Response {
  const body: TidesResponse = { timeZone: "UTC", unit: "cm", reference: "CD", tides };
  return Response.json(body);
}

const today: TideResponse = { type: "High", time: "2026-10-19T03:48:00+00:00", levelCm: 223 };
const lastWeek: TideResponse = { type: "Low", time: "2026-10-12T09:45:00+00:00", levelCm: 143 };
const nextWeek: TideResponse = { type: "Low", time: "2026-10-22T09:45:00+00:00", levelCm: 150 };

async function renderLoaded(fetch: ReturnType<typeof vi.fn>) {
  vi.stubGlobal("fetch", fetch);
  const { result } = renderHook(() => useTides("BGO"));
  await waitFor(() => expect(result.current.state.status).toBe("loaded"));
  return result;
}

describe("useTides", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date("2026-10-19T10:00:00Z"));
  });

  afterEach(() => vi.useRealTimers());

  it("loads today and tomorrow first", async () => {
    const fetch = vi.fn().mockResolvedValueOnce(tidesResponse([today]));
    await renderLoaded(fetch);

    expect(fetch).toHaveBeenCalledWith("/v1/stations/BGO/tides?from=2026-10-19&days=2", {
      signal: expect.any(AbortSignal),
    });
  });

  it("adds the 7 days before the first day in front", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(tidesResponse([today]))
      .mockResolvedValueOnce(tidesResponse([lastWeek]));
    const result = await renderLoaded(fetch);

    await act(() => result.current.loadEarlier());

    expect(fetch).toHaveBeenLastCalledWith("/v1/stations/BGO/tides?from=2026-10-12&days=7", {
      signal: expect.any(AbortSignal),
    });
    expect(result.current.state).toMatchObject({ tides: [lastWeek, today], from: "2026-10-12" });
    expect(result.current.earlier).toBe("idle");
  });

  it("adds the 7 days after the last day at the end", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(tidesResponse([today]))
      .mockResolvedValueOnce(tidesResponse([nextWeek]));
    const result = await renderLoaded(fetch);

    await act(() => result.current.loadLater());

    expect(fetch).toHaveBeenLastCalledWith("/v1/stations/BGO/tides?from=2026-10-21&days=7", {
      signal: expect.any(AbortSignal),
    });
    expect(result.current.state).toMatchObject({ tides: [today, nextWeek], end: "2026-10-28" });
    expect(result.current.later).toBe("idle");
  });

  it("ends an edge when the station has no tide data past it", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(tidesResponse([today]))
      .mockResolvedValueOnce(new Response(null, { status: 404 }));
    const result = await renderLoaded(fetch);

    await act(() => result.current.loadEarlier());

    expect(result.current.earlier).toBe("end");
    expect(result.current.state).toMatchObject({ tides: [today] });
  });

  it("keeps the loaded tides when loading more days fails", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(tidesResponse([today]))
      .mockResolvedValueOnce(new Response(null, { status: 502 }));
    const result = await renderLoaded(fetch);

    await act(() => result.current.loadLater());

    expect(result.current.later).toBe("failed");
    expect(result.current.state).toMatchObject({ tides: [today] });
  });
});
