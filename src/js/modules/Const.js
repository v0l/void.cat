/**
 * Change Log
 * 1.0 - https://github.com/v0l/void.cat/commit/b0a49e5bc28d62ebd954fe344c6f604b952e905c
 * 1.1 - https://github.com/v0l/void.cat/commit/e3f7f2a59e86f86a27a06d8725f4f6401af7f190
 * 1.2 - TBC
 */

/**
 * @constant {string} - Stores the current app version
 */
export const AppVersion = require('../../../package.json').version;
/**
 * @constant {string} - The hashing algo to use to verify the file
 */
export const HashingAlgo = 'SHA-256';
/**
 * @constant {string} - The encryption algoritm to use for file uploads
 */
export const EncryptionAlgo = 'AES-CBC';
/**
 * @constant {object} - The 'algo' argument for importing/exporting/generating keys
 */
export const EncryptionKeyDetails = { name: EncryptionAlgo, length: 128 };
/**
 * @constant {object} - The 'algo' argument for importing/exporting/generating hmac keys
 */
export const HMACKeyDetails = { name: 'HMAC', hash: HashingAlgo };
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