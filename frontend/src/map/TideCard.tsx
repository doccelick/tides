import { Button } from "@/components/ui/button";
import { CurrentLevel } from "@/tides/CurrentLevel";
import { TidePanel } from "@/tides/TidePanel";
import { XIcon } from "lucide-react";

interface TideCardProps {
  name: string;
  // The station or location, for example `/v1/stations/BGO` or `/v1/locations/3`.
  path: string;
  onClose: () => void;
}

export function TideCard({ name, path, onClose }: TideCardProps) {
  return (
    <section aria-label={name} className="border-t bg-background p-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">{name}</h2>
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
      <CurrentLevel path={path} />
      <TidePanel key={path} path={path} />
    </section>
  );
}
