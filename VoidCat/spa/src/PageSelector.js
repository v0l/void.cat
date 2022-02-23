import "./PageSelector.css";

export function PageSelector(props) {
    const total = props.total;
    const pageSize = props.pageSize;
    const page = props.page;
    const onSelectPage = props.onSelectPage;
    const options = {
        showPages: 2,
        ...(props.options || {})
    };

    let totalPages = Math.floor(total / pageSize);
    let first = Math.max(0, page - options.showPages);
    let firstDiff = page - first;
    let last = Math.min(totalPages, page + options.showPages + options.showPages - firstDiff);

    let buttons = [];
    for (let x = first; x <= last; x++) {
        buttons.push(<div onClick={(e) => onSelectPage(x)} key={x}>{x+1}</div>);
    }

    return (
        <div className="page-buttons">
            <div onClick={() => onSelectPage(0)}>&lt;&lt;</div>
            {buttons}
            <div onClick={() => onSelectPage(totalPages)}>&gt;&gt;</div>
            <small>Total: {total}</small>
        </div>
    );
}