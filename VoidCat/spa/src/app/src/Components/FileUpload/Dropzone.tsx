import "./Dropzone.css";
import { Fragment, useEffect, useState } from "react";
import { FileUpload } from "./FileUpload";

export function Dropzone() {
  let [files, setFiles] = useState<Array<File>>([]);

  function selectFiles() {
    let i = document.createElement("input");
    i.setAttribute("type", "file");
    i.setAttribute("multiple", "");
    i.addEventListener("change", function (evt) {
      if (evt.target && "files" in evt.target) {
        setFiles(evt.target.files as Array<File>);
      }
    });
    i.click();
  }

  function dropFiles(e: DragEvent | ClipboardEvent) {
    e.preventDefault();
    e.stopPropagation();
    if ("dataTransfer" in e && (e.dataTransfer?.files?.length ?? 0) > 0) {
      setFiles([...e.dataTransfer!.files]);
    } else if (
      "clipboardData" in e &&
      (e.clipboardData?.files?.length ?? 0) > 0
    ) {
      setFiles([...e.clipboardData!.files]);
    }
  }

  function renderUploads() {
    let fElm = [];
    for (let f of files) {
      fElm.push(<FileUpload file={f} key={f.name} />);
    }
    return <Fragment>{fElm}</Fragment>;
  }

  function renderDrop() {
    return (
      <div className="drop" onClick={selectFiles}>
        <div>
          Click me!
          <small>Or drop files here</small>
        </div>
      </div>
    );
  }

  useEffect(() => {
    document.addEventListener("paste", dropFiles);
    document.addEventListener("drop", dropFiles);
    document.addEventListener("dragover", dropFiles);
    return () => {
      document.removeEventListener("paste", dropFiles);
      document.removeEventListener("drop", dropFiles);
      document.removeEventListener("dragover", dropFiles);
    };
  }, []);

  return files.length === 0 ? renderDrop() : renderUploads();
}
