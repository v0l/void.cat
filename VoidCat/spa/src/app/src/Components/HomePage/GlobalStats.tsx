import moment from "moment";
import { useSelector } from "react-redux";

import Icon from "@/Components/Shared/Icon";
import { RootState } from "@/Store";
import { FormatBytes } from "@/Util";

export function GlobalStats() {
  let stats = useSelector((s: RootState) => s.info.info);

  return (
    <>
      <div className="flex justify-around py-2">
        <div className="flex gap-2 items-center">
          <Icon name="upload-cloud" />
          {FormatBytes(stats?.bandwidth?.ingress ?? 0, 2)}
        </div>
        <div className="flex gap-2 items-center">
          <Icon name="download-cloud" />
          {FormatBytes(stats?.bandwidth?.egress ?? 0, 2)}
        </div>
        <div className="flex gap-2 items-center">
          <Icon name="save" />
          {FormatBytes(stats?.totalBytes ?? 0, 2)}
        </div>
        <div className="flex gap-2 items-center">
          <Icon name="hash" />
          {stats?.count ?? 0}
        </div>

      </div>
      {stats?.buildInfo && (
        <div className="fixed bottom-2 left-2 text-xs text-slate-700">
          {stats.buildInfo.version}-{stats.buildInfo.gitHash}
          <br />
          {moment(stats.buildInfo.buildTime).fromNow()}
        </div>
      )}
    </>
  );
}
