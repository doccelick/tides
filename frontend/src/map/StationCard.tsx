import type { StationResponse } from "@/api/generated";
import { Button } from "@/components/ui/button";
import { CurrentLevel } from "@/tides/CurrentLevel";
import { TidePanel } from "@/tides/TidePanel";
import { XIcon } from "lucide-react";

interface StationCardProps {
  station: StationResponse;
  onClose: () => void;
}

export function StationCard({ station, onClose }: StationCardProps) {
  return (
    <section aria-label={station.name} className="border-t bg-background p-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">{station.name}</h2>
        <Button
          variant="ghost"
          size="icon"
          aria-label="Close"
          className="size-11"
          onClick={onClose}
        >
          <XIcon />
        </Button>
      </div>
      <CurrentLevel code={station.code} />
      <TidePanel code={station.code} />
    </section>
  );
}
