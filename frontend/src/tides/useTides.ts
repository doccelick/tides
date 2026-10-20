import { useEffect, useState } from "react";
import type { TideResponse, TidesResponse } from "@/api/generated";

type TidesState =
  | { status: "loading" }
  | { status: "loaded"; tides: TideResponse[] }
  | { status: "no-data" }
  | { status: "failed" };

export function useTides(code: string) {
  const [state, setState] = useState<TidesState>({ status: "loading" });

  useEffect(() => {
    const controller = new AbortController();

    const fetchData = async () => {
      try {
        const response = await fetch(`/v1/stations/${code}/tides`, { signal: controller.signal });

        if (response.status === 404) {
          setState({ status: "no-data" });
          return;
        }

        if (!response.ok) {
          throw new Error(`Tides request failed with ${response.status}`);
        }

        const result: TidesResponse = await response.json();
        setState({ status: "loaded", tides: result.tides });
      } catch (error: unknown) {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }
        setState({ status: "failed" });
      }
    };

    fetchData();

    return () => controller.abort();
  }, [code]);

  return state;
}
