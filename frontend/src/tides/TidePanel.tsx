import { useImperativeHandle, useLayoutEffect, useRef, useState, type Ref } from "react";
import type { TodayHandle } from "./TodayHandle";
import { useTides } from "./useTides";
import { findNextTideIndex } from "./findNextTideIndex";
import { TideTile, TideTileSkeleton } from "./TideTile";
import { EdgeTile } from "./EdgeTile";
import { TideAttribution } from "./TideAttribution";
import { useDragScroll } from "./useDragScroll";

interface TidePanelProps {
  path: string;
  ref?: Ref<TodayHandle>;
}

interface StripLayout {
  firstTideTime: string | undefined;
  scrollWidth: number;
}

const stripClassName =
  "flex snap-x snap-mandatory gap-2 overflow-x-auto py-3 [scrollbar-width:none] cursor-grab active:cursor-grabbing";

export function TidePanel({ path, ref }: TidePanelProps) {
  const { state, earlier, later, loadEarlier, loadLater } = useTides(path);
  const [now] = useState(() => new Date());

  const stripRef = useRef<HTMLUListElement>(null);
  const nextIndex = state.status === "loaded" ? findNextTideIndex(state.tides, now) : -1;
  const firstTideTime = state.status === "loaded" ? state.tides[0]?.time : undefined;

  const hasScrolledToNext = useRef(false);
  const lastLayout = useRef<StripLayout | null>(null);

  function scrollToNext() {
    const strip = stripRef.current;
    if (strip === null || nextIndex === -1) return;

    // The tile that loads earlier days comes first until there are no earlier days.
    const firstTideChild = earlier === "end" ? 0 : 1;
    const nextTile = strip.children[nextIndex + firstTideChild] as HTMLElement;
    strip.scrollLeft = nextTile.offsetLeft;
  }

  useImperativeHandle(ref, () => ({ scrollToToday: scrollToNext }));

  // Scrolls to the next tide once, when the first days load.
  useLayoutEffect(() => {
    if (stripRef.current === null || hasScrolledToNext.current) return;

    hasScrolledToNext.current = true;
    scrollToNext();
  });

  // Keeps the same tiles in view when earlier days are added in front of them.
  useLayoutEffect(() => {
    const strip = stripRef.current;
    if (strip === null) return;

    const previous = lastLayout.current;
    if (previous !== null && previous.firstTideTime !== firstTideTime) {
      strip.scrollLeft += strip.scrollWidth - previous.scrollWidth;
    }
    lastLayout.current = { firstTideTime, scrollWidth: strip.scrollWidth };
  });

  const dragHandlers = useDragScroll();

  if (state.status === "loading") {
    return (
      <ul aria-label="Tides" aria-busy="true" className={stripClassName}>
        <TideTileSkeleton />
        <TideTileSkeleton />
      </ul>
    );
  }
  if (state.status === "no-data") return <p>No tide data for this place.</p>;
  if (state.status === "failed") return <p role="alert">Couldn't load tides</p>;

  return (
    <>
      <ul aria-label="Tides" className={stripClassName} ref={stripRef} {...dragHandlers}>
        <EdgeTile
          status={earlier}
          failedMessage="Couldn't load earlier tides"
          onLoad={loadEarlier}
        />
        {state.tides.map((tide, index) => (
          <TideTile key={tide.time} tide={tide} now={now} isPast={index < nextIndex} />
        ))}
        <EdgeTile status={later} failedMessage="Couldn't load later tides" onLoad={loadLater} />
      </ul>
      <div className="text-xs text-muted-foreground">
        <p>Levels relative to chart datum.</p>
        <TideAttribution />
      </div>
    </>
  );
}
