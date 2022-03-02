import FeatherIcon from "feather-icons-react";
import {useState} from "react";
import {btnDisable, btnEnable} from "./Util";

export function NoPaywallConfig(props) {
    const [saveStatus, setSaveStatus] = useState();
    const privateFile = props.privateFile;
    const onSaveConfig = props.onSaveConfig;

    async function saveConfig(e) {
        if(!btnDisable(e.target)) return;
        
        let cfg = {
            editSecret: privateFile.metadata.editSecret
        };

        if (typeof onSaveConfig === "function") {
            setSaveStatus(await onSaveConfig(cfg));
        }
        btnEnable(e.target);
    }

    return (
        <div>
            <div className="btn" onClick={saveConfig}>Save</div>
            {saveStatus ? <FeatherIcon icon={saveStatus === true ? "check-circle" : "alert-circle"}/> : null}
        </div>
    )
}