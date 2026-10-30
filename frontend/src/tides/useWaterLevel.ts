import { useEffect, useState } from "react";
import type { WaterLevelResponse } from "@/api/generated";

type WaterLevelState =
  | { status: "loading" }
  | { status: "loaded"; waterLevel: WaterLevelResponse }
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

        const waterLevel: WaterLevelResponse = await response.json();
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
