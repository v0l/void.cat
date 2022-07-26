export class RateCalculator {
    constructor() {
        this.reports = [];
        this.lastLoaded = 0;
    }

    ResetLastLoaded() {
        this.lastLoaded = 0;
    }
    
    ReportProgress(amount) {
        this.reports.push({
            time: new Date().getTime(),
            amount
        });
    }

    ReportLoaded(loaded) {
        this.reports.push({
            time: new Date().getTime(),
            amount: loaded - this.lastLoaded
        });
        this.lastLoaded = loaded;
    }

    RateWindow(s) {
        let total = 0.0;

        let windowStart = new Date().getTime() - (s * 1000);
        for (let r of this.reports) {
            if (r.time >= windowStart) {
                total += r.amount;
            }
        }

        return total / s;
    }
}