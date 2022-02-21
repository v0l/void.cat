import {useState} from "react";

import "./FileEdit.css";
import {StrikePaywallConfig} from "./StrikePaywallConfig";

export function FileEdit(props) {
    const [paywall, setPaywall] = useState();

    const privateFile = JSON.parse(window.localStorage.getItem(props.file.id));
    if (!privateFile) {
        return null;
    }

    function renderPaywallConfig() {
        switch (paywall) {
            case 1: {
                return <StrikePaywallConfig file={privateFile}/>
            }
        }
        return null;
    }

    const meta = props.file.metadata;
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
                <select onChange={(e) => setPaywall(parseInt(e.target.value))}>
                    <option value={0}>None</option>
                    <option value={1}>Strike</option>
                </select>
                {renderPaywallConfig()}
            </div>
        </div>
    );
}