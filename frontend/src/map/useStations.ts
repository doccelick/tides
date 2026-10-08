import type { StationResponse, StationsResponse } from "@/api/generated";
import { useEffect, useState } from "react";

export function useStations() {
  const [stations, setStations] = useState<StationResponse[]>([]);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    const controller = new AbortController();

    const fetchData = async () => {
      try {
        const response = await fetch("/v1/stations", { signal: controller.signal });

        if (!response.ok) {
          throw new Error(`Stations request failed with ${response.status}`);
        }

        const result: StationsResponse = await response.json();
        setStations(result.stations);
      } catch (error: unknown) {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }
        setFailed(true);
      }
    };

    fetchData();

    return () => controller.abort();
  }, []);

  return { stations, failed };
}
