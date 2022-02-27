import "./Header.css";
import {Link} from "react-router-dom";
import {useSelector} from "react-redux";
import {InlineProfile} from "./InlineProfile";

export function Header() {
    const profile = useSelector(state => state.login.profile);

    return (
        <div className="header page">
            <div className="title">
                <Link to="/">void.cat</Link>
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