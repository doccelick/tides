import { useRef, type PointerEvent } from "react";

export function useDragScroll() {
  // The pointer's x position at the last move, or null when no drag is active.
  const lastX = useRef<number | null>(null);

  function onPointerDown(event: PointerEvent<HTMLElement>) {
    // Touch and pen already scroll natively.
    if (event.pointerType !== "mouse") return;
    // Pointer capture would send the click to the strip instead of the button.
    if (event.target instanceof Element && event.target.closest("button") !== null) return;

    // Stops the drag from selecting text.
    event.preventDefault();

    const strip = event.currentTarget;
    lastX.current = event.clientX;
    strip.setPointerCapture(event.pointerId);
    // Snapping would pull the strip back to a tile on every move.
    strip.style.scrollSnapType = "none";
  }

  function onPointerMove(event: PointerEvent<HTMLElement>) {
    if (lastX.current === null) return;

    // Scrolls by the distance since the last move, so scroll changes made elsewhere during the drag are kept.
    event.currentTarget.scrollLeft -= event.clientX - lastX.current;
    lastX.current = event.clientX;
  }

  function endDrag(event: PointerEvent<HTMLElement>) {
    if (lastX.current === null) return;

    lastX.current = null;
    // Restores the snapping from the class, so the strip settles on the nearest tile.
    event.currentTarget.style.scrollSnapType = "";
  }

  return { onPointerDown, onPointerMove, onPointerUp: endDrag, onPointerCancel: endDrag };
}
