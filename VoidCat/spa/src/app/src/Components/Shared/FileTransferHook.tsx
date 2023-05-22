import {useState} from "react";
import {RateCalculator} from "./RateCalculator";

export function useFileTransfer() {
    const [speed, setSpeed] = useState(0);
    const [progress, setProgress] = useState(0);
    const calc = new RateCalculator();

    return {
        speed, progress,
        setFileSize: (size: number) => {
            calc.SetFileSize(size);
        },
        update: (bytes: number) => {
            calc.ReportProgress(bytes);
            setSpeed(calc.GetSpeed());
            setProgress(calc.GetProgress());
        },
        loaded: (loaded: number) => {
            calc.ReportLoaded(loaded);
            setSpeed(calc.GetSpeed());
            setProgress(calc.GetProgress());
        },
        reset: () => calc.Reset()
    }
}