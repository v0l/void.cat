import {Fragment, useEffect, useState} from "react";
import {useParams} from "react-router-dom";
import {useApi} from "./Api";
import {ApiHost, DefaultAvatar, UserFlags} from "./Const";
import "./Profile.css";
import {useDispatch, useSelector} from "react-redux";
import {logout, setProfile as setGlobalProfile} from "./LoginState";
import {DigestAlgo} from "./FileUpload";
import {buf2hex, hasFlag} from "./Util";
import moment from "moment";
import FeatherIcon from "feather-icons-react";
import {FileList} from "./FileList";
import {VoidButton} from "./VoidButton";

export function Profile() {
    const [profile, setProfile] = useState();
    const [noProfile, setNoProfile] = useState(false);
    const [saved, setSaved] = useState(false);
    const [emailCode, setEmailCode] = useState("");
    const [emailCodeError, setEmailCodeError] = useState("");
    const [newCodeSent, setNewCodeSent] = useState(false);
    const auth = useSelector(state => state.login.jwt);
    const localProfile = useSelector(state => state.login.profile);

    const canEdit = localProfile?.id === profile?.id;
    const needsEmailVerify = canEdit && (profile?.flags & UserFlags.EmailVerified) !== UserFlags.EmailVerified;
    const cantEditProfile = canEdit && !needsEmailVerify;

    const {Api} = useApi();
    const params = useParams();
    const dispatch = useDispatch();

    async function loadProfile() {
        let p = await Api.getUser(params.id);
        if (p.ok && p.status === 200) {
            setProfile(await p.json());
        } else {
            setNoProfile(true);
        }
    }

    function editUsername(v) {
        setProfile({
            ...profile,
            displayName: v
        });
    }

    function toggleFlag(v) {
        setProfile({
            ...profile,
            flags: (profile.flags ^ v)
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

        if (req.ok) {
            let rsp = await req.json();
            if (rsp.ok) {
                setProfile({
                    ...profile,
                    avatar: rsp.file.id
                });
            }
        }

    }

    async function saveUser(e) {
        let r = await Api.updateUser({
            id: profile.id,
            avatar: profile.avatar,
            displayName: profile.displayName,
            flags: profile.flags
        });
        if (r.ok) {
            // saved
            dispatch(setGlobalProfile(profile));
            setSaved(true);
        }
    }

    async function submitCode(e) {
        let r = await Api.submitVerifyCode(profile.id, emailCode);
        if (r.ok) {
            await loadProfile();
        } else {
            setEmailCodeError("Invalid or expired code.");
        }
    }

    async function sendNewCode() {
        setNewCodeSent(true);
        let r = await Api.sendNewCode(profile.id);
        if (!r.ok) {
            setNewCodeSent(false);
        }
    }

    function renderEmailVerify() {
        return (
            <Fragment>
                <h2>Please enter email verification code</h2>
                <small>Your account will automatically be deleted in 7 days if you do not verify your email
                    address.</small>
                <br/>
                <input type="text" placeholder="Verification code" value={emailCode}
                       onChange={(e) => setEmailCode(e.target.value)}/>
                <VoidButton onClick={submitCode}>Submit</VoidButton>
                <VoidButton onClick={() => dispatch(logout())}>Logout</VoidButton>
                <br/>
                {emailCodeError ? <b>{emailCodeError}</b> : null}
                {emailCodeError && !newCodeSent ? <a onClick={sendNewCode}>Send verfication email</a> : null}
            </Fragment>
        );
    }

    function renderProfileEdit() {
        return (
            <Fragment>
                <dl>
                    <dt>Public Profile:</dt>
                    <dd>
                        <input type="checkbox" checked={hasFlag(profile.flags, UserFlags.PublicProfile)}
                               onChange={(e) => toggleFlag(UserFlags.PublicProfile)}/>
                    </dd>
                    <dt>Public Uploads:</dt>
                    <dd>
                        <input type="checkbox" checked={hasFlag(profile.flags, UserFlags.PublicUploads)}
                               onChange={(e) => toggleFlag(UserFlags.PublicUploads)}/>
                    </dd>

                </dl>
                <div className="flex flex-center">
                    <div>
                        <VoidButton onClick={saveUser}>Save</VoidButton>
                    </div>
                    <div>
                        {saved ? <FeatherIcon icon="check-circle"/> : null}
                    </div>
                    <div>
                        <VoidButton onClick={() => dispatch(logout())}>Logout</VoidButton>
                    </div>
                </div>
            </Fragment>
        );
    }

    useEffect(() => {
        loadProfile();
    }, []);

    useEffect(() => {
        if (saved === true) {
            setTimeout(() => setSaved(false), 1000);
        }
    }, [saved]);

    if (profile) {
        let avatarUrl = profile.avatar ?? DefaultAvatar;
        if (!avatarUrl.startsWith("http")) {
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
                        {cantEditProfile ?
                            <input value={profile.displayName}
                                   onChange={(e) => editUsername(e.target.value)}/>
                            : profile.displayName}
                    </div>
                    <div className="flex">
                        <div className="flx-1">
                            <div className="avatar" style={avatarStyles}>
                                {cantEditProfile ? <div className="edit-avatar" onClick={() => changeAvatar()}>
                                    <h3>Edit</h3>
                                </div> : null}
                            </div>
                        </div>
                        <div className="flx-1">
                            <dl>
                                <dt>Created</dt>
                                <dd>{moment(profile.created).fromNow()}</dd>
                                <dt>Roles</dt>
                                <dd>{profile.roles.map(a => <span key={a} className="btn">{a}</span>)}</dd>
                            </dl>
                        </div>
                    </div>
                    {cantEditProfile ? renderProfileEdit() : null}
                    {needsEmailVerify ? renderEmailVerify() : null}
                    <h1>Uploads</h1>
                    <FileList loadPage={(req) => Api.listUserFiles(profile.id, req)}/>
                </div>
            </div>
        );
    } else if (noProfile === true) {
        return (
            <div className="page">
                <h1>No profile found :(</h1>
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