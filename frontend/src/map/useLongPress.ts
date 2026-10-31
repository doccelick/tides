import { useEffect, useRef } from "react";
import type { MapLayerMouseEvent, MapLayerTouchEvent } from "react-map-gl/maplibre";

export interface PressPosition {
  longitude: number;
  latitude: number;
}

interface LongPressOptions {
  onLongPress: (position: PressPosition) => void;
  onClick: () => void;
}

const pressDelayMs = 500;
// How far the pointer can move during a press, in pixels, before it counts as a drag.
const moveTolerancePx = 10;

function toPosition(event: MapLayerMouseEvent | MapLayerTouchEvent): PressPosition {
  return { longitude: event.lngLat.lng, latitude: event.lngLat.lat };
}

// Returns map event handlers. A left-button or one-finger press that holds still fires onLongPress.
// onClick gets the map's other clicks.
export function useLongPress({ onLongPress, onClick }: LongPressOptions) {
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const start = useRef<{ x: number; y: number } | null>(null);
  // Set when a long press fires. The map sends a click on release, and onClick skips that click.
  const fired = useRef(false);

  useEffect(
    () => () => {
      if (timer.current !== null) clearTimeout(timer.current);
    },
    [],
  );

  function cancel() {
    if (timer.current !== null) clearTimeout(timer.current);
    timer.current = null;
    start.current = null;
  }

  function begin(event: MapLayerMouseEvent | MapLayerTouchEvent) {
    cancel();
    start.current = { x: event.point.x, y: event.point.y };
    const position = toPosition(event);

    timer.current = setTimeout(() => {
      timer.current = null;
      fired.current = true;
      onLongPress(position);
    }, pressDelayMs);
  }

  function move(event: MapLayerMouseEvent | MapLayerTouchEvent) {
    if (start.current === null) return;

    const distance = Math.hypot(event.point.x - start.current.x, event.point.y - start.current.y);
    if (distance > moveTolerancePx) cancel();
  }

  return {
    onMouseDown(event: MapLayerMouseEvent) {
      fired.current = false;
      if (event.originalEvent.button === 0) begin(event);
    },
    onMouseMove: move,
    onMouseUp: cancel,
    onTouchStart(event: MapLayerTouchEvent) {
      fired.current = false;
      if (event.points.length === 1) begin(event);
      else cancel();
    },
    onTouchMove: move,
    onTouchEnd: cancel,
    onTouchCancel: cancel,
    onDragStart: cancel,
    onClick() {
      if (fired.current) {
        fired.current = false;
        return;
      }
      onClick();
    },
  };
}
