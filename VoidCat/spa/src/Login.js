import {useState} from "react";
import {useDispatch} from "react-redux";
import {setAuth} from "./LoginState";

export function Login(props) {
    const [username, setUsername] = useState();
    const [password, setPassword] = useState();
    const dispatch = useDispatch();

    async function login(e) {
        e.target.disabled = true;
        
        let req = await fetch("/auth/login", {
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
            dispatch(setAuth(rsp.jwt));
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
            <button onClick={login}>Login</button>
        </div>
    );
}