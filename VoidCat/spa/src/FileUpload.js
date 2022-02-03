import {useEffect, useState} from "react";

import "./FileUpload.css";
import {FormatBytes} from "./Util";
import {RateCalculator} from "./RateCalculator";

export function FileUpload(props) {
    let [speed, setSpeed] = useState(0);
    let [progress, setProgress] = useState(0);
    let [result, setResult] = useState();
    let calc = new RateCalculator();
    
    function handleProgress(e) {
        console.log(e);
        if(e instanceof ProgressEvent) {
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
                if(offset > props.file.size) {
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
        
        if(req.ok) {
            let rsp = await req.json();
            console.log(rsp);
            setResult(rsp);
        }
    }

    async function doXHRUpload() {
        let xhr = await new Promise((resolve, reject) => {
            let req = new XMLHttpRequest();
            req.onreadystatechange = (ev) => {
                if(req.readyState === XMLHttpRequest.DONE && req.status === 200) {
                    let rsp = JSON.parse(req.responseText);
                    resolve(rsp);
                }
            };
            req.upload.onprogress = handleProgress;
            req.open("POST", "/upload");
            req.setRequestHeader("Content-Type", props.file.type);
            req.setRequestHeader("X-Filename", props.file.name);
            req.send(props.file);
        });
       
        setResult(xhr);
    }
    
    function renderStatus() {
        if(result) {
            return (
                <dl>
                    <dt>Link:</dt>
                    <dd><a target="_blank" href={`/${result.id}`}>{result.id}</a></dd>
                </dl>
            );
        } else {
            return (
                <dl>
                    <dt>Speed:</dt>
                    <dd>{FormatBytes(speed)}/s</dd>
                    <dt>Progress:</dt>
                    <dd>{(progress * 100).toFixed(0)}%</dd>
                </dl>
            );
        }
    }

    useEffect(() => {
        console.log(props.file);
        doXHRUpload();
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
        </div>
    );
}