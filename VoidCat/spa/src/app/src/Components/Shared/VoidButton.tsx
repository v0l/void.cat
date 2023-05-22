import React, {MouseEvent, ReactNode, useEffect, useState} from "react";
import Icon from "./Icon";

interface VoidButtonProps {
    onClick?: (e: MouseEvent<HTMLDivElement>) => Promise<unknown> | unknown
    options?: {
        showSuccess: boolean
    }
    children: ReactNode
}

export function VoidButton(props: VoidButtonProps) {
    const options = {
        showSuccess: false,
        ...props.options
    };
    const [disabled, setDisabled] = useState(false);
    const [success, setSuccess] = useState(false);

    async function handleClick(e: MouseEvent<HTMLDivElement>) {
        if (disabled) return;
        setDisabled(true);

        let fn = props.onClick;
        try {
            if (typeof fn === "function") {
                const ret = fn(e);
                if (ret && typeof ret === "object" && "then" in ret) {
                    await (ret as Promise<unknown>);
                }
                setSuccess(options.showSuccess);
            }
        } catch (e) {
            console.error(e);
        }

        setDisabled(false);
    }

    useEffect(() => {
        if (success) {
            setTimeout(() => setSuccess(false), 1000);
        }
    }, [success]);

    return (
        <div className="flex-inline flex-center">
            <div>
                <div className={`btn${disabled ? " disabled" : ""}`} onClick={handleClick}>
                    {props.children}
                </div>
            </div>
            {success && <div><Icon name="check-circle"/></div>}
        </div>
    );
}