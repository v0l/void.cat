import {Fragment, useState} from "react";
import {FileUpload} from "./FileUpload";

import "./Dropzone.css";

export function Dropzone(props) {
    let [files, setFiles] = useState([]);

    function selectFiles(e) {
        let i = document.createElement('input');
        i.setAttribute('type', 'file');
        i.setAttribute('multiple', '');
        i.addEventListener('change', function (evt) {
            setFiles(evt.target.files);
        });
        i.click();
    }

    function renderUploads() {
        let fElm = [];
        for(let f of files) {
            fElm.push(<FileUpload file={f} key={f.name}/>);
        }
        return (
            <Fragment>
                {fElm}
            </Fragment>
        );
    }

    function renderDrop() {
        return (
            <div className="drop" onClick={selectFiles}>
                <h3>Drop files here!</h3>
            </div>
        );
    }

    return files.length === 0 ? renderDrop() : renderUploads();
}