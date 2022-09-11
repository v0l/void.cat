import {sjclcodec} from "../../codecBytes";
import sjcl from "sjcl";
import {buf2hex} from "./Util";

/**
 * AES-GCM TransformStream
 */
export class StreamEncryption {
    constructor(key, iv, params) {
        if (key === undefined && iv === undefined) {
            key = buf2hex(window.crypto.getRandomValues(new Uint8Array(16)));
            iv = buf2hex(window.crypto.getRandomValues(new Uint8Array(12)));
        }
        if (typeof key === "string" && typeof iv === "string") {
            key = sjcl.codec.hex.toBits(key);
            iv = sjcl.codec.hex.toBits(iv);
        } else if (!Array.isArray(key) || !Array.isArray(iv)) {
            throw "Key and IV must be hex string or bitArray";
        }
        if (typeof params === "string") {
            params = JSON.parse(params);
        }

        this.TagSize = params?.ts ?? 128;
        this.ChunkSize = params?.cs ?? (1024 * 1024 * 10);
        this.aes = new sjcl.cipher.aes(key);
        this.key = key;
        this.iv = iv;

        console.log(`ts=${this.TagSize}, cs=${this.ChunkSize}, key=${key}, iv=${this.iv}`);
    }

    /**
     * Return formatted encryption key
     * @returns {string}
     */
    getKey() {
        return `${sjcl.codec.hex.fromBits(this.key)}:${sjcl.codec.hex.fromBits(this.iv)}`;
    }
    
    /**
     * Get encryption params
     * @returns {{cs: (*|number), ts: number}}
     */
    getParams() {
        return {
            ts: this.TagSize,
            cs: this.ChunkSize
        }
    }

    /**
     * Get encryption TransformStream
     * @returns {TransformStream<any, any>}
     */
    getEncryptionTransform() {
        return this._getCryptoStream(0);
    }

    /**
     * Get decryption TransformStream
     * @returns {TransformStream<any, any>}
     */
    getDecryptionTransform() {
        return this._getCryptoStream(1);
    }

    _getCryptoStream(mode) {
        let offset = 0;
        let buffer = new Uint8Array(this.ChunkSize + (mode === 1 ? this.TagSize / 8 : 0));
        return new window.TransformStream({
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
                                sjcl.mode.gcm.encrypt(this.aes, buff, this.iv, [], this.TagSize) :
                                sjcl.mode.gcm.decrypt(this.aes, buff, this.iv, [], this.TagSize)
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
                        sjcl.mode.gcm.encrypt(this.aes, buff, this.iv, [], this.TagSize) :
                        sjcl.mode.gcm.decrypt(this.aes, buff, this.iv, [], this.TagSize)
                );
                controller.enqueue(new Uint8Array(encryptedBuf));
            }
        }, {
            highWaterMark: this.ChunkSize
        });
    }
}