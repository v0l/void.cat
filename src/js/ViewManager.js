/**
* @constructor Creates an instance of the ViewManager
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
            $('#page-view .file-info-size').textContent = Utils.FormatBytes(fi.data.Size);
            $('#page-view .file-info-views').textContent = fi.data.Views.toLocaleString();
            $('#page-view .file-info-last-download').textContent = new Date(fi.data.LastView * 1000).toLocaleString();
            $('#page-view .file-info-uploaded').textContent = new Date(fi.data.Uploaded * 1000).toLocaleString();

            await this.ShowPreview(fi.data);
        }
    };

    this.ShowPreview = async function (fileinfo) {
        let cap_info = await Api.CaptchaInfo();

        let nelm = document.importNode($("template[id='tmpl-view-default']").content, true);
        nelm.querySelector('.view-file-id').textContent = fileinfo.FileId;
        nelm.querySelector('.view-key').textContent = this.key;
        nelm.querySelector('.view-iv').textContent = this.iv;
        nelm.querySelector('.btn-download').addEventListener('click', function () {
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
                }else {
                    Log.E('No recaptcha config set');
                }
            }.bind({
                id: this.fileinfo.FileId,
                cap_info: this.cap_info
            });
            fd.DownloadFile().then(function (file) {
                if (file !== null) {
                    var objurl = URL.createObjectURL(file);
                    var dl_link = document.createElement('a');
                    dl_link.href = objurl;
                    dl_link.download = file.name;
                    dl_link.click();
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

    this.LoadView();
};