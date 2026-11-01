import { useState, type ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { TideGraph } from "@/samples/TideGraph";
import { CurrentLevel } from "@/tides/CurrentLevel";
import { TidePanel } from "@/tides/TidePanel";
import { XIcon } from "lucide-react";

type TideView = "tides" | "graph";

interface TideCardProps {
  name: string;
  // The station or location, for example `/v1/stations/BGO` or `/v1/locations/3`.
  path: string;
  // Only locations have stored water levels, so only they can switch to the graph.
  locationId?: number;
  // Changes when the stored tides change, so the tide panel loads them again.
  tidesVersion?: number;
  onClose: () => void;
  // Shown below the tides, for parts that only a location has.
  children?: ReactNode;
}

export function TideCard({
  name,
  path,
  locationId,
  tidesVersion = 0,
  onClose,
  children,
}: TideCardProps) {
  const [view, setView] = useState<TideView>("tides");
  const showsGraph = locationId !== undefined && view === "graph";

  return (
    <section aria-label={name} className="max-h-[60svh] overflow-y-auto border-t bg-background p-4">
      <div className="flex items-center gap-2">
        <h2 className="min-w-0 flex-1 truncate text-lg font-semibold">{name}</h2>
        {locationId !== undefined && <TideViewToggle value={view} onValueChange={setView} />}
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
      {showsGraph ? (
        <TideGraph key={tidesVersion} locationId={locationId} />
      ) : (
        <TidePanel key={`${path}#${tidesVersion}`} path={path} />
      )}
      {children}
    </section>
  );
}

interface TideViewToggleProps {
  value: TideView;
  onValueChange: (view: TideView) => void;
}

function TideViewToggle({ value, onValueChange }: TideViewToggleProps) {
  return (
    <div role="group" aria-label="View" className="flex gap-1">
      <Button
        variant={value === "tides" ? "secondary" : "ghost"}
        aria-pressed={value === "tides"}
        className="h-11"
        onClick={() => onValueChange("tides")}
      >
        Tides
      </Button>
      <Button
        variant={value === "graph" ? "secondary" : "ghost"}
        aria-pressed={value === "graph"}
        className="h-11"
        onClick={() => onValueChange("graph")}
      >
        Graph
      </Button>
    </div>
  );
}
