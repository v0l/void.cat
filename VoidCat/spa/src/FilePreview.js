import {Fragment, useEffect, useState} from "react";
import {useParams} from "react-router-dom";
import {TextPreview} from "./TextPreview";
import FeatherIcon from "feather-icons-react";
import "./FilePreview.css";
import {FileEdit} from "./FileEdit";
import {FilePaywall} from "./FilePaywall";
import {useApi} from "./Api";
import {Helmet} from "react-helmet";
import {FormatBytes} from "./Util";
import {ApiHost} from "./Const";
import {InlineProfile} from "./InlineProfile";

export function FilePreview() {
    const {Api} = useApi();
    const params = useParams();
    const [info, setInfo] = useState();
    const [order, setOrder] = useState();
    const [link, setLink] = useState("#");

    async function loadInfo() {
        let req = await Api.fileInfo(params.id);
        if (req.ok) {
            let info = await req.json();
            setInfo(info);
        }
    }

    function renderTypes() {
        if (info.paywall && info.paywall.service !== 0) {
            if (!order) {
                return <FilePaywall file={info} onPaid={loadInfo}/>;
            }
        }

        if (info.metadata) {
            switch (info.metadata.mimeType) {
                case "image/avif":
                case "image/bmp":
                case "image/gif":
                case "image/svg+xml":
                case "image/tiff":
                case "image/webp":
                case "image/jpg":
                case "image/jpeg":
                case "image/png": {
                    return <img src={link} alt={info.metadata.name}/>;
                }
                case "audio/aac":
                case "audio/opus":
                case "audio/wav":
                case "audio/webm":
                case "audio/midi":
                case "audio/mpeg":
                case "audio/ogg": {
                    return <audio src={link} controls/>;
                }
                case "video/x-msvideo":
                case "video/mpeg":
                case "video/ogg":
                case "video/mp2t":
                case "video/mp4":
                case "video/matroksa":
                case "video/x-matroska":
                case "video/webm":
                case "video/quicktime": {
                    return <video src={link} controls/>;
                }
                case "application/json":
                case "text/javascript":
                case "text/html":
                case "text/csv":
                case "text/css":
                case "text/plain": {
                    return <TextPreview link={link}/>;
                }
                case "application/pdf": {
                    return <object data={link}/>;
                }
                default: {
                    return <h3>{info.metadata?.name ?? info.id}</h3>
                }
            }
        }
        return null;
    }

    function renderOpenGraphTags() {
        let tags = [
            <meta key="og-site_name" property={"og:site_name"} content={"void.cat"}/>,
            <meta key="og-title" property={"og:title"} content={info?.metadata?.name}/>,
            <meta key="og-description" property={"og:description"} content={info?.metadata?.description}/>,
            <meta key="og-url" property={"og:url"} content={`https://${window.location.host}/${info?.id}`}/>
        ];

        const mime = info?.metadata?.mimeType;
        if (mime?.startsWith("image/")) {
            tags.push(<meta key="og-image" property={"og:image"} content={link}/>);
            tags.push(<meta key="og-image-type" property={"og:image:type"} content={mime}/>);
        } else if (mime?.startsWith("video/")) {
            tags.push(<meta key="og-video" property={"og:video"} content={link}/>);
            tags.push(<meta key="og-video-type" property={"og:video:type"} content={mime}/>);
        } else if (mime?.startsWith("audio/")) {
            tags.push(<meta key="og-audio" property={"og:audio"} content={link}/>);
            tags.push(<meta key="og-audio-type" property={"og:audio:type"} content={mime}/>);
        }

        return tags;
    }

    useEffect(() => {
        loadInfo();
    }, []);

    useEffect(() => {
        if (info) {
            let order = window.localStorage.getItem(`paywall-${info.id}`);
            if (order) {
                let orderObj = JSON.parse(order);
                setOrder(orderObj);
                setLink(`${ApiHost}/d/${info.id}?orderId=${orderObj.id}`);
            } else {
                setLink(`${ApiHost}/d/${info.id}`);
            }
        }
    }, [info]);

    return (
        <div className="preview page">
            {info ? (
                <Fragment>
                    <Helmet>
                        <title>void.cat - {info.metadata?.name ?? info.id}</title>
                        {info.metadata?.description ? <meta name="description" content={info.metadata?.description}/> : null}
                        {renderOpenGraphTags()}
                    </Helmet>
                    <div className="flex flex-center">
                        <div className="flx-grow">
                            {info.uploader ? <InlineProfile profile={info.uploader}/> : null}
                        </div>
                        <div>
                            <a className="btn" href={link} download={info.metadata?.name ?? info.id}>Download</a>
                        </div>
                    </div>
                    {renderTypes()}
                    <div className="file-stats">
                        <div>
                            <FeatherIcon icon="download-cloud"/>
                            {FormatBytes(info?.bandwidth?.egress ?? 0, 2)}
                        </div>
                        <div>
                            <FeatherIcon icon="hard-drive"/>
                            {FormatBytes(info?.metadata?.size ?? 0, 2)}
                        </div>
                    </div>
                    <FileEdit file={info}/>
                </Fragment>
            ) : "Not Found"}
        </div>
    );
}