import { Bar, BarChart, Tooltip, XAxis } from "recharts";
import moment from "moment";
import { BandwidthPoint } from "@void-cat/api";

import { FormatBytes } from "Util";

interface MetricsGraphProps {
  metrics?: Array<BandwidthPoint>;
}
export function MetricsGraph({ metrics }: MetricsGraphProps) {
  if (!metrics || metrics.length === 0) return null;

  return (
    <BarChart
      width={Math.min(window.innerWidth, 900)}
      height={200}
      data={metrics}
      margin={{ left: 0, right: 0 }}
      style={{ userSelect: "none" }}
    >
      <XAxis
        dataKey="time"
        tickFormatter={(v) => `${moment(v).format("DD-MMM")}`}
      />
      <Bar dataKey="egress" fill="#ccc" />
      <Tooltip
        formatter={(v) => FormatBytes(v as number)}
        labelStyle={{ color: "#aaa" }}
        itemStyle={{ color: "#eee" }}
        contentStyle={{ backgroundColor: "#111" }}
      />
    </BarChart>
  );
}
