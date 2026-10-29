import type { TideResponse, TidesResponse } from "@/api/generated";

// Returns null when the station has no tide data for the range.
export async function fetchTides(
  code: string,
  from: string,
  days: number,
  signal?: AbortSignal,
): Promise<TideResponse[] | null> {
  const response = await fetch(`/v1/stations/${code}/tides?from=${from}&days=${days}`, { signal });

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`Tides request failed with ${response.status}`);
  }

  const result: TidesResponse = await response.json();
  return result.tides;
}
