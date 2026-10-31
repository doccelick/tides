import { Button } from "@/components/ui/button";
import type { SampleInterval } from "./fetchSamples";

interface IntervalToggleProps {
  value: SampleInterval;
  onValueChange: (interval: SampleInterval) => void;
}

const intervals: SampleInterval[] = [10, 60];

export function IntervalToggle({ value, onValueChange }: IntervalToggleProps) {
  return (
    <div role="group" aria-label="Interval" className="flex gap-1">
      {intervals.map((interval) => (
        <Button
          key={interval}
          variant={interval === value ? "secondary" : "ghost"}
          aria-pressed={interval === value}
          className="h-11"
          onClick={() => onValueChange(interval)}
        >
          {interval} min
        </Button>
      ))}
    </div>
  );
}
