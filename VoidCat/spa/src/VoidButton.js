import {useEffect, useState} from "react";
import FeatherIcon from "feather-icons-react";

export function VoidButton(props) {
    const options = {
        showSuccess: false,
        ...props.options
    };
    const [success, setSuccess] = useState(false);
    
    async function handleClick(e) {
        if (e.target.classList.contains("disabled")) return;
        e.target.classList.add("disabled");

        let fn = props.onClick;
        if (typeof fn === "function") {
            let ret = fn(e);
            if (typeof ret === "object" && typeof ret.then === "function") {
                await ret;
            }
            setSuccess(options.showSuccess);
        }

        e.target.classList.remove("disabled");
    }

    useEffect(() => {
        if (success === true) {
            setTimeout(() => setSuccess(false), 1000);
        }
    }, [success]);
    
    return (
        <div className="flex-inline flex-center">
            <div>
                <div className="btn" onClick={handleClick}>{props.children}</div>    
            </div>
            {success ? <div><FeatherIcon icon="check-circle"/></div> : null}
        </div>
    );
}