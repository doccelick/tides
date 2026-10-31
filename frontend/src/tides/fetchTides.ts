import type { TideResponse, TideSource, TidesResponse } from "@/api/generated";

// The source is per response, and the strip joins several responses.
export type SourcedTide = TideResponse & { source: TideSource };

// `path` is the station or location, for example `/v1/stations/BGO` or `/v1/locations/3`.
// Returns null when there is no tide data for the range.
export async function fetchTides(
  path: string,
  from: string,
  days: number,
  signal?: AbortSignal,
): Promise<SourcedTide[] | null> {
  const response = await fetch(`${path}/tides?from=${from}&days=${days}`, { signal });

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`Tides request failed with ${response.status}`);
  }

  const result: TidesResponse = await response.json();
  return result.tides.map((tide) => ({ ...tide, source: result.source }));
}
