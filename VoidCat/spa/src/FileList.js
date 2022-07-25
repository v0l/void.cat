import moment from "moment";
import {Link} from "react-router-dom";
import {useDispatch} from "react-redux";
import {useEffect, useState} from "react";
import {FormatBytes} from "./Util";
import {logout} from "./LoginState";
import {PagedSortBy, PageSortOrder} from "./Const";
import {PageSelector} from "./PageSelector";

export function FileList(props) {
    const loadPage = props.loadPage;
    const actions = props.actions;
    const dispatch = useDispatch();
    const [files, setFiles] = useState();
    const [page, setPage] = useState(0);
    const pageSize = 20;
    const [accessDenied, setAccessDenied] = useState(false);

    async function loadFileList() {
        let pageReq = {
            page: page,
            pageSize,
            sortBy: PagedSortBy.Date,
            sortOrder: PageSortOrder.Dsc
        };
        let req = await loadPage(pageReq);
        if (req.ok) {
            setFiles(await req.json());
        } else if (req.status === 401) {
            dispatch(logout());
        } else if (req.status === 403) {
            setAccessDenied(true);
        }
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
                {actions ? actions(i) : null}
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
        <table>
            <thead>
            <tr>
                <th>Id</th>
                <th>Name</th>
                <th>Uploaded</th>
                <th>Size</th>
                <th>Egress</th>
                {actions ? <th>Actions</th> : null}
            </tr>
            </thead>
            <tbody>
            {files ? files.results.map(a => renderItem(a)) : <tr>
                <td colSpan={99}>No files</td>
            </tr>}
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