/**
 * @constant {Object}
 */
const App = {
    Elements: {
        get Dropzone() { return $('#dropzone') },
        get Uploads() { return $('#uploads') },
        get PageView() { return $('#page-view') },
        get PageUpload() { return $('#page-upload') },
        get PageFaq() { return $('#page-faq') }
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

    /**
     * Sets up the page
     */
    Init: async function () {
        if (location.hash !== "") {
            if (location.hash == "#faq") {
                let faq_headers = document.querySelectorAll('#page-faq .faq-header');
                for (let x = 0; x < faq_headers.length; x++) {
                    faq_headers[x].addEventListener('click', function() {
                        this.nextElementSibling.classList.toggle("show");
                    }.bind(faq_headers[x]));
                }
                App.Elements.PageUpload.style.display = "none";
                App.Elements.PageFaq.style.display = "block";
            } else {
                App.Elements.PageUpload.style.display = "none";
                App.Elements.PageView.style.display = "block";
                new ViewManager();
            }
        } else {
            App.Elements.PageUpload.style.display = "block";
            App.Elements.PageView.style.display = "none";
            new DropzoneManager(App.Elements.Dropzone);

        }
        
        let stats = await Api.GetSiteInfo();
        if(stats.ok){
            let elms = document.querySelectorAll("#footer-stats div span");
            elms[0].textContent = stats.data.basic_stats.Files;
            elms[1].textContent = Utils.FormatBytes(stats.data.basic_stats.Size, 2);
            elms[2].textContent = Utils.FormatBytes(stats.data.basic_stats.Transfer_24h, 2);
        }
    }
};

setTimeout(App.Init);