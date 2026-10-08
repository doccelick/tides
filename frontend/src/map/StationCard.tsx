import type { StationResponse } from "@/api/generated";
import { Button } from "@/components/ui/button";
import { XIcon } from "lucide-react";

interface StationCardProps {
  station: StationResponse;
  onClose: () => void;
}

export function StationCard({ station, onClose }: StationCardProps) {
  return (
    <section
      aria-label={station.name}
      className="border-t bg-background p-4 flex items-center justify-between"
    >
      <h2>{station.name}</h2>
      <Button variant="ghost" size="icon" aria-label="Close" className="size-11" onClick={onClose}>
        <XIcon />
      </Button>
    </section>
  );
}
