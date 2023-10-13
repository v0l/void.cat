import "./FileUpload.css";
import { useEffect, useMemo, useState } from "react";
import { useSelector } from "react-redux";
import { UploadState, VoidFileResponse } from "@void-cat/api";

import { VoidButton } from "../Shared/VoidButton";
import { useFileTransfer } from "../Shared/FileTransferHook";

import { RootState } from "Store";
import { ConstName, FormatBytes } from "Util";
import useApi from "Hooks/UseApi";

interface FileUploadProps {
  file: File | Blob;
}

export function FileUpload({ file }: FileUploadProps) {
  const info = useSelector((s: RootState) => s.info.info);
  const { speed, progress, loaded, setFileSize, reset } = useFileTransfer();
  const Api = useApi();
  const [result, setResult] = useState<VoidFileResponse>();
  const [error, setError] = useState("");
  const [uState, setUState] = useState(UploadState.NotStarted);
  const [challenge, setChallenge] = useState("");
  const [encryptionKey, setEncryptionKey] = useState("");
  const [encrypt, setEncrypt] = useState(true);

  const uploader = useMemo(() => {
    return Api.getUploader(
      file,
      setUState,
      loaded,
      setChallenge,
      info?.uploadSegmentSize,
    );
  }, [Api, file]);

  useEffect(() => {
    uploader.setEncryption(encrypt);
  }, [uploader, encrypt]);

  useEffect(() => {
    reset();
    setFileSize(file.size);
    if (!uploader.canEncrypt() && uState === UploadState.NotStarted) {
      startUpload().catch(console.error);
    }
  }, [file, uploader, uState]);

  async function startUpload() {
    setUState(UploadState.Starting);
    try {
      const result = await uploader.upload();
      console.debug(result);
      if (result.ok) {
        setUState(UploadState.Done);
        setResult(result.file);
        setEncryptionKey(uploader.getEncryptionKey() ?? "");
        window.localStorage.setItem(
          result.file!.id,
          JSON.stringify(result.file!),
        );
      } else {
        setUState(UploadState.Failed);
        setError(result.errorMessage!);
      }
    } catch (e) {
      setUState(UploadState.Failed);
      if (e instanceof Error) {
        setError(e.message);
      } else {
        setError("Unknown error");
      }
    }
  }

  function renderStatus() {
    if (result && uState === UploadState.Done) {
      let link = `/${result.id}`;
      return (
        <dl>
          <dt>Link:</dt>
          <dd>
            <a target="_blank" href={link} rel="noreferrer">
              {result.id}
            </a>
          </dd>
          {encryptionKey ? (
            <>
              <dt>Encryption Key:</dt>
              <dd>
                <VoidButton
                  onClick={() => navigator.clipboard.writeText(encryptionKey)}
                >
                  Copy
                </VoidButton>
              </dd>
            </>
          ) : null}
        </dl>
      );
    } else if (uState === UploadState.NotStarted) {
      return (
        <>
          <dl>
            <dt>Encrypt file:</dt>
            <dd>
              <input
                type="checkbox"
                checked={encrypt}
                onChange={(e) => setEncrypt(e.target.checked)}
              />
            </dd>
          </dl>
          <VoidButton onClick={() => startUpload()}>Upload</VoidButton>
        </>
      );
    } else {
      return (
        <dl>
          <dt>Speed:</dt>
          <dd>{FormatBytes(speed)}/s</dd>
          <dt>Progress:</dt>
          <dd>{(progress * 100).toFixed(0)}%</dd>
          <dt>Status:</dt>
          <dd>{error ? error : ConstName(UploadState, uState)}</dd>
        </dl>
      );
    }
  }

  function getChallengeElement() {
    let elm = document.createElement("iframe");
    elm.contentWindow?.document.write(challenge);
    return <div dangerouslySetInnerHTML={{ __html: elm.outerHTML }} />;
  }

  return (
    <div className="upload">
      <div className="info">
        <dl>
          <dt>Name:</dt>
          <dd>{file.name}</dd>
          <dt>Size:</dt>
          <dd>{FormatBytes(file.size)}</dd>
        </dl>
      </div>
      <div className="status">{renderStatus()}</div>
      {uState === UploadState.Challenge && (
        <div
          className="iframe-challenge"
          onClick={() => window.location.reload()}
        >
          {getChallengeElement()}
        </div>
      )}
    </div>
  );
}
