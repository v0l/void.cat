Service Worker
===

Its reccomended to proxy pass the script so any updates are served automatically to your clients like so (Nginx), You can of course host the script yourself aswell.
```
location /void_auto_loader.js {
    proxy_pass https://v3.void.cat/dist/void_auto_loader.js;
}
```

Limitations
====
Currently its not reccomended to embed large resources on any pages as these need to be loaded into ram before decrypting, this service worker is only reccomended for small to medium images that can be downloaded and decrypted quickly.

Keep and eye on this page for an update on this though, in the near future i hope to upgrade to using the new [Streams API](https://developer.mozilla.org/en-US/docs/Web/API/Streams_API) which would allow streaming of content from the server without the need for buffering in ram.