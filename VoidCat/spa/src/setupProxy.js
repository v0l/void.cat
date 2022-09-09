const {createProxyMiddleware} = require('http-proxy-middleware');
const settings = require("../package.json");

module.exports = function (app) {
    const proxy = createProxyMiddleware({
        target: settings.proxy,
        changeOrigin: true,
        secure: false
    });

    app.use('/admin', proxy);
    app.use('/d', proxy);
    app.use('/info', proxy);
    app.use('/upload', proxy);
    app.use('/auth', proxy);
    app.use('/swagger', proxy);
    app.use('/user', proxy);
};