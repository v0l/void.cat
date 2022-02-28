## Void.cat
Free, simple file hosting

### Features
- Profiles
- File bandwidth statistics
- Administration features
- File download paywall

### Running

Use the docker image to run void.cat:

`docker run --rm -it -p 8080:80 ghcr.io/v0l/void.cat/app:latest`

Then open your browser at http://localhost:8080.

**The first registration will be set as admin, 
so make sure to create your own account**

### Usage

Simply drag and drop your files into the dropzone, 
or paste your screenshots or files into the browser window.

From cli you can upload with `curl`:
```bash
curl -X POST \
  --data-binary @spicy_memes.jpg \
  "https://void.cat/upload?cli=true"
```

This command will return the direct download URL only. 
To get the json output simply remove the `?cli=true` from the url.