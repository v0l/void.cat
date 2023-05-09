import {useDispatch} from "react-redux";
import {ReactNode, useEffect, useState} from "react";
import moment from "moment";

import {logout} from "../LoginState";
import {PageSelector} from "../Components/Shared/PageSelector";

import useApi from "Hooks/UseApi";
import {AdminProfile, AdminUserListResult, ApiError, PagedResponse, PagedSortBy, PageSortOrder} from "Api";

interface UserListProps {
    actions: (u: AdminProfile) => ReactNode
}

export function UserList({actions}: UserListProps) {
    const AdminApi = useApi();
    const dispatch = useDispatch();

    const [users, setUsers] = useState<PagedResponse<AdminUserListResult>>();
    const [page, setPage] = useState(0);
    const pageSize = 10;
    const [accessDenied, setAccessDenied] = useState<boolean>();

    async function loadUserList() {
        try {
            const pageReq = {
                page: page,
                pageSize,
                sortBy: PagedSortBy.Date,
                sortOrder: PageSortOrder.Dsc
            };
            const rsp = await AdminApi.adminUserList(pageReq);
            setUsers(rsp);
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

    function renderUser(r: AdminUserListResult) {
        return (
            <tr key={r.user.id}>
                <td><a href={`/u/${r.user.id}`}>{r.user.name}</a></td>
                <td>{moment(r.user.created).fromNow()}</td>
                <td>{moment(r.user.lastLogin).fromNow()}</td>
                <td>{r.uploads}</td>
                <td>{actions(r.user)}</td>
            </tr>
        );
    }

    useEffect(() => {
        loadUserList().catch(console.error);
    }, [page]);

    if (accessDenied === true) {
        return <h3>Access Denied</h3>;
    }

    return (
        <table>
            <thead>
            <tr>
                <th>Name</th>
                <th>Created</th>
                <th>Last Login</th>
                <th>Files</th>
                <th>Actions</th>
            </tr>
            </thead>
            <tbody>
            {users && users.results.map(renderUser)}
            </tbody>
            <tbody>
            <tr>
                <td>
                    {users && <PageSelector
                        onSelectPage={(x) => setPage(x)}
                        page={page}
                        total={users.totalResults}
                        pageSize={pageSize}/>}
                </td>
            </tr>
            </tbody>
        </table>
    );
}