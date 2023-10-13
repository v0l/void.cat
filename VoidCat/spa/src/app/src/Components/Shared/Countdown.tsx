import { useEffect, useState } from "react";

interface CountdownProps {
  to: number | string | Date;
  onEnded?: () => void;
}
export function Countdown({ onEnded, to }: CountdownProps) {
  const [time, setTime] = useState(0);

  useEffect(() => {
    const t = setInterval(() => {
      const toDate = new Date(to).getTime();
      const now = new Date().getTime();
      const seconds = (toDate - now) / 1000.0;
      setTime(Math.max(0, seconds));
      if (seconds <= 0 && typeof onEnded === "function") {
        onEnded();
      }
    }, 100);
    return () => clearInterval(t);
  }, []);

  return <div>{time.toFixed(1)}s</div>;
}
