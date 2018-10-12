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
const XHR = function(method, url, data){
    return new Promise(function (resolve, reject) {
        let x = new XMLHttpRequest();
        x.onreadystatechange = function (ev) {
            if (ev.target.readyState === 4) {
                resolve(ev.target);
            }
        };
        x.onerror = function (ev) {
            reject(ev);
        };
        x.open(method, url, true);
        if (method === "POST" && typeof data === "object" && data !== null) {
            x.setRequestHeader('Content-Type', 'application/json');
            x.send(JSON.stringify(data));
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

    this.OpenFileSelect = function(ev){
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
                    this.HandleProgress('progress-hash-file', 1); //no progress from crypto.subtle.digest so we cant show any progress
                    resolve(hash);
                }.bind(this));
            }.bind(this);

            fr.onprogress = function (ev) {
                this.HandleProgress('progress-load-file', ev.loaded / parseFloat(ev.total));
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
    this.SetStatus = function (value){
        this.domNode.status.textContent = `Status: ${value}`;
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

                break;
            }
            case 'state-hash-start': {
                this.SetStatus('Hashing..');
                this.SetProgressBar(0);
                break;
            }
            case 'state-hash-end': {
                
                break;
            }
            case 'state-upload-start': {
                this.SetStatus('Uploading..');
                this.SetProgressBar(0);
                break;
            }
            case 'state-upload-end': {

                break;
            }
            case 'progress-load-file': {
                this.SetProgressBar(progress < 0.01 ? 0.01 : progress);
                break;
            }
            case 'progress-hash-file': {
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
        nelm.fileInfo = nelm.querySelector('.file-info');
        nelm.progress = nelm.querySelector('.upload-progress span');
        nelm.progressBar = nelm.querySelector('.upload-progress div');
        nelm.status = nelm.querySelector('.status');

        nelm.fileInfo.textContent = this.file.name;
        this.domNode = nelm;
        $('#uploads').appendChild(nelm);
    };

    /**
     * Processes the file upload
     * @return {Promise}
     */
    this.ProcessUpload = async function () {
        Log.I(`Starting upload for ${this.file.name}`);
        this.CreateNode();

        let h256 = App.Utils.ArrayToHex(await this.HashFile());
        let h160 = CryptoJS.RIPEMD160(h256);
        Log.I(`${this.file.name} hash is: ${h256} (${h160})`);
    };
};
App.Init();
