interface RateReport {
    time: number
    amount: number
}

export class RateCalculator {
    #reports: Array<RateReport> = [];
    #lastLoaded = 0;
    #progress = 0;
    #speed = 0;
    #fileSize = 0;

    constructor() {
        this.Reset();
    }

    SetFileSize(size: number) {
        this.#fileSize = size;
    }

    GetProgress() {
        return this.#progress;
    }

    GetSpeed() {
        return this.#speed;
    }

    Reset() {
        this.#reports = [];
        this.#lastLoaded = 0;
        this.#progress = 0;
        this.#speed = 0;
    }

    ReportProgress(amount: number) {
        this.#reports.push({
            time: new Date().getTime(),
            amount
        });
        this.#lastLoaded += amount;
        this.#progress = this.#lastLoaded / this.#fileSize;
        this.#speed = this.RateWindow(5);
    }

    ReportLoaded(loaded: number) {
        this.#reports.push({
            time: new Date().getTime(),
            amount: loaded - this.#lastLoaded
        });
        this.#lastLoaded = loaded;
        this.#progress = this.#lastLoaded / this.#fileSize;
        this.#speed = this.RateWindow(5);
    }

    RateWindow(s: number) {
        let total = 0.0;

        const windowStart = new Date().getTime() - (s * 1000);
        for (let r of this.#reports) {
            if (r.time >= windowStart) {
                total += r.amount;
            }
        }

        return total / s;
    }
}