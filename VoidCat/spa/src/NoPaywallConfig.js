import FeatherIcon from "feather-icons-react";
import {useState} from "react";

export function NoPaywallConfig(props) {
    const [saveStatus, setSaveStatus] = useState();
    const privateFile = props.privateFile;
    const onSaveConfig = props.onSaveConfig;

    async function saveConfig(e) {
        e.target.disabled = true;
        let cfg = {
            editSecret: privateFile.metadata.editSecret
        };

        if (typeof onSaveConfig === "function") {
            setSaveStatus(await onSaveConfig(cfg));
        }
        e.target.disabled = false;
    }

    return (
        <div>
            <button onClick={saveConfig}>Save</button>
            {saveStatus ? <FeatherIcon icon={saveStatus === true ? "check-circle" : "alert-circle"}/> : null}
        </div>
    )
}