import "./FileEdit.css";
import {useState} from "react";
import {useSelector} from "react-redux";
import moment from "moment";

import {StrikePaymentConfig} from "./StrikePaymentConfig";
import {NoPaymentConfig} from "./NoPaymentConfig";
import {VoidButton} from "../Shared/VoidButton";

import useApi from "Hooks/UseApi";
import {RootState} from "Store";
import {Payment, PaymentServices, SetPaymentConfigRequest, VoidFileResponse} from "Api";

interface FileEditProps {
    file: VoidFileResponse
}

export function FileEdit({file}: FileEditProps) {
    const Api = useApi();
    const profile = useSelector((s: RootState) => s.login.profile);
    const [payment, setPayment] = useState(file.payment?.service);
    const [name, setName] = useState(file.metadata?.name ?? "");
    const [description, setDescription] = useState(file.metadata?.description ?? "");
    const [expiry, setExpiry] = useState<number | undefined>(file.metadata?.expires ? moment(file.metadata?.expires).unix() * 1000 : undefined);

    const localFile = window.localStorage.getItem(file.id);
    const privateFile: VoidFileResponse = profile?.id === file.uploader?.id
        ? file
        : localFile ? JSON.parse(localFile) : undefined;
    if (!privateFile?.metadata?.editSecret) {
        return null;
    }

    async function savePaymentConfig(cfg: SetPaymentConfigRequest) {
        try {
            await Api.setPaymentConfig(file.id, cfg);
            return true;
        } catch (e) {
            console.error(e);
            return false;
        }
    }

    async function saveMeta() {
        const meta = {
            name,
            description,
            editSecret: privateFile?.metadata?.editSecret,
            expires: moment(expiry).toISOString()
        };
        await Api.updateFileMetadata(file.id, meta);
    }

    function renderPaymentConfig() {
        switch (payment) {
            case PaymentServices.None: {
                return <NoPaymentConfig privateFile={privateFile} onSaveConfig={savePaymentConfig}/>;
            }
            case PaymentServices.Strike: {
                return <StrikePaymentConfig file={file} privateFile={privateFile} onSaveConfig={savePaymentConfig}/>
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
                               value={expiry ? moment(expiry).toISOString().replace("Z", "") : ""}
                               max={moment.utc().add(1, "year").toISOString().replace("Z", "")}
                               min={moment.utc().toISOString().replace("Z", "")}
                               onChange={(e) => {
                                   if (e.target.value.length > 0) {
                                       setExpiry(moment.utc(e.target.value).unix() * 1000);
                                   } else {
                                       setExpiry(undefined);
                                   }
                               }}/>
                    </dd>
                </dl>
                <VoidButton onClick={() => saveMeta()} options={{showSuccess: true}}>
                    Save
                </VoidButton>
            </div>
            <div className="flx-1">
                <h3>Payment Config</h3>
                Type:
                <select onChange={(e) => setPayment(parseInt(e.target.value))} value={payment}>
                    <option value={0}>None</option>
                    <option value={1}>Strike</option>
                </select>
                {renderPaymentConfig()}
            </div>
        </div>
    );
}