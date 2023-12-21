## Void.cat
Free, simple file hosting

### Features
- Profiles
- File bandwidth statistics
- Administration features
- File download paywall

### Running

Use the docker image to run void.cat:

`docker run --rm -it -p 8080:80 git.v0l.io/kieran/void-cat:latest`

Then open your browser at http://localhost:8080.

**The first registration will be set as admin, 
so make sure to create your own account**

### Deploying
Docker compose is the best option for most as this sets up postgres / redis / clamav.

Run the following commands to get going:
```bash
git clone https://git.v0l.io/Kieran/void.cat
cd void.cat/
docker compose up -d
```

You should now be able to access void.cat on `http://localhost`.

If you already have something running on port `80` you may have problems, you can modify the `docker-compose.yml` 
file to change the port.

You can modify the site config in `./VoidCat/appsettings.compose.json`, this is recommended for anything other 
than a simple test

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

Or you can create an alias function in `~/bash_aliases` like so: 
```bash
vcu() {
  echo "Uploading $1"
  curl -X POST \
    -H "V-Content-Type: $(file --mime-type -b $1)" \
    -H "V-Full-Digest: $(sha256sum -bz $1 | cut -d' ' -f1)" \
    -H "V-Filename: $1" \
    --data-binary @$1 \
    "https://void.cat/upload?cli=true"
  echo -e ""
}
```

Uploading from cli will simply become `vcu memes.jpg`

You can also upload files to your user account by specifying an API key in the curl command:
```bash
  -H "Authorization: Bearer MY_API_KEY"
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
