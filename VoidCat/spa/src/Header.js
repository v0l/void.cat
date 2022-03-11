import "./Header.css";
import {Link} from "react-router-dom";
import {useDispatch, useSelector} from "react-redux";
import {InlineProfile} from "./InlineProfile";
import {useApi} from "./Api";
import {logout, setProfile} from "./LoginState";
import {useEffect} from "react";

export function Header() {
    const dispatch = useDispatch();
    const jwt = useSelector(state => state.login.jwt);
    const profile = useSelector(state => state.login.profile)
    const {Api} = useApi();

    async function initProfile() {
        if (jwt && !profile) {
            let rsp = await Api.getUser("me");
            if (rsp.ok) {
                dispatch(setProfile(await rsp.json()));
            } else {
                dispatch(logout());
            }
        }
    }

    useEffect(() => {
        initProfile();
    }, []);

    return (
        <div className="header page">
            <div className="title">
                <Link to="/">{window.location.hostname}</Link>
            </div>
            {profile ?
                <InlineProfile profile={profile} options={{
                    showName: false
                }}/> :
                <Link to="/login">
                    <div className="btn">Login</div>
                </Link>}
        </div>
    )
}