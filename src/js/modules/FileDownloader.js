import * as Const from './Const.js';
import { VBF } from './VBF.js';
import { XHR, Utils, Log } from './Util.js';
import { bytes_to_base64, HmacSha256, AES_CBC } from 'asmcrypto.js';

/**
 * File download and decryption class
 * @class
 * @param {object} fileinfo - The file info from the api response
 * @param {string} key - The key to use for decryption
 * @param {string} iv - The IV to use for decryption
 */
function FileDownloader(fileinfo, key, iv) {
    this.fileinfo = fileinfo;
    this.key = key;
    this.iv = iv;

    /**
     * Track download stats
     */
    this.downloadStats = {
        lastRate: 0,
        lastLoaded: 0,
        lastProgress: 0
    };

    /**
     * Gets the url for downloading
     * @returns {string} URL to download from 
     */
    this.GetLink = function () {
        return (this.fileinfo.DownloadHost !== null ? `${self.location.protocol}//${this.fileinfo.DownloadHost}` : '') + `/${this.fileinfo.FileId}`;
    };

    /**
     * Handles progress messages from file download
     */
    this.HandleProgress = function (type, progress) {
        switch (type) {
            case 'progress-download': {
                if (typeof this.onprogress === 'function') {
                    this.onprogress(progress);
                }
                break;
            }
            case 'progress-speed': {
                if (typeof this.oninfo === 'function') {
                    this.oninfo(progress);
                }
                break;
            }
            case 'decrypt-start': {
                if (typeof this.oninfo === 'function') {
                    this.oninfo('Decrypting..');
                }
                break;
            }
            case 'download-complete': {
                if (typeof this.oninfo === 'function') {
                    this.oninfo('Done!');
                }
                break;
            }
            case 'rate-limited': {
                if (typeof this.onratelimit === 'function') {
                    this.onratelimit(progress);
                }
                break;
            }
        }
    };

    /**
     * Streams the file download response
     * @returns {Promise<Response>} The response object to decrypt the download
     */
    this.StreamResponse = async function () {
        let link = this.GetLink();
        let response = await fetch(link, {
            mode: 'cors'
        });

        let void_download = {
            body: response.body,
            fileinfo: this.fileinfo,
            aes: new AES_CBC(new Uint8Array(Utils.HexToArray(this.key)), new Uint8Array(Utils.HexToArray(this.iv)), true),
            hmac: new HmacSha256(new Uint8Array(Utils.HexToArray(this.key))),
            buff: new Uint8Array(),
            pull(controller) {
                if (this.reader === undefined) {
                    this.reader = this.body.getReader();
                }
                return (async function () {
                    Log.I(`${this.fileinfo.FileId} Starting..`);
                    var isStart = true;
                    var decOffset = 0;
                    var headerLen = 0;
                    var fileHeader = null;
                    var hmacBytes = null;
                    while (true) {
                        let { done, value } = await this.reader.read();
                        if (done) {
                            if (this.buff.byteLength > 0) {
                                //pad the remaining data with PKCS#7
                                var toDecrypt = null;
                                let padding = 16 - (this.buff.byteLength % 16);
                                if(padding !== 0){
                                    let tmpBuff = new Uint8Array(this.buff.byteLength + padding);
                                    tmpBuff.fill(padding);
                                    tmpBuff.set(this.buff, 0);
                                    this.buff = null;
                                    this.buff = tmpBuff;
                                }
                                let decBytes = this.aes.AES_Decrypt_process(this.buff);
                                this.hmac.process(decBytes);
                                controller.enqueue(decBytes);
                                this.buff = null;
                            }
                            let last = this.aes.AES_Decrypt_finish();
                            this.hmac.process(last);
                            this.hmac.finish();
                            controller.enqueue(last);

                            //check hmac
                            let h1 = Utils.ArrayToHex(hmacBytes);
                            let h2 = Utils.ArrayToHex(this.hmac.result)
                            if (h1 === h2) {
                                Log.I(`HMAC verify ok!`);
                            } else {
                                Log.E(`HMAC verify failed (${h1} !== ${h2})`);
                                //controller.cancel();
                                //return;
                            }
                            Log.I(`${this.fileinfo.FileId} Download complete!`);
                            controller.close();
                            return;
                        }

                        var sliceStart = 0;
                        var sliceEnd = value.byteLength;

                        //!Slice this only once!!
                        var toDecrypt = value;
                        if (isStart) {
                            let header = VBF.ParseStart(value.buffer);
                            if (header !== null) {
                                Log.I(`${this.fileinfo.FileId} blob header version is ${header.version} uploaded on ${header.uploaded} (Magic: ${Utils.ArrayToHex(header.magic)})`);
                                sliceStart = VBF.SliceToEncryptedPart(header.version, value);
                            } else {
                                throw "Invalid VBF header";
                            }
                        } else if (fileHeader != null && decOffset + toDecrypt.byteLength + headerLen + 2 >= fileHeader.len) {
                            sliceEnd -= 32; //hash is on the end (un-encrypted)
                            hmacBytes = toDecrypt.slice(sliceEnd);
                        }

                        const GetAdjustedLen = function () {
                            return sliceEnd - sliceStart;
                        };

                        //decrypt
                        //append last remaining buffer if any
                        if (this.buff.byteLength > 0) {
                            let tmpd = new Uint8Array(this.buff.byteLength + GetAdjustedLen());
                            tmpd.set(this.buff, 0);
                            tmpd.set(toDecrypt.slice(sliceStart, sliceEnd), this.buff.byteLength);
                            sliceEnd += this.buff.byteLength;
                            toDecrypt = tmpd;
                            this.buff = new Uint8Array();
                        }

                        let blkRem = GetAdjustedLen() % 16;
                        if (blkRem !== 0) {
                            //save any remaining data into our buffer
                            this.buff = toDecrypt.slice(sliceEnd - blkRem, sliceEnd);
                            sliceEnd -= blkRem;
                        }

                        let encBytes = toDecrypt.slice(sliceStart, sliceEnd);
                        let decBytes = this.aes.AES_Decrypt_process(encBytes);
                        decOffset += decBytes.byteLength;

                        //read header
                        if (isStart) {
                            headerLen = new Uint16Array(decBytes.slice(0, 2))[0];
                            let header = new TextDecoder('utf-8').decode(decBytes.slice(2, 2 + headerLen));
                            Log.I(`${this.fileinfo.FileId} got header ${header}`);
                            fileHeader = JSON.parse(header);
                            decBytes = decBytes.slice(2 + headerLen);
                        }

                        //Log.I(`${this.fileinfo.FileId} Decrypting ${toDecrypt.byteLength} bytes, got ${decBytes.byteLength} bytes`);
                        this.hmac.process(decBytes);
                        controller.enqueue(decBytes);

                        isStart = false;
                    }
                }.bind(this))();
            }
        }

        let sr = new ReadableStream(void_download);
        return new Response(sr);
    };

    /**
     * Downloads the file
     * @returns {Promise<File>} The loaded and decripted file
     */
    this.DownloadFile = async function () {
        let link = this.GetLink();
        Log.I(`Starting download from: ${link}`);
        if (this.fileinfo.IsLegacyUpload) {
            return {
                isLegacy: true,
                name: this.fileinfo.LegacyFilename,
                mime: this.fileinfo.LegacyMime,
                url: link
            };
        } else {
            let rsp = await XHR('GET', link, undefined, undefined, undefined, function (ev) {
                let now = new Date().getTime();
                let dxLoaded = ev.loaded - this.downloadStats.lastLoaded;
                let dxTime = now - this.downloadStats.lastProgress;

                this.downloadStats.lastLoaded = ev.loaded;
                this.downloadStats.lastProgress = now;

                this.HandleProgress('progress-speed', `${Utils.FormatBytes(dxLoaded / (dxTime / 1000.0), 2)}/s`);
                this.HandleProgress('progress-download', ev.loaded / (ev.lengthComputable ? parseFloat(ev.total) : this.fileinfo.Size));
            }.bind(this), function (req) {
                req.responseType = "arraybuffer";
            });

            if (rsp.status === 200) {
                this.HandleProgress('decrypt-start');
                let fd_decrypted = await this.DecryptFile(rsp.response);
                this.HandleProgress('download-complete');
                return fd_decrypted;
            } else if (rsp.status === 429) {
                this.HandleProgress('rate-limited');
            }
        }

        return null;
    };

    /**
     * Decrypts the raw VBF file
     * @returns {Promise<*>} The decrypted file 
     */
    this.DecryptFile = async function (blob) {
        let header = VBF.Parse(blob);
        let hash_text = Utils.ArrayToHex(header.hmac);

        Log.I(`${this.fileinfo.FileId} blob header version is ${header.version} and hash is ${hash_text} uploaded on ${header.uploaded} (Magic: ${Utils.ArrayToHex(header.magic)})`);

        let key_raw = Utils.HexToArray(this.key);
        let iv_raw = Utils.HexToArray(this.iv);
        Log.I(`${this.fileinfo.FileId} decrypting with key ${this.key} and iv ${this.iv}`);

        let key = await crypto.subtle.importKey("raw", key_raw, Const.EncryptionKeyDetails, false, ['decrypt']);
        let keyhmac = await crypto.subtle.importKey("raw", key_raw, Const.HMACKeyDetails, false, ['verify']);

        let enc_data = VBF.GetEncryptedPart(header.version, blob);
        let decrypted_file = await crypto.subtle.decrypt({ name: Const.EncryptionAlgo, iv: iv_raw }, key, enc_data);

        //read the header 
        let json_header_length = new Uint16Array(decrypted_file.slice(0, 2))[0];
        let json_header_text = new TextDecoder('utf-8').decode(decrypted_file.slice(2, json_header_length + 2));
        Log.I(`${this.fileinfo.FileId} header is ${json_header_text}`);

        //hash the file to verify
        let file_data = decrypted_file.slice(2 + json_header_length);
        let hmac_verify = await crypto.subtle.verify(Const.HMACKeyDetails, keyhmac, header.hmac, file_data);
        if (hmac_verify) {
            Log.I(`${this.fileinfo.FileId} HMAC verified!`);

            let header_obj = JSON.parse(json_header_text);
            return { blob: new Blob([file_data], { type: header_obj.mime }), name: header_obj.name };
        } else {
            throw "HMAC verify failed";
        }
    };
};

export { FileDownloader };