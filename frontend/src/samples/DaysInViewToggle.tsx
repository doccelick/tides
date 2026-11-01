import { Button } from "@/components/ui/button";

export type DaysInView = 1 | 2 | 3;

interface DaysInViewToggleProps {
  value: DaysInView;
  onValueChange: (days: DaysInView) => void;
}

const choices: DaysInView[] = [1, 2, 3];

export function DaysInViewToggle({ value, onValueChange }: DaysInViewToggleProps) {
  return (
    <div role="group" aria-label="Days in view" className="flex gap-1">
      {choices.map((days) => (
        <Button
          key={days}
          variant={days === value ? "secondary" : "ghost"}
          aria-pressed={days === value}
          aria-label={days === 1 ? "1 day" : `${days} days`}
          className="h-11"
          onClick={() => onValueChange(days)}
        >
          {days}d
        </Button>
      ))}
    </div>
  );
}
