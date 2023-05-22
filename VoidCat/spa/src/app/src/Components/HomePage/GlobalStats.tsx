import "./GlobalStats.css";
import {Fragment} from "react";
import moment from "moment";
import {useSelector} from "react-redux";

import Icon from "Components/Shared/Icon";
import {RootState} from "Store";
import {FormatBytes} from "Util";

export function GlobalStats() {
    let stats = useSelector((s: RootState) => s.info.info);

    return (
        <Fragment>
            <dl className="stats">
                <div>
                    <Icon name="upload-cloud"/>
                    {FormatBytes(stats?.bandwidth?.ingress ?? 0, 2)}
                </div>
                <div>
                    <Icon name="download-cloud"/>
                    {FormatBytes(stats?.bandwidth?.egress ?? 0, 2)}
                </div>
                <div>
                    <Icon name="save"/>
                    {FormatBytes(stats?.totalBytes ?? 0, 2)}
                </div>
                <div>
                    <Icon name="hash"/>
                    {stats?.count ?? 0}
                </div>
            </dl>
            {stats?.buildInfo && <div className="build-info">
                {stats.buildInfo.version}-{stats.buildInfo.gitHash}
                <br/>
                {moment(stats.buildInfo.buildTime).fromNow()}
            </div>}
        </Fragment>
    );
}