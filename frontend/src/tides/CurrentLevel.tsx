import { ArrowDownIcon, ArrowUpIcon, CircleHelpIcon } from "lucide-react";
import type { TideResponse } from "@/api/generated";
import { Button } from "@/components/ui/button";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Skeleton } from "@/components/ui/skeleton";
import { formatTideLevel, formatTideTime } from "./format";
import { useNextTide } from "./useNextTide";
import { useWaterLevel } from "./useWaterLevel";

interface CurrentLevelProps {
  path: string;
}

export function CurrentLevel({ path }: CurrentLevelProps) {
  const state = useWaterLevel(path);
  const nextTide = useNextTide(path);

  if (state.status === "loading") return <Skeleton className="h-5 w-20" />;
  if (state.status === "no-data") return null;
  if (state.status === "failed") {
    return (
      <p role="alert" className="text-sm">
        Couldn't load water level
      </p>
    );
  }

  const { waterLevel } = state;
  const time = formatTideTime(waterLevel.time);
  const details =
    "stationName" in waterLevel
      ? `Estimated from ${waterLevel.stationName}, ${waterLevel.distanceKm} km away, at ${time}.`
      : `Measured at ${time}.`;

  return (
    <div className="flex items-center gap-1 text-sm">
      {nextTide !== null && <TrendArrow nextTide={nextTide} />}
      <span className="font-semibold tabular-nums">{formatTideLevel(waterLevel.levelCm)}</span>
      <Popover>
        <PopoverTrigger
          render={<Button variant="ghost" size="icon-xs" aria-label="About this level" />}
        >
          <CircleHelpIcon />
        </PopoverTrigger>
        <PopoverContent className="w-auto max-w-64 gap-1 text-sm">
          <p>{details}</p>
          {nextTide !== null && <p>{describeTrend(nextTide)}</p>}
        </PopoverContent>
      </Popover>
    </div>
  );
}

// The water rises towards a high tide and falls towards a low one.
function TrendArrow({ nextTide }: { nextTide: TideResponse }) {
  const isRising = nextTide.type === "High";
  const Icon = isRising ? ArrowUpIcon : ArrowDownIcon;
  return <Icon role="img" aria-label={isRising ? "Rising" : "Falling"} className="size-4" />;
}

function describeTrend(nextTide: TideResponse): string {
  const time = formatTideTime(nextTide.time);
  return nextTide.type === "High"
    ? `Rising until high tide at ${time}.`
    : `Falling until low tide at ${time}.`;
}
