import { useEffect, useState } from "react";
import type { EstimatedWaterLevelResponse, WaterLevelResponse } from "@/api/generated";

// Stations return a measured level and locations an estimate.
type WaterLevel = WaterLevelResponse | EstimatedWaterLevelResponse;

type WaterLevelState =
  | { status: "loading" }
  | { status: "loaded"; waterLevel: WaterLevel }
  | { status: "no-data" }
  | { status: "failed" };

export function useWaterLevel(path: string) {
  const [state, setState] = useState<WaterLevelState>({ status: "loading" });

  useEffect(() => {
    const controller = new AbortController();

    const fetchData = async () => {
      try {
        const response = await fetch(`${path}/water-level`, {
          signal: controller.signal,
        });

        if (response.status === 404) {
          setState({ status: "no-data" });
          return;
        }

        if (!response.ok) {
          throw new Error(`Water level request failed with ${response.status}`);
        }

        const waterLevel: WaterLevel = await response.json();
        setState({ status: "loaded", waterLevel });
      } catch (error: unknown) {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }
        setState({ status: "failed" });
      }
    };

    fetchData();

    return () => controller.abort();
  }, [path]);

  return state;
}
