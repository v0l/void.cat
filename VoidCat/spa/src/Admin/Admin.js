import {useSelector} from "react-redux";
import {Login} from "../Login";

export function Admin(props) {
    const auth = useSelector((state) => state.login.jwt);
    
    if(!auth) {
        return <Login/>;
    } else {
        return (
            <div>
                <h3>Admin</h3>
            </div>
        );
    }
}