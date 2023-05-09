import {useEffect, useState} from "react";
import moment from "moment";

import {VoidButton} from "../Shared/VoidButton";
import VoidModal from "../Shared/VoidModal";

import useApi from "Hooks/UseApi";
import {ApiKey} from "Api";

export default function ApiKeyList() {
    const Api = useApi();
    const [apiKeys, setApiKeys] = useState<ApiKey[]>([]);
    const [newApiKey, setNewApiKey] = useState<ApiKey>();
    const DefaultExpiry = 1000 * 60 * 60 * 24 * 90;

    async function loadApiKeys() {
        try {
            const keys = await Api.listApiKeys();
            setApiKeys(keys);
        } catch (e) {
            console.error(e);
        }
    }

    async function createApiKey() {
        try {
            const rsp = await Api.createApiKey({
                expiry: new Date(new Date().getTime() + DefaultExpiry)
            });
            setNewApiKey(rsp);
        } catch (e) {
            console.error(e);
        }
    }

    function openDocs() {
        window.open("/swagger", "_blank")
    }

    useEffect(() => {
        loadApiKeys().catch(console.error);
    }, []);

    return (
        <>
            <div className="flex flex-center">
                <div className="flx-grow">
                    <h1>API Keys</h1>
                </div>
                <div>
                    <VoidButton onClick={() => createApiKey()}>+New</VoidButton>
                    <VoidButton onClick={() => openDocs()}>Docs</VoidButton>
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
            {newApiKey &&
                <VoidModal title="New Api Key" style={{maxWidth: "50vw"}}>
                    Please save this now as it will not be shown again:
                    <pre className="copy">{newApiKey.token}</pre>
                    <VoidButton onClick={() => setNewApiKey(undefined)}>Close</VoidButton>
                </VoidModal>}
        </>
    );
}