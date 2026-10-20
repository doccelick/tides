import type { TideResponse } from "@/api/generated";
import { ArrowUpIcon, ArrowDownIcon } from "lucide-react";
import { cn } from "cn";
import { formatTideDay, formatTideLevel, formatTideTime } from "./format";
import { Skeleton } from "@/components/ui/skeleton";

interface TideTileProps {
  tide: TideResponse;
  now: Date;
  isPast: boolean;
}

const tileClassName = "snap-start shrink-0 w-40 rounded-lg border p-3";

export function TideTile({ tide, now, isPast }: TideTileProps) {
  const isHigh = tide.type === "High";
  const Icon = isHigh ? ArrowUpIcon : ArrowDownIcon;
  const label = isHigh ? "High" : "Low";

  return (
    <li className={cn(tileClassName, isPast && "opacity-50")}>
      <p className="flex items-center gap-1 text-sm">
        <Icon className="size-4" />
        {label}
      </p>
      <p
        className={cn(
          "text-2xl font-semibold tabular-nums",
          isPast && "font-normal text-muted-foreground",
        )}
      >
        {formatTideLevel(tide.levelCm)}
      </p>
      <p className="text-sm">
        {formatTideTime(tide.time)} {formatTideDay(tide.time, now)}
      </p>
    </li>
  );
}

export function TideTileSkeleton() {
  return (
    <li className={tileClassName}>
      <Skeleton className="h-5 w-20" />
      <Skeleton className="mt-1 h-8 w-16" />
      <Skeleton className="mt-1 h-5 w-24" />
    </li>
  );
}
