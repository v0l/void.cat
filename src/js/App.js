/**
 * @constant {Object}
 */
const App = {
    get Version() { return AppVersion },

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

setTimeout(App.Init);