import { UploadState, VoidUploader } from "./upload";
import { VoidUploadResult } from "./index";

export class XHRUploader extends VoidUploader {
  canEncrypt(): boolean {
    return false;
  }

  setEncryption() {
    //noop
  }

  getEncryptionKey() {
    return undefined;
  }

  async upload(headers?: HeadersInit): Promise<VoidUploadResult> {
    this.onStateChange?.(UploadState.Hashing);
    const hash = await this.digest(this.file);
    if (this.file.size > this.maxChunkSize) {
      return await this.#doSplitXHRUpload(hash, this.maxChunkSize, headers);
    } else {
      return await this.#xhrSegment(
        this.file,
        hash,
        undefined,
        undefined,
        1,
        1,
        headers,
      );
    }
  }

  async #doSplitXHRUpload(
    hash: string,
    splitSize: number,
    headers?: HeadersInit,
  ) {
    let xhr: VoidUploadResult | null = null;
    const segments = Math.ceil(this.file.size / splitSize);
    for (let s = 0; s < segments; s++) {
      const offset = s * splitSize;
      const slice = this.file.slice(offset, offset + splitSize, this.file.type);
      xhr = await this.#xhrSegment(
        slice,
        hash,
        xhr?.file?.id,
        xhr?.file?.metadata?.editSecret,
        s + 1,
        segments,
        headers,
      );
      if (!xhr.ok) {
        break;
      }
    }
    return xhr!;
  }

  /**
   * Upload a segment of the file
   * @param segment
   * @param fullDigest Full file hash
   * @param id
   * @param editSecret
   * @param part Segment number
   * @param partOf Total number of segments
   * @param headers
   */
  async #xhrSegment(
    segment: ArrayBuffer | Blob,
    fullDigest: string,
    id?: string,
    editSecret?: string,
    part?: number,
    partOf?: number,
    headers?: HeadersInit,
  ) {
    this.onStateChange?.(UploadState.Uploading);

    const absoluteUrl = id ? `${this.uri}/upload/${id}` : `${this.uri}/upload`;
    const authValue = this.auth
      ? await this.auth(absoluteUrl, "POST")
      : undefined;

    return await new Promise<VoidUploadResult>((resolve, reject) => {
      try {
        const req = new XMLHttpRequest();
        req.onreadystatechange = () => {
          if (req.readyState === XMLHttpRequest.DONE && req.status === 200) {
            const rsp = JSON.parse(req.responseText) as VoidUploadResult;
            resolve(rsp);
          } else if (
            req.readyState === XMLHttpRequest.DONE &&
            req.status === 403
          ) {
            const contentType = req.getResponseHeader("content-type");
            if (contentType?.toLowerCase().trim().indexOf("text/html") === 0) {
              this.onProxyChallenge?.(req.response);
              this.onStateChange?.(UploadState.Challenge);
              reject(new Error("CF Challenge"));
            }
          }
        };
        req.upload.onprogress = (e) => {
          if (e instanceof ProgressEvent) {
            this.onProgress?.(e.loaded);
          }
        };
        req.open("POST", absoluteUrl);
        req.setRequestHeader("Content-Type", "application/octet-stream");
        req.setRequestHeader(
          "V-Content-Type",
          !this.file.type ? "application/octet-stream" : this.file.type,
        );
        req.setRequestHeader(
          "V-Filename",
          "name" in this.file ? this.file.name : "",
        );
        req.setRequestHeader("V-Full-Digest", fullDigest);
        req.setRequestHeader("V-Segment", `${part}/${partOf}`);
        if (authValue) {
          req.withCredentials = true;
          req.setRequestHeader("Authorization", authValue);
        }
        if (editSecret) {
          req.setRequestHeader("V-EditSecret", editSecret);
        }
        if (headers) {
          for (const [k, v] of Object.entries(headers)) {
            req.setRequestHeader(k, v);
          }
        }
        req.send(segment);
      } catch (e) {
        reject(e);
      }
    });
  }
}
