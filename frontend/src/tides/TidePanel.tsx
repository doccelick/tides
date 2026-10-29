import { useLayoutEffect, useRef, useState } from "react";
import { useTides } from "./useTides";
import { findNextTideIndex } from "./findNextTideIndex";
import { TideTile, TideTileSkeleton } from "./TideTile";
import { EdgeTile } from "./EdgeTile";
import { useDragScroll } from "./useDragScroll";

interface TidePanelProps {
  code: string;
}

interface StripLayout {
  firstTideTime: string | undefined;
  scrollWidth: number;
}

const stripClassName =
  "flex snap-x snap-mandatory gap-2 overflow-x-auto py-3 [scrollbar-width:none] cursor-grab active:cursor-grabbing";

export function TidePanel({ code }: TidePanelProps) {
  const { state, earlier, later, loadEarlier, loadLater } = useTides(code);
  const [now] = useState(() => new Date());

  const stripRef = useRef<HTMLUListElement>(null);
  const nextIndex = state.status === "loaded" ? findNextTideIndex(state.tides, now) : -1;
  const firstTideTime = state.status === "loaded" ? state.tides[0]?.time : undefined;

  const hasScrolledToNext = useRef(false);
  const lastLayout = useRef<StripLayout | null>(null);

  // Scrolls to the next tide once, when the first days load.
  useLayoutEffect(() => {
    const strip = stripRef.current;
    if (strip === null || hasScrolledToNext.current) return;

    hasScrolledToNext.current = true;
    if (nextIndex === -1) return;

    // On the first load the first child is the tile that loads earlier days.
    const nextTile = strip.children[nextIndex + 1] as HTMLElement;
    strip.scrollLeft = nextTile.offsetLeft;
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
  if (state.status === "no-data") return <p>No tide data for this station.</p>;
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
        <p>
          Tide data from{" "}
          <a
            href="https://vannstand.kartverket.no/tideapi_en.html"
            target="_blank"
            rel="noreferrer"
            className="underline underline-offset-2"
          >
            Kartverket
          </a>
          ,{" "}
          <a
            href="https://creativecommons.org/licenses/by/4.0/"
            target="_blank"
            rel="noreferrer"
            className="whitespace-nowrap underline underline-offset-2"
          >
            CC BY 4.0
          </a>
        </p>
      </div>
    </>
  );
}
