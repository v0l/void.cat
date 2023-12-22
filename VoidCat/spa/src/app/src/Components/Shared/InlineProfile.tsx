import "./InlineProfile.css";
import { CSSProperties } from "react";
import { Link } from "react-router-dom";
import { Profile } from "@void-cat/api";

import { DefaultAvatar } from "@/Const";

const DefaultSize = 64;

interface InlineProfileProps {
  profile: Profile;
  options?: {
    size?: number;
    showName?: boolean;
    link?: boolean;
  };
}

export function InlineProfile({ profile, options }: InlineProfileProps) {
  options = {
    size: DefaultSize,
    showName: true,
    link: true,
    ...options,
  };

  let avatarUrl = profile.avatar ?? DefaultAvatar;
  if (!avatarUrl.startsWith("http")) {
    avatarUrl = `/d/${avatarUrl}`;
  }
  let avatarStyles = {
    backgroundImage: `url(${avatarUrl})`,
  } as CSSProperties;
  if (options.size !== DefaultSize) {
    avatarStyles.width = `${options.size}px`;
    avatarStyles.height = `${options.size}px`;
  }

  const elms = (
    <div className="small-profile">
      <div className="avatar" style={avatarStyles} />
      {options.showName ? <div className="name">{profile.name}</div> : null}
    </div>
  );
  if (options.link === true) {
    return <Link to={`/u/${profile.id}`}>{elms}</Link>;
  }
  return elms;
}
