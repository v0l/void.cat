import "./FooterLinks.css"
import StrikeLogo from "./image/strike.png";

export function FooterLinks(){    
    return (
        <div className="footer">
            <a href="https://discord.gg/8BkxTGs" target="_blank">Discord</a>
            <a href="https://invite.strike.me/KS0FYF" target="_blank">Get Strike <img src={StrikeLogo} alt="Strike logo"/> </a>
            <a href="https://github.com/v0l/void.cat" target="_blank">GitHub</a>
        </div>
    );
}