import { act, renderHook } from "@testing-library/react";
import type { MapLayerMouseEvent, MapLayerTouchEvent } from "react-map-gl/maplibre";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useLongPress } from "./useLongPress";

const lngLat = { lng: 14.4, lat: 67.3 };

function mouseEvent(x: number, y: number, button = 0): MapLayerMouseEvent {
  return {
    point: { x, y },
    lngLat,
    originalEvent: { button },
  } as unknown as MapLayerMouseEvent;
}

function touchEvent(x: number, y: number, touches = 1): MapLayerTouchEvent {
  return {
    point: { x, y },
    points: Array.from({ length: touches }, () => ({ x, y })),
    lngLat,
  } as unknown as MapLayerTouchEvent;
}

function setup() {
  const onLongPress = vi.fn();
  const onClick = vi.fn();
  const { result } = renderHook(() => useLongPress({ onLongPress, onClick }));
  return { handlers: () => result.current, onLongPress, onClick };
}

describe("useLongPress", () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it("fires with the pressed position after holding still", () => {
    const { handlers, onLongPress } = setup();

    act(() => handlers().onMouseDown(mouseEvent(100, 100)));
    act(() => vi.advanceTimersByTime(500));

    expect(onLongPress).toHaveBeenCalledWith({ longitude: 14.4, latitude: 67.3 });
  });

  it("does not fire when released early", () => {
    const { handlers, onLongPress } = setup();

    act(() => handlers().onMouseDown(mouseEvent(100, 100)));
    act(() => vi.advanceTimersByTime(300));
    act(() => handlers().onMouseUp());
    act(() => vi.advanceTimersByTime(500));

    expect(onLongPress).not.toHaveBeenCalled();
  });

  it("does not fire when the pointer moves away", () => {
    const { handlers, onLongPress } = setup();

    act(() => handlers().onTouchStart(touchEvent(100, 100)));
    act(() => handlers().onTouchMove(touchEvent(120, 100)));
    act(() => vi.advanceTimersByTime(500));

    expect(onLongPress).not.toHaveBeenCalled();
  });

  it("fires when the pointer moves only slightly", () => {
    const { handlers, onLongPress } = setup();

    act(() => handlers().onTouchStart(touchEvent(100, 100)));
    act(() => handlers().onTouchMove(touchEvent(104, 103)));
    act(() => vi.advanceTimersByTime(500));

    expect(onLongPress).toHaveBeenCalledOnce();
  });

  it("does not fire for a two-finger touch", () => {
    const { handlers, onLongPress } = setup();

    act(() => handlers().onTouchStart(touchEvent(100, 100, 2)));
    act(() => vi.advanceTimersByTime(500));

    expect(onLongPress).not.toHaveBeenCalled();
  });

  it("does not fire for the right mouse button", () => {
    const { handlers, onLongPress } = setup();

    act(() => handlers().onMouseDown(mouseEvent(100, 100, 2)));
    act(() => vi.advanceTimersByTime(500));

    expect(onLongPress).not.toHaveBeenCalled();
  });

  it("does not fire when the map starts dragging", () => {
    const { handlers, onLongPress } = setup();

    act(() => handlers().onMouseDown(mouseEvent(100, 100)));
    act(() => handlers().onDragStart());
    act(() => vi.advanceTimersByTime(500));

    expect(onLongPress).not.toHaveBeenCalled();
  });

  it("ignores the click that follows a long press", () => {
    const { handlers, onClick } = setup();

    act(() => handlers().onMouseDown(mouseEvent(100, 100)));
    act(() => vi.advanceTimersByTime(500));
    act(() => handlers().onMouseUp());
    act(() => handlers().onClick());

    expect(onClick).not.toHaveBeenCalled();
  });

  it("passes on a normal click", () => {
    const { handlers, onClick } = setup();

    act(() => handlers().onMouseDown(mouseEvent(100, 100)));
    act(() => handlers().onMouseUp());
    act(() => handlers().onClick());

    expect(onClick).toHaveBeenCalledOnce();
  });
});
