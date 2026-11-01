import { useEffect, useState } from "react";
import type { TideResponse } from "@/api/generated";
import { fetchTides } from "./fetchTides";
import { findNextTideIndex } from "./findNextTideIndex";
import { toUtcDay } from "./utcDay";

// Today and tomorrow always hold the next tide, since tides come about every six hours.
const days = 2;

// Returns the next high or low tide, or null while it loads and when it can't be found.
export function useNextTide(path: string): TideResponse | null {
  const [nextTide, setNextTide] = useState<TideResponse | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    const now = new Date();

    const fetchData = async () => {
      try {
        const tides = await fetchTides(path, toUtcDay(now), days, controller.signal);
        if (tides === null) return;

        setNextTide(tides[findNextTideIndex(tides, now)] ?? null);
      } catch {
        // The next tide only adds a hint to the level, so a failed request leaves it out.
      }
    };

    fetchData();

    return () => controller.abort();
  }, [path]);

  return nextTide;
}
