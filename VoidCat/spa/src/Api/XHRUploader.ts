import {UploadState, VoidUploader} from "./Upload";
import {VoidUploadResult} from "./index";

export class XHRUploader extends VoidUploader {
    canEncrypt(): boolean {
        return false;
    }

    setEncryption() {
        //noop
    }

    getEncryptionKey() {
        return undefined;
    }

    async upload(): Promise<VoidUploadResult> {
        this.onStateChange(UploadState.Hashing);
        const hash = await this.digest(this.file);
        if (this.file.size > this.maxChunkSize) {
            return await this.#doSplitXHRUpload(hash, this.maxChunkSize);
        } else {
            return await this.#xhrSegment(this.file, hash);
        }
    }

    async #doSplitXHRUpload(hash: string, splitSize: number) {
        let xhr = null;
        const segments = Math.ceil(this.file.size / splitSize);
        for (let s = 0; s < segments; s++) {
            const offset = s * splitSize;
            const slice = this.file.slice(offset, offset + splitSize, this.file.type);
            xhr = await this.#xhrSegment(slice, hash, xhr?.file?.id, xhr?.file?.metadata?.editSecret, s + 1, segments);
            if (!xhr.ok) {
                break;
            }
        }
        return xhr!;
    }

    /**
     * Upload a segment of the file
     * @param segment
     * @param fullDigest Full file hash
     * @param id
     * @param editSecret
     * @param part Segment number
     * @param partOf Total number of segments
     */
    async #xhrSegment(segment: ArrayBuffer | Blob, fullDigest: string, id?: string, editSecret?: string, part?: number, partOf?: number) {
        this.onStateChange(UploadState.Uploading);

        return await new Promise<VoidUploadResult>((resolve, reject) => {
            try {
                const req = new XMLHttpRequest();
                req.onreadystatechange = () => {
                    if (req.readyState === XMLHttpRequest.DONE && req.status === 200) {
                        const rsp = JSON.parse(req.responseText) as VoidUploadResult;
                        resolve(rsp);
                    } else if (req.readyState === XMLHttpRequest.DONE && req.status === 403) {
                        const contentType = req.getResponseHeader("content-type");
                        if (contentType?.toLowerCase().trim().indexOf("text/html") === 0) {
                            this.onProxyChallenge(req.response);
                            this.onStateChange(UploadState.Challenge);
                            reject(new Error("CF Challenge"));
                        }
                    }
                };
                req.upload.onprogress = (e) => {
                    if (e instanceof ProgressEvent) {
                        this.onProgress(e.loaded);
                    }
                };
                req.open("POST", id ? `${this.uri}/upload/${id}` : `${this.uri}/upload`);
                req.setRequestHeader("Content-Type", "application/octet-stream");
                req.setRequestHeader("V-Content-Type", !this.file.type ? "application/octet-stream" : this.file.type);
                req.setRequestHeader("V-Filename", this.file.name);
                req.setRequestHeader("V-Full-Digest", fullDigest);
                req.setRequestHeader("V-Segment", `${part}/${partOf}`)
                if (this.auth) {
                    req.withCredentials = true;
                    req.setRequestHeader("Authorization", `Bearer ${this.auth}`);
                }
                if (editSecret) {
                    req.setRequestHeader("V-EditSecret", editSecret);
                }
                req.send(segment);
            } catch (e) {
                reject(e);
            }
        });
    }
}