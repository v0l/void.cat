import FeatherIcon from "feather-icons-react";
import {useState} from "react";
import {VoidButton} from "./VoidButton";

export function NoPaymentConfig(props) {
    const [saveStatus, setSaveStatus] = useState();
    const privateFile = props.privateFile;
    const onSaveConfig = props.onSaveConfig;

    async function saveConfig() {
        let cfg = {
            editSecret: privateFile.metadata.editSecret
        };

        if (typeof onSaveConfig === "function") {
            setSaveStatus(await onSaveConfig(cfg));
        }
    }

    return (
        <div>
            <VoidButton onClick={saveConfig}>Save</VoidButton>
            {saveStatus ? <FeatherIcon icon={saveStatus === true ? "check-circle" : "alert-circle"}/> : null}
        </div>
    )
}