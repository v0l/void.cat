export interface IconProps {
  name: string;
  size?: number;
  className?: string;
  onClick?: React.MouseEventHandler;
}

const Icon = (props: IconProps) => {
  const size = props.size || 20;
  const href = "/icons.svg#" + props.name;

  return (
    <svg
      width={size}
      height={size}
      className={props.className}
      onClick={props.onClick}
    >
      <use href={href} />
    </svg>
  );
};

export function IconButton({ onClick, ...props }: IconProps) {
  return (
    <button
      onClick={onClick}
      className="p-2 bg-slate-800 rounded-xl hover:bg-slate-600"
    >
      <Icon {...props} />
    </button>
  );
}
export default Icon;
