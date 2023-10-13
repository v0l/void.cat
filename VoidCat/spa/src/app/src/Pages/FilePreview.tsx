import "./FilePreview.css";
import { Fragment, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Helmet } from "react-helmet";
import {
  PaymentOrder,
  VoidFileResponse,
  StreamEncryption,
} from "@void-cat/api";

import { TextPreview } from "../Components/FilePreview/TextPreview";
import { FileEdit } from "../Components/FileEdit/FileEdit";
import { FilePayment } from "../Components/FilePreview/FilePayment";
import { InlineProfile } from "../Components/Shared/InlineProfile";
import { VoidButton } from "../Components/Shared/VoidButton";
import { useFileTransfer } from "../Components/Shared/FileTransferHook";
import Icon from "../Components/Shared/Icon";

import useApi from "Hooks/UseApi";
import { FormatBytes } from "Util";
import { ApiHost } from "Const";

export function FilePreview() {
  const Api = useApi();
  const params = useParams();
  const [info, setInfo] = useState<VoidFileResponse>();
  const [order, setOrder] = useState<PaymentOrder>();
  const [link, setLink] = useState("#");
  const [key, setKey] = useState("");
  const [error, setError] = useState("");
  const { speed, progress, update, setFileSize } = useFileTransfer();

  async function loadInfo() {
    if (params.id) {
      const i = await Api.fileInfo(params.id);
      setInfo(i);
    }
  }

  function isFileEncrypted() {
    return "string" === typeof info?.metadata?.encryptionParams;
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
    if (!info) return;

    try {
      let hashKey = key.match(/([0-9a-z]{32}):([0-9a-z]{24})/);
      if (hashKey?.length === 3) {
        let [key, iv] = [hashKey[1], hashKey[2]];
        let enc = new StreamEncryption(
          key,
          iv,
          info.metadata?.encryptionParams,
        );

        let rsp = await fetch(link);
        if (rsp.ok) {
          const reader = rsp.body
            ?.pipeThrough(enc.getDecryptionTransform())
            .pipeThrough(decryptionProgressTransform());
          const newResponse = new Response(reader);
          setLink(window.URL.createObjectURL(await newResponse.blob()));
        }
      } else {
        setError("Invalid encryption key format");
      }
    } catch (e) {
      if (e instanceof Error) {
        setError(e.message);
      } else {
        setError("Unknown error");
      }
    }
  }

  function decryptionProgressTransform() {
    return new TransformStream({
      transform: (chunk, controller) => {
        update(chunk.length);
        controller.enqueue(chunk);
      },
    });
  }

  function renderEncryptedDownload() {
    if (!isFileEncrypted() || isDecrypted() || isPaymentRequired()) return;
    return (
      <div className="encrypted">
        <h3>This file is encrypted, please enter the encryption key:</h3>
        <input
          type="password"
          placeholder="Encryption key"
          value={key}
          onChange={(e) => setKey(e.target.value)}
        />
        <VoidButton onClick={() => decryptFile()}>Decrypt</VoidButton>
        {progress > 0 &&
          `${(100 * progress).toFixed(0)}% (${FormatBytes(speed)}/s)`}
        {error && <h4 className="error">{error}</h4>}
      </div>
    );
  }

  function renderPayment() {
    if (!info) return;

    if (info.payment && info.payment.service !== 0 && !order) {
      return <FilePayment file={info} onPaid={loadInfo} />;
    }
  }

  function renderPreview() {
    if (!canAccessFile() || !info) return;

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
          return <img src={link} alt={info.metadata.name} />;
        }
        case "audio/aac":
        case "audio/opus":
        case "audio/wav":
        case "audio/webm":
        case "audio/midi":
        case "audio/mpeg":
        case "audio/ogg": {
          return <audio src={link} controls />;
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
          return <video src={link} controls />;
        }
        case "application/json":
        case "text/javascript":
        case "text/html":
        case "text/csv":
        case "text/css":
        case "text/plain": {
          return <TextPreview link={link} />;
        }
        case "application/pdf": {
          return <object data={link} />;
        }
        default: {
          return <h3>{info.metadata?.name ?? info.id}</h3>;
        }
      }
    }
  }

  function renderOpenGraphTags() {
    const tags = [
      <meta
        key="og-site_name"
        property={"og:site_name"}
        content={"void.cat"}
      />,
      <meta
        key="og-title"
        property={"og:title"}
        content={info?.metadata?.name}
      />,
      <meta
        key="og-description"
        property={"og:description"}
        content={info?.metadata?.description}
      />,
      <meta
        key="og-url"
        property={"og:url"}
        content={`https://${window.location.host}/${info?.id}`}
      />,
    ];

    const mime = info?.metadata?.mimeType;
    if (mime?.startsWith("image/")) {
      tags.push(<meta key="og-image" property={"og:image"} content={link} />);
      tags.push(
        <meta key="og-image-type" property={"og:image:type"} content={mime} />,
      );
    } else if (mime?.startsWith("video/")) {
      tags.push(<meta key="og-video" property={"og:video"} content={link} />);
      tags.push(
        <meta key="og-video-type" property={"og:video:type"} content={mime} />,
      );
    } else if (mime?.startsWith("audio/")) {
      tags.push(<meta key="og-audio" property={"og:audio"} content={link} />);
      tags.push(
        <meta key="og-audio-type" property={"og:audio:type"} content={mime} />,
      );
    }

    return tags;
  }

  function renderVirusWarning() {
    if (info?.virusScan?.isVirus === true) {
      let scanResult = info.virusScan;
      return (
        <div className="virus-warning">
          <p>
            This file apears to be a virus, take care when downloading this
            file.
          </p>
          Detected as:
          <pre>{scanResult.names}</pre>
        </div>
      );
    }
  }

  useEffect(() => {
    loadInfo().catch(console.error);
  }, []);

  useEffect(() => {
    if (info) {
      const fileLink = info.metadata?.url ?? `${ApiHost}/d/${info.id}`;
      setFileSize(info.metadata?.size ?? 0);

      const order = window.localStorage.getItem(`payment-${info.id}`);
      if (order) {
        const orderObj = JSON.parse(order);
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
            {info.metadata?.description ? (
              <meta name="description" content={info.metadata?.description} />
            ) : null}
            {renderOpenGraphTags()}
          </Helmet>
          {renderVirusWarning()}
          <div className="flex flex-center">
            <div className="flx-grow">
              {info.uploader ? <InlineProfile profile={info.uploader} /> : null}
            </div>
            <div>
              {canAccessFile() && (
                <>
                  <a className="btn" href={info?.metadata?.magnetLink}>
                    <Icon name="link" size={14} className="mr10" />
                    Magnet
                  </a>
                  <a
                    className="btn"
                    href={`${link}.torrent`}
                    download={info.metadata?.name ?? info.id}
                  >
                    <Icon name="file" size={14} className="mr10" />
                    Torrent
                  </a>
                  <a
                    className="btn"
                    href={link}
                    download={info.metadata?.name ?? info.id}
                  >
                    <Icon name="download" size={14} className="mr10" />
                    Direct Download
                  </a>
                </>
              )}
            </div>
          </div>
          {renderPayment()}
          {renderPreview()}
          {renderEncryptedDownload()}
          <div className="file-stats">
            <div>
              <Icon name="download-cloud" />
              {FormatBytes(info?.bandwidth?.egress ?? 0, 2)}
            </div>
            <div>
              <Icon name="save" />
              {FormatBytes(info?.metadata?.size ?? 0, 2)}
            </div>
          </div>
          <FileEdit file={info} />
        </Fragment>
      ) : (
        "Not Found"
      )}
    </div>
  );
}
