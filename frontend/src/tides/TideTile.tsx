import type { Ref } from "react";
import { ArrowUpIcon, ArrowDownIcon, CloudIcon, DatabaseIcon } from "lucide-react";
import { cn } from "cn";
import { formatTideDay, formatTideLevel, formatTideTime } from "./format";
import type { SourcedTide } from "./fetchTides";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

interface TideTileProps {
  tide: SourcedTide;
  now: Date;
  isPast: boolean;
}

const tileClassName = "snap-start shrink-0 w-40 rounded-lg border p-3";

export function TideTile({ tide, now, isPast }: TideTileProps) {
  const isHigh = tide.type === "High";
  const Icon = isHigh ? ArrowUpIcon : ArrowDownIcon;
  const label = isHigh ? "High" : "Low";
  const isStored = tide.source === "Database";
  const SourceIcon = isStored ? DatabaseIcon : CloudIcon;
  const sourceLabel = isStored ? "From database" : "From Kartverket";

  return (
    <li className={cn(tileClassName, isPast && "opacity-50")}>
      <p className="flex items-center gap-1 text-sm">
        <Icon className="size-4" />
        {label}
        <SourceIcon
          role="img"
          aria-label={sourceLabel}
          className="ml-auto size-3 text-muted-foreground"
        />
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

interface TideTileSkeletonProps {
  ref?: Ref<HTMLLIElement>;
}

export function TideTileSkeleton({ ref }: TideTileSkeletonProps) {
  return (
    <li ref={ref} className={tileClassName}>
      <Skeleton className="h-5 w-20" />
      <Skeleton className="mt-1 h-8 w-16" />
      <Skeleton className="mt-1 h-5 w-24" />
    </li>
  );
}

interface TideTileRetryProps {
  message: string;
  onRetry: () => void;
}

export function TideTileRetry({ message, onRetry }: TideTileRetryProps) {
  return (
    <li className={tileClassName}>
      <p role="alert" className="text-sm">
        {message}
      </p>
      <Button variant="outline" size="sm" className="mt-2" onClick={onRetry}>
        Retry
      </Button>
    </li>
  );
}
