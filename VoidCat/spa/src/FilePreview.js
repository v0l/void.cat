import {Fragment, useEffect, useState} from "react";
import {useParams} from "react-router-dom";
import {TextPreview} from "./TextPreview";

import "./FilePreview.css";

export function FilePreview() {
    const params = useParams();
    const [info, setInfo] = useState();

    async function loadInfo() {
        let req = await fetch(`/upload/${params.id}`);
        if (req.ok) {
            let info = await req.json();
            setInfo(info);
        }
    }

    function renderTypes() {
        let link = `/d/${info.id}`;
        if (info.metadata) {
            switch (info.metadata.mimeType) {
                case "image/jpg":
                case "image/jpeg":
                case "image/png": {
                    return <img src={link} alt={info.metadata.name}/>;
                }
                case "video/mp4":
                case "video/matroksa":
                case "video/x-matroska":
                case "video/webm":
                case "video/quicktime": {
                    return <video src={link} controls/>;
                }
                case "text/plain": {
                    return <TextPreview link={link}/>;
                }
                case "application/pdf": {
                    return <object data={link}/>;
                }
            }
        }
        return null;
    }

    useEffect(() => {
        loadInfo();
    }, []);

    return (
        <div className="preview">
            {info ? (
                <Fragment>
                    this.Download(<a className="btn" href={`/d/${info.id}`}>{info.metadata?.name ?? info.id}</a>)
                    {renderTypes()}
                </Fragment>
            ) : "Not Found"}
        </div>
    );
}