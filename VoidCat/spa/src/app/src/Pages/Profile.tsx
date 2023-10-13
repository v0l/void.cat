import "./Profile.css";
import { Fragment, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { default as moment } from "moment";
import { useLoaderData } from "react-router-dom";
import { Profile } from "@void-cat/api";

import useApi from "Hooks/UseApi";
import { RootState } from "Store";
import { DefaultAvatar } from "Const";

import { logout, setProfile as setGlobalProfile } from "../LoginState";
import { FileList } from "../Components/Shared/FileList";
import { VoidButton } from "../Components/Shared/VoidButton";
import ApiKeyList from "../Components/Profile/ApiKeyList";

export function ProfilePage() {
  const dispatch = useDispatch();
  const loader = useLoaderData();
  const Api = useApi();

  const [profile, setProfile] = useState(loader as Profile | null);
  const [emailCode, setEmailCode] = useState("");
  const [emailCodeError, setEmailCodeError] = useState("");
  const [newCodeSent, setNewCodeSent] = useState(false);

  const localProfile = useSelector((s: RootState) => s.login.profile);

  const canEdit = localProfile?.id === profile?.id;
  const needsEmailVerify = canEdit && profile?.needsVerification === true;
  const cantEditProfile = canEdit && !needsEmailVerify;

  async function changeAvatar() {
    const res = await new Promise<Array<File>>((resolve) => {
      let i = document.createElement("input");
      i.setAttribute("type", "file");
      i.setAttribute("multiple", "");
      i.addEventListener("change", async function (evt) {
        resolve((evt.target as any).files);
      });
      i.click();
    });

    const file = res[0];
    const uploader = Api.getUploader(file);
    const rsp = await uploader.upload();
    if (rsp.ok) {
      setProfile({
        ...profile,
        avatar: rsp.file?.id,
      } as Profile);
    }
  }

  async function saveUser(p: Profile) {
    try {
      await Api.updateUser(p);
      dispatch(setGlobalProfile(p));
    } catch (e) {
      console.error(e);
    }
  }

  async function submitCode(id: string, code: string) {
    try {
      await Api.submitVerifyCode(id, code);
    } catch (e) {
      console.error(e);
      setEmailCodeError("Invalid or expired code.");
    }
  }

  async function sendNewCode(id: string) {
    setNewCodeSent(true);
    try {
      await Api.sendNewCode(id);
    } catch (e) {
      console.error(e);
      setNewCodeSent(false);
    }
  }

  function renderEmailVerify() {
    if (!profile) return;

    return (
      <Fragment>
        <h2>Please enter email verification code</h2>
        <small>
          Your account will automatically be deleted in 7 days if you do not
          verify your email address.
        </small>
        <br />
        <input
          type="text"
          placeholder="Verification code"
          value={emailCode}
          onChange={(e) => setEmailCode(e.target.value)}
        />
        <VoidButton onClick={() => submitCode(profile.id, emailCode)}>
          Submit
        </VoidButton>
        <VoidButton
          onClick={() => {
            dispatch(logout());
          }}
        >
          Logout
        </VoidButton>
        <br />
        {emailCodeError && <b>{emailCodeError}</b>}
        {emailCodeError && !newCodeSent && (
          <a onClick={() => sendNewCode(profile.id)}>Send verification email</a>
        )}
      </Fragment>
    );
  }

  function renderProfileEdit() {
    if (!profile) return;

    return (
      <Fragment>
        <dl>
          <dt>Public Profile:</dt>
          <dd>
            <input
              type="checkbox"
              checked={profile.publicProfile}
              onChange={(e) =>
                setProfile({
                  ...profile,
                  publicProfile: e.target.checked,
                })
              }
            />
          </dd>
          <dt>Public Uploads:</dt>
          <dd>
            <input
              type="checkbox"
              checked={profile.publicUploads}
              onChange={(e) =>
                setProfile({
                  ...profile,
                  publicUploads: e.target.checked,
                })
              }
            />
          </dd>
        </dl>
        <div className="flex flex-center">
          <div>
            <VoidButton
              onClick={() => saveUser(profile)}
              options={{ showSuccess: true }}
            >
              Save
            </VoidButton>
          </div>
          <div>
            <VoidButton
              onClick={() => {
                dispatch(logout());
              }}
            >
              Logout
            </VoidButton>
          </div>
        </div>
      </Fragment>
    );
  }

  if (profile) {
    let avatarUrl = profile.avatar ?? DefaultAvatar;
    if (!avatarUrl.startsWith("http")) {
      // assume void-cat hosted avatar
      avatarUrl = `/d/${avatarUrl}`;
    }
    let avatarStyles = {
      backgroundImage: `url(${avatarUrl})`,
    };
    return (
      <div className="page">
        <div className="profile">
          <div className="name">
            {cantEditProfile ? (
              <input
                value={profile.name}
                onChange={(e) =>
                  setProfile({
                    ...profile,
                    name: e.target.value,
                  })
                }
              />
            ) : (
              profile.name
            )}
          </div>
          <div className="flex">
            <div className="flx-1">
              <div className="avatar" style={avatarStyles}>
                {cantEditProfile ? (
                  <div className="edit-avatar" onClick={() => changeAvatar()}>
                    <h3>Edit</h3>
                  </div>
                ) : null}
              </div>
            </div>
            <div className="flx-1">
              <dl>
                <dt>Created</dt>
                <dd>{moment(profile.created).fromNow()}</dd>
                <dt>Roles</dt>
                <dd>
                  {profile.roles.map((a) => (
                    <span key={a} className="btn">
                      {a}
                    </span>
                  ))}
                </dd>
              </dl>
            </div>
          </div>
          {cantEditProfile ? renderProfileEdit() : null}
          {needsEmailVerify ? renderEmailVerify() : null}
          <h1>Uploads</h1>
          <FileList loadPage={(req) => Api.listUserFiles(profile.id, req)} />
          {cantEditProfile ? <ApiKeyList /> : null}
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
