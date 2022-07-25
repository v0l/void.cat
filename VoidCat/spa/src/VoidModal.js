import "./VoidModal.css";

export default function VoidModal(props) {
    const title = props.title;
    const style = props.style;
    
    return (
        <div className="modal-bg">
            <div className="modal" style={style}>
                <div className="modal-header">
                    {title ?? "Unknown modal"}
                </div>
                <div className="modal-body">
                    {props.children ?? "Missing body"}
                </div>
            </div>
        </div>
    )
}