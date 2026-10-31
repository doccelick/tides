import { useLayoutEffect, useRef, useState } from "react";
import { Skeleton } from "@/components/ui/skeleton";
import { TideAttribution } from "@/tides/TideAttribution";
import { useDragScroll } from "@/tides/useDragScroll";
import { DaysInViewToggle, type DaysInView } from "./DaysInViewToggle";
import { GraphEdge } from "./GraphEdge";
import { levelLineStepCm } from "./levelLines";
import { TideChart } from "./TideChart";
import { useSampleStrip } from "./useSampleStrip";

interface TideGraphProps {
  locationId: number;
}

interface StripLayout {
  from: string | undefined;
  scrollWidth: number;
}

const millisecondsPerDay = 24 * 60 * 60 * 1000;

// How far in from the left edge now is when the graph opens, as a share of the view's width.
const nowInset = 0.1;

const stripClassName =
  "flex overflow-x-auto [scrollbar-width:none] cursor-grab active:cursor-grabbing";

function dayStart(day: string): number {
  return Date.parse(`${day}T00:00:00Z`);
}

export function TideGraph({ locationId }: TideGraphProps) {
  const { state, earlier, later, loadEarlier, loadLater } = useSampleStrip(locationId);
  const [now] = useState(() => new Date());
  const [daysInView, setDaysInView] = useState<DaysInView>(2);

  const stripRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<HTMLDivElement>(null);
  const hasScrolledToNow = useRef(false);
  const lastLayout = useRef<StripLayout | null>(null);
  // The share of the chart's width at the left edge of the view, kept while the days in view change.
  const leftShare = useRef<number | null>(null);

  const from = state.status === "loaded" ? state.from : undefined;

  // Scrolls to now once, when the first days load.
  useLayoutEffect(() => {
    const strip = stripRef.current;
    const chart = chartRef.current;
    if (strip === null || chart === null || state.status !== "loaded") return;
    if (hasScrolledToNow.current) return;

    hasScrolledToNow.current = true;
    const start = dayStart(state.from);
    const share = (now.getTime() - start) / (dayStart(state.end) - start);
    strip.scrollLeft = chart.offsetLeft + share * chart.offsetWidth - nowInset * strip.clientWidth;
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
    leftShare.current = null;
  }, [daysInView]);

  const dragHandlers = useDragScroll();

  function changeDaysInView(days: DaysInView) {
    const strip = stripRef.current;
    const chart = chartRef.current;
    if (strip !== null && chart !== null) {
      leftShare.current = (strip.scrollLeft - chart.offsetLeft) / chart.offsetWidth;
    }
    setDaysInView(days);
  }

  if (state.status === "loading") return <Skeleton className="my-3 h-40 w-full" />;
  if (state.status === "no-data") {
    return <p className="py-3 text-sm">No imported water levels for today.</p>;
  }
  if (state.status === "failed") {
    return (
      <p role="alert" className="py-3 text-sm">
        Couldn't load water levels
      </p>
    );
  }

  const loadedDays = (dayStart(state.end) - dayStart(state.from)) / millisecondsPerDay;

  return (
    <div className="flex flex-col gap-2 py-3">
      <div className="flex justify-end">
        <DaysInViewToggle value={daysInView} onValueChange={changeDaysInView} />
      </div>
      <div ref={stripRef} className={stripClassName} {...dragHandlers}>
        <GraphEdge
          status={earlier}
          endMessage="No imported data before this day"
          failedMessage="Couldn't load earlier days"
          onLoad={loadEarlier}
        />
        <div
          ref={chartRef}
          className="h-40 shrink-0"
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
      <div className="text-xs text-muted-foreground">
        <p>Lines every {levelLineStepCm} cm above chart datum. The red line is now.</p>
        <TideAttribution />
      </div>
    </div>
  );
}
