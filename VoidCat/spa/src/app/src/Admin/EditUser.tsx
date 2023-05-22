import {useState} from "react";
import {useSelector} from "react-redux";
import {AdminProfile} from "@void-cat/api";

import {VoidButton} from "../Components/Shared/VoidButton";
import useApi from "Hooks/UseApi";
import {RootState} from "Store";

export default function EditUser({user, onClose}: {user: AdminProfile, onClose: () => void}) {

    const adminApi = useApi();
    const fileStores = useSelector((state: RootState) => state.info?.info?.fileStores ?? ["local-disk"])
    const [storage, setStorage] = useState(user.storage);
    const [email, setEmail] = useState(user.email);

    async function updateUser() {
        await adminApi.adminUpdateUser({
            ...user,
            email,
            storage
        });
        onClose();
    }

    return (
        <>
            Editing user '{user.name}' ({user.id})
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
            <VoidButton onClick={() => updateUser()}>Save</VoidButton>
            <VoidButton onClick={() => onClose()}>Cancel</VoidButton>
        </>
    );
}