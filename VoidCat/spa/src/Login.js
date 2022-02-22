import {useState} from "react";
import {useDispatch} from "react-redux";
import {setAuth} from "./LoginState";

import "./Login.css";

export function Login(props) {
    const [username, setUsername] = useState();
    const [password, setPassword] = useState();
    const [error, setError] = useState();
    const dispatch = useDispatch();

    async function login(e, url) {
        e.target.disabled = true;
        setError(null);
        
        let req = await fetch(`/auth/${url}`, {
            method: "POST",
            body: JSON.stringify({
                username, password
            }),
            headers: {
                "content-type": "application/json"
            }
        });
        if (req.ok) {
            let rsp = await req.json();
            if(rsp.jwt) {
                dispatch(setAuth(rsp.jwt));
            } else {
                setError(rsp.error);
            }
        }
        e.target.disabled = false;
    }

    return (
        <div className="login">
            <h2>Login</h2>
            <dl>
                <dt>Username:</dt>
                <dd><input onChange={(e) => setUsername(e.target.value)}/></dd>
                <dt>Password:</dt>
                <dd><input type="password" onChange={(e) => setPassword(e.target.value)}/></dd>
            </dl>
            <button onClick={(e) => login(e, "login")}>Login</button>
            <button onClick={(e) => login(e, "register")}>Register</button>
            {error ? <div className="error-msg">{error}</div> : null}
        </div>
    );
}