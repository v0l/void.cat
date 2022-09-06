import {useState} from "react";

import {StrikePaywallConfig} from "./StrikePaywallConfig";
import {NoPaywallConfig} from "./NoPaywallConfig";
import {useApi} from "./Api";
import "./FileEdit.css";
import {useSelector} from "react-redux";
import {VoidButton} from "./VoidButton";
import moment from "moment";

export function FileEdit(props) {
    const {Api} = useApi();
    const file = props.file;
    const meta = file.metadata;
    const profile = useSelector(state => state.login.profile);
    const [paywall, setPaywall] = useState(file.paywall?.service);
    const [name, setName] = useState(meta?.name);
    const [description, setDescription] = useState(meta?.description);
    const [expiry, setExpiry] = useState(meta?.expires === undefined || meta?.expires === null ? null : moment(meta?.expires).unix() * 1000);

    const privateFile = file?.uploader?.id && profile?.id === file.uploader.id
        ? file
        : JSON.parse(window.localStorage.getItem(file.id));
    if (!privateFile || privateFile?.metadata?.editSecret === null) {
        return null;
    }

    async function saveConfig(cfg) {
        let req = await Api.setPaywallConfig(file.id, cfg);
        return req.ok;
    }

    async function saveMeta() {
        let meta = {
            name,
            description,
            editSecret: privateFile?.metadata?.editSecret,
            expires: moment(expiry).toISOString()
        };
        await Api.updateMetadata(file.id, meta);
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

    return (
        <div className="file-edit flex">
            <div className="flx-1">
                <h3>File info</h3>
                <dl>
                    <dt>Filename:</dt>
                    <dd><input type="text" value={name} onChange={(e) => setName(e.target.value)}/></dd>
                    <dt>Description:</dt>
                    <dd><input type="text" value={description} onChange={(e) => setDescription(e.target.value)}/></dd>
                    <dt>Expiry</dt>
                    <dd>
                        <input type="datetime-local"
                               value={expiry === null ? "" : moment(expiry).toISOString().replace("Z", "")}
                               max={moment.utc().add(1, "year").toISOString().replace("Z", "")}
                               min={moment.utc().toISOString().replace("Z", "")}
                               onChange={(e) => {
                                   console.log(e.target.value);
                                   if (e.target.value.length > 0) {
                                       setExpiry(moment.utc(e.target.value).unix() * 1000);
                                   } else {
                                       setExpiry(null);
                                   }

                               }}/>
                    </dd>
                </dl>
                <VoidButton onClick={(e) => saveMeta()} options={{showSuccess: true}}>Save</VoidButton>
            </div>
            <div className="flx-1">
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