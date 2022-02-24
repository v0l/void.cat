async function getJson(method, url, auth, body) {
    let headers = {
        "Accept": "application/json"
    };
    if (auth) {
        headers["Authorization"] = `Bearer ${auth}`;
    }
    if (body) {
        headers["Content-Type"] = "application/json";
    }

    return await fetch(url, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined
    });
}

export const AdminApi = {
    fileList: (auth, pageReq) => getJson("POST", "/admin/file", auth, pageReq),
    deleteFile: (auth, id) => getJson("DELETE", `/admin/file/${id}`, auth),
    userList: (auth, pageReq) => getJson("POST", `/admin/user`, auth, pageReq)
}

export const Api = {
    stats: () => getJson("GET", "/stats"),
    fileInfo: (id) => getJson("GET", `/upload/${id}`),
    setPaywallConfig: (id, cfg) => getJson("POST", `/upload/${id}/paywall`, undefined, cfg),
    createOrder: (id) => getJson("GET", `/upload/${id}/paywall`),
    getOrder: (file, order) => getJson("GET", `/upload/${file}/paywall/${order}`),
    login: (username, password) => getJson("POST", `/auth/login`, undefined, {username, password}),
    register: (username, password) => getJson("POST", `/auth/register`, undefined, {username, password})
}