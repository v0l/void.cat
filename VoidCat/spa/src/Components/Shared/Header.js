import "./Header.css";
import {Link} from "react-router-dom";
import {useDispatch, useSelector} from "react-redux";
import {InlineProfile} from "./InlineProfile";
import {useApi} from "./Api";
import {logout, setAuth, setProfile} from "../../LoginState";
import {useEffect} from "react";
import {setInfo} from "../../SiteInfoStore";

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
        } else if(window.location.pathname === "/login" && window.location.hash.length > 1) {
            dispatch(setAuth({
                jwt: window.location.hash.substring(1),
                profile: null
            }));
        }
    }
    async function loadStats() {
        let req = await Api.info();
        if (req.ok) {
            dispatch(setInfo(await req.json()));
        }
    }

    useEffect(() => {
        initProfile();
        loadStats();
    }, [jwt]);

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