import "./FooterLinks.css"
import {useSelector} from "react-redux";
import {Link} from "react-router-dom";

import {RootState} from "Store";

export function FooterLinks() {
    const profile = useSelector((s:RootState) => s.login.profile);

    return (
        <div className="footer">
            <a href="https://discord.gg/8BkxTGs" target="_blank" rel="noreferrer">
                Discord
            </a>
            <a href="https://git.v0l.io/Kieran/void.cat/" target="_blank" rel="noreferrer">
                GitHub
            </a>
            <Link to="/donate">Donate</Link>
            {profile?.roles?.includes("Admin") ? <a href="/admin">Admin</a> : null}
        </div>
    );
}