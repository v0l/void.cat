import {VoidButton} from "../VoidButton";
import {useState} from "react";
import {useSelector} from "react-redux";
import {useApi} from "../Api";

export default function EditUser(props) {
    const user = props.user;
    const onClose = props.onClose;

    const adminApi = useApi().AdminApi;
    const fileStores = useSelector((state) => state.info?.stats?.fileStores ?? ["local-disk"])
    const [storage, setStorage] = useState(user.storage);
    const [email, setEmail] = useState(user.email);

    async function updateUser() {
        await adminApi.updateUser({
            id: user.id,
            email,
            storage
        });
        onClose();
    }

    return (
        <>
            Editing user '{user.displayName}' ({user.id})
            <dl>
                <dt>Email:</dt>
                <dd><input type="text" value={email} onChange={(e) => setEmail(e.target.value)}/></dd>

                <dt>File storage:</dt>
                <dd>
                    <select value={storage} onChange={(e) => setStorage(e.target.value)}>
                        <option disabled={true}>Current: {storage}</option>
                        {fileStores.map(e => <option key={e}>{e}</option>)}
                    </select>
                </dd>

                <dt>Roles:</dt>
                <dd>{user.roles.map(e => <span className="btn" key={e}>{e}</span>)}</dd>
            </dl>
            <VoidButton onClick={(e) => updateUser()}>Save</VoidButton>
            <VoidButton onClick={(e) => onClose()}>Cancel</VoidButton>
        </>
    );
}