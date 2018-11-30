const VoidFetch = function (event) {
    let re = /\/([a-z0-9]{26,27}):([a-z0-9]{32}):([a-z0-9]{32})$/i;
    if (re.test(event.request.url)) {
        let rx = re.exec(event.request.url);
        let id = rx[1];
        let key = rx[2];
        let iv = rx[3];
        Log.I(`AutoDownloader taking request: ${id}`);

        event.respondWith(async function () {
            const client = await clients.get(event.clientId);
            if (!client) return;

            let fd = new FileDownloader({
                FileId: id
            }, key, iv);
            let rsp = await fetch(`https://v3.void.cat/${id}`, {
                mode: 'cors',
                headers: {
                    'X-Void-Embeded': '1' //this is needed to detect allow cross origin requests
                }
            });
            let blob = await rsp.arrayBuffer();
            if (blob.byteLength > 0) {
                let dec_file = await fd.DecryptFile(blob);
                return new Response(dec_file.blob);
            } else {
                throw "Invalid data recieved from server";
            }
        }());
    }
}

self.addEventListener('fetch', VoidFetch);