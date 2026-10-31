import type { LocationResponse, LocationsResponse } from "@/api/generated";
import { useEffect, useState } from "react";

export function useLocations() {
  const [locations, setLocations] = useState<LocationResponse[]>([]);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    const controller = new AbortController();

    const fetchData = async () => {
      try {
        const response = await fetch("/v1/locations", { signal: controller.signal });

        if (!response.ok) {
          throw new Error(`Locations request failed with ${response.status}`);
        }

        const result: LocationsResponse = await response.json();
        setLocations(result.locations);
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

  function add(location: LocationResponse) {
    setLocations((current) => [...current, location]);
  }

  return { locations, failed, add };
}
