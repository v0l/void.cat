import moment from "moment";
import {Link} from "react-router-dom";
import {useDispatch, useSelector} from "react-redux";
import {useEffect, useState} from "react";
import {FormatBytes} from "../Util";
import {AdminApi} from "../Api";
import {logout} from "../LoginState";
import {PagedSortBy, PageSortOrder} from "../Const";
import {PageSelector} from "../PageSelector";

export function FileList(props) {
    const auth = useSelector((state) => state.login.jwt);
    const dispatch = useDispatch();
    const [files, setFiles] = useState();
    const [page, setPage] = useState(0);
    const pageSize = 10;
    const [accessDenied, setAccessDenied] = useState();

    async function loadFileList() {
        let pageReq = {
            page: page,
            pageSize,
            sortBy: PagedSortBy.Date,
            sortOrder: PageSortOrder.Dsc
        };
        let req = await AdminApi.fileList(auth, pageReq);
        if (req.ok) {
            setFiles(await req.json());
        } else if (req.status === 401) {
            dispatch(logout());
        } else if (req.status === 403) {
            setAccessDenied(true);
        }
    }

    async function deleteFile(e, id) {
        e.target.disabled = true;
        if (window.confirm(`Are you sure you want to delete: ${id}?`)) {
            let req = await AdminApi.deleteFile(auth, id);
            if (req.ok) {
                setFiles({
                    ...files,
                    results: files.results.filter(a => a.id !== id)
                });
            } else {
                alert("Failed to delete file!");
            }
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
    }, [page]);

    if (accessDenied === true) {
        return <h3>Access Denied</h3>;
    }
    
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
            {files ? files.results.map(a => renderItem(a)) : null}
            </tbody>
            <tbody>
            <tr>
                <td colSpan={999}>{files ?
                    <PageSelector onSelectPage={(x) => setPage(x)} page={page} total={files.totalResults}
                                  pageSize={pageSize}/> : null}</td>
            </tr>
            </tbody>
        </table>
    );
}