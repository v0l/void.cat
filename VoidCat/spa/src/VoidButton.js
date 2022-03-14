export function VoidButton(props) {
    async function handleClick(e) {
        if (e.target.classList.contains("disabled")) return;
        e.target.classList.add("disabled");

        let fn = props.onClick;
        if (typeof fn === "function") {
            let ret = fn(e);
            if (typeof ret === "object" && typeof ret.then === "function") {
                await ret;
            }
        }

        e.target.classList.remove("disabled");
    }

    return <div className="btn" onClick={handleClick}>{props.children}</div>;
}