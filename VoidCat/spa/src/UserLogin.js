import {Login} from "./Login";
import {useSelector} from "react-redux";
import {useNavigate} from "react-router-dom";
import {useEffect} from "react";

export function UserLogin() {
    const auth = useSelector((state) => state.login.jwt);
    const navigate = useNavigate();

    useEffect(() => {
        if (auth) {
            navigate("/");
        }
    }, [auth]);

    return (
        <div className="page">
            <Login/>
        </div>
    )
}