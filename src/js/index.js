import * as App from './modules/App.js';
import '../scss/style.scss';

if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/sw.js').then(function (registration) {
        console.log(`ServiceWorker registration successful with scope: ${registration.scope}`);
    }, function (err) {
        console.error(`ServiceWorker registration failed: ${err}`);
    });
}

App.Init();