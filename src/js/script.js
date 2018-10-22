
/**
 * @constant {string} - The encryption algoritm to use for file uploads
 */
const EncryptionAlgo = 'AES-GCM';
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
        get Uploads() { return $('#uploads') }
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
        new DropzoneManager(App.Elements.Dropzone)
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
 * @returns {Promise<XMLHttpRequest>} The completed request
 */
const XHR = function (method, url, data, headers, progress) {
    return new Promise(function (resolve, reject) {
        let x = new XMLHttpRequest();
        x.onreadystatechange = function (ev) {
            if (ev.target.readyState === 4) {
                resolve(ev.target);
            }
        };
        x.upload.onprogress = function (ev) {
            if (typeof progress === "function") {
                progress(ev);
            }
        };
        x.onerror = function (ev) {
            reject(ev);
        };
        x.open(method, url, true);

        //set headers if they are passed
        if (typeof headers === "object") {
            for (let x in headers) {
                x.setRequestHeader(x, headers[x]);
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
 * File upload handler class
 * @class
 * @param {File} file - The file handle to upload
 */
const FileUpload = function (file) {
    this.hasCrypto = typeof window.crypto.subtle === "object";
    this.file = file;
    this.domNode = null;
    this.key = null;
    this.iv = new Uint32Array(16);

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
                crypto.subtle.digest("SHA-256", ev.target.result).then(function (hash) {
                    this.HandleProgress('state-hash-end');
                    resolve({
                        h256: hash,
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
        this.key = await crypto.subtle.generateKey({ name: EncryptionAlgo, length: 128 }, true, ['encrypt', 'decrypt']);
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
    this.CreateHeader = function() {
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
        let h256 = App.Utils.ArrayToHex(hash_data.h256);
        Log.I(`${this.file.name} hash is: ${h256}`);

        //check file params are ok
        //TODO: call to api to check file info

        //create blob for encryption
        Log.I(`Using header: ${header}`);
        let header_data = new TextEncoder().encode(header);
        
        let encryption_payload = new Uint8Array(2 + header_data.byteLength + hash_data.data.byteLength);
        let header_length_data = new Uint16Array(1);
        header_length_data[0] = header_data.byteLength; //header length
        encryption_payload.set(header_length_data, 0);
        encryption_payload.set(new Uint8Array(header_data), 2); //the file info header
        encryption_payload.set(new Uint8Array(hash_data.data), 2 + header_data.byteLength); 
        
        //encrypt with the key
        Log.I(`Encrypting ${this.file.name} with key ${await this.HexKey()} and IV ${this.HexIV()}`)
        let encryptedData = await this.EncryptFile(encryption_payload);

        //upload the encrypted file data
        Log.I(`Uploading file ${this.file.name}`);
        let upload_payload = new Uint8Array(1 + hash_data.h256.byteLength + encryptedData.byteLength);

        upload_payload[0] = 1; //blob version
        upload_payload.set(new Uint8Array(hash_data.h256), 1);
        upload_payload.set(new Uint8Array(encryptedData), 1 + hash_data.h256.byteLength);

        let uploadResult = await this.UploadData(upload_payload);

        Log.I(`Got response for file ${this.file.name}: ${JSON.stringify(uploadResult)}`);
        if (uploadResult.status === 200) {
            this.domNode.state.parentNode.style.display = "none";
            this.domNode.progress.parentNode.style.display = "none";
            this.domNode.links.style.display = "";

            let nl = document.createElement("a");
            nl.target = "_blank";
            nl.href = `${window.location.protocol}//${window.location.host}/#${uploadResult.pub_hash}:${await this.TextKey()}`;
            nl.textContent = this.file.name;
            this.domNode.links.appendChild(nl);
        }
    };
};
App.Init();
