import {useSelector} from "react-redux";
import {useNavigate} from "react-router-dom";
import {useEffect} from "react";

import {Login} from "../Components/Shared/Login";
import {RootState} from "Store";

export function UserLogin() {
    const auth = useSelector((s: RootState) => s.login.jwt);
    const navigate = useNavigate();

    useEffect(() => {
        if (auth) {
            navigate("/");
        }
    }, [auth, navigate]);

    return (
        <div className="page">
            <Login/>
        </div>
    )
}