import react from "@vitejs/plugin-react";
import {visualizer} from "rollup-plugin-visualizer";
import {defineConfig} from "vite";
import {vitePluginVersionMark} from "vite-plugin-version-mark";

export default defineConfig({
    plugins: [
        react(),
        visualizer({
            open: true,
            gzipSize: true,
            filename: "build/stats.html",
        }),
        vitePluginVersionMark({
            name: "void_cat",
            ifGitSHA: true,
            command: "git describe --always --tags",
            ifMeta: false,
        }),
    ],
    assetsInclude: [],
    build: {
        outDir: "build"
    },
    base: "/",
    clearScreen: false,
    resolve: {
        alias: {
            "@": "/src",
        },
    },
    define: {}
});

