import { useRef, type PointerEvent } from "react";

interface DragStart {
  startX: number;
  startScrollLeft: number;
}

export function useDragScroll() {
  const drag = useRef<DragStart | null>(null);

  function onPointerDown(event: PointerEvent<HTMLElement>) {
    // Touch and pen already scroll natively.
    if (event.pointerType !== "mouse") return;

    // Stops the drag from selecting text.
    event.preventDefault();

    const strip = event.currentTarget;
    drag.current = { startX: event.clientX, startScrollLeft: strip.scrollLeft };
    strip.setPointerCapture(event.pointerId);
    // Snapping would pull the strip back to a tile on every move.
    strip.style.scrollSnapType = "none";
  }

  function onPointerMove(event: PointerEvent<HTMLElement>) {
    if (drag.current === null) return;

    const moved = event.clientX - drag.current.startX;
    event.currentTarget.scrollLeft = drag.current.startScrollLeft - moved;
  }

  function endDrag(event: PointerEvent<HTMLElement>) {
    if (drag.current === null) return;

    drag.current = null;
    // Restores the snapping from the class, so the strip settles on the nearest tile.
    event.currentTarget.style.scrollSnapType = "";
  }

  return { onPointerDown, onPointerMove, onPointerUp: endDrag, onPointerCancel: endDrag };
}
