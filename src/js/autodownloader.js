const VoidFetch = function (event) {
    let re = /\/([a-z0-9]{27})$/i;
    if (re.test(event.request.url)) {
        let id = re.exec(event.request.url)[1];
        Log.I(`AutoDownloader taking request: ${id}`);

        event.respondWith(async function () {
            const client = await clients.get(event.clientId);
            if (!client) return;

            return new Promise(function (resolve, reject) {
                let mc = new MessageChannel();
                mc.port1.onmessage = async function (mc_event) {
                    let fd = new FileDownloader({
                        FileId: id
                    }, mc_event.data.key, mc_event.data.iv);
                    let rsp = await fetch(`https://mnl.test/${id}`, {
                        mode: 'cors',
                        headers: {
                            'X-Void-Embeded': '1'
                        }
                    });
                    let blob = await rsp.arrayBuffer();
                    if (blob.byteLength > 0) {
                        resolve(new Response(await fd.DecryptFile(blob)));
                    } else {
                        reject("Invalid data recieved from server");
                    }
                };

                client.postMessage(id, [mc.port2]);
            });
        }());
    }
}

self.addEventListener('fetch', VoidFetch);