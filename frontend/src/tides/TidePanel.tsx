import { useLayoutEffect, useRef, useState } from "react";
import { useTides } from "./useTides";
import { findNextTideIndex } from "./findNextTideIndex";
import { TideTile, TideTileSkeleton } from "./TideTile";
import { useDragScroll } from "./useDragScroll";

interface TidePanelProps {
  code: string;
}

const stripClassName =
  "flex snap-x snap-mandatory gap-2 overflow-x-auto py-3 [scrollbar-width:none] cursor-grab active:cursor-grabbing";

export function TidePanel({ code }: TidePanelProps) {
  const state = useTides(code);
  const [now] = useState(() => new Date());

  const stripRef = useRef<HTMLUListElement>(null);
  const nextIndex = state.status === "loaded" ? findNextTideIndex(state.tides, now) : -1;

  useLayoutEffect(() => {
    const strip = stripRef.current;
    if (strip === null || nextIndex === -1) return;
    const nextTile = strip.children[nextIndex] as HTMLElement;
    strip.scrollLeft = nextTile.offsetLeft;
  }, [nextIndex]);

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
        {state.tides.map((tide, index) => (
          <TideTile key={tide.time} tide={tide} now={now} isPast={index < nextIndex} />
        ))}
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
