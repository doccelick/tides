import { useEffect, useRef, useState } from "react";
import type { ImportsResponse, SampleResponse } from "@/api/generated";
import type { UtcRange } from "@/imports/importPresets";
import type { EdgeStatus } from "@/tides/useTides";
import { addDays, toUtcDay } from "@/tides/utcDay";
import { fetchSamples } from "./fetchSamples";
import { importedSpan } from "./importedSpan";

type SampleStripState =
  | { status: "loading" }
  // `from` is the first UTC day loaded and `end` the day after the last.
  // `span` is the run of imported days that the strip can grow into.
  | { status: "loaded"; samples: SampleResponse[]; from: string; end: string; span: UtcRange }
  | { status: "no-data" }
  | { status: "failed" };

const firstDays = 4;
const moreDays = 7;
// A level every 10 minutes keeps the highs and lows at their real times when 1 to 3 days are in view.
const interval = 10;

function isAbort(error: unknown): boolean {
  return error instanceof DOMException && error.name === "AbortError";
}

// YYYY-MM-DD days sort as text, so the earlier day is the smaller string.
function earlierDay(a: string, b: string): string {
  return a < b ? a : b;
}

function laterDay(a: string, b: string): string {
  return a > b ? a : b;
}

async function fetchSpan(locationId: number, day: string, signal: AbortSignal) {
  const response = await fetch(`/v1/locations/${locationId}/imports`, { signal });

  if (!response.ok) {
    throw new Error(`Imports request failed with ${response.status}`);
  }

  const result: ImportsResponse = await response.json();
  return importedSpan(result.imports, day);
}

// Loads the imported water levels from today, and more days at either edge until the imported days end.
export function useSampleStrip(locationId: number) {
  const [state, setState] = useState<SampleStripState>({ status: "loading" });
  const [earlier, setEarlier] = useState<EdgeStatus>("idle");
  const [later, setLater] = useState<EdgeStatus>("idle");
  const signal = useRef<AbortSignal | undefined>(undefined);
  // Set when a request starts, so a second call is blocked before the "loading" status renders.
  const inFlight = useRef({ earlier: false, later: false });

  useEffect(() => {
    const controller = new AbortController();
    signal.current = controller.signal;
    const today = toUtcDay(new Date());

    const fetchData = async () => {
      try {
        const span = await fetchSpan(locationId, today, controller.signal);

        if (span === null) {
          setState({ status: "no-data" });
          return;
        }

        const end = earlierDay(addDays(today, firstDays), span.to);
        const result = await fetchSamples(
          locationId,
          { from: today, to: end },
          interval,
          controller.signal,
        );

        if (result === null) {
          setState({ status: "no-data" });
          return;
        }

        setState({ status: "loaded", samples: result.samples, from: today, end, span });
        setEarlier(today === span.from ? "end" : "idle");
        setLater(end === span.to ? "end" : "idle");
      } catch (error: unknown) {
        if (isAbort(error)) {
          return;
        }
        setState({ status: "failed" });
      }
    };

    fetchData();

    return () => controller.abort();
  }, [locationId]);

  async function loadEarlier() {
    if (
      state.status !== "loaded" ||
      inFlight.current.earlier ||
      earlier === "loading" ||
      earlier === "end"
    )
      return;

    const from = laterDay(addDays(state.from, -moreDays), state.span.from);
    inFlight.current.earlier = true;
    setEarlier("loading");

    try {
      const result = await fetchSamples(
        locationId,
        { from, to: state.from },
        interval,
        signal.current,
      );

      if (result === null) {
        setEarlier("end");
        return;
      }

      setState((current) =>
        current.status === "loaded"
          ? { ...current, samples: [...result.samples, ...current.samples], from }
          : current,
      );
      setEarlier(from === state.span.from ? "end" : "idle");
    } catch (error: unknown) {
      if (isAbort(error)) {
        return;
      }
      setEarlier("failed");
    } finally {
      inFlight.current.earlier = false;
    }
  }

  async function loadLater() {
    if (
      state.status !== "loaded" ||
      inFlight.current.later ||
      later === "loading" ||
      later === "end"
    )
      return;

    const end = earlierDay(addDays(state.end, moreDays), state.span.to);
    inFlight.current.later = true;
    setLater("loading");

    try {
      const result = await fetchSamples(
        locationId,
        { from: state.end, to: end },
        interval,
        signal.current,
      );

      if (result === null) {
        setLater("end");
        return;
      }

      setState((current) =>
        current.status === "loaded"
          ? { ...current, samples: [...current.samples, ...result.samples], end }
          : current,
      );
      setLater(end === state.span.to ? "end" : "idle");
    } catch (error: unknown) {
      if (isAbort(error)) {
        return;
      }
      setLater("failed");
    } finally {
      inFlight.current.later = false;
    }
  }

  return { state, earlier, later, loadEarlier, loadLater };
}
