import "./Admin.css";
import {useSelector} from "react-redux";
import {FileList} from "../FileList";
import {UserList} from "./UserList";
import {Navigate} from "react-router-dom";
import {useApi} from "../Api";
import {VoidButton} from "../VoidButton";

export function Admin() {
    const auth = useSelector((state) => state.login.jwt);
    const {AdminApi} = useApi();


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
                <UserList/>

                <h2>Files</h2>
                <FileList loadPage={AdminApi.fileList} actions={(i) => {
                    return <td>
                        <VoidButton onClick={(e) => deleteFile(e, i.id)}>Delete</VoidButton>
                    </td>
                }}/>
            </div>
        );
    }
}