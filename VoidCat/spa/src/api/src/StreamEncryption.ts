import {sjclcodec} from "./codecBytes";
import sjcl, {SjclCipher} from "sjcl";

import {buf2hex} from "Util";

interface EncryptionParams {
    ts: number,
    cs: number
}

/**
 * AES-GCM TransformStream
 */
export class StreamEncryption {
    readonly #tagSize: number;
    readonly #chunkSize: number;
    readonly #aes: SjclCipher;
    readonly #key: sjcl.BitArray;
    readonly #iv: sjcl.BitArray;

    constructor(key: string | sjcl.BitArray | undefined, iv: string | sjcl.BitArray | undefined, params?: EncryptionParams | string) {
        if (!key && !iv) {
            key = buf2hex(globalThis.crypto.getRandomValues(new Uint8Array(16)));
            iv = buf2hex(globalThis.crypto.getRandomValues(new Uint8Array(12)));
        }
        if (typeof key === "string" && typeof iv === "string") {
            key = sjcl.codec.hex.toBits(key);
            iv = sjcl.codec.hex.toBits(iv);
        } else if (!Array.isArray(key) || !Array.isArray(iv)) {
            throw "Key and IV must be hex string or bitArray";
        }
        if (typeof params === "string") {
            params = JSON.parse(params) as EncryptionParams;
        }

        this.#tagSize = params?.ts ?? 128;
        this.#chunkSize = params?.cs ?? (1024 * 1024 * 10);
        this.#aes = new sjcl.cipher.aes(key);
        this.#key = key;
        this.#iv = iv;

        console.log(`ts=${this.#tagSize}, cs=${this.#chunkSize}, key=${key}, iv=${this.#iv}`);
    }

    /**
     * Return formatted encryption key
     */
    getKey() {
        return `${sjcl.codec.hex.fromBits(this.#key)}:${sjcl.codec.hex.fromBits(this.#iv)}`;
    }

    /**
     * Get encryption params
     */
    getParams() {
        return {
            ts: this.#tagSize,
            cs: this.#chunkSize
        }
    }

    /**
     * Get encryption TransformStream
     */
    getEncryptionTransform() {
        return this._getCryptoStream(0);
    }

    /**
     * Get decryption TransformStream
     */
    getDecryptionTransform() {
        return this._getCryptoStream(1);
    }

    _getCryptoStream(mode: number) {
        let offset = 0;
        let buffer = new Uint8Array(this.#chunkSize + (mode === 1 ? this.#tagSize / 8 : 0));
        return new TransformStream({
            transform: async (chunk, controller) => {
                chunk = await chunk;
                try {
                    let toBuffer = Math.min(chunk.byteLength, buffer.byteLength - offset);
                    buffer.set(chunk.slice(0, toBuffer), offset);
                    offset += toBuffer;

                    if (offset === buffer.byteLength) {
                        let buff = sjclcodec.toBits(buffer);
                        let encryptedBuf = sjclcodec.fromBits(
                            mode === 0 ?
                                sjcl.mode.gcm.encrypt(this.#aes, buff, this.#iv, [], this.#tagSize) :
                                sjcl.mode.gcm.decrypt(this.#aes, buff, this.#iv, [], this.#tagSize)
                        );
                        controller.enqueue(new Uint8Array(encryptedBuf));

                        offset = chunk.byteLength - toBuffer;
                        buffer.set(chunk.slice(toBuffer));
                    }
                } catch (e) {
                    console.error(e);
                    throw e;
                }
            },
            flush: (controller) => {
                let lastBuffer = buffer.slice(0, offset);
                let buff = sjclcodec.toBits(lastBuffer);
                let encryptedBuf = sjclcodec.fromBits(
                    mode === 0 ?
                        sjcl.mode.gcm.encrypt(this.#aes, buff, this.#iv, [], this.#tagSize) :
                        sjcl.mode.gcm.decrypt(this.#aes, buff, this.#iv, [], this.#tagSize)
                );
                controller.enqueue(new Uint8Array(encryptedBuf));
            }
        }, {
            highWaterMark: this.#chunkSize
        });
    }
}