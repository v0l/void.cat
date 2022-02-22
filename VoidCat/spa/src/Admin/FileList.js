import moment from "moment";
import {Link} from "react-router-dom";
import {useSelector} from "react-redux";
import {useEffect, useState} from "react";
import {FormatBytes} from "../Util";

import "./FileList.css";

export function FileList(props) {
    const auth = useSelector((state) => state.login.jwt);
    const [files, setFiles] = useState([]);

    async function loadFileList() {
        let req = await fetch("/admin/file", {
            headers: {
                "authorization": `Bearer ${auth}`
            }
        });
        if (req.ok) {
            setFiles(await req.json());
        }
    }

    async function deleteFile(e, id) {
        e.target.disabled = true;

        let req = await fetch(`/admin/file/${id}`, {
            method: "DELETE",
            headers: {
                "authorization": `Bearer ${auth}`
            }
        });
        if (req.ok) {
            setFiles([
                ...files.filter(a => a.id !== id)
            ]);
        } else {
            alert("Failed to delete file!");
        }
        e.target.disabled = false;
    }

    function renderItem(i) {
        const meta = i.metadata;
        const bw = i.bandwidth;

        return (
            <tr key={i.id}>
                <td><Link to={`/${i.id}`}>{i.id.substring(0, 4)}..</Link></td>
                <td>{meta?.name ? (meta?.name.length > 20 ? `${meta?.name.substring(0, 20)}..` : meta?.name) : null}</td>
                <td>{meta?.uploaded ? moment(meta?.uploaded).fromNow() : null}</td>
                <td>{meta?.size ? FormatBytes(meta?.size, 2) : null}</td>
                <td>{bw ? FormatBytes(bw.egress, 2) : null}</td>
                <td>
                    <button onClick={(e) => deleteFile(e, i.id)}>Delete</button>
                </td>
            </tr>
        );
    }

    useEffect(() => {
        loadFileList()
    }, []);

    return (
        <table className="file-list">
            <thead>
            <tr>
                <td>Id</td>
                <td>Name</td>
                <td>Uploaded</td>
                <td>Size</td>
                <td>Egress</td>
                <td>Actions</td>
            </tr>
            </thead>
            <tbody>
            {files?.map(a => renderItem(a))}
            </tbody>
        </table>
    );
}