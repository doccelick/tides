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
  if (state.status === "failed") return <p role="alert">Couldn't load water level</p>;

  const { waterLevel } = state;
  const time = formatTideTime(waterLevel.time);

  return (
    <p>
      <span className="font-semibold tabular-nums">{formatTideLevel(waterLevel.levelCm)}</span>{" "}
      {"stationName" in waterLevel
        ? `(estimated from ${waterLevel.stationName}, ${waterLevel.distanceKm} km away, ${time})`
        : `(measured ${time})`}
    </p>
  );
}
