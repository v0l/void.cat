import {useState} from "react";

import "./FileEdit.css";
import {StrikePaywallConfig} from "./StrikePaywallConfig";
import {NoPaywallConfig} from "./NoPaywallConfig";

export function FileEdit(props) {
    const file = props.file;
    const [paywall, setPaywall] = useState(file.paywall?.service);

    const privateFile = JSON.parse(window.localStorage.getItem(file.id));
    if (!privateFile) {
        return null;
    }

    async function saveConfig(cfg) {
        let req = await fetch(`/upload/${file.id}/paywall`, {
            method: "POST",
            body: JSON.stringify(cfg),
            headers: {
                "Content-Type": "application/json"
            }
        });
        return req.ok;
    }
    
    function renderPaywallConfig() {
        switch (paywall) {
            case 0: {
                return <NoPaywallConfig privateFile={privateFile} onSaveConfig={saveConfig}/>;
            }
            case 1: {
                return <StrikePaywallConfig file={file} privateFile={privateFile} onSaveConfig={saveConfig}/>
            }
        }
        return null;
    }

    const meta = file.metadata;
    return (
        <div className="file-edit">
            <div>
                <h3>File info</h3>
                <dl>
                    <dt>Filename:</dt>
                    <dd><input type="text" value={meta.name}/></dd>
                    <dt>Description:</dt>
                    <dd><input type="text" value={meta.description}/></dd>
                </dl>
                
            </div>
            <div>
                <h3>Paywall Config</h3>
                Type:
                <select onChange={(e) => setPaywall(parseInt(e.target.value))} value={paywall}>
                    <option value={0}>None</option>
                    <option value={1}>Strike</option>
                </select>
                {renderPaywallConfig()}
            </div>
        </div>
    );
}