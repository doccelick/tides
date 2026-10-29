import { useEffect, useRef, useState } from "react";
import type { TideResponse } from "@/api/generated";
import { fetchTides } from "./fetchTides";
import { addDays, toUtcDay } from "./utcDay";

type TidesState =
  | { status: "loading" }
  // `from` is the first UTC day loaded and `end` the day after the last.
  | { status: "loaded"; tides: TideResponse[]; from: string; end: string }
  | { status: "no-data" }
  | { status: "failed" };

// "end" means the station has no tide data past that edge.
export type EdgeStatus = "idle" | "loading" | "failed" | "end";

const firstDays = 2;
const moreDays = 7;

function isAbort(error: unknown): boolean {
  return error instanceof DOMException && error.name === "AbortError";
}

export function useTides(code: string) {
  const [state, setState] = useState<TidesState>({ status: "loading" });
  const [earlier, setEarlier] = useState<EdgeStatus>("idle");
  const [later, setLater] = useState<EdgeStatus>("idle");
  const signal = useRef<AbortSignal | undefined>(undefined);

  useEffect(() => {
    const controller = new AbortController();
    signal.current = controller.signal;
    const from = toUtcDay(new Date());

    const fetchData = async () => {
      try {
        const tides = await fetchTides(code, from, firstDays, controller.signal);

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
  }, [code]);

  async function loadEarlier() {
    if (state.status !== "loaded" || earlier === "loading" || earlier === "end") return;

    const from = addDays(state.from, -moreDays);
    setEarlier("loading");

    try {
      const tides = await fetchTides(code, from, moreDays, signal.current);

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
    }
  }

  async function loadLater() {
    if (state.status !== "loaded" || later === "loading" || later === "end") return;

    const from = state.end;
    setLater("loading");

    try {
      const tides = await fetchTides(code, from, moreDays, signal.current);

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
    }
  }

  return { state, earlier, later, loadEarlier, loadLater };
}
