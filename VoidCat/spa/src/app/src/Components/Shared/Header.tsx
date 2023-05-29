import "./Header.css";
import VoidCat from "../../image/voidcat.png";

import {useEffect} from "react";
import {Link} from "react-router-dom";
import {useDispatch, useSelector} from "react-redux";

import {InlineProfile} from "./InlineProfile";
import {logout, setAuth, setProfile} from "../../LoginState";
import {setInfo} from "../../SiteInfoStore";

import useApi from "Hooks/UseApi";
import {RootState} from "Store";

export function Header() {
    const dispatch = useDispatch();
    const jwt = useSelector((s: RootState) => s.login.jwt);
    const profile = useSelector((s: RootState) => s.login.profile)
    const Api = useApi();

    async function initProfile() {
        if (jwt && !profile) {
            try {
                const me = await Api.getUser("me");
                dispatch(setProfile(me));
            } catch (e) {
                console.error(e);
                dispatch(logout());
            }
        } else if (window.location.pathname === "/login" && window.location.hash.length > 1) {
            dispatch(setAuth({
                jwt: window.location.hash.substring(1)
            }));
        }
    }

    async function loadStats() {
        const info = await Api.info();
        dispatch(setInfo(info));
    }

    useEffect(() => {
        initProfile().catch(console.error);
        loadStats().catch(console.error);
    }, [jwt]);

    return (
        <div className="header page">

            <img src={VoidCat} alt="logo" className="logo"/>
            <div className="title">
                <Link to="/">
                    {window.location.hostname}
                </Link>
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