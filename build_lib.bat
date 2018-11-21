@echo off
google-closure-compiler src/js/Const.js src/js/Util.js src/js/FileDownloader.js src/js/VBF.js --js_output_file dist/void_lib.js --language_out ECMASCRIPT_NEXT