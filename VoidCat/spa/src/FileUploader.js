import {DefaultAvatar} from "./Const";
import {Link} from "react-router-dom";
import "./FileUploader.css";

export function FileUploader(props) {
    const uploader = props.uploader;

    let avatarStyles = {
        backgroundImage: `url(${uploader.avatar ?? DefaultAvatar})`
    };

    return (
        <div className="uploader-info">
            <Link to={`/u/${uploader.id}`}>
                <div className="small-profile">
                    <div className="avatar" style={avatarStyles}/>
                    <div className="name">{uploader.displayName}</div>
                </div>
            </Link>
        </div>
    )
}