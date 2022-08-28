import {Fragment} from "react";
import FeatherIcon from "feather-icons-react";
import {FormatBytes} from "./Util";

import "./GlobalStats.css";
import moment from "moment";
import {useSelector} from "react-redux";

export function GlobalStats() {
    let stats = useSelector(state => state.info.info);

    return (
        <Fragment>
            <dl className="stats">
                <div>
                    <FeatherIcon icon="upload-cloud"/>
                    {FormatBytes(stats?.bandwidth?.ingress ?? 0, 2)}
                </div>
                <div>
                    <FeatherIcon icon="download-cloud"/>
                    {FormatBytes(stats?.bandwidth?.egress ?? 0, 2)}
                </div>
                <div>
                    <FeatherIcon icon="hard-drive"/>
                    {FormatBytes(stats?.totalBytes ?? 0, 2)}
                </div>
                <div>
                    <FeatherIcon icon="hash"/>
                    {stats?.count ?? 0}
                </div>
            </dl>
            {stats?.buildInfo ? <div className="build-info">
                {stats.buildInfo.version}-{stats.buildInfo.gitHash}
                <br/>
                {moment(stats.buildInfo.buildTime).fromNow()}
            </div> : null}
        </Fragment>
    );
}