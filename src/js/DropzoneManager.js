/**
* @constructor Creates an instance of the DropzoneManager
* @param {HTMLElement} dz - Dropzone element 
*/
const DropzoneManager = function (dz) {
    this.dz = dz;

    this.SetUI = function() {
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
            for (let z = 0; z < fl.length; z++) {
                new FileUpload(fl[z]).ProcessUpload();
            }
        }.bind(this));
        i.click();
    };

    this.dz.addEventListener('click', this.OpenFileSelect.bind(this), false);
};