import "./Admin.css";
import {useState} from "react";
import {useSelector} from "react-redux";
import {Navigate} from "react-router-dom";

import {FileList} from "../Components/Shared/FileList";
import {UserList} from "./UserList";
import {useApi} from "../Components/Shared/Api";
import {VoidButton} from "../Components/Shared/VoidButton";
import VoidModal from "../Components/Shared/VoidModal";
import EditUser from "./EditUser";

export function Admin() {
    const auth = useSelector((state) => state.login.jwt);
    const {AdminApi} = useApi();
    const [editUser, setEditUser] = useState(null);

    async function deleteFile(e, id) {
        if (window.confirm(`Are you sure you want to delete: ${id}?`)) {
            let req = await AdminApi.deleteFile(id);
            if (req.ok) {

            } else {
                alert("Failed to delete file!");
            }
        }
    }

    if (!auth) {
        return <Navigate to="/login"/>;
    } else {
        return (
            <div className="admin">
                <h2>Users</h2>
                <UserList actions={(i) => [
                    <VoidButton key={`delete-${i.id}`}>Delete</VoidButton>,
                    <VoidButton key={`edit-${i.id}`} onClick={(e) => setEditUser(i)}>Edit</VoidButton>
                ]}/>

                <h2>Files</h2>
                <FileList loadPage={AdminApi.fileList} actions={(i) => {
                    return <td>
                        <VoidButton onClick={(e) => deleteFile(e, i.id)}>Delete</VoidButton>
                    </td>
                }}/>

                {editUser !== null ?
                    <VoidModal title="Edit user">
                        <EditUser user={editUser} onClose={() => setEditUser(null)}/>
                    </VoidModal> : null}
            </div>
        );
    }
}