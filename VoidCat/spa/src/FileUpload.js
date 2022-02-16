import {useEffect, useState} from "react";

import "./FileUpload.css";
import {buf2hex, ConstName, FormatBytes} from "./Util";
import {RateCalculator} from "./RateCalculator";
import {upload} from "@testing-library/user-event/dist/upload";

const UploadState = {
    NotStarted: 0,
    Starting: 1,
    Hashing: 2,
    Uploading: 3,
    Done: 4,
    Failed: 5,
    Challenge: 6
};

export function FileUpload(props) {
    const [speed, setSpeed] = useState(0);
    const [progress, setProgress] = useState(0);
    const [result, setResult] = useState();
    const [uState, setUState] = useState(UploadState.NotStarted);
    const [challenge, setChallenge] = useState();
    const calc = new RateCalculator();

    function handleProgress(e) {
        console.log(e);
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
                "Content-Type": props.file.type,
                "X-Filename": props.file.name
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
     * @param id {string}
     * @param editSecret {string?}
     * @returns {Promise<any>}
     */
    async function xhrSegment(segment, id, editSecret) {
        const DigestAlgo = "SHA-256";
        setUState(UploadState.Hashing);
        const digest = await crypto.subtle.digest(DigestAlgo, segment);
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
                req.open("POST", typeof (id) === "string" ? `/upload/${id}` : "/upload");
                req.setRequestHeader("Content-Type", props.file.type);
                req.setRequestHeader("X-Filename", props.file.name);
                req.setRequestHeader("X-Digest", buf2hex(digest));
                if (typeof (editSecret) === "string") {
                    req.setRequestHeader("X-EditSecret", editSecret);
                }
                req.send(segment);
            } catch (e) {
                reject(e);
            }
        });
    }

    async function doXHRUpload() {
        // upload file in segments of 50MB
        const UploadSize = 50_000_000;

        let xhr = null;
        const segments = props.file.size / UploadSize;
        for (let s = 0; s < segments; s++) {
            let offset = s * UploadSize;
            let slice = props.file.slice(offset, offset + UploadSize, props.file.type);
            xhr = await xhrSegment(await slice.arrayBuffer(), xhr?.file?.id, xhr?.file?.editSecret);
            if (!xhr.ok) {
                break;
            }
        }
        if (xhr.ok) {
            setUState(UploadState.Done);
            setResult(xhr.file);
        } else {
            setUState(UploadState.Failed);
            setResult(xhr.errorMessage);
        }
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
                    <iframe src={`data:text/html;charset=utf-8,${encodeURIComponent(challenge)}`}/>
                </div>
                : null}
        </div>
    );
}