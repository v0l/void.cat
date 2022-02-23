import {useEffect, useState} from "react";
import FeatherIcon from "feather-icons-react";
import {FormatBytes} from "./Util";

import "./GlobalStats.css";
import {Api} from "./Api";

export function GlobalStats(props) {
    let [stats, setStats] = useState();

    async function loadStats() {
        let req = await Api.stats();
        if (req.ok) {
            setStats(await req.json());
        }
    }

    useEffect(() => loadStats(), []);

    return (
        <dl className="stats">
            <div>
                <FeatherIcon icon="upload-cloud" />
                {FormatBytes(stats?.bandwidth?.ingress ?? 0, 2)}
            </div>
            <div>
                <FeatherIcon icon="download-cloud" />
                {FormatBytes(stats?.bandwidth?.egress ?? 0, 2)}
            </div>
            <div>
                <FeatherIcon icon="database" />
                {FormatBytes(stats?.totalBytes ?? 0, 2)}
            </div>
            <div>
                <FeatherIcon icon="hash" />
                {stats?.count ?? 0}
            </div>
        </dl>
    );
}