import { useState } from "react";
import { Area, AreaChart, CartesianGrid, ReferenceLine, XAxis, YAxis } from "recharts";
import type { SampleResponse } from "@/api/generated";
import { ChartContainer, type ChartConfig } from "@/components/ui/chart";
import { dayTicks } from "./dayTicks";
import { levelLines } from "./levelLines";

interface TideChartProps {
  samples: SampleResponse[];
  now: Date;
}

interface ChartPoint {
  time: number;
  level: number;
}

const chartConfig = {
  level: { label: "Water level", color: "var(--chart-2)" },
} satisfies ChartConfig;

const tickFormat = new Intl.DateTimeFormat("en-GB", { weekday: "short", day: "numeric" });

// Fills the width of its parent, so the parent decides how many days fit in view.
export function TideChart({ samples, now }: TideChartProps) {
  // The curve draws in once. Days added at the edges later appear without replaying it.
  const [animate, setAnimate] = useState(true);

  const points: ChartPoint[] = samples.map((sample) => ({
    time: Date.parse(sample.time),
    level: sample.levelCm,
  }));
  const ticks = points.length > 0 ? dayTicks(points[0].time, points[points.length - 1].time) : [];
  const lines = levelLines(points.map((point) => point.level));

  return (
    <ChartContainer
      config={chartConfig}
      role="figure"
      aria-label="Water level"
      className="aspect-auto h-full w-full"
    >
      <AreaChart data={points} margin={{ top: 4, right: 0, bottom: 0, left: 0 }}>
        <CartesianGrid horizontal={false} />
        <XAxis
          dataKey="time"
          type="number"
          scale="time"
          domain={["dataMin", "dataMax"]}
          ticks={ticks}
          tickFormatter={(time: number) => tickFormat.format(time)}
          tick={{ textAnchor: "start", dx: 4 }}
          tickLine={false}
          axisLine={false}
        />
        <YAxis hide domain={[lines.bottom, lines.top]} />
        {lines.values.map((level) => (
          <ReferenceLine
            key={level}
            y={level}
            stroke="var(--muted-foreground)"
            strokeOpacity={level % 100 === 0 ? 0.4 : 0.15}
          />
        ))}
        <Area
          dataKey="level"
          type="monotone"
          stroke="var(--color-level)"
          fill="var(--color-level)"
          fillOpacity={0.2}
          isAnimationActive={animate}
          onAnimationEnd={() => setAnimate(false)}
        />
        <ReferenceLine x={now.getTime()} stroke="var(--destructive)" strokeDasharray="3 2" />
      </AreaChart>
    </ChartContainer>
  );
}
