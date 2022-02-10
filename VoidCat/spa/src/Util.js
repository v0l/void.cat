import * as Const from "./Const";
/**
 * Formats bytes into binary notation
 * @param {number} b - The value in bytes
 * @param {number} [f=2] - The number of decimal places to use
 * @returns {string} Bytes formatted in binary notation
 */
export function FormatBytes(b, f) {
    f = typeof f === 'number' ? 2 : f;
    if (b >= Const.YiB)
        return (b / Const.YiB).toFixed(f) + ' YiB';
    if (b >= Const.ZiB)
        return (b / Const.ZiB).toFixed(f) + ' ZiB';
    if (b >= Const.EiB)
        return (b / Const.EiB).toFixed(f) + ' EiB';
    if (b >= Const.PiB)
        return (b / Const.PiB).toFixed(f) + ' PiB';
    if (b >= Const.TiB)
        return (b / Const.TiB).toFixed(f) + ' TiB';
    if (b >= Const.GiB)
        return (b / Const.GiB).toFixed(f) + ' GiB';
    if (b >= Const.MiB)
        return (b / Const.MiB).toFixed(f) + ' MiB';
    if (b >= Const.kiB)
        return (b / Const.kiB).toFixed(f) + ' KiB';
    return b.toFixed(f) + ' B'
}

export function buf2hex(buffer) {
    return [...new Uint8Array(buffer)].map(x => x.toString(16).padStart(2, '0')).join('');
}

export function ConstName(type, val) {
    for(let [k, v] of Object.entries(type)) {
        if(v === val) {
            return k;
        }
    }
}