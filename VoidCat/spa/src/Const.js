import preval from "preval.macro";

export const ApiHost = preval`module.exports = process.env.API_HOST || '';`;

export const DefaultAvatar = "https://i.imgur.com/8A5Fu65.jpeg";

/**
 * @constant {number} - Size of 1 kiB
 */
export const kiB = Math.pow(1024, 1);
/**
 * @constant {number} - Size of 1 MiB
 */
export const MiB = Math.pow(1024, 2);
/**
 * @constant {number} - Size of 1 GiB
 */
export const GiB = Math.pow(1024, 3);
/**
 * @constant {number} - Size of 1 TiB
 */
export const TiB = Math.pow(1024, 4);
/**
 * @constant {number} - Size of 1 PiB
 */
export const PiB = Math.pow(1024, 5);
/**
 * @constant {number} - Size of 1 EiB
 */
export const EiB = Math.pow(1024, 6);
/**
 * @constant {number} - Size of 1 ZiB
 */
export const ZiB = Math.pow(1024, 7);
/**
 * @constant {number} - Size of 1 YiB
 */
export const YiB = Math.pow(1024, 8);

export const PaywallCurrencies = {
    BTC: 0,
    USD: 1,
    EUR: 2,
    GBP: 3
}

export const PaywallServices = {
    None: 0,
    Strike: 1
}

export const PaywallOrderState = {
    Unpaid: 0,
    Paid: 1,
    Expired: 2
}

export const PagedSortBy = {
    Name: 0,
    Date: 1,
    Size: 2,
    Id: 3
}

export const PageSortOrder = {
    Asc: 0,
    Dsc: 1
}

export const UserFlags = {
    PublicProfile: 1,
    PublicUploads: 2,
    EmailVerified: 4
}