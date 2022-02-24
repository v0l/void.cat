import {useDispatch, useSelector} from "react-redux";
import {useEffect, useState} from "react";
import {PagedSortBy, PageSortOrder} from "../Const";
import {useApi} from "../Api";
import {logout} from "../LoginState";
import {PageSelector} from "../PageSelector";
import moment from "moment";

export function UserList() {
    const {AdminApi} = useApi();
    const auth = useSelector((state) => state.login.jwt);
    const dispatch = useDispatch();
    const [users, setUsers] = useState();
    const [page, setPage] = useState(0);
    const pageSize = 10;
    const [accessDenied, setAccessDenied] = useState();

    async function loadUserList() {
        let pageReq = {
            page: page,
            pageSize,
            sortBy: PagedSortBy.Id,
            sortOrder: PageSortOrder.Asc
        };
        let req = await AdminApi.userList(auth, pageReq);
        if (req.ok) {
            setUsers(await req.json());
        } else if (req.status === 401) {
            dispatch(logout());
        } else if(req.status === 403) {
            setAccessDenied(true);
        }
    }

    function renderUser(u) {
        return (
            <tr>
                <td><a href={`/u/${u.id}`}>{u.id.substring(0, 4)}..</a></td>
                <td>{moment(u.created).fromNow()}</td>
                <td>{moment(u.lastLogin).fromNow()}</td>
                <td>0</td>
                <td>{u.roles.join(", ")}</td>
                <td>
                    <button>Delete</button>
                    <button>SetRoles</button>
                </td>
            </tr>
        );
    }
    
    useEffect(() => {
        loadUserList();
    }, [page]);

    if (accessDenied === true) {
        return <h3>Access Denied</h3>;
    }
    
    return (
        <table>
            <thead>
            <tr>
                <td>Id</td>
                <td>Created</td>
                <td>Last Login</td>
                <td>Files</td>
                <td>Roles</td>
                <td>Actions</td>
            </tr>
            </thead>
            <tbody>
            {users ? users.results.map(renderUser) : null}
            </tbody>
            <tbody>
            <tr>
                <td>
                    {users ? <PageSelector onSelectPage={(x) => setPage(x)} page={page} total={users.totalResults}
                                           pageSize={pageSize}/> : null}
                </td>
            </tr>
            </tbody>
        </table>
    );
}