import { useEffect, useState } from "react";
import type { SampleResponse } from "@/api/generated";
import type { UtcRange } from "@/imports/importPresets";
import { fetchSamples, type SampleInterval } from "./fetchSamples";

type SamplesState =
  | { status: "loading" }
  | { status: "loaded"; samples: SampleResponse[] }
  | { status: "no-data" }
  | { status: "failed" };

// Fetches again when the range or interval changes. The current samples stay shown until the new ones have loaded.
export function useSamples(locationId: number, range: UtcRange, interval: SampleInterval) {
  const [state, setState] = useState<SamplesState>({ status: "loading" });
  const { from, to } = range;

  useEffect(() => {
    const controller = new AbortController();

    const fetchData = async () => {
      try {
        const result = await fetchSamples(locationId, { from, to }, interval, controller.signal);

        if (result === null) {
          setState({ status: "no-data" });
          return;
        }

        setState({ status: "loaded", samples: result.samples });
      } catch (error: unknown) {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }
        setState({ status: "failed" });
      }
    };

    fetchData();

    return () => controller.abort();
  }, [locationId, from, to, interval]);

  return state;
}
