import {Fragment, useEffect, useState} from "react";
import {useParams} from "react-router-dom";
import {TextPreview} from "./TextPreview";
import FeatherIcon from "feather-icons-react";
import "./FilePreview.css";
import {FileEdit} from "./FileEdit";
import {FilePaywall} from "./FilePaywall";
import {Api} from "./Api";
import {Helmet} from "react-helmet";
import {FormatBytes} from "./Util";

export function FilePreview() {
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
                case "image/jpg":
                case "image/jpeg":
                case "image/png": {
                    return <img src={link} alt={info.metadata.name}/>;
                }
                case "audio/mp3":
                case "audio/ogg":
                case "video/mp4":
                case "video/matroksa":
                case "video/x-matroska":
                case "video/webm":
                case "video/quicktime": {
                    return <video src={link} controls/>;
                }
                case "text/css":
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

    useEffect(() => {
        if (info) {
            let order = window.localStorage.getItem(`paywall-${info.id}`);
            if (order) {
                let orderObj = JSON.parse(order);
                setOrder(orderObj);
                setLink(`/d/${info.id}?orderId=${orderObj.id}`);
            } else {
                setLink(`/d/${info.id}`);
            }
        }
    }, [info]);

    return (
        <div className="preview">
            {info ? (
                <Fragment>
                    <Helmet>
                        <title>void.cat - {info.metadata?.name}</title>
                        <meta name="description" content={info.metadata?.description}/>
                    </Helmet>
                    <a className="btn" href={link}>{info.metadata?.name ?? info.id}</a>
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