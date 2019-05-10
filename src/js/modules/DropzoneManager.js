import { FileUpload } from './FileUpload.js';

/**
* @constructor Creates an instance of the DropzoneManager
* @param {HTMLElement} dz - Dropzone element 
*/
function DropzoneManager(dz) {
    this.dz = dz;

    this.SetUI = function () {
        document.querySelector('#page-upload div:nth-child(1)').removeAttribute("style");
        document.querySelector('#uploads').removeAttribute("style");
    };

    this.OpenFileSelect = function (ev) {
        let i = document.createElement('input');
        i.setAttribute('type', 'file');
        i.setAttribute('multiple', '');
        i.addEventListener('change', function (evt) {
            this.SetUI();
            let fl = evt.target.files;
            let host = window.site_info.ok ? window.site_info.data.upload_host : window.location.host;

            for (let z = 0; z < fl.length; z++) {
                new FileUpload(fl[z], host).ProcessUpload();
            }
        }.bind(this));
        i.click();
    };

    this.dz.addEventListener('click', this.OpenFileSelect.bind(this), false);
};

export { DropzoneManager };