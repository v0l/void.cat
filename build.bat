@echo off
call sass src/css/style.scss dist/style.css
call google-closure-compiler ^
	src/js/Const.js ^
	src/js/Util.js ^
	src/js/App.js ^
	src/js/DropzoneManager.js ^
	src/js/FileDownloader.js ^
	src/js/FileUpload.js ^
	src/js/VBF.js ^
	src/js/ViewManager.js ^
--js_output_file dist/script.min.js --language_out ECMASCRIPT_NEXT