/**
 * @constant {Object}
 */
const App = {
    Loaded: false,
    CharJsLoaded: false,

    Elements: {
        get Dropzone() { return $('#dropzone') },
        get Uploads() { return $('#uploads') },
        get PageView() { return $('#page-view') },
        get PageUpload() { return $('#page-upload') },
        get PageFaq() { return $('#page-faq') },
        get PageStats() { return $('#page-stats') },
        get PageDonate() { return $('#page-donate') }
    },

    Templates: {
        get Upload() { return $("template[id='tmpl-upload']") }
    },

    get IsEdge() {
        return /Edge/.test(navigator.userAgent);
    },

    get IsChrome() {
        return !App.IsEdge && /^Mozilla.*Chrome/.test(navigator.userAgent);
    },

    get IsFirefox() {
        return !App.IsEdge && /^Mozilla.*Firefox/.test(navigator.userAgent);
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

    ResetView: function () {
        App.Elements.PageView.style.display = "none";
        App.Elements.PageUpload.style.display = "none";
        App.Elements.PageFaq.style.display = "none";
        App.Elements.PageStats.style.display = "none";
        App.Elements.PageDonate.style.display = "none";
    },

    ShowStats: async function () {
        location.hash = "#stats";
        App.ResetView();

        if (!App.CharJsLoaded) {
            await App.InsertScript("//cdnjs.cloudflare.com/ajax/libs/moment.js/2.22.2/moment.min.js", () => {
                return typeof moment !== "undefined";
            });
            await App.InsertScript("//cdnjs.cloudflare.com/ajax/libs/Chart.js/2.7.3/Chart.min.js", () => {
                return typeof Chart !== "undefined";
            });
        }

        let api_rsp = await Api.GetTxChart();
        if (api_rsp.ok) {
            let ctx = $('#weektxgraph').getContext('2d');
            new Chart(ctx, {
                type: 'line',
                data: api_rsp.data,
                options: {
                    scales: {
                        xAxes: [{
                            type: 'time',
                            time: {
                                source: 'data'
                            }
                        }],
                        yAxes: [{
                            ticks: {
                                beginAtZero: true,
                                callback: function(label, index, labels) {
                                    return Utils.FormatBytes(label);
                                }
                            }
                        }]
                    }
                }
            });
        }
        App.Elements.PageStats.style.display = "block";
    },

    ShowFAQ: function () {
        location.hash = "#faq";
        App.ResetView();
        App.Elements.PageFaq.style.display = "block";
    },

    ShowDonate: function () {
        location.hash = "#donate";
        App.ResetView();
        App.Elements.PageDonate.style.display = "block";
    },

    /**
     * Sets up the page
     */
    Init: async function () {
        App.CheckBrowserSupport();
        App.MakePolyfills();

        App.ResetView();

        if (location.hash !== "") {
            if (location.hash == "#faq") {
                App.ShowFAQ();
            } else if (location.hash == "#stats") {
                App.ShowStats();
            } else if (location.hash == "#donate") {
                App.ShowDonate();
            } else {
                App.Elements.PageView.style.display = "block";
                new ViewManager();
            }
            window.site_info = await Api.GetSiteInfo();
        } else {
            window.site_info = await Api.GetSiteInfo();
            App.Elements.PageUpload.style.display = "block";
            $('#dropzone').innerHTML = `Click me!<br><small>(${Utils.FormatBytes(window.site_info.data.max_upload_size)} max)</small>`;
            new DropzoneManager(App.Elements.Dropzone);
        }

        if (window.site_info.ok) {
            let elms = document.querySelectorAll("#footer-stats div span");
            elms[0].textContent = window.site_info.data.basic_stats.Files;
            elms[1].textContent = Utils.FormatBytes(window.site_info.data.basic_stats.Size, 2);
            elms[2].textContent = Utils.FormatBytes(window.site_info.data.basic_stats.Transfer_24h, 2);
        }

        let faq_headers = document.querySelectorAll('#page-faq .faq-header');
        for (let x = 0; x < faq_headers.length; x++) {
            faq_headers[x].addEventListener('click', function () {
                this.nextElementSibling.classList.toggle("show");
            }.bind(faq_headers[x]));
        }

        App.Loaded = true;
    },

    /**
     * Adds in polyfills for this browser
     */
    MakePolyfills: function () {
        if (typeof TextEncoder === "undefined" || typeof TextDecoder === "undefined") {
            App.InsertScript("//unpkg.com/text-encoding@0.6.4/lib/encoding-indexes.js");
            App.InsertScript("//unpkg.com/text-encoding@0.6.4/lib/encoding.js");
        }
    },

    /**
     * Adds a script tag at the top of the header
     * @param {string} src - The script src url
     * @param {function} fnWait - Function to use in promise to test if the script is loaded
     */
    InsertScript: async function (src, fnWait) {
        var before = document.head.getElementsByTagName('script')[0];
        var newlink = document.createElement('script');
        newlink.src = src;
        document.head.insertBefore(newlink, before);

        if (typeof fnWait === "function") {
            await new Promise((resolve, reject) => {
                let timer = setInterval(() => {
                    if (fnWait()) {
                        clearInterval(timer);
                        resolve();
                    }
                }, 100);
            });
        }
    },

    /**
     * Checks browser version
     */
    CheckBrowserSupport: function () {
        if (!App.IsFirefox) {
            if (App.IsChrome) {
                App.AddNoticeItem("Uploads bigger then 100MiB usually crash Chrome when uploading. Please upload with Firefox. Or check <a target=\"_blank\" href=\"https://github.com/v0l/void.cat/releases\">GitHub</a> for tools.");
            }
            if (App.IsEdge) {
                let edge_version = /Edge\/([0-9]{1,3}\.[0-9]{1,5})/.exec(navigator.userAgent)[1];
                Log.I(`Edge version is: ${edge_version}`);
                if (parseFloat(edge_version) < 18.18218) {
                    App.AddNoticeItem("Upload progress isn't reported in the version of Edge you are using, see <a target=\"_blank\" href=\"https://developer.microsoft.com/en-us/microsoft-edge/platform/issues/12224510/\">here for more info</a>.");
                }
            }

            document.querySelector('#page-notice').style.display = "block";
        }
    },

    /**
     * Adds a notice to the UI notice box
     * @param {string} txt - Message to add to notice list
     */
    AddNoticeItem: function (txt) {
        let ne = document.createElement('li');
        ne.innerHTML = txt;
        document.querySelector('#page-notice ul').appendChild(ne);
    }
};

setTimeout(App.Init);