import "./FooterLinks.css"
import StrikeLogo from "../../image/strike.png";
import {useSelector} from "react-redux";

export function FooterLinks() {
    const profile = useSelector(state => state.login.profile);

    return (
        <div className="footer">
            <a href="https://discord.gg/8BkxTGs" target="_blank">
                Discord
            </a>
            <a href="https://invite.strike.me/KS0FYF" target="_blank">
                Get Strike <img src={StrikeLogo} alt="Strike logo"/>
            </a>
            <a href="https://github.com/v0l/void.cat" target="_blank">
                GitHub
            </a>
            {profile?.roles?.includes("Admin") ? <a href="/admin">Admin</a> : null}
        </div>
    );
}