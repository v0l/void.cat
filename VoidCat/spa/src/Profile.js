import {useEffect, useState} from "react";
import {useParams} from "react-router-dom";
import {useApi} from "./Api";
import {DefaultAvatar} from "./Const";

import "./Profile.css";

export function Profile() {
    const [profile, setProfile] = useState();
    const {Api} = useApi();
    const params = useParams();

    async function loadProfile() {
        let p = await Api.getUser(params.id);
        if (p.ok) {
            setProfile(await p.json());
        }
    }

    useEffect(() => {
        loadProfile();
    }, []);

    if (profile) {
        let avatarStyles = {
            backgroundImage: `url(${profile.avatar ?? DefaultAvatar})`
        };
        return (
            <div className="page">
                <div className="profile">
                    <h2>{profile.displayName}</h2>
                    <div className="avatar" style={avatarStyles}/>
                    <div className="roles">
                        <h3>Roles:</h3>
                        {profile.roles.map(a => <span className="btn">{a}</span>)}
                    </div>
                </div>
            </div>
        );
    } else {
        return (
            <div className="page">
                <h1>Loading..</h1>
            </div>
        );
    }
}