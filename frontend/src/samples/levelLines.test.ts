import { describe, expect, it } from "vitest";
import { levelLines } from "./levelLines";

describe("levelLines", () => {
  it("draws a line every 50 cm from chart datum to above the highest level", () => {
    expect(levelLines([84, 260, 102])).toEqual({
      bottom: 0,
      top: 300,
      values: [0, 50, 100, 150, 200, 250, 300],
    });
  });

  it("ends on the line when the highest level is on it", () => {
    expect(levelLines([100, 250]).top).toBe(250);
  });

  it("starts below chart datum when the water falls below it", () => {
    expect(levelLines([-12, 180])).toMatchObject({
      bottom: -50,
      values: [-50, 0, 50, 100, 150, 200],
    });
  });

  it("keeps one step without levels", () => {
    expect(levelLines([])).toEqual({ bottom: 0, top: 50, values: [0, 50] });
  });
});
