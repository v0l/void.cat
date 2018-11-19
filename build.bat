@echo off
sass src/css/style.scss dist/style.css && google-closure-compiler src/js/script.js --js_output_file dist/script.min.js --language_out ECMASCRIPT_NEXT