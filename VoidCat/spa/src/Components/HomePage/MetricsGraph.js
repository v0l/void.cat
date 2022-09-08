import {Bar, BarChart, Tooltip, XAxis} from "recharts";
import {FormatBytes} from "../Shared/Util";
import moment from "moment";

export function MetricsGraph(props) {
    const metrics = props.metrics;

    if (!metrics?.timeSeriesMetrics || metrics?.timeSeriesMetrics.length === 0) return null;

    return (
        <BarChart
            width={720}
            height={200}
            data={metrics.timeSeriesMetrics}
            margin={{left: 0, right: 0}}
            style={{userSelect: "none"}}>
            <XAxis dataKey="time" tickFormatter={(v, i) => `${moment(v).format("DD-MMM")}`}/>
            <Bar dataKey="egress" fill="#ccc"/>
            <Tooltip formatter={(v) => FormatBytes(v, 2)} labelStyle={{color: "#aaa"}} itemStyle={{color: "#eee"}}
                     contentStyle={{backgroundColor: "#111"}}/>
        </BarChart>
    );
}