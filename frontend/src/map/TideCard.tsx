import { useRef, useState, type ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { DaysInViewToggle, type DaysInView } from "@/samples/DaysInViewToggle";
import { TideGraph } from "@/samples/TideGraph";
import { CurrentLevel } from "@/tides/CurrentLevel";
import { TidePanel } from "@/tides/TidePanel";
import type { TodayHandle } from "@/tides/TodayHandle";
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
  const [daysInView, setDaysInView] = useState<DaysInView>(2);
  const todayRef = useRef<TodayHandle>(null);
  const showsGraph = locationId !== undefined && view === "graph";

  return (
    // The height fits the tides or the graph, so the parts below them are reached by scrolling the card.
    <section
      aria-label={name}
      className="max-h-[min(60svh,18rem)] overflow-y-auto border-t bg-background px-4 py-3"
    >
      <div className="flex items-center gap-2">
        <div className="min-w-0 flex-1">
          <h2 className="truncate text-lg font-semibold">{name}</h2>
          <CurrentLevel path={path} />
        </div>
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
      <div className="mt-1 flex items-center gap-1">
        {locationId !== undefined && <TideViewToggle value={view} onValueChange={setView} />}
        <Button
          variant="outline"
          className="ml-auto h-11"
          onClick={() => todayRef.current?.scrollToToday()}
        >
          Today
        </Button>
        {showsGraph && <DaysInViewToggle value={daysInView} onValueChange={setDaysInView} />}
      </div>
      {showsGraph ? (
        <TideGraph
          key={tidesVersion}
          locationId={locationId}
          daysInView={daysInView}
          ref={todayRef}
        />
      ) : (
        <TidePanel key={`${path}#${tidesVersion}`} path={path} ref={todayRef} />
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
