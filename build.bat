@echo off
sass src/style.scss dist/style.css && google-closure-compiler src/script.js src/ripemd160.js --js_output_file dist/script.min.js --language_out ECMASCRIPT_NEXT