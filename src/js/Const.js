/**
 * @constant {string} - Stores the current app version
 */
const AppVersion = "1.0";
/**
 * @constant {string} - The hashing algo to use to verify the file
 */
const HashingAlgo = 'SHA-256';
/**
 * @constant {string} - The encryption algoritm to use for file uploads
 */
const EncryptionAlgo = 'AES-CBC';
/**
 * @constant {object} - The 'algo' argument for importing/exporting/generating keys
 */
const EncryptionKeyDetails = { name: EncryptionAlgo, length: 128 };
/**
 * @constant {object} - The 'algo' argument for importing/exporting/generating hmac keys
 */
const HMACKeyDetails = { name: 'HMAC', hash: HashingAlgo };
/**
 * @constant {number} - Size of 1 kiB
 */
const kiB = Math.pow(1024, 1);
/**
 * @constant {number} - Size of 1 MiB
 */
const MiB = Math.pow(1024, 2);
/**
 * @constant {number} - Size of 1 GiB
 */
const GiB = Math.pow(1024, 3);
/**
 * @constant {number} - Size of 1 TiB
 */
const TiB = Math.pow(1024, 4);
/**
 * @constant {number} - Size of 1 PiB
 */
const PiB = Math.pow(1024, 5);
/**
 * @constant {number} - Size of 1 EiB
 */
const EiB = Math.pow(1024, 6);
/**
 * @constant {number} - Size of 1 ZiB
 */
const ZiB = Math.pow(1024, 7);
/**
 * @constant {number} - Size of 1 YiB
 */
const YiB = Math.pow(1024, 8);