import "./FilePreview.css";
import {Fragment, useEffect, useState} from "react";
import {useParams} from "react-router-dom";
import FeatherIcon from "feather-icons-react";
import {Helmet} from "react-helmet";

import {TextPreview} from "../Components/FilePreview/TextPreview";
import {FileEdit} from "../Components/FileEdit/FileEdit";
import {FilePayment} from "../Components/FilePreview/FilePayment";
import {useApi} from "../Components/Shared/Api";
import {FormatBytes} from "../Components/Shared/Util";
import {ApiHost} from "../Components/Shared/Const";
import {InlineProfile} from "../Components/Shared/InlineProfile";
import {StreamEncryption} from "../Components/Shared/StreamEncryption";
import {VoidButton} from "../Components/Shared/VoidButton";
import {useFileTransfer} from "../Components/Shared/FileTransferHook";

export function FilePreview() {
    const {Api} = useApi();
    const params = useParams();
    const [info, setInfo] = useState();
    const [order, setOrder] = useState();
    const [link, setLink] = useState("#");
    const [key, setKey] = useState("");
    const [error, setError] = useState("");
    const {speed, progress, update, setFileSize} = useFileTransfer();

    async function loadInfo() {
        let req = await Api.fileInfo(params.id);
        if (req.ok) {
            let info = await req.json();
            setInfo(info);
        }
    }

    function isFileEncrypted() {
        return "string" === typeof info?.metadata?.encryptionParams
    }

    function isDecrypted() {
        return link.startsWith("blob:");
    }

    function isPaymentRequired() {
        return info?.payment?.required === true && !order;
    }
    
    function canAccessFile() {
        if (isPaymentRequired()) {
            return false;
        }
        if (isFileEncrypted() && !isDecrypted()) {
            return false;
        }
        return true;
    }

    async function decryptFile() {
        try {
            let hashKey = key.match(/([0-9a-z]{32}):([0-9a-z]{24})/);
            if (hashKey?.length === 3) {
                let [key, iv] = [hashKey[1], hashKey[2]];
                let enc = new StreamEncryption(key, iv, info.metadata?.encryptionParams);

                let rsp = await fetch(link);
                if (rsp.ok) {
                    let reader = rsp.body
                        .pipeThrough(enc.getDecryptionTransform())
                        .pipeThrough(decryptionProgressTransform());
                    let newResponse = new Response(reader);
                    setLink(window.URL.createObjectURL(await newResponse.blob(), {type: info.metadata.mimeType}));
                }
            } else {
                setError("Invalid encryption key format");
            }
        } catch (e) {
            setError(e.message);
        }
    }

    function decryptionProgressTransform() {
        return new window.TransformStream({
            transform: (chunk, controller) => {
                update(chunk.length);
                controller.enqueue(chunk);            
            }
        });
    }
    
    function renderEncryptedDownload() {
        if (!isFileEncrypted() || isDecrypted() || isPaymentRequired()) return;
        return (
            <div className="encrypted">
                <h3>This file is encrypted, please enter the encryption key:</h3>
                <input type="password" placeholder="Encryption key" value={key}
                       onChange={(e) => setKey(e.target.value)}/>
                <VoidButton onClick={() => decryptFile()}>Decrypt</VoidButton>
                {progress > 0 ? `${(100 * progress).toFixed(0)}% (${FormatBytes(speed)}/s)` : null}
                {error ? <h4 className="error">{error}</h4> : null}
            </div>
        );
    }

    function renderPayment() {
        if (info.payment && info.payment.service !== 0) {
            if (!order) {
                return <FilePayment file={info} onPaid={loadInfo}/>;
            }
        }

        return null;
    }

    function renderPreview() {
        if (!canAccessFile()) return;

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

    function renderVirusWarning() {
        if (info.virusScan && info.virusScan.isVirus === true) {
            let scanResult = info.virusScan;
            return (
                <div className="virus-warning">
                    <p>
                        This file apears to be a virus, take care when downloading this file.
                    </p>
                    Detected as:
                    <pre>
                        {scanResult.names}
                    </pre>
                </div>
            );
        }
    }

    useEffect(() => {
        loadInfo();
    }, []);

    useEffect(() => {
        if (info) {
            let fileLink = info.metadata?.url ?? `${ApiHost}/d/${info.id}`;
            setFileSize(info.metadata.size);
            
            let order = window.localStorage.getItem(`payment-${info.id}`);
            if (order) {
                let orderObj = JSON.parse(order);
                setOrder(orderObj);
                setLink(`${fileLink}?orderId=${orderObj.id}`);
            } else {
                setLink(fileLink);
            }
        }
    }, [info]);

    return (
        <div className="preview page">
            {info ? (
                <Fragment>
                    <Helmet>
                        <title>void.cat - {info.metadata?.name ?? info.id}</title>
                        {info.metadata?.description ?
                            <meta name="description" content={info.metadata?.description}/> : null}
                        {renderOpenGraphTags()}
                    </Helmet>
                    {renderVirusWarning()}
                    <div className="flex flex-center">
                        <div className="flx-grow">
                            {info.uploader ? <InlineProfile profile={info.uploader}/> : null}
                        </div>
                        <div>
                            {canAccessFile() ?
                                <a className="btn" href={link}
                                   download={info.metadata?.name ?? info.id}>Download</a> : null}
                        </div>
                    </div>
                    {renderPayment()}
                    {renderPreview()}
                    {renderEncryptedDownload()}
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