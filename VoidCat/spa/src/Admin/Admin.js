import {useDispatch, useSelector} from "react-redux";
import {FileList} from "./FileList";
import {UserList} from "./UserList";

import "./Admin.css";
import {logout} from "../LoginState";
import {Navigate} from "react-router-dom";

export function Admin() {
    const auth = useSelector((state) => state.login.jwt);
    const dispatch = useDispatch();
    
    if (!auth) {
        return <Navigate to="/login"/>;
    } else {
        return (
            <div className="admin">
                <h2>Admin</h2>
                <button onClick={() => dispatch(logout())}>Logout</button>
                
                <h4>Users</h4>
                <UserList/>

                <h4>Files</h4>
                <FileList/>
            </div>
        );
    }
}