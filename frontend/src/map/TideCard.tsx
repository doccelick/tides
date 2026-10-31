import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { CurrentLevel } from "@/tides/CurrentLevel";
import { TidePanel } from "@/tides/TidePanel";
import { XIcon } from "lucide-react";

interface TideCardProps {
  name: string;
  // The station or location, for example `/v1/stations/BGO` or `/v1/locations/3`.
  path: string;
  // Changes when the stored tides change, so the tide panel loads them again.
  tidesVersion?: number;
  onClose: () => void;
  // Shown below the tides, for parts that only a location has.
  children?: ReactNode;
}

export function TideCard({ name, path, tidesVersion = 0, onClose, children }: TideCardProps) {
  return (
    <section aria-label={name} className="max-h-[60svh] overflow-y-auto border-t bg-background p-4">
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
      <TidePanel key={`${path}#${tidesVersion}`} path={path} />
      {children}
    </section>
  );
}
