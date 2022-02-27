import "./Admin.css";
import {useSelector} from "react-redux";
import {FileList} from "./FileList";
import {UserList} from "./UserList";
import {Navigate} from "react-router-dom";

export function Admin() {
    const auth = useSelector((state) => state.login.jwt);

    if (!auth) {
        return <Navigate to="/login"/>;
    } else {
        return (
            <div className="admin">
                <h4>Users</h4>
                <UserList/>

                <h4>Files</h4>
                <FileList/>
            </div>
        );
    }
}