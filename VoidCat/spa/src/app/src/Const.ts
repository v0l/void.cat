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