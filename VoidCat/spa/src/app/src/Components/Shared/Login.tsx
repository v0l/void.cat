import { useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import HCaptcha from "@hcaptcha/react-hcaptcha";

import "./Login.css";
import { setAuth } from "@/LoginState";
import { VoidButton } from "./VoidButton";

import useApi from "@/Hooks/UseApi";
import { RootState } from "@/Store";

export function Login() {
  const Api = useApi();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [captchaResponse, setCaptchaResponse] = useState("");
  const captchaKey = useSelector((s: RootState) => s.info.info?.captchaSiteKey);
  const oAuthProviders = useSelector(
    (s: RootState) => s.info.info?.oAuthProviders,
  );
  const dispatch = useDispatch();

  async function login(fnLogin: typeof Api.login) {
    setError("");

    try {
      const rsp = await fnLogin(username, password, captchaResponse);
      if (rsp.jwt) {
        dispatch(
          setAuth({
            jwt: rsp.jwt,
            profile: rsp.profile!,
          }),
        );
      } else {
        setError(rsp.error!);
      }
    } catch (e) {
      if (e instanceof Error) {
        setError(e.message);
      }
    }
  }

  return (
    <div className="login">
      <h2>Login</h2>
      <dl>
        <dt>Username:</dt>
        <dd>
          <input
            type="text"
            placeholder="user@example.com"
            onChange={(e) => setUsername(e.target.value)}
            value={username}
          />
        </dd>
        <dt>Password:</dt>
        <dd>
          <input
            type="password"
            onChange={(e) => setPassword(e.target.value)}
            value={password}
          />
        </dd>
      </dl>
      {captchaKey ? (
        <HCaptcha
          sitekey={captchaKey}
          onVerify={(v) => setCaptchaResponse(v)}
        />
      ) : null}
      <VoidButton onClick={() => login(Api.login.bind(Api))}>Login</VoidButton>
      <VoidButton onClick={() => login(Api.register.bind(Api))}>
        Register
      </VoidButton>
      <br />
      {oAuthProviders
        ? oAuthProviders.map((a) => (
            <VoidButton
              key={a}
              onClick={() => (window.location.href = `/auth/${a}`)}
            >
              Login with {a}
            </VoidButton>
          ))
        : null}
      {error && <div className="error-msg">{error}</div>}
    </div>
  );
}
