import {Fragment, useState} from "react";
import {FileUpload} from "./FileUpload";

export function Uploader(props) {
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
            fElm.push(<FileUpload file={f}/>);
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

    return (
        <div className="app">
            {files.length === 0 ? renderDrop() : renderUploads()}
        </div>
    );
}