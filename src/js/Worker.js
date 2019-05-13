import { Util, Log, Api } from './modules/Util.js';
import { ViewManager } from './modules/ViewManager.js';
import { FileDownloader } from './modules/FileDownloader.js';

const VoidFetch = function (event) {
    let vm = new ViewManager();

    let hs = vm.ParseFrag(new URL(event.request.url).pathname.substr(1));
    if (hs !== null) {
        Log.I(`Worker taking request: ${hs.id}`);

        event.respondWith(async function () {
            const client = await clients.get(event.clientId);

            let fi = await Api.GetFileInfo(hs.id);
            if (fi.ok) {
                let fd = new FileDownloader(fi.data, hs.key, hs.iv);
                return await fd.StreamResponse();
            } else {
                return Response.error();
            }
        }());
    }
}

self.addEventListener('fetch', VoidFetch);