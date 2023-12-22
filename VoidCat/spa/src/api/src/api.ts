import {
    AdminProfile,
    AdminUserListResult,
    ApiError,
    ApiKey,
    LoginSession,
    PagedRequest,
    PagedResponse,
    PaymentOrder,
    Profile,
    SetPaymentConfigRequest,
    SiteInfoResponse,
    VoidFileResponse,
} from "./index";
import {
    ProgressHandler,
    ProxyChallengeHandler,
    StateChangeHandler,
    VoidUploader,
} from "./upload";
import {StreamUploader} from "./stream-uploader";
import {XHRUploader} from "./xhr-uploader";

export type AuthHandler = (url: string, method: string) => Promise<string>;

export class VoidApi {
    readonly #uri: string;
    readonly #auth?: AuthHandler;

    constructor(uri?: string, auth?: AuthHandler) {
        this.#uri = uri ?? "";
        this.#auth = auth;
    }

    async #req<T>(method: string, url: string, body?: object): Promise<T> {
        const absoluteUrl = `${this.#uri}${url}`;
        const headers: HeadersInit = {
            Accept: "application/json",
        };
        if (this.#auth) {
            headers["Authorization"] = await this.#auth(absoluteUrl, method);
        }
        if (body) {
            headers["Content-Type"] = "application/json";
        }

        const res = await fetch(absoluteUrl, {
            method,
            headers,
            mode: "cors",
            body: body ? JSON.stringify(body) : undefined,
        });
        const text = await res.text();
        if (res.ok) {
            return text ? (JSON.parse(text) as T) : ({} as T);
        } else {
            throw new ApiError(res.status, text);
        }
    }

    /**
     * Get uploader for uploading files
     */
    getUploader(
        file: File | Blob,
        stateChange?: StateChangeHandler,
        progress?: ProgressHandler,
        proxyChallenge?: ProxyChallengeHandler,
        chunkSize?: number,
    ): VoidUploader {
        if (StreamUploader.canUse()) {
            return new StreamUploader(
                this.#uri,
                file,
                stateChange,
                progress,
                proxyChallenge,
                this.#auth,
                chunkSize,
            );
        } else {
            return new XHRUploader(
                this.#uri,
                file,
                stateChange,
                progress,
                proxyChallenge,
                this.#auth,
                chunkSize,
            );
        }
    }

    /**
     * General site information
     */
    info() {
        return this.#req<SiteInfoResponse>("GET", "/info");
    }

    fileInfo(id: string) {
        return this.#req<VoidFileResponse>("GET", `/upload/${id}`);
    }

    setPaymentConfig(id: string, cfg: SetPaymentConfigRequest) {
        return this.#req("POST", `/upload/${id}/payment`, cfg);
    }

    createOrder(id: string) {
        return this.#req<PaymentOrder>("GET", `/upload/${id}/payment`);
    }

    getOrder(file: string, order: string) {
        return this.#req<PaymentOrder>("GET", `/upload/${file}/payment/${order}`);
    }

    login(username: string, password: string, captcha?: string) {
        return this.#req<LoginSession>("POST", `/auth/login`, {
            username,
            password,
            captcha,
        });
    }

    register(username: string, password: string, captcha?: string) {
        return this.#req<LoginSession>("POST", `/auth/register`, {
            username,
            password,
            captcha,
        });
    }

    getUser(id: string) {
        return this.#req<Profile>("GET", `/user/${id}`);
    }

    updateUser(u: Profile) {
        return this.#req<void>("POST", `/user/${u.id}`, u);
    }

    listUserFiles(uid: string, pageReq: PagedRequest) {
        return this.#req<PagedResponse<VoidFileResponse>>(
            "POST",
            `/user/${uid}/files`,
            pageReq,
        );
    }

    submitVerifyCode(uid: string, code: string) {
        return this.#req<void>("POST", `/user/${uid}/verify`, {code});
    }

    sendNewCode(uid: string) {
        return this.#req<void>("GET", `/user/${uid}/verify`);
    }

    updateFileMetadata(id: string, meta: any) {
        return this.#req<void>("POST", `/upload/${id}/meta`, meta);
    }

    listApiKeys() {
        return this.#req<Array<ApiKey>>("GET", `/auth/api-key`);
    }

    createApiKey(req: any) {
        return this.#req<ApiKey>("POST", `/auth/api-key`, req);
    }

    adminListFiles(pageReq: PagedRequest) {
        return this.#req<PagedResponse<VoidFileResponse>>(
            "POST",
            "/admin/file",
            pageReq,
        );
    }

    adminDeleteFile(id: string) {
        return this.#req<void>("DELETE", `/admin/file/${id}`);
    }

    adminUserList(pageReq: PagedRequest) {
        return this.#req<PagedResponse<AdminUserListResult>>(
            "POST",
            `/admin/users`,
            pageReq,
        );
    }

    adminUpdateUser(u: AdminProfile) {
        return this.#req<void>("POST", `/admin/update-user`, u);
    }
}
