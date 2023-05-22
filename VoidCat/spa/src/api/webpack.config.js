// Generated using webpack-cli https://github.com/webpack/webpack-cli

const path = require('path');
const isProduction = process.env.NODE_ENV == 'production';
const config = {
    entry: './src/index.ts',
    devtool: isProduction ? "source-map" : "eval",
    output: {
        path: path.resolve(__dirname, 'dist'),
        clean: true,
        library: {
            name: "@void-cat/api",
            type: "umd"
        }
    },
    plugins: [],
    module: {
        rules: [
            {
                test: /\.(ts|tsx)$/i,
                loader: 'ts-loader',
                exclude: ['/node_modules/'],
            }
        ],
    },
    resolve: {
        preferRelative: true,
        roots: [path.resolve(__dirname, 'src'), "..."],
        extensions: ['.tsx', '.ts', '.jsx', '.js', '...'],
        fallback: {
            crypto: false
        }
    },
};

module.exports = () => {
    if (isProduction) {
        config.mode = 'production';
    } else {
        config.mode = 'development';
    }
    return config;
};
