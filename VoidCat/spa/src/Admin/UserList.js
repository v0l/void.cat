import {useDispatch} from "react-redux";
import {useEffect, useState} from "react";
import moment from "moment";
import {PagedSortBy, PageSortOrder} from "../Components/Shared/Const";
import {useApi} from "../Components/Shared/Api";
import {logout} from "../LoginState";
import {PageSelector} from "../Components/Shared/PageSelector";

export function UserList(props) {
    const {AdminApi} = useApi();
    const dispatch = useDispatch();
    const [users, setUsers] = useState();
    const [page, setPage] = useState(0);
    const pageSize = 10;
    const [accessDenied, setAccessDenied] = useState();
    const actions = props.actions;

    async function loadUserList() {
        let pageReq = {
            page: page,
            pageSize,
            sortBy: PagedSortBy.Date,
            sortOrder: PageSortOrder.Dsc
        };
        let req = await AdminApi.userList(pageReq);
        if (req.ok) {
            setUsers(await req.json());
        } else if (req.status === 401) {
            dispatch(logout());
        } else if (req.status === 403) {
            setAccessDenied(true);
        }
    }

    function renderUser(obj) {
        const user = obj.user;
        return (
            <tr key={user.id}>
                <td><a href={`/u/${user.id}`}>{user.displayName}</a></td>
                <td>{moment(user.created).fromNow()}</td>
                <td>{moment(user.lastLogin).fromNow()}</td>
                <td>{obj.uploads}</td>
                <td>{actions(user)}</td>
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
                <th>Name</th>
                <th>Created</th>
                <th>Last Login</th>
                <th>Files</th>
                <th>Actions</th>
            </tr>
            </thead>
            <tbody>
            {users ? users.results.map(renderUser) : null}
            </tbody>
            <tbody>
            <tr>
                <td>
                    {users ? <PageSelector
                        onSelectPage={(x) => setPage(x)}
                        page={page}
                        total={users.totalResults}
                        pageSize={pageSize}/> : null}
                </td>
            </tr>
            </tbody>
        </table>
    );
}