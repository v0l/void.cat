﻿import {useSelector} from "react-redux";
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
            userList: (pageReq) => getJson("POST", `/admin/users`, pageReq, auth),
            updateUser: (user) => getJson("POST", `/admin/update-user`, user, auth)
        },
        Api: {
            info: () => getJson("GET", "/info"),
            fileInfo: (id) => getJson("GET", `/upload/${id}`, undefined, auth),
            setPaymentConfig: (id, cfg) => getJson("POST", `/upload/${id}/payment`, cfg, auth),
            createOrder: (id) => getJson("GET", `/upload/${id}/payment`),
            getOrder: (file, order) => getJson("GET", `/upload/${file}/payment/${order}`),
            login: (username, password, captcha) => getJson("POST", `/auth/login`, {username, password, captcha}),
            register: (username, password, captcha) => getJson("POST", `/auth/register`, {username, password, captcha}),
            getUser: (id) => getJson("GET", `/user/${id}`, undefined, auth),
            updateUser: (u) => getJson("POST", `/user/${u.id}`, u, auth),
            listUserFiles: (uid, pageReq) => getJson("POST", `/user/${uid}/files`, pageReq, auth),
            submitVerifyCode: (uid, code) => getJson("POST", `/user/${uid}/verify`, code, auth),
            sendNewCode: (uid) => getJson("GET", `/user/${uid}/verify`, undefined, auth),
            updateMetadata: (id, meta) => getJson("POST", `/upload/${id}/meta`, meta, auth),
            listApiKeys: () => getJson("GET", `/auth/api-key`, undefined, auth),
            createApiKey: (req) => getJson("POST", `/auth/api-key`, req, auth)
        }
    };
}