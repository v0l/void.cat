import "./InlineProfile.css";
import {DefaultAvatar} from "./Const";
import {Link} from "react-router-dom";

const DefaultSize = 64;

export function InlineProfile(props) {
    const profile = props.profile;
    const options = {
        size: DefaultSize,
        showName: true,
        link: true,
        ...props.options
    };

    let avatarUrl = profile.avatar ?? DefaultAvatar;
    if (!avatarUrl.startsWith("http")) {
        avatarUrl = `/d/${avatarUrl}`;
    }
    let avatarStyles = {
        backgroundImage: `url(${avatarUrl})`
    };
    if (options.size !== DefaultSize) {
        avatarStyles.width = `${options.size}px`;
        avatarStyles.height = `${options.size}px`;
    }

    let elms = (
        <div className="small-profile">
            <div className="avatar" style={avatarStyles}/>
            {options.showName ? <div className="name">{profile.displayName}</div> : null}
        </div>
    );
    if (options.link === true) {
        return <Link to={`/u/${profile.id}`}>{elms}</Link>
    }
    return elms;
}