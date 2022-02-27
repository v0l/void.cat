import "./Admin.css";
import {useSelector} from "react-redux";
import {FileList} from "../FileList";
import {UserList} from "./UserList";
import {Navigate} from "react-router-dom";
import {useApi} from "../Api";

export function Admin() {
    const auth = useSelector((state) => state.login.jwt);
    const {AdminApi} = useApi();


    async function deleteFile(e, id) {
        e.target.disabled = true;
        if (window.confirm(`Are you sure you want to delete: ${id}?`)) {
            let req = await AdminApi.deleteFile(id);
            if (req.ok) {
                
            } else {
                alert("Failed to delete file!");
            }
        }
        e.target.disabled = false;
    }
    
    if (!auth) {
        return <Navigate to="/login"/>;
    } else {
        return (
            <div className="admin">
                <h4>Users</h4>
                <UserList/>

                <h4>Files</h4>
                <FileList loadPage={AdminApi.fileList} actions={(i) => {
                    return <td>
                        <button onClick={(e) => deleteFile(e, i.id)}>Delete</button>
                    </td>
                }}/>
            </div>
        );
    }
}