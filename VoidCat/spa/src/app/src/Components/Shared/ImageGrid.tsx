import "./ImageGrid.css";

import {ApiError, PagedRequest, PagedResponse, PagedSortBy, PageSortOrder, VoidFileResponse} from "@void-cat/api";
import {useEffect, useState} from "react";
import {Link} from "react-router-dom";
import {useDispatch} from "react-redux";

import {logout} from "../../LoginState";
import {PageSelector} from "./PageSelector";

interface ImageGridProps {
    loadPage: (req: PagedRequest) => Promise<PagedResponse<any>>
}

export default function ImageGrid(props: ImageGridProps) {
    const loadPage = props.loadPage;
    const dispatch = useDispatch();
    const [files, setFiles] = useState<PagedResponse<VoidFileResponse>>();
    const [page, setPage] = useState(0);
    const pageSize = 100;
    const [accessDenied, setAccessDenied] = useState<boolean>();

    async function loadFileList() {
        try {
            const pageReq = {
                page: page,
                pageSize,
                sortBy: PagedSortBy.Date,
                sortOrder: PageSortOrder.Dsc
            };
            const rsp = await loadPage(pageReq);
            setFiles(rsp);
        } catch (e) {
            console.error(e);
            if (e instanceof ApiError) {
                if (e.statusCode === 401) {
                    dispatch(logout());
                } else if (e.statusCode === 403) {
                    setAccessDenied(true);
                }
            }
        }
    }

    useEffect(() => {
        loadFileList().catch(console.error)
    }, [page]);

    function renderPreview(info: VoidFileResponse) {
        const link = `/d/${info.id}`;

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
                    return <img src={link} alt={info.metadata.name}/>;
                }
                case "audio/aac":
                case "audio/opus":
                case "audio/wav":
                case "audio/webm":
                case "audio/midi":
                case "audio/mpeg":
                case "audio/ogg": {
                    return <audio src={link}/>;
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
                    return <video src={link}/>;
                }
                default: {
                    return <b>{info.metadata?.name ?? info.id}</b>
                }
            }
        }
    }

    if (accessDenied) {
        return <h3>Access Denied</h3>
    }

    return <>
        <div className="image-grid">
            {files?.results.map(v => <Link key={v.id} to={`/${v.id}`}>
                {renderPreview(v)}
            </Link>)}
        </div>
        <PageSelector onSelectPage={(x) => setPage(x)} page={page} total={files?.totalResults ?? 0}
                      pageSize={pageSize}/>
    </>
}