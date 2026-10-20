import { Skeleton } from "@/components/ui/skeleton";
import { formatTideLevel, formatTideTime } from "./format";
import { useWaterLevel } from "./useWaterLevel";

interface CurrentLevelProps {
  code: string;
}

export function CurrentLevel({ code }: CurrentLevelProps) {
  const state = useWaterLevel(code);

  if (state.status === "loading") return <Skeleton className="h-5 w-40" />;
  if (state.status === "no-data") return null;
  if (state.status === "failed") return <p role="alert">Couldn't load water level</p>;

  return (
    <p>
      <span className="font-semibold tabular-nums">
        {formatTideLevel(state.waterLevel.levelCm)}
      </span>{" "}
      (measured {formatTideTime(state.waterLevel.time)})
    </p>
  );
}
