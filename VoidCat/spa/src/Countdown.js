import {useEffect, useState} from "react";

export function Countdown(props) {
    const [time, setTime] = useState(0);
    const onEnded = props.onEnded;

    useEffect(() => {
        let t = setInterval(() => {
            let to = new Date(props.to).getTime();
            let now = new Date().getTime();
            let seconds = (to - now) / 1000.0;
            setTime(Math.max(0, seconds));
            if (seconds <= 0 && typeof onEnded === "function") {
                onEnded();
            }
        }, 100);
        return () => clearInterval(t);
    }, [])

    return <div>{time.toFixed(1)}s</div>
}