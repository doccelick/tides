import { CircleHelpIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Skeleton } from "@/components/ui/skeleton";
import { formatTideLevel, formatTideTime } from "./format";
import { useWaterLevel } from "./useWaterLevel";

interface CurrentLevelProps {
  path: string;
}

export function CurrentLevel({ path }: CurrentLevelProps) {
  const state = useWaterLevel(path);

  if (state.status === "loading") return <Skeleton className="h-5 w-40" />;
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
      <span className="font-semibold tabular-nums">{formatTideLevel(waterLevel.levelCm)}</span>
      <Popover>
        <PopoverTrigger
          render={<Button variant="ghost" size="icon-xs" aria-label="About this level" />}
        >
          <CircleHelpIcon />
        </PopoverTrigger>
        <PopoverContent className="w-auto max-w-64 text-sm">{details}</PopoverContent>
      </Popover>
    </div>
  );
}
