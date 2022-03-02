import {useState} from "react";
import {useDispatch} from "react-redux";
import {setAuth} from "./LoginState";
import {useApi} from "./Api";
import "./Login.css";
import {btnDisable, btnEnable} from "./Util";

export function Login() {
    const {Api} = useApi();
    const [username, setUsername] = useState();
    const [password, setPassword] = useState();
    const [error, setError] = useState();
    const dispatch = useDispatch();

    async function login(e, fnLogin) {
        if(!btnDisable(e.target)) return;
        setError(null);

        let req = await fnLogin(username, password);
        if (req.ok) {
            let rsp = await req.json();
            if (rsp.jwt) {
                dispatch(setAuth(rsp));
            } else {
                setError(rsp.error);
            }
        }

        btnEnable(e.target);
    }

    return (
        <div className="login">
            <h2>Login</h2>
            <dl>
                <dt>Username:</dt>
                <dd><input type="text" onChange={(e) => setUsername(e.target.value)} placeholder="user@example.com"/></dd>
                <dt>Password:</dt>
                <dd><input type="password" onChange={(e) => setPassword(e.target.value)}/></dd>
            </dl>
            <div className="btn" onClick={(e) => login(e, Api.login)}>Login</div>
            <div className="btn" onClick={(e) => login(e, Api.register)}>Register</div>
            {error ? <div className="error-msg">{error}</div> : null}
        </div>
    );
}