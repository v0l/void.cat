import {VoidUploadResult} from "./index";
import sjcl from "sjcl";
import {sjclcodec} from "./codecBytes";
import {buf2hex} from "./Util";
/**
 * Generic upload state
 */
export enum UploadState {
    NotStarted,
    Starting,
    Hashing,
    Uploading,
    Done,
    Failed,
    Challenge
}

export type StateChangeHandler = (s: UploadState) => void;
export type ProgressHandler = (loaded: number) => void;
export type ProxyChallengeHandler = (html: string) => void;

/**
 * Base file uploader class
 */
export abstract class VoidUploader {
    protected uri: string;
    protected file: File | Blob;
    protected auth?: string;
    protected maxChunkSize: number;
    protected onStateChange: StateChangeHandler;
    protected onProgress: ProgressHandler;
    protected onProxyChallenge: ProxyChallengeHandler;

    constructor(
        uri: string,
        file: File | Blob,
        stateChange: StateChangeHandler,
        progress: ProgressHandler,
        proxyChallenge: ProxyChallengeHandler,
        auth?: string,
        chunkSize?: number
    ) {
        this.uri = uri;
        this.file = file;
        this.onStateChange = stateChange;
        this.onProgress = progress;
        this.onProxyChallenge = proxyChallenge;
        this.auth = auth;
        this.maxChunkSize = chunkSize ?? Number.MAX_VALUE;
    }

    /**
     * SHA-256 hash the entire blob
     * @param file
     * @protected
     */
    protected async digest(file: Blob) {
        const ChunkSize = 1024 * 1024;

        // must compute hash in chunks, subtle crypto cannot hash files > 2Gb
        const sha = new sjcl.hash.sha256();
        let progress = 0;
        for (let x = 0; x < Math.ceil(file.size / ChunkSize); x++) {
            const offset = x * ChunkSize;
            const slice = file.slice(offset, offset + ChunkSize);
            const chunk = await slice.arrayBuffer();
            sha.update(sjclcodec.toBits(new Uint8Array(chunk)));
            this.onProgress(progress += chunk.byteLength);
        }
        return buf2hex(sjclcodec.fromBits(sha.finalize()));
    }

    abstract upload(): Promise<VoidUploadResult>;

    /**
     * Can we use local encryption
     */
    abstract canEncrypt(): boolean;

    /**
     * Enable/Disable encryption, file will be encrypted on the fly locally before uploading
     */
    abstract setEncryption(s: boolean): void;

    /**
     * Get the encryption key, should be called after enableEncryption()
     */
    abstract getEncryptionKey(): string | undefined;
}