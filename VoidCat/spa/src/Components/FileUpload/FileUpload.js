import "./FileUpload.css";
import {useEffect, useState} from "react";
import {useSelector} from "react-redux";

import {buf2hex, ConstName, FormatBytes} from "../Shared/Util";
import {ApiHost} from "../Shared/Const";
import {StreamEncryption} from "../Shared/StreamEncryption";
import {VoidButton} from "../Shared/VoidButton";
import {useFileTransfer} from "../Shared/FileTransferHook";

const UploadState = {
    NotStarted: 0,
    Starting: 1,
    Hashing: 2,
    Uploading: 3,
    Done: 4,
    Failed: 5,
    Challenge: 6
};

export const DigestAlgo = "SHA-256";

export function FileUpload(props) {
    const auth = useSelector(state => state.login.jwt);
    const info = useSelector(state => state.info.info);
    const {speed, progress, loaded, setFileSize, reset, update} = useFileTransfer();
    const [result, setResult] = useState();
    const [uState, setUState] = useState(UploadState.NotStarted);
    const [challenge, setChallenge] = useState();
    const [encryptionKey, setEncryptionKey] = useState();
    const [encrypt, setEncrypt] = useState(true);

    function handleProgress(e) {
        if (e instanceof ProgressEvent) {
            loaded(e.loaded);
        }
    }

    async function doStreamUpload() {
        setFileSize(props.file.size);
        setUState(UploadState.Hashing);
        let hash = await digest(props.file);
        reset();
        let offset = 0;

        async function readChunk(size) {
            if (offset > props.file.size) {
                return new Uint8Array(0);
            }
            let end = Math.min(offset + size, props.file.size);
            let blob = props.file.slice(offset, end, props.file.type);
            let data = await blob.arrayBuffer();
            offset += data.byteLength;
            return new Uint8Array(data);
        }

        let rs = new ReadableStream({
            start: async () => {
                setUState(UploadState.Uploading);
            },
            pull: async (controller) => {
                try {
                    let chunk = await readChunk(controller.desiredSize);
                    if (chunk.byteLength === 0) {
                        controller.close();
                        return;
                    }
                    update(chunk.length);
                    controller.enqueue(chunk);
                } catch (e) {
                    console.error(e);
                    throw e;
                }
            },
            cancel: (reason) => {
                console.log(reason);
            },
            type: "bytes"
        }, {
            highWaterMark: 1024 * 1024
        });

        let enc = encrypt ? (() => {
            let ret = new StreamEncryption();
            setEncryptionKey(ret.getKey());
            return ret;
        })() : null;
        rs = encrypt ? rs.pipeThrough(enc.getEncryptionTransform()) : rs;

        let headers = {
            "Content-Type": "application/octet-stream",
            "V-Content-Type": props.file.type,
            "V-Filename": props.file.name,
            "V-Full-Digest": hash
        };
        if (encrypt) {
            headers["V-EncryptionParams"] = JSON.stringify(enc.getParams());
        }
        if (auth) {
            headers["Authorization"] = `Bearer ${auth}`;
        }
        let req = await fetch("/upload", {
            method: "POST",
            mode: "cors",
            body: rs,
            headers,
            duplex: 'half'
        });

        if (req.ok) {
            let rsp = await req.json();
            console.log(rsp);
            handleResult(rsp);
        }
    }

    /**
     * Upload a segment of the file
     * @param segment {ArrayBuffer}
     * @param fullDigest {string} Full file hash
     * @param id {string?}
     * @param editSecret {string?}
     * @param part {int?} Segment number
     * @param partOf {int?} Total number of segments
     * @returns {Promise<any>}
     */
    async function xhrSegment(segment, fullDigest, id, editSecret, part, partOf) {
        setUState(UploadState.Uploading);

        return await new Promise((resolve, reject) => {
            try {
                let req = new XMLHttpRequest();
                req.onreadystatechange = (ev) => {
                    if (req.readyState === XMLHttpRequest.DONE && req.status === 200) {
                        let rsp = JSON.parse(req.responseText);
                        console.log(rsp);
                        resolve(rsp);
                    } else if (req.readyState === XMLHttpRequest.DONE && req.status === 403) {
                        let contentType = req.getResponseHeader("content-type");
                        if (contentType.toLowerCase().trim().indexOf("text/html") === 0) {
                            setChallenge(req.response);
                            setUState(UploadState.Challenge);
                            reject();
                        }
                    }
                };
                req.upload.onprogress = handleProgress;
                req.open("POST", typeof (id) === "string" ? `${ApiHost}/upload/${id}` : `${ApiHost}/upload`);
                req.setRequestHeader("Content-Type", "application/octet-stream");
                req.setRequestHeader("V-Content-Type", props.file.type.length === 0 ? "application/octet-stream" : props.file.type);
                req.setRequestHeader("V-Filename", props.file.name);
                req.setRequestHeader("V-Full-Digest", fullDigest);
                req.setRequestHeader("V-Segment", `${part}/${partOf}`)
                if (auth) {
                    req.setRequestHeader("Authorization", `Bearer ${auth}`);
                }
                if (typeof (editSecret) === "string") {
                    req.setRequestHeader("V-EditSecret", editSecret);
                }
                req.withCredentials = true;
                req.send(segment);
            } catch (e) {
                reject(e);
            }
        });
    }

    async function doXHRUpload() {
        setFileSize(props.file.size);
        let uploadSize = info.uploadSegmentSize ?? Number.MAX_VALUE;

        setUState(UploadState.Hashing);
        let hash = await digest(props.file);
        reset();
        if (props.file.size >= uploadSize) {
            await doSplitXHRUpload(hash, uploadSize);
        } else {
            let xhr = await xhrSegment(props.file, hash);
            handleResult(xhr);
        }
    }

    async function doSplitXHRUpload(hash, splitSize) {
        let xhr = null;
        const segments = Math.ceil(props.file.size / splitSize);
        for (let s = 0; s < segments; s++) {
            reset();
            let offset = s * splitSize;
            let slice = props.file.slice(offset, offset + splitSize, props.file.type);
            xhr = await xhrSegment(slice, hash, xhr?.file?.id, xhr?.file?.metadata?.editSecret, s + 1, segments);
            if (!xhr.ok) {
                break;
            }
        }
        handleResult(xhr);
    }

    function handleResult(result) {
        if (result.ok) {
            setUState(UploadState.Done);
            setResult(result.file);
            window.localStorage.setItem(result.file.id, JSON.stringify(result.file));
        } else {
            setUState(UploadState.Failed);
            setResult(result.errorMessage);
        }
    }

    function getChromeVersion() {
        let raw = navigator.userAgent.match(/Chrom(e|ium)\/([0-9]+)\./);
        return raw ? parseInt(raw[2], 10) : false;
    }

    async function digest(file) {
        let h = await window.crypto.subtle.digest(DigestAlgo, await file.arrayBuffer());
        return buf2hex(new Uint8Array(h));
    }

    function renderStatus() {
        if (result) {
            let link = `/${result.id}`;
            return uState === UploadState.Done ?
                <dl>
                    <dt>Link:</dt>
                    <dd><a target="_blank" href={link}>{result.id}</a></dd>
                    {encryptionKey ? <>
                        <dt>Encryption Key:</dt>
                        <dd>
                            <VoidButton onClick={() => navigator.clipboard.writeText(encryptionKey)}>Copy</VoidButton>
                        </dd>
                    </> : null}
                </dl>
                : <b>{result}</b>;
        } else if (uState === UploadState.NotStarted) {
            return (
                <>
                    <dl>
                        <dt>Encrypt file:</dt>
                        <dd><input type="checkbox" checked={encrypt} onChange={(e) => setEncrypt(e.target.checked)}/>
                        </dd>
                    </dl>
                    <VoidButton onClick={() => doStreamUpload()}>Upload</VoidButton>
                </>
            )
        } else {
            return (
                <dl>
                    <dt>Speed:</dt>
                    <dd>{FormatBytes(speed)}/s</dd>
                    <dt>Progress:</dt>
                    <dd>{(progress * 100).toFixed(0)}%</dd>
                    <dt>Status:</dt>
                    <dd>{ConstName(UploadState, uState)}</dd>
                </dl>
            );
        }
    }

    function getChallengeElement() {
        let elm = document.createElement("iframe");
        elm.contentWindow.document.write(challenge);
        return <div dangerouslySetInnerHTML={{__html: elm.outerHTML}}/>;
    }

    useEffect(() => {
        let chromeVersion = getChromeVersion();
        if (chromeVersion >= 105) {
            //doStreamUpload().catch(console.error);
        } else {
            doXHRUpload().catch(console.error);
        }
    }, []);

    return (
        <div className="upload">
            <div className="info">
                <dl>
                    <dt>Name:</dt>
                    <dd>{props.file.name}</dd>
                    <dt>Size:</dt>
                    <dd>{FormatBytes(props.file.size)}</dd>
                </dl>
            </div>
            <div className="status">
                {renderStatus()}
            </div>
            {uState === UploadState.Challenge ?
                <div className="iframe-challenge" onClick={() => window.location.reload()}>
                    {getChallengeElement()}
                </div>
                : null}
        </div>
    );
}