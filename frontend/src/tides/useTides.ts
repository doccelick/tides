import { useEffect, useRef, useState } from "react";
import { fetchTides, type SourcedTide } from "./fetchTides";
import { addDays, toUtcDay } from "./utcDay";

type TidesState =
  | { status: "loading" }
  // `from` is the first UTC day loaded and `end` the day after the last.
  | { status: "loaded"; tides: SourcedTide[]; from: string; end: string }
  | { status: "no-data" }
  | { status: "failed" };

// "end" means there is no tide data past that edge.
export type EdgeStatus = "idle" | "loading" | "failed" | "end";

const firstDays = 2;
const moreDays = 7;

function isAbort(error: unknown): boolean {
  return error instanceof DOMException && error.name === "AbortError";
}

export function useTides(path: string) {
  const [state, setState] = useState<TidesState>({ status: "loading" });
  const [earlier, setEarlier] = useState<EdgeStatus>("idle");
  const [later, setLater] = useState<EdgeStatus>("idle");
  const signal = useRef<AbortSignal | undefined>(undefined);
  // Set when a request starts, so a second call is blocked before the "loading" status renders.
  // The status check blocks calls after the request ends and before the new tides render.
  const inFlight = useRef({ earlier: false, later: false });

  useEffect(() => {
    const controller = new AbortController();
    signal.current = controller.signal;
    const from = toUtcDay(new Date());

    const fetchData = async () => {
      try {
        const tides = await fetchTides(path, from, firstDays, controller.signal);

        if (tides === null) {
          setState({ status: "no-data" });
          return;
        }

        setState({ status: "loaded", tides, from, end: addDays(from, firstDays) });
      } catch (error: unknown) {
        if (isAbort(error)) {
          return;
        }
        setState({ status: "failed" });
      }
    };

    fetchData();

    return () => controller.abort();
  }, [path]);

  async function loadEarlier() {
    if (
      state.status !== "loaded" ||
      inFlight.current.earlier ||
      earlier === "loading" ||
      earlier === "end"
    )
      return;

    const from = addDays(state.from, -moreDays);
    inFlight.current.earlier = true;
    setEarlier("loading");

    try {
      const tides = await fetchTides(path, from, moreDays, signal.current);

      if (tides === null) {
        setEarlier("end");
        return;
      }

      setState((current) =>
        current.status === "loaded"
          ? { ...current, tides: [...tides, ...current.tides], from }
          : current,
      );
      setEarlier("idle");
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

    const from = state.end;
    inFlight.current.later = true;
    setLater("loading");

    try {
      const tides = await fetchTides(path, from, moreDays, signal.current);

      if (tides === null) {
        setLater("end");
        return;
      }

      setState((current) =>
        current.status === "loaded"
          ? { ...current, tides: [...current.tides, ...tides], end: addDays(from, moreDays) }
          : current,
      );
      setLater("idle");
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
