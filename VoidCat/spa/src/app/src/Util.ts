import * as Const from "@/Const";

/**
 * Formats bytes into binary notation
 * @param b The value in bytes
 * @param f The number of decimal places to use
 * @returns Bytes formatted in binary notation
 */
export function FormatBytes(b: number, f?: number) {
  f ??= 2;
  if (b >= Const.YiB) return (b / Const.YiB).toFixed(f) + " YiB";
  if (b >= Const.ZiB) return (b / Const.ZiB).toFixed(f) + " ZiB";
  if (b >= Const.EiB) return (b / Const.EiB).toFixed(f) + " EiB";
  if (b >= Const.PiB) return (b / Const.PiB).toFixed(f) + " PiB";
  if (b >= Const.TiB) return (b / Const.TiB).toFixed(f) + " TiB";
  if (b >= Const.GiB) return (b / Const.GiB).toFixed(f) + " GiB";
  if (b >= Const.MiB) return (b / Const.MiB).toFixed(f) + " MiB";
  if (b >= Const.kiB) return (b / Const.kiB).toFixed(f) + " KiB";
  return b.toFixed(f) + " B";
}

export function buf2hex(buffer: number[] | ArrayBuffer) {
  return [...new Uint8Array(buffer)]
    .map((x) => x.toString(16).padStart(2, "0"))
    .join("");
}

export function ConstName(type: object, val: any) {
  for (let [k, v] of Object.entries(type)) {
    if (v === val) {
      return k;
    }
  }
}

export function FormatCurrency(value: number, currency: string | number) {
  switch (currency) {
    case 0:
    case "BTC": {
      let hasDecimals = value % 1 > 0;
      return `₿${value.toLocaleString(undefined, {
        minimumFractionDigits: hasDecimals ? 8 : 0, // Sats
        maximumFractionDigits: 11, // MSats
      })}`;
    }
    case 1:
    case "USD": {
      return value.toLocaleString(undefined, {
        style: "currency",
        currency: "USD",
      });
    }
    case 2:
    case "EUR": {
      return value.toLocaleString(undefined, {
        style: "currency",
        currency: "EUR",
      });
    }
    case 3:
    case "GBP": {
      return value.toLocaleString(undefined, {
        style: "currency",
        currency: "GBP",
      });
    }
  }
  return value.toString();
}
