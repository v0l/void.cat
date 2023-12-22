import { useDispatch } from "react-redux";
import { ReactNode, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import moment from "moment";
import {
  ApiError,
  PagedRequest,
  PagedResponse,
  PagedSortBy,
  PageSortOrder,
  VoidFileResponse,
} from "@void-cat/api";

import { logout } from "@/LoginState";
import { PageSelector } from "./PageSelector";

import { FormatBytes } from "@/Util";

interface FileListProps {
  actions?: (f: VoidFileResponse) => ReactNode;
  loadPage: (req: PagedRequest) => Promise<PagedResponse<any>>;
}

export function FileList(props: FileListProps) {
  const loadPage = props.loadPage;
  const actions = props.actions;
  const dispatch = useDispatch();
  const [files, setFiles] = useState<PagedResponse<VoidFileResponse>>();
  const [page, setPage] = useState(0);
  const pageSize = 20;
  const [accessDenied, setAccessDenied] = useState<boolean>();

  async function loadFileList() {
    try {
      const pageReq = {
        page: page,
        pageSize,
        sortBy: PagedSortBy.Date,
        sortOrder: PageSortOrder.Dsc,
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

  function renderItem(i: VoidFileResponse) {
    const meta = i.metadata;
    const bw = i.bandwidth;

    return (
      <tr key={i.id}>
        <td>
          <Link to={`/${i.id}`}>{i.id.substring(0, 4)}..</Link>
        </td>
        <td>
          {meta?.name
            ? meta?.name.length > 20
              ? `${meta?.name.substring(0, 20)}..`
              : meta?.name
            : null}
        </td>
        <td>{meta?.uploaded ? moment(meta?.uploaded).fromNow() : null}</td>
        <td>{meta?.size ? FormatBytes(meta?.size, 2) : null}</td>
        <td>{bw ? FormatBytes(bw.egress, 2) : null}</td>
        {actions ? actions(i) : null}
      </tr>
    );
  }

  useEffect(() => {
    loadFileList().catch(console.error);
  }, [page]);

  if (accessDenied) {
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
        {files ? (
          files.results.map((a) => renderItem(a))
        ) : (
          <tr>
            <td colSpan={99}>No files</td>
          </tr>
        )}
      </tbody>
      <tbody>
        <tr>
          <td colSpan={999}>
            {files && (
              <PageSelector
                onSelectPage={(x) => setPage(x)}
                page={page}
                total={files.totalResults}
                pageSize={pageSize}
              />
            )}
          </td>
        </tr>
      </tbody>
    </table>
  );
}
