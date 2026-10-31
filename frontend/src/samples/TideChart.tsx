import { Area, AreaChart, CartesianGrid, XAxis, YAxis } from "recharts";
import type { SampleResponse } from "@/api/generated";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import { formatTideLevel } from "@/tides/format";
import { dayTicks } from "./dayTicks";

interface TideChartProps {
  samples: SampleResponse[];
}

interface ChartPoint {
  time: number;
  level: number;
}

const chartConfig = {
  level: { label: "Water level", color: "var(--chart-2)" },
} satisfies ChartConfig;

const tickFormat = new Intl.DateTimeFormat("en-GB", { weekday: "short", day: "numeric" });

const tooltipFormat = new Intl.DateTimeFormat("en-GB", {
  weekday: "short",
  day: "numeric",
  month: "short",
  hour: "2-digit",
  minute: "2-digit",
});

export function TideChart({ samples }: TideChartProps) {
  const points: ChartPoint[] = samples.map((sample) => ({
    time: Date.parse(sample.time),
    level: sample.levelCm,
  }));
  const ticks = points.length > 0 ? dayTicks(points[0].time, points[points.length - 1].time) : [];

  return (
    <ChartContainer
      config={chartConfig}
      role="figure"
      aria-label="Water level"
      className="aspect-auto h-48 w-full"
    >
      <AreaChart data={points} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
        <CartesianGrid vertical={false} />
        <XAxis
          dataKey="time"
          type="number"
          scale="time"
          domain={["dataMin", "dataMax"]}
          ticks={ticks}
          tickFormatter={(time: number) => tickFormat.format(time)}
          tickLine={false}
          axisLine={false}
        />
        <YAxis
          width={40}
          tickFormatter={(level: number) => `${level}`}
          tickLine={false}
          axisLine={false}
        />
        <ChartTooltip
          content={
            <ChartTooltipContent
              hideIndicator
              labelFormatter={(_, payload) => tooltipFormat.format(payload[0]?.payload.time)}
              formatter={(level) => formatTideLevel(Number(level))}
            />
          }
        />
        <Area
          dataKey="level"
          type="monotone"
          stroke="var(--color-level)"
          fill="var(--color-level)"
          fillOpacity={0.2}
          isAnimationActive={false}
        />
      </AreaChart>
    </ChartContainer>
  );
}
