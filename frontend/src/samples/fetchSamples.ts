import type { GetLocationSamplesData, SamplesResponse } from "@/api/generated";
import type { UtcRange } from "@/imports/importPresets";

export type SampleInterval = NonNullable<NonNullable<GetLocationSamplesData["query"]>["interval"]>;

const millisecondsPerDay = 24 * 60 * 60 * 1000;

// Returns null when the location's imports don't cover the whole range.
export async function fetchSamples(
  locationId: number,
  range: UtcRange,
  interval: SampleInterval,
  signal?: AbortSignal,
): Promise<SamplesResponse | null> {
  const days =
    (Date.parse(`${range.to}T00:00:00Z`) - Date.parse(`${range.from}T00:00:00Z`)) /
    millisecondsPerDay;
  const response = await fetch(
    `/v1/locations/${locationId}/samples?from=${range.from}&days=${days}&interval=${interval}`,
    { signal },
  );

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`Samples request failed with ${response.status}`);
  }

  return response.json();
}
