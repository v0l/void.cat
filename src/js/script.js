
/**
 * @constant {string} - The hashing algo to use to verify the file
 */
const HashingAlgo = 'SHA-256';
/**
 * @constant {string} - The encryption algoritm to use for file uploads
 */
const EncryptionAlgo = 'AES-CBC';
/**
 * @constant {object} - The 'algo' argument for importing/exporting/generating keys
 */
const EncryptionKeyDetails = { name: EncryptionAlgo, length: 128 };
/**
 * @constant {object} - The 'algo' argument for importing/exporting/generating hmac keys
 */
const HMACKeyDetails = { name: 'HMAC', hash: HashingAlgo };
/**
 * @constant {number} - Size of 1 kiB
 */
const kiB = Math.pow(1024, 1);
/**
 * @constant {number} - Size of 1 MiB
 */
const MiB = Math.pow(1024, 2);
/**
 * @constant {number} - Size of 1 GiB
 */
const GiB = Math.pow(1024, 3);
/**
 * @constant {number} - Size of 1 TiB
 */
const TiB = Math.pow(1024, 4);
/**
 * @constant {number} - Size of 1 PiB
 */
const PiB = Math.pow(1024, 5);
/**
 * @constant {number} - Size of 1 EiB
 */
const EiB = Math.pow(1024, 6);
/**
 * @constant {number} - Size of 1 ZiB
 */
const ZiB = Math.pow(1024, 7);
/**
 * @constant {number} - Size of 1 YiB
 */
const YiB = Math.pow(1024, 8);
/**
 * @constant {function} - Helper function for document.querySelector
 * @param {string} selector - The selector to use in the query
 * @returns {HTMLElement} The first selected element
 */
const $ = (selector) => document.querySelector(selector);

const Log = {
    I: (msg) => console.log(`[App_v ${App.Version}][I]: ${msg}`),
    W: (msg) => console.warn(`[App_v ${App.Version}][W]: ${msg}`),
    E: (msg) => console.error(`[App_v ${App.Version}][E]: ${msg}`)
};

/**
 * @constant {Object}
 */
const App = {
    get Version() { return "1.0" },

    Elements: {
        get Dropzone() { return $('#dropzone') },
        get Uploads() { return $('#uploads') },
        get PageView() { return $('#page-view') },
        get PageUpload() { return $('#page-upload') }
    },

    Templates: {
        get Upload() { return $("template[id='tmpl-upload']") }
    },

    /**
     * Uploads the files as selected by the input form
     * @param {Element} ctx 
     * @returns {Promise}
     */
    UploadFiles: async function (ctx) {
        let files = ctx.files;
        let proc_files = [];

        for (let x = 0; x < files.length; x++) {
            let fu = new FileUpload(files[x]);
            proc_files[proc_files.length] = fu.ProcessUpload();
        }

        await Promise.all(proc_files);
    },

    Utils: {
        /**
         * Formats an ArrayBuffer to hex
         * @param {ArrayBuffer} buffer - Input data to convert to hex
         * @returns {string} The encoded data as a hex string
         */
        ArrayToHex: (buffer) => Array.prototype.map.call(new Uint8Array(buffer), x => ('00' + x.toString(16)).slice(-2)).join(''),

        /**
         * Converts hex to ArrayBuffer
         * @param {string} hex - The hex to parse into ArrayBuffer
         * @returns {ArrayBuffer} The parsed hex data
         */
        HexToArray: (hex) => {
            let ret = new Uint8Array(hex.length / 2)

            for (let i = 0; i < hex.length; i += 2) {
                ret[i / 2] = parseInt(hex.substring(i, i + 2), 16)
            }

            return ret.buffer
        },

        /**
         * Formats bytes into binary notation
         * @param {number} b - The value in bytes
         * @param {number} [f=2] - The number of decimal places to use
         * @returns {string} Bytes formatted in binary notation
         */
        FormatBytes: (b, f) => {
            f = typeof f === 'number' ? 2 : f;
            if (b >= YiB)
                return (b / YiB).toFixed(f) + ' YiB';
            if (b >= ZiB)
                return (b / ZiB).toFixed(f) + ' ZiB';
            if (b >= EiB)
                return (b / EiB).toFixed(f) + ' EiB';
            if (b >= PiB)
                return (b / PiB).toFixed(f) + ' PiB';
            if (b >= TiB)
                return (b / TiB).toFixed(f) + ' TiB';
            if (b >= GiB)
                return (b / GiB).toFixed(f) + ' GiB';
            if (b >= MiB)
                return (b / MiB).toFixed(f) + ' MiB';
            if (b >= kiB)
                return (b / kiB).toFixed(f) + ' KiB';
            return b.toFixed(f) + ' B'
        }
    },

    /**
     * Sets up the page
     */
    Init: function () {
        if (location.hash !== "") {
            App.Elements.PageUpload.style.display = "none";
            App.Elements.PageView.style.display = "block";
            new ViewManager();
        } else {
            App.Elements.PageUpload.style.display = "block";
            App.Elements.PageView.style.display = "none";
            new DropzoneManager(App.Elements.Dropzone);
        }
    }
};

/**
 * Make a HTTP request with promise
 * @param {string} method - HTTP method for this request
 * @param {string} url - Request URL
 * @param {[object]} data - Request payload (method must be post)
 * @returns {Promise<XMLHttpRequest>} The completed request
 */
const JsonXHR = async function (method, url, data) {
    return await XHR(method, url, JSON.stringify(data), {
        'Content-Type': 'application/json'
    });
};

/**
 * Make a HTTP request with promise
 * @param {string} method - HTTP method for this request
 * @param {string} url - Request URL
 * @param {[*]} data - Request payload (method must be post)
 * @param {[*]} headers - Headers to add to the request
 * @param {[function]} uploadprogress - Progress function from data uploads
 * @param {[function]} downloadprogress - Progress function for data downloads
 * @param {[function]} editrequest - Function that can edit the request before its sent
 * @returns {Promise<XMLHttpRequest>} The completed request
 */
const XHR = function (method, url, data, headers, uploadprogress, downloadprogress, editrequest) {
    return new Promise(function (resolve, reject) {
        let x = new XMLHttpRequest();
        x.onreadystatechange = function (ev) {
            if (ev.target.readyState === 4) {
                resolve(ev.target);
            }
        };
        x.upload.onprogress = function (ev) {
            if (typeof uploadprogress === "function") {
                uploadprogress(ev);
            }
        };
        x.onprogress = function (ev) {
            if (typeof downloadprogress === "function") {
                downloadprogress(ev);
            }
        };
        x.onerror = function (ev) {
            reject(ev);
        };
        x.open(method, url, true);

        if (typeof editrequest === "function") {
            editrequest(x);
        }
        //set headers if they are passed
        if (typeof headers === "object") {
            for (let h in headers) {
                x.setRequestHeader(h, headers[h]);
            }
        }
        if (method === "POST" && typeof data !== "undefined") {
            x.send(data);
        } else {
            x.send();
        }
    })
};

/**
 * Calls api handler
 */
const Api = {
    DoRequest: async function (req) {
        return JSON.parse((await JsonXHR('POST', '/api', req)).response);
    },

    GetFileInfo: async function (id) {
        return await Api.DoRequest({
            cmd: 'file_info',
            id: id
        });
    }
};

/**
* @constructor Creates an instance of the DropzoneManager
* @param {HTMLElement} dz - Dropzone element 
*/
const DropzoneManager = function (dz) {
    this.dz = dz;

    this.OpenFileSelect = function (ev) {
        let i = document.createElement('input');
        i.setAttribute('type', 'file');
        i.setAttribute('multiple', '');
        i.addEventListener('change', function (evt) {
            let fl = evt.target.files;
            for (let z = 0; z < fl.length; z++) {
                new FileUpload(fl[z]).ProcessUpload();
            }
        }.bind(this));
        i.click();
    };

    this.dz.addEventListener('click', this.OpenFileSelect.bind(this), false);
};

/**
 * 
 */
const ViewManager = function () {
    this.id = null;
    this.key = null;
    this.iv = null;

    this.ParseUrlHash = function () {
        let hs = window.location.hash.substr(1).split(':');
        this.id = hs[0];
        this.key = hs[1];
        this.iv = hs[2];
    };

    this.LoadView = async function () {
        this.ParseUrlHash();

        let fi = await Api.GetFileInfo(this.id);

        if (fi.ok === true) {
            $('#page-view .file-info-size').textContent = App.Utils.FormatBytes(fi.data.Size);
            $('#page-view .file-info-views').textContent = fi.data.Views.toLocaleString();
            $('#page-view .file-info-last-download').textContent = new Date(fi.data.LastView * 1000).toLocaleString();
            $('#page-view .file-info-uploaded').textContent = new Date(fi.data.Uploaded * 1000).toLocaleString();

            await this.ShowPreview(fi.data);
        }
    };

    this.ShowPreview = async function (fileinfo) {
        let nelm = document.importNode($("template[id='tmpl-view-default']").content, true);
        nelm.querySelector('.view-file-id').textContent = fileinfo.FileId;
        nelm.querySelector('.view-key').textContent = this.key;
        nelm.querySelector('.view-iv').textContent = this.iv;
        nelm.querySelector('.btn-download').addEventListener('click', function () {
            let fd = new FileDownloader(this.fileinfo, this.self.key, this.self.iv);
            fd.onprogress = function(x) {
                this.elm_bar.style.width = `${100 * x}%`;
                this.elm_bar_label.textContent = `${(100 * x).toFixed(0)}%`;
            }.bind({ 
                elm_bar_label: document.querySelector('.view-download-progress div:nth-child(1)'),
                elm_bar: document.querySelector('.view-download-progress div:nth-child(2)')
            });
            fd.DownloadFile().then(function (file){
                var objurl = URL.createObjectURL(file);
                var dl_link = document.createElement('a');
                dl_link.href = objurl;
                dl_link.download = file.name;
                dl_link.click();
            });
        }.bind({
            self: this,
            fileinfo: fileinfo
        }));
        $('#page-view').appendChild(nelm);

    };

    this.LoadView();
};

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

    this.HandleProgress = function(type, progress) {
        switch(type){
            case 'progress-download':{
                if(typeof this.onprogress === 'function'){
                    this.onprogress(progress);
                }
            }
        }
    };

    /**
     * Downloads the file
     * @returns {Promise<File>} The loaded and decripted file
     */
    this.DownloadFile = async function () {
        let link = (this.fileinfo.DownloadHost !== null ? `${window.location.protocol}//${this.fileinfo.DownloadHost}` : '') + `/${this.fileinfo.FileId}`
        Log.I(`Starting download from: ${link}`);
        let fd = await XHR('GET', link, undefined, undefined, undefined, function (ev) {
            this.HandleProgress('progress-download',  ev.loaded / parseFloat(ev.total));
        }.bind(this), function (req) {
            req.responseType = "arraybuffer";
        });

        let blob = fd.response;
        let header = VBF.Parse(blob);
        let hash_text = App.Utils.ArrayToHex(header.hmac);

        Log.I(`${this.fileinfo.FileId} blob header version is ${header.version} and hash is ${hash_text} uploaded on ${header.uploaded}`);

        //attempt decryption
        try {
            let key_raw = App.Utils.HexToArray(this.key);
            let iv_raw = App.Utils.HexToArray(this.iv);
            Log.I(`${this.fileinfo.FileId} decrypting with key ${this.key} and iv ${this.iv}`);

            let key = await crypto.subtle.importKey("raw", key_raw, EncryptionKeyDetails, false, ['decrypt']);
            let keyhmac = await crypto.subtle.importKey("raw", key_raw, HMACKeyDetails, false, ['verify']);

            let decrypted_file = await crypto.subtle.decrypt({ name: EncryptionAlgo, iv: iv_raw }, key, blob.slice(VBF.HeaderSize));

            //read the header 
            let json_header_length = new Uint16Array(decrypted_file)[0];
            let json_header_text = new TextDecoder('utf-8').decode(decrypted_file.slice(2, json_header_length + 2));
            Log.I(`${this.fileinfo.FileId} header is ${json_header_text}`);

            //hash the file to verify
            let file_data = decrypted_file.slice(2 + json_header_length);
            let hmac_verify = await crypto.subtle.verify("HMAC", keyhmac, header.hmac, file_data);
            if (hmac_verify) {
                Log.I(`${this.fileinfo.FileId} HMAC verified!`);

                let header_obj = JSON.parse(json_header_text);
                return new File([file_data], header_obj.name, {
                    type: header_obj.mime
                });
            } else {
                throw "HMAC verify failed";
            }
        } catch (ex) {
            Log.E(`${this.fileinfo.FileId} error decrypting file: ${ex}`);
        }
    };
};

/**
 * File upload handler class
 * @class
 * @param {File} file - The file handle to upload
 */
const FileUpload = function (file) {
    this.hasCrypto = typeof window.crypto.subtle === "object";
    this.file = file;
    this.domNode = null;
    this.key = null;
    this.hmackey = null;
    this.iv = new Uint8Array(16);

    /**
     * Track uplaod stats
     */
    this.uploadStats = {
        lastRate: 0,
        lastLoaded: 0,
        lastProgress: 0
    };

    /**
     * Get the encryption key as hex
     * @returns {Promise<string>} The encryption get in hex
     */
    this.HexKey = async () => {
        return App.Utils.ArrayToHex(await crypto.subtle.exportKey('raw', this.key));
    };

    /**
     * Get the IV as hex
     * @returns {string} The IV for envryption has hex
     */
    this.HexIV = () => {
        return App.Utils.ArrayToHex(this.iv);
    };

    /**
     * Returns the formatted key and iv as hex
     * @returns {Promise<string>} The key:iv as hex
     */
    this.TextKey = async () => {
        return `${await this.HexKey()}:${this.HexIV()}`;
    };

    /**
     * Loads the file and SHA256 hashes it
     * @return {Promise<ArrayBuffer>}
     */
    this.HashFile = async () => {
        return new Promise(function (resolve, reject) {
            var fr = new FileReader();

            fr.onloadstart = function (ev) {
                this.HandleProgress('state-load-start');
            }.bind(this);

            fr.onloadend = function (ev) {
                this.HandleProgress('state-load-end');
            }.bind(this);

            fr.onload = function (ev) {
                this.HandleProgress('state-hash-start');
                crypto.subtle.sign("HMAC", this.hmackey, ev.target.result).then(function (hash) {
                    this.HandleProgress('state-hash-end');
                    resolve({
                        hash: hash,
                        data: ev.target.result
                    });
                }.bind(this));
            }.bind(this);

            fr.onprogress = function (ev) {
                this.HandleProgress('progress', ev.loaded / parseFloat(ev.total));
            }.bind(this);

            fr.onerror = function (ev) {
                this.HandleError({
                    type: 'FileReaderError',
                    error: ev.target.error
                })
            }.bind(this);

            fr.readAsArrayBuffer(this.file);
        }.bind(this));
    };

    /**
     * Sets the width of the progress bar for this upload
     * @param {number} value - The value of the progress
     */
    this.SetProgressBar = function (value) {
        this.domNode.progress.textContent = `${(100 * value).toFixed(1)}%`;
        this.domNode.progressBar.style.width = `${(100 * value)}%`;
    };

    /**
     * Sets the status label for this upload
     * @param {string} value - The status label
     */
    this.SetStatus = function (value) {
        this.domNode.state.textContent = `Status: ${value}`;
    };

    /**
     * Sets the speed value on the UI
     */
    this.SetSpeed = function (value) {
        this.domNode.filespeed.textContent = value;
    };

    /**
     * Handles progress messages from the upload process and updates the UI
     * @param {string} type - The progress event type
     * @param {number} progress - The percentage of this progress type
     */
    this.HandleProgress = function (type, progress) {
        switch (type) {
            case 'state-load-start': {
                this.SetStatus('Loading file..');
                this.SetProgressBar(0);
                break;
            }
            case 'state-load-end': {
                this.SetProgressBar(1);
                break;
            }
            case 'state-hash-start': {
                this.SetStatus('Hashing..');
                this.SetProgressBar(0);
                break;
            }
            case 'state-hash-end': {
                this.SetProgressBar(1);
                break;
            }
            case 'state-pre-check-start': {
                this.SetStatus('Checking file info..');
                this.SetProgressBar(0);
                break;
            }
            case 'state-pre-check-end': {
                this.SetProgressBar(1);
                break;
            }
            case 'state-encrypt-start': {
                this.SetStatus('Encrypting..');
                this.SetProgressBar(0);
                break;
            }
            case 'state-encrypt-end': {
                this.SetProgressBar(1);
                break;
            }
            case 'state-upload-start': {
                this.SetStatus('Uploading..');
                this.SetProgressBar(0);
                break;
            }
            case 'state-upload-end': {
                this.SetProgressBar(1);
                this.SetSpeed("Done");
                break;
            }
            case 'progress': {
                this.SetProgressBar(progress < 0.01 ? 0.01 : progress);
                break;
            }
        }
    };

    /**
     * Handles upload errors to display on the UI
     */
    this.HandleError = function (err) {
        Log.E(err.error);
        switch (err.type) {
            case 'FileReaderError': {
                this.SetProgressBar('1px');
                break;
            }
        }
    };

    /**
     * Creates a template for the upload to show progress
     */
    this.CreateNode = function () {
        let nelm = document.importNode(App.Templates.Upload.content, true);

        nelm.filename = nelm.querySelector('.file-info .file-info-name');
        nelm.filesize = nelm.querySelector('.file-info .file-info-size');
        nelm.filespeed = nelm.querySelector('.file-info .file-info-speed');
        nelm.progress = nelm.querySelector('.upload-progress span');
        nelm.progressBar = nelm.querySelector('.upload-progress div');
        nelm.state = nelm.querySelector('.status .status-state');
        nelm.key = nelm.querySelector('.status .status-key');
        nelm.links = nelm.querySelector('.links');
        nelm.errors = nelm.querySelector('.errors');

        nelm.filename.textContent = this.file.name;
        nelm.filesize.textContent = App.Utils.FormatBytes(this.file.size, 2);
        this.domNode = nelm;

        $('#uploads').appendChild(nelm);
    };

    /**
     * Generates a new key to use for encrypting the file
     * @returns {Promise<CryptoKey>} The new key
     */
    this.GenerateKey = async function () {
        this.key = await crypto.subtle.generateKey(EncryptionKeyDetails, true, ['encrypt', 'decrypt']);
        this.hmackey = await crypto.subtle.importKey("raw", await crypto.subtle.exportKey('raw', this.key), HMACKeyDetails, false, ["sign"]);

        crypto.getRandomValues(this.iv);

        this.domNode.key.textContent = `Key: ${await this.TextKey()}`;
        return this.key;
    };

    /**
     * Encrypts the file using the key and iv
     * @param {BufferSource} fileData - The data to encrypt
     * @returns {Promise<ArrayBuffer>} - The Encrypted data
     */
    this.EncryptFile = async function (fileData) {
        this.HandleProgress('state-encrypt-start');
        let encryptedData = await crypto.subtle.encrypt({
            name: EncryptionAlgo,
            iv: this.iv
        }, this.key, fileData);
        this.HandleProgress('state-encrypt-end');
        return encryptedData;
    };

    /**
     * Uploads Blob data to site
     * @param {Blob|BufferSource} fileData - The encrypted file data to upload
     * @returns {Promise<object>} The json result
     */
    this.UploadData = async function (fileData) {
        this.uploadStats.lastProgress = new Date().getTime();
        this.HandleProgress('state-upload-start');
        let uploadResult = await XHR("POST", "/upload", fileData, undefined, function (ev) {
            let now = new Date().getTime();
            let dxLoaded = ev.loaded - this.uploadStats.lastLoaded;
            let dxTime = now - this.uploadStats.lastProgress;

            this.uploadStats.lastLoaded = ev.loaded;
            this.uploadStats.lastProgress = now;

            this.SetSpeed(`${App.Utils.FormatBytes(dxLoaded / (dxTime / 1000.0), 2)}/s`);
            this.HandleProgress('progress', ev.loaded / parseFloat(ev.total));
        }.bind(this));

        this.HandleProgress('state-upload-end');
        return JSON.parse(uploadResult.response);
    };

    /**
     * Creates a header object to be prepended to the file for encrypting
     * @returns {any}
     */
    this.CreateHeader = function () {
        return {
            name: this.file.name,
            mime: this.file.type,
            len: this.file.size
        };
    };

    /**
     * Processes the file upload
     * @return {Promise}
     */
    this.ProcessUpload = async function () {
        Log.I(`Starting upload for ${this.file.name}`);
        this.CreateNode();

        await this.GenerateKey();
        let header = JSON.stringify(this.CreateHeader());
        let hash_data = await this.HashFile();
        let h256 = App.Utils.ArrayToHex(hash_data.hash);
        Log.I(`${this.file.name} hash is: ${h256}`);

        //create blob for encryption
        let header_data = new TextEncoder().encode(header);
        Log.I(`Using header: ${header} (length=${header_data.byteLength})`);

        let encryption_payload = new Uint8Array(2 + header_data.byteLength + hash_data.data.byteLength);
        let header_length_data = new Uint16Array(1);
        header_length_data[0] = header_data.byteLength; //header length
        encryption_payload.set(header_length_data, 0);
        encryption_payload.set(new Uint8Array(header_data), 2); //the file info header
        encryption_payload.set(new Uint8Array(hash_data.data), 2 + header_data.byteLength);

        //encrypt with the key
        Log.I(`Encrypting ${this.file.name} with key ${await this.HexKey()} and IV ${this.HexIV()}`)
        let encryptedData = await this.EncryptFile(encryption_payload);

        Log.I(`Uploading file ${this.file.name}`);
        let upload_payload = VBF.Create(hash_data.hash, encryptedData);
        let uploadResult = await this.UploadData(upload_payload);

        Log.I(`Got response for file ${this.file.name}: ${JSON.stringify(uploadResult)}`);
        this.domNode.state.parentNode.style.display = "none";
        this.domNode.progress.parentNode.style.display = "none";

        if (uploadResult.status === 200) {
            this.domNode.links.style.display = "";

            let nl = document.createElement("a");
            nl.target = "_blank";
            nl.href = `${window.location.protocol}//${window.location.host}/#${uploadResult.id}:${await this.TextKey()}`;
            nl.textContent = this.file.name;
            this.domNode.links.appendChild(nl);
        } else {
            this.domNode.errors.style.display = "";
            this.domNode.errors.textContent = uploadResult.msg;
        }
    };
};

const VBF = {
    Version: 1,
    HeaderSize: 37,

    Create: function(hash, encryptedData) {
        //upload the encrypted file data
        let upload_payload = new Uint8Array(VBF.HeaderSize + encryptedData.byteLength);

        let created = new ArrayBuffer(4);
        new DataView(created).setUint32(0, parseInt(new Date().getTime() / 1000), true);

        upload_payload[0] = VBF.Version; //blob version
        upload_payload.set(new Uint8Array(hash), 1);
        upload_payload.set(new Uint8Array(created), hash.byteLength + 1);
        upload_payload.set(new Uint8Array(encryptedData), VBF.HeaderSize);

        return upload_payload;
    },

    /**
     * Parses the header of the raw file
     * @param {ArrayBuffer} data - Raw data from the server
     * @returns {*} The header 
     */
    Parse: function(data) {
        let version = new Uint8Array(data)[0];
        let hmac = data.slice(1, 33);
        let uploaded = new DataView(data.slice(33, 37)).getUint32(0, true);

        return {
            version,
            hmac,
            uploaded
        };
    }
};

setTimeout(App.Init);
