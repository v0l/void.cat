import {useSelector} from "react-redux";
import {Login} from "../Login";
import {FileList} from "./FileList";
import {UserList} from "./UserList";

import "./Admin.css";

export function Admin() {
    const auth = useSelector((state) => state.login.jwt);

    if (!auth) {
        return <Login/>;
    } else {
        return (
            <div className="admin">
                <h2>Admin</h2>

                <h4>Users</h4>
                <UserList/>

                <h4>Files</h4>
                <FileList/>
            </div>
        );
    }
}