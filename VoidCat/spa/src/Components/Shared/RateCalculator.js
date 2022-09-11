export class RateCalculator {
    constructor() {
        this.Reset();
        this.fileSize = 0;
    }

    SetFileSize(size) {
        this.fileSize = size;
    }
    
    GetProgress() {
        return this.progress;
    }
    
    GetSpeed() {
        return this.speed;
    }
    
    Reset() {
        this.reports = [];
        this.lastLoaded = 0;
        this.progress = 0;
        this.speed = 0;
    }
    
    ReportProgress(amount) {
        this.reports.push({
            time: new Date().getTime(),
            amount
        });
        this.lastLoaded += amount;
        this.progress = this.lastLoaded / parseFloat(this.fileSize);
        this.speed = this.RateWindow(5);
    }

    ReportLoaded(loaded) {
        this.reports.push({
            time: new Date().getTime(),
            amount: loaded - this.lastLoaded
        });
        this.lastLoaded = loaded;
        this.progress = this.lastLoaded / parseFloat(this.fileSize);
        this.speed = this.RateWindow(5);
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