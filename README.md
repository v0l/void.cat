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
export FILE=memes.jpg
curl -X POST \
  -H "V-Content-Type: $(file --mime-type -b $FILE)" \
  -H "V-Full-Digest: $(sha256sum -bz $FILE | cut -d' ' -f1)" \
  -H "V-Filename: $FILE" \
  --data-binary @$FILE \
  "https://void.cat/upload?cli=true"
```

This command will return the direct download URL only. 
To get the json output simply remove the `?cli=true` from the url.

### Development
To run postgres in local use:
```
docker run --rm -it -p 5432:5432 -e POSTGRES_DB=void -e POSTGRES_PASSWORD=postgres postgres -d postgres
```

To run MinIO in local use:
```
docker run --rm -it -p 9000:9000 -p 9001:9001 minio/minio -- server /data --console-address ":9001"
```
