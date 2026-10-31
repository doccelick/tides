import { useState } from "react";
import { Skeleton } from "@/components/ui/skeleton";
import { importPresets, resolvePreset, type UtcRange } from "@/imports/importPresets";
import { RangeSelect } from "@/imports/RangeSelect";
import type { SampleInterval } from "./fetchSamples";
import { IntervalToggle } from "./IntervalToggle";
import { TideChart } from "./TideChart";
import { useSamples } from "./useSamples";

interface SamplesPanelProps {
  locationId: number;
}

interface SamplesChartProps {
  locationId: number;
  range: UtcRange;
  interval: SampleInterval;
}

const defaultPresetLabel = "Next 7 days";

export function SamplesPanel({ locationId }: SamplesPanelProps) {
  const [now] = useState(() => new Date());
  const [presetLabel, setPresetLabel] = useState(defaultPresetLabel);
  const [interval, setSampleInterval] = useState<SampleInterval>(10);

  const preset = importPresets.find((p) => p.label === presetLabel);
  const range = preset === undefined ? null : resolvePreset(preset, now);

  return (
    <section aria-label="Tide chart" className="mt-3 flex flex-col gap-2 border-t pt-3">
      <div className="flex gap-2">
        <div className="min-w-0 flex-1">
          <RangeSelect
            now={now}
            value={presetLabel}
            onValueChange={setPresetLabel}
            label="Chart range"
          />
        </div>
        <IntervalToggle value={interval} onValueChange={setSampleInterval} />
      </div>
      {range !== null && <SamplesChart locationId={locationId} range={range} interval={interval} />}
    </section>
  );
}

function SamplesChart({ locationId, range, interval }: SamplesChartProps) {
  const state = useSamples(locationId, range, interval);

  if (state.status === "loading") return <Skeleton className="h-48 w-full" />;
  if (state.status === "no-data") {
    return <p className="text-sm text-muted-foreground">Not imported for this range.</p>;
  }
  if (state.status === "failed") {
    return (
      <p role="alert" className="text-sm">
        Couldn't load water levels
      </p>
    );
  }

  return <TideChart samples={state.samples} />;
}
