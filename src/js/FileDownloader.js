/**
 * File download and decryption class
 * @class
 * @param {object} fileinfo - The file info from the api response
 * @param {string} key - The key to use for decryption
 * @param {string} iv - The IV to use for decryption
 */
const FileDownloader = function (fileinfo, key, iv) {
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
     * Downloads the file
     * @returns {Promise<File>} The loaded and decripted file
     */
    this.DownloadFile = async function () {
        let link = (this.fileinfo.DownloadHost !== null ? `${window.location.protocol}//${this.fileinfo.DownloadHost}` : '') + `/${this.fileinfo.FileId}`;
        Log.I(`Starting download from: ${link}`);
        if(this.fileinfo.IsLegacyUpload) {
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

        Log.I(`${this.fileinfo.FileId} blob header version is ${header.version} and hash is ${hash_text} uploaded on ${header.uploaded}`);

        let key_raw = Utils.HexToArray(this.key);
        let iv_raw = Utils.HexToArray(this.iv);
        Log.I(`${this.fileinfo.FileId} decrypting with key ${this.key} and iv ${this.iv}`);

        let key = await crypto.subtle.importKey("raw", key_raw, EncryptionKeyDetails, false, ['decrypt']);
        let keyhmac = await crypto.subtle.importKey("raw", key_raw, HMACKeyDetails, false, ['verify']);

        let decrypted_file = await crypto.subtle.decrypt({ name: EncryptionAlgo, iv: iv_raw }, key, blob.slice(VBF.HeaderSize));

        //read the header 
        let json_header_length = new Uint16Array(decrypted_file.slice(0, 2))[0];
        let json_header_text = new TextDecoder('utf-8').decode(decrypted_file.slice(2, json_header_length + 2));
        Log.I(`${this.fileinfo.FileId} header is ${json_header_text}`);

        //hash the file to verify
        let file_data = decrypted_file.slice(2 + json_header_length);
        let hmac_verify = await crypto.subtle.verify(HMACKeyDetails, keyhmac, header.hmac, file_data);
        if (hmac_verify) {
            Log.I(`${this.fileinfo.FileId} HMAC verified!`);

            let header_obj = JSON.parse(json_header_text);
            return { blob: new Blob([file_data], { type: header_obj.mime }), name: header_obj.name };
        } else {
            throw "HMAC verify failed";
        }
    };
};