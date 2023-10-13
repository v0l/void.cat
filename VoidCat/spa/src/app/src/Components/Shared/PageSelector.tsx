import "./PageSelector.css";

interface PageSelectorProps {
  total: number;
  pageSize: number;
  page: number;
  onSelectPage?: (v: number) => void;
  options?: {
    showPages: number;
  };
}

export function PageSelector(props: PageSelectorProps) {
  const total = props.total;
  const pageSize = props.pageSize;
  const page = props.page;
  const onSelectPage = props.onSelectPage;
  const options = {
    showPages: 3,
    ...props.options,
  };

  const totalPages = Math.floor(total / pageSize);
  const first = Math.max(0, page - options.showPages);
  const firstDiff = page - first;
  const last = Math.min(
    totalPages,
    page + options.showPages + options.showPages - firstDiff,
  );

  const buttons = [];
  for (let x = first; x <= last; x++) {
    buttons.push(
      <div
        onClick={() => onSelectPage?.(x)}
        key={x}
        className={page === x ? "active" : ""}
      >
        {x + 1}
      </div>,
    );
  }

  return (
    <div className="page-buttons">
      <div onClick={() => onSelectPage?.(0)}>&lt;&lt;</div>
      {buttons}
      <div onClick={() => onSelectPage?.(totalPages)}>&gt;&gt;</div>
      <small>Total: {total}</small>
    </div>
  );
}
