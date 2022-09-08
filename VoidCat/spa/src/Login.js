import {useState} from "react";
import {useDispatch, useSelector} from "react-redux";
import {setAuth} from "./LoginState";
import {useApi} from "./Api";
import "./Login.css";
import HCaptcha from "@hcaptcha/react-hcaptcha";
import {VoidButton} from "./VoidButton";

export function Login() {
    const {Api} = useApi();
    const [username, setUsername] = useState();
    const [password, setPassword] = useState();
    const [error, setError] = useState();
    const [captchaResponse, setCaptchaResponse] = useState();
    const captchaKey = useSelector(state => state.info.info?.captchaSiteKey);
    const dispatch = useDispatch();

    async function login(fnLogin) {
        setError(null);

        let req = await fnLogin(username, password, captchaResponse);
        if (req.ok) {
            let rsp = await req.json();
            if (rsp.jwt) {
                dispatch(setAuth(rsp));
            } else {
                setError(rsp.error);
            }
        }
    }

    return (
        <div className="login">
            <h2>Login</h2>
            <dl>
                <dt>Username:</dt>
                <dd><input type="text" onChange={(e) => setUsername(e.target.value)} placeholder="user@example.com"/>
                </dd>
                <dt>Password:</dt>
                <dd><input type="password" onChange={(e) => setPassword(e.target.value)}/></dd>
            </dl>
            {captchaKey ? <HCaptcha sitekey={captchaKey} onVerify={setCaptchaResponse}/> : null}
            <VoidButton onClick={() => login(Api.login)}>Login</VoidButton>
            <VoidButton onClick={() => login(Api.register)}>Register</VoidButton>
            <br/>
            <VoidButton onClick={() => window.location.href = `/auth/discord`}>Login with Discord</VoidButton>
            {error ? <div className="error-msg">{error}</div> : null}
        </div>
    );
}