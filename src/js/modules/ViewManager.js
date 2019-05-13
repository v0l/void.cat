import { Api, Utils, Log, $ } from './Util.js';
import { FileDownloader } from './FileDownloader.js';
import { base64_to_bytes } from 'asmcrypto.js';

/**
* @constructor Creates an instance of the ViewManager
*/
function ViewManager() {
    this.id = null;
    this.key = null;
    this.iv = null;

    /**
     * Parse URL hash fragment and return its components
     * @param {string} frag - The Url hash fragment excluding the #
     * @returns {*} - The details decoded from the hash
     */
    this.ParseFrag = function (frag) {
        if (frag.indexOf(':') !== -1) {
            let hs = frag.split(':');
            return {
                id: hs[0],
                key: hs[1],
                iv: hs[2]
            }
        } else if (frag.length === Utils.Base64Len(52)) { //base64 encoded id:key:iv
            let hs = base64_to_bytes(frag);
            return {
                id: Utils.ArrayToHex(hs.slice(0, 20)),
                key: Utils.ArrayToHex(hs.slice(20, 36)),
                iv: Utils.ArrayToHex(hs.slice(36))
            };
        }
        return null;
    };

    /**
     * Loads the view for downloading files
     */
    this.LoadView = async function () {
        let uh = this.ParseFrag(window.location.hash.substr(1));
        if (uh !== null) {
            this.id = uh.id;
            this.key = uh.key;
            this.iv = uh.iv;
        }

        let fi = await Api.GetFileInfo(this.id);

        if (fi.ok === true) {
            $('#page-view .file-info-size').textContent = Utils.FormatBytes(fi.data.Size);
            $('#page-view .file-info-views').textContent = fi.data.Views.toLocaleString();
            $('#page-view .file-info-last-download').textContent = new Date(fi.data.LastView * 1000).toLocaleString();
            $('#page-view .file-info-uploaded').textContent = new Date(fi.data.Uploaded * 1000).toLocaleString();

            await this.ShowPreview(fi.data);
        }
    };

    /**
     * Starts the browser download action
     * @param {string} url - The url to download
     * @param {string?} name - The filename to donwload
     */
    this.DownloadLink = function(url, name) {
        var dl_link = document.createElement('a');
        dl_link.href = url;
        if (name !== undefined) {
            dl_link.download = name;
        }
        dl_link.style.display = "none";
        let lnk = document.body.appendChild(dl_link);
        lnk.click();
        document.body.removeChild(lnk);
    };

    this.ShowPreview = async function (fileinfo) {
        let cap_info = await Api.CaptchaInfo();

        let nelm = document.importNode($("template[id='tmpl-view-default']").content, true);
        nelm.querySelector('.view-file-id').textContent = fileinfo.FileId;
        if (fileinfo.IsLegacyUpload) {
            let keyrow = nelm.querySelector('.view-key');
            keyrow.textContent = fileinfo.LegacyFilename;
            keyrow.previousElementSibling.textContent = "Filename:";
            nelm.querySelector('.view-iv').parentNode.style.display = "none";
            nelm.querySelector('.view-transfer-stats').style.display = "none";
        } else {
            nelm.querySelector('.view-key').textContent = this.key;
            nelm.querySelector('.view-iv').textContent = this.iv;
        }
        nelm.querySelector('.btn-download').addEventListener('click', async function () {
            //detect if the service worker is installed
            if ('serviceWorker' in navigator) {
                let swreg = await navigator.serviceWorker.getRegistration();
                if (swreg !== null) {
                    Log.I(`Service worker detected, using ${swreg.scope} for download..`);
                    let elm_bar_label = document.querySelector('.view-download-progress div:nth-child(1)');
                    let elm_bar = document.querySelector('.view-download-progress div:nth-child(2)');
                    navigator.serviceWorker.addEventListener('message', event => {
                        let msg = event.data;
                        switch(msg.type) {
                            case "progress": {
                                elm_bar.style.width = `${100 * msg.value}%`;
                                elm_bar_label.textContent = `${(100 * msg.value).toFixed(0)}%`;
                                break;
                            }
                            case "info": {
                                document.querySelector('.view-download-label-speed').textContent = msg.value;
                                break;
                            }
                        }
                    });

                    let link = (this.fileinfo.DownloadHost !== null ? `${window.location.protocol}//${this.fileinfo.DownloadHost}` : '') + `/${window.location.hash.substr(1)}`;
                    this.self.DownloadLink(link);
                    return;
                }
            }

            let fd = new FileDownloader(this.fileinfo, this.self.key, this.self.iv);
            fd.onprogress = function (x) {
                this.elm_bar.style.width = `${100 * x}%`;
                this.elm_bar_label.textContent = `${(100 * x).toFixed(0)}%`;
            }.bind({
                elm_bar_label: document.querySelector('.view-download-progress div:nth-child(1)'),
                elm_bar: document.querySelector('.view-download-progress div:nth-child(2)')
            });
            fd.oninfo = function (v) {
                this.elm.textContent = v;
            }.bind({
                elm: document.querySelector('.view-download-label-speed')
            });
            fd.onratelimit = function () {
                if (this.cap_info.ok) {
                    window.grecaptcha.execute(this.cap_info.data.site_key, { action: 'download_rate_limit' }).then(async function (token) {
                        let api_rsp = await Api.VerifyCaptchaRateLimit(this.id, token);
                        if (api_rsp.ok) {
                            document.querySelector('.btn-download').click(); //simulate button press to start download again
                        } else {
                            alert('Captcha check failed, are you a robot?');
                        }
                    }.bind({ id: this.id }));
                } else {
                    Log.E('No recaptcha config set');
                }
            }.bind({
                id: this.fileinfo.FileId,
                cap_info: this.cap_info
            });
            fd.DownloadFile().then(function (file) {
                if (file !== null) {
                    var objurl = file.isLegacy !== undefined ? file.url : URL.createObjectURL(file.blob);
                    DownloadLink(objurl, file.isLegacy === undefined ? file.name : undefined);
                }
            }).catch(function (err) {
                alert(err);
            });
        }.bind({
            self: this,
            fileinfo,
            cap_info
        }));
        $('#page-view').appendChild(nelm);

        if (cap_info.ok) {
            let st = document.createElement('script');
            st.src = "https://www.google.com/recaptcha/api.js?render=" + cap_info.data.site_key;
            st.async = true;
            document.body.appendChild(st);
        }
    };
};

export { ViewManager };