@echo off
google-closure-compiler src/js/Const.js src/js/Util.js src/js/FileDownloader.js src/js/VBF.js src/js/autodownloader.js --js_output_file dist/void_auto_loader.js --language_out ECMASCRIPT_NEXT