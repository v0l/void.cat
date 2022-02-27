import {Fragment, useEffect, useState} from "react";
import {useParams} from "react-router-dom";
import {useApi} from "./Api";
import {ApiHost, DefaultAvatar} from "./Const";
import "./Profile.css";
import {useDispatch, useSelector} from "react-redux";
import {setProfile as setGlobalProfile} from "./LoginState";
import {DigestAlgo} from "./FileUpload";
import {buf2hex} from "./Util";

export function Profile() {
    const [profile, setProfile] = useState();
    const auth = useSelector(state => state.login.jwt);
    const localProfile = useSelector(state => state.login.profile);
    const canEdit = localProfile?.id === profile?.id;
    const {Api} = useApi();
    const params = useParams();
    const dispatch = useDispatch();

    async function loadProfile() {
        let p = await Api.getUser(params.id);
        if (p.ok) {
            setProfile(await p.json());
        }
    }

    function editUsername(v) {
        setProfile({
            ...profile,
            displayName: v
        });
    }

    function editPublic(v) {
        setProfile({
            ...profile,
            public: v
        });
    }

    async function changeAvatar() {
        let res = await new Promise((resolve, reject) => {
            let i = document.createElement('input');
            i.setAttribute('type', 'file');
            i.setAttribute('multiple', '');
            i.addEventListener('change', async function (evt) {
                resolve(evt.target.files);
            });
            i.click();
        });

        const file = res[0];
        const buf = await file.arrayBuffer();
        const digest = await crypto.subtle.digest(DigestAlgo, buf);
        
        let req = await fetch(`${ApiHost}/upload`, {
            mode: "cors",
            method: "POST",
            body: buf,
            headers: {
                "Content-Type": "application/octet-stream",
                "V-Content-Type": file.type,
                "V-Filename": file.name,
                "V-Digest": buf2hex(digest),
                "Authorization": `Bearer ${auth}`
            }
        });
        
        if(req.ok) {
            let rsp = await req.json();
            if(rsp.ok) {
                setProfile({
                    ...profile,
                    avatar: rsp.file.id
                });
            }
        }
        
    } 
    
    async function saveUser() {
        let r = await Api.updateUser({
            id: profile.id,
            avatar: profile.avatar,
            displayName: profile.displayName,
            public: profile.public
        });
        if (r.ok) {
            // saved
            dispatch(setGlobalProfile(profile));
        }
    }

    useEffect(() => {
        loadProfile();
    }, []);

    if (profile) {
        let avatarUrl = profile.avatar ?? DefaultAvatar;
        if(!avatarUrl.startsWith("http")) {
            // assume void-cat hosted avatar
            avatarUrl = `/d/${avatarUrl}`;
        }
        let avatarStyles = {
            backgroundImage: `url(${avatarUrl})`
        };
        return (
            <div className="page">
                <div className="profile">
                    <div className="name">
                        {canEdit ?
                            <input value={profile.displayName}
                                   onChange={(e) => editUsername(e.target.value)}/>
                            : profile.displayName}
                    </div>
                    <div className="avatar" style={avatarStyles}>
                        {canEdit ? <div className="edit-avatar" onClick={() => changeAvatar()}>
                            <h3>Edit</h3>
                        </div> : null}
                    </div>
                    <div className="roles">
                        <h3>Roles:</h3>
                        {profile.roles.map(a => <span className="btn">{a}</span>)}
                    </div>
                    {canEdit ?
                        <Fragment>
                            <p>
                                <label>Public Profile:</label>
                                <input type="checkbox" checked={profile.public}
                                       onChange={(e) => editPublic(e.target.checked)}/>
                            </p>
                            <div className="btn" onClick={saveUser}>Save</div>
                        </Fragment> : null}
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