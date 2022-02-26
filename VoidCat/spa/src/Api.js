import {useSelector} from "react-redux";
import {ApiHost} from "./Const";

export function useApi() {
    const auth = useSelector(state => state.login.jwt);

    async function getJson(method, url, body, token) {
        let headers = {
            "Accept": "application/json"
        };
        if (token) {
            headers["Authorization"] = `Bearer ${token}`;
        }
        if (body) {
            headers["Content-Type"] = "application/json";
        }

        return await fetch(`${ApiHost}${url}`, {
            method,
            headers,
            mode: "cors",
            body: body ? JSON.stringify(body) : undefined
        });
    }
    
    return {
        AdminApi: {
            fileList: (pageReq) => getJson("POST", "/admin/file", pageReq, auth),
            deleteFile: (id) => getJson("DELETE", `/admin/file/${id}`, undefined, auth),
            userList: (pageReq) => getJson("POST", `/admin/user`, pageReq, auth)
        },
        Api: {
            stats: () => getJson("GET", "/stats"),
            fileInfo: (id) => getJson("GET", `/upload/${id}`),
            setPaywallConfig: (id, cfg) => getJson("POST", `/upload/${id}/paywall`, cfg, auth),
            createOrder: (id) => getJson("GET", `/upload/${id}/paywall`),
            getOrder: (file, order) => getJson("GET", `/upload/${file}/paywall/${order}`),
            login: (username, password) => getJson("POST", `/auth/login`, {username, password}),
            register: (username, password) => getJson("POST", `/auth/register`, {username, password})
        }
    };
}