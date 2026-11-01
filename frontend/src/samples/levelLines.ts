export const levelLineStepCm = 50;

export interface LevelLines {
  bottom: number;
  top: number;
  // A line every 50 cm from bottom to top, both included.
  values: number[];
}

// The chart starts at chart datum unless the water falls below it, and ends on the first line above the highest level.
export function levelLines(levels: number[]): LevelLines {
  const lowest = Math.min(0, ...levels);
  const highest = Math.max(levelLineStepCm, ...levels);
  const bottom = Math.floor(lowest / levelLineStepCm) * levelLineStepCm;
  const top = Math.ceil(highest / levelLineStepCm) * levelLineStepCm;

  const values: number[] = [];
  for (let level = bottom; level <= top; level += levelLineStepCm) {
    values.push(level);
  }
  return { bottom, top, values };
}
