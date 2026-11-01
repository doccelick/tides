import { useImperativeHandle, useLayoutEffect, useRef, useState, type Ref } from "react";
import { Skeleton } from "@/components/ui/skeleton";
import { TideAttribution } from "@/tides/TideAttribution";
import type { TodayHandle } from "@/tides/TodayHandle";
import { useDragScroll } from "@/tides/useDragScroll";
import type { DaysInView } from "./DaysInViewToggle";
import { GraphEdge } from "./GraphEdge";
import { levelLineStepCm } from "./levelLines";
import { TideChart } from "./TideChart";
import { useSampleStrip } from "./useSampleStrip";

interface TideGraphProps {
  locationId: number;
  daysInView: DaysInView;
  ref?: Ref<TodayHandle>;
}

interface StripLayout {
  from: string | undefined;
  scrollWidth: number;
}

const millisecondsPerDay = 24 * 60 * 60 * 1000;

// How far in from the left edge now is when scrolled to, as a share of the view's width.
const nowInset = 0.1;

const stripClassName =
  "flex overflow-x-auto [scrollbar-width:none] cursor-grab active:cursor-grabbing";

function dayStart(day: string): number {
  return Date.parse(`${day}T00:00:00Z`);
}

export function TideGraph({ locationId, daysInView, ref }: TideGraphProps) {
  const { state, earlier, later, loadEarlier, loadLater } = useSampleStrip(locationId);
  const [now] = useState(() => new Date());

  const stripRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<HTMLDivElement>(null);
  const hasScrolledToNow = useRef(false);
  const lastLayout = useRef<StripLayout | null>(null);
  // The share of the chart's width at the left edge of the view, updated on every scroll.
  const leftShare = useRef<number | null>(null);

  const from = state.status === "loaded" ? state.from : undefined;

  function scrollToNow() {
    const strip = stripRef.current;
    const chart = chartRef.current;
    if (strip === null || chart === null || state.status !== "loaded") return;

    const start = dayStart(state.from);
    const share = (now.getTime() - start) / (dayStart(state.end) - start);
    strip.scrollLeft = chart.offsetLeft + share * chart.offsetWidth - nowInset * strip.clientWidth;
  }

  useImperativeHandle(ref, () => ({ scrollToToday: scrollToNow }));

  // Scrolls to now once, when the first days load.
  useLayoutEffect(() => {
    if (stripRef.current === null || hasScrolledToNow.current) return;

    hasScrolledToNow.current = true;
    scrollToNow();
  });

  // Keeps the same days in view when earlier days are added in front of them.
  useLayoutEffect(() => {
    const strip = stripRef.current;
    if (strip === null) return;

    const previous = lastLayout.current;
    if (previous !== null && previous.from !== from) {
      strip.scrollLeft += strip.scrollWidth - previous.scrollWidth;
    }
    lastLayout.current = { from, scrollWidth: strip.scrollWidth };
  });

  // Keeps the same time at the left edge when the days in view change.
  useLayoutEffect(() => {
    const strip = stripRef.current;
    const chart = chartRef.current;
    if (strip === null || chart === null || leftShare.current === null) return;

    strip.scrollLeft = chart.offsetLeft + leftShare.current * chart.offsetWidth;
  }, [daysInView]);

  const dragHandlers = useDragScroll();

  function recordLeftShare() {
    const strip = stripRef.current;
    const chart = chartRef.current;
    if (strip === null || chart === null) return;

    leftShare.current = (strip.scrollLeft - chart.offsetLeft) / chart.offsetWidth;
  }

  if (state.status === "loading") return <Skeleton className="my-2 h-28 w-full" />;
  if (state.status === "no-data") {
    return <p className="py-2 text-sm">No imported water levels for today.</p>;
  }
  if (state.status === "failed") {
    return (
      <p role="alert" className="py-2 text-sm">
        Couldn't load water levels
      </p>
    );
  }

  const loadedDays = (dayStart(state.end) - dayStart(state.from)) / millisecondsPerDay;

  return (
    <div className="flex flex-col gap-1 py-2">
      <div ref={stripRef} className={stripClassName} onScroll={recordLeftShare} {...dragHandlers}>
        <GraphEdge
          status={earlier}
          endMessage="No imported data before this day"
          failedMessage="Couldn't load earlier days"
          onLoad={loadEarlier}
        />
        <div
          ref={chartRef}
          className="h-28 shrink-0"
          style={{ width: `${(loadedDays / daysInView) * 100}%` }}
        >
          {/* A new key draws the curve in again when the days in view change. */}
          <TideChart key={daysInView} samples={state.samples} now={now} />
        </div>
        <GraphEdge
          status={later}
          endMessage="No imported data after this day"
          failedMessage="Couldn't load later days"
          onLoad={loadLater}
        />
      </div>
      <p className="text-xs text-muted-foreground">
        Lines every {levelLineStepCm} cm above chart datum · <TideAttribution />
      </p>
    </div>
  );
}
