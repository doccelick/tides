import { useEffect, useState } from "react";
import type { ImportResponse, ImportsResponse } from "@/api/generated";

type ImportsState =
  | { status: "loading" }
  | { status: "loaded"; imports: ImportResponse[] }
  | { status: "failed" };

export function useImports(locationId: number) {
  const [state, setState] = useState<ImportsState>({ status: "loading" });
  const [version, setVersion] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    const fetchData = async () => {
      try {
        const response = await fetch(`/v1/locations/${locationId}/imports`, {
          signal: controller.signal,
        });

        if (!response.ok) {
          throw new Error(`Imports request failed with ${response.status}`);
        }

        const result: ImportsResponse = await response.json();
        setState({ status: "loaded", imports: result.imports });
      } catch (error: unknown) {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }
        setState({ status: "failed" });
      }
    };

    fetchData();

    return () => controller.abort();
  }, [locationId, version]);

  // Fetches the imports again. The current list stays shown until the new one has loaded.
  function reload() {
    setVersion((current) => current + 1);
  }

  return { state, reload };
}
