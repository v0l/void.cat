import {useEffect, useState} from "react";
import {ConstName, FormatBytes} from "./Util";
import {RateCalculator} from "./RateCalculator";
import * as CryptoJS from 'crypto-js';

import "./FileUpload.css";
import {useSelector} from "react-redux";
import {ApiHost} from "./Const";

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
    const [speed, setSpeed] = useState(0);
    const [progress, setProgress] = useState(0);
    const [result, setResult] = useState();
    const [uState, setUState] = useState(UploadState.NotStarted);
    const [challenge, setChallenge] = useState();
    const calc = new RateCalculator();

    function handleProgress(e) {
        if (e instanceof ProgressEvent) {
            let newProgress = e.loaded / e.total;

            calc.ReportLoaded(e.loaded);
            setSpeed(calc.RateWindow(5));
            setProgress(newProgress);
        }
    }

    async function doStreamUpload() {
        let offset = 0;
        let rs = new ReadableStream({
            start: (controller) => {

            },
            pull: async (controller) => {
                if (offset > props.file.size) {
                    controller.cancel();
                }

                let requestedSize = props.file.size / controller.desiredSize;
                console.log(`Reading ${requestedSize} Bytes`);

                let end = Math.min(offset + requestedSize, props.file.size);
                let blob = props.file.slice(offset, end, props.file.type);
                controller.enqueue(await blob.arrayBuffer());
                offset += blob.size;
            },
            cancel: (reason) => {

            }
        }, {
            highWaterMark: 100
        });

        let req = await fetch("/upload", {
            method: "POST",
            body: rs,
            headers: {
                "Content-Type": "application/octet-stream",
                "V-Content-Type": props.file.type,
                "V-Filename": props.file.name
            }
        });

        if (req.ok) {
            let rsp = await req.json();
            console.log(rsp);
            setResult(rsp);
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
        let uploadSize = info.uploadSegmentSize ?? Number.MAX_VALUE;

        setUState(UploadState.Hashing);
        let hash = await digest(props.file);
        calc.Reset();
        if(props.file.size >= uploadSize) {
            await doSplitXHRUpload(hash, uploadSize);
        } else {
            let xhr = await xhrSegment(props.file, hash);
            handleXHRResult(xhr);
        }
    }

    async function doSplitXHRUpload(hash, splitSize) {
        let xhr = null;
        setProgress(0);
        const segments = Math.ceil(props.file.size / splitSize);
        for (let s = 0; s < segments; s++) {
            calc.Reset();
            let offset = s * splitSize;
            let slice = props.file.slice(offset, offset + splitSize, props.file.type);
            let segment = await slice.arrayBuffer();
            xhr = await xhrSegment(segment, xhr?.file?.id, xhr?.file?.metadata?.editSecret, hash, s + 1, segments);
            if (!xhr.ok) {
                break;
            }
        }
        handleXHRResult(xhr);
    }
    
    function handleXHRResult(xhr) {
        if (xhr.ok) {
            setUState(UploadState.Done);
            setResult(xhr.file);
            window.localStorage.setItem(xhr.file.id, JSON.stringify(xhr.file));
        } else {
            setUState(UploadState.Failed);
            setResult(xhr.errorMessage);
        }
    }
    
    async function digest(file) {
        const chunkSize = 100_000_000;
        let sha = CryptoJS.algo.SHA256.create();
        for (let x = 0; x < Math.ceil(file.size / chunkSize); x++) {
            let offset = x * chunkSize;
            let slice = file.slice(offset, offset + chunkSize, file.type);
            let data = Uint32Array.from(await slice.arrayBuffer());
            sha.update(new CryptoJS.lib.WordArray.init(data, slice.length));

            calc.ReportLoaded(offset);
            setSpeed(calc.RateWindow(5));
            setProgress(offset / parseFloat(file.size));
        }
        return sha.finalize().toString();
    }

    function renderStatus() {
        if (result) {
            return uState === UploadState.Done ?
                <dl>
                    <dt>Link:</dt>
                    <dd><a target="_blank" href={`/${result.id}`}>{result.id}</a></dd>
                </dl>
                : <b>{result}</b>;
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
        console.log(props.file);
        doXHRUpload().catch(console.error);
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