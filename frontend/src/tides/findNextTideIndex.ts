import type { TideResponse } from "@/api/generated";

export function findNextTideIndex(tides: TideResponse[], now: Date): number {
  return tides.findIndex((tide) => Date.parse(tide.time) >= now.getTime());
}
