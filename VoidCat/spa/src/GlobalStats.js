import { useEffect, useState } from "react";
import { FormatBytes } from "./Util";

import "./GlobalStats.css";

export function GlobalStats(props) {
    let [stats, setStats] = useState();

    async function loadStats() {
        let req = await fetch("/stats");
        if (req.ok) {
            setStats(await req.json());
        }
    }

    useEffect(() => loadStats(), []);

    return (
        <div className="stats">
            <div>Ingress:</div>
            <div>{FormatBytes(stats?.bandwidth?.ingress ?? 0)}</div>
            <div>Egress:</div>
            <div>{FormatBytes(stats?.bandwidth?.egress ?? 0)}</div>
            <div>Storage:</div>
            <div>{FormatBytes(stats?.totalBytes ?? 0)}</div>
        </div>
    );
}