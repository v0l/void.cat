import {useEffect, useState} from "react";
import moment from "moment";

import {useApi} from "../Shared/Api";
import {VoidButton} from "../Shared/VoidButton";
import VoidModal from "../Shared/VoidModal";

export default function ApiKeyList() {
    const {Api} = useApi();
    const [apiKeys, setApiKeys] = useState([]);
    const [newApiKey, setNewApiKey] = useState();
    const DefaultExpiry = 1000 * 60 * 60 * 24 * 90;

    async function loadApiKeys() {
        let keys = await Api.listApiKeys();
        setApiKeys(await keys.json());
    }

    async function createApiKey() {
        let rsp = await Api.createApiKey({
            expiry: new Date(new Date().getTime() + DefaultExpiry)
        });
        if (rsp.ok) {
            setNewApiKey(await rsp.json());
        }
    }
    
    function openDocs() {
        window.open("/swagger", "_blank")
    }
    
    useEffect(() => {
        if (Api) {
            loadApiKeys();
        }
    }, []);

    return (
        <>
            <div className="flex flex-center">
                <div className="flx-grow">
                    <h1>API Keys</h1>
                </div>
                <div>
                    <VoidButton onClick={(e) => createApiKey()}>+New</VoidButton>
                    <VoidButton onClick={(e) => openDocs()}>Docs</VoidButton>
                </div>
            </div>
            <table>
                <thead>
                <tr>
                    <th>Id</th>
                    <th>Created</th>
                    <th>Expiry</th>
                    <th>Actions</th>
                </tr>
                </thead>
                <tbody>
                {apiKeys.map(e => <tr key={e.id}>
                    <td>{e.id}</td>
                    <td>{moment(e.created).fromNow()}</td>
                    <td>{moment(e.expiry).fromNow()}</td>
                    <td>
                        <VoidButton>Delete</VoidButton>
                    </td>
                </tr>)}
                </tbody>
            </table>
            {newApiKey ?
                <VoidModal title="New Api Key" style={{maxWidth: "50vw"}}>
                    Please save this now as it will not be shown again:
                    <pre className="copy">{newApiKey.token}</pre>
                    <VoidButton onClick={(e) => setNewApiKey(undefined)}>Close</VoidButton>
                </VoidModal> : null}
        </>
    );
}