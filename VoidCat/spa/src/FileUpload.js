import {useEffect, useState} from "react";

import "./FileUpload.css";
import {FormatBytes} from "./Util";

export function FileUpload(props) {
    let [speed, setSpeed] = useState(0);
    let [progress, setProgress] = useState(0);
    let [result, setResult] = useState();
    
    async function doUpload() {
        let req = await fetch("/upload", {
            method: "POST",
            body: props.file,
            headers: {
                "content-type": "application/octet-stream"
            }
        });
        
        if(req.ok) {
            let rsp = await req.json();
            console.log(rsp);
            setResult(rsp);
        }
    }

    async function updateMetadata(result) {
        let metaReq = {
            editSecret: result.editSecret,
            metadata: {
                name: props.file.name,
                mimeType: props.file.type
            }
        };

        let req = await fetch(`/upload/${result.id}`, {
            method: "PATCH",
            body: JSON.stringify(metaReq),
            headers: {
                "content-type": "application/json"
            }
        });

        if (req.ok) {
            // nothing
        }
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
        doUpload();
    }, []);

    useEffect(() => {
        if (result) {
            updateMetadata(result);
        }
    }, [result]);

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