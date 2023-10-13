import "./VoidModal.css";
import { CSSProperties, ReactNode } from "react";

interface VoidModalProps {
  title?: string;
  style?: CSSProperties;
  children: ReactNode;
}
export default function VoidModal(props: VoidModalProps) {
  const title = props.title;
  const style = props.style;

  return (
    <div className="modal-bg">
      <div className="modal" style={style}>
        <div className="modal-header">{title ?? "Unknown modal"}</div>
        <div className="modal-body">{props.children ?? "Missing body"}</div>
      </div>
    </div>
  );
}
