import {UploadState, VoidUploader} from "./Upload";
import {VoidUploadResult} from "./index";
import {StreamEncryption} from "./StreamEncryption";

export class StreamUploader extends VoidUploader {
    #encrypt?: StreamEncryption;

    static canUse() {
        const rawUA = globalThis.navigator.userAgent.match(/Chrom(e|ium)\/([0-9]+)\./);
        const majorVersion = rawUA ? parseInt(rawUA[2], 10) : 0;
        return majorVersion >= 105 && "getRandomValues" in globalThis.crypto && globalThis.location.protocol === "https:";
    }

    canEncrypt(): boolean {
        return true;
    }

    setEncryption(s: boolean) {
        if (s) {
            this.#encrypt = new StreamEncryption(undefined, undefined, undefined);
        } else {
            this.#encrypt = undefined;
        }
    }

    getEncryptionKey() {
        return this.#encrypt?.getKey()
    }

    async upload(): Promise<VoidUploadResult> {
        this.onStateChange(UploadState.Hashing);
        const hash = await this.digest(this.file);
        let offset = 0;

        const DefaultChunkSize = 1024 * 1024;
        const rsBase = new ReadableStream({
            start: async () => {
                this.onStateChange(UploadState.Uploading);
            },
            pull: async (controller) => {
                const chunk = await this.readChunk(offset, controller.desiredSize ?? DefaultChunkSize);
                if (chunk.byteLength === 0) {
                    controller.close();
                    return;
                }
                this.onProgress(offset + chunk.byteLength);
                offset += chunk.byteLength
                controller.enqueue(chunk);
            },
            cancel: (reason) => {
                console.log(reason);
            },
            type: "bytes"
        }, {
            highWaterMark: DefaultChunkSize
        });

        const headers = {
            "Content-Type": "application/octet-stream",
            "V-Content-Type": !this.file.type ? "application/octet-stream" : this.file.type,
            "V-Filename": this.file.name,
            "V-Full-Digest": hash
        } as Record<string, string>;
        if (this.#encrypt) {
            headers["V-EncryptionParams"] = JSON.stringify(this.#encrypt!.getParams());
        }
        if (this.auth) {
            headers["Authorization"] = `Bearer ${this.auth}`;
        }
        const req = await fetch(`${this.uri}/upload`, {
            method: "POST",
            mode: "cors",
            body: this.#encrypt ? rsBase.pipeThrough(this.#encrypt!.getEncryptionTransform()) : rsBase,
            headers,
            // @ts-ignore New stream spec
            duplex: 'half'
        });

        if (req.ok) {
            return await req.json() as VoidUploadResult;
        } else {
            throw new Error("Unknown error");
        }
    }

    async readChunk(offset: number, size: number) {
        if (offset > this.file.size) {
            return new Uint8Array(0);
        }
        const end = Math.min(offset + size, this.file.size);
        const blob = this.file.slice(offset, end, this.file.type);
        const data = await blob.arrayBuffer();
        return new Uint8Array(data);
    }
}