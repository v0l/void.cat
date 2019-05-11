/**
 * @constant {object} - Creates and decodes VBF file format
 */
const VBF = {
    Version: 2,

    /**
     * Creates a VBF file with the specified encryptedData using the current version
     * @param {BufferSource} hash
     * @param {BufferSource} encryptedData 
     * @param {number} version 
     * @returns {Uint8Array} VBF formatted file 
     */
    Create: function (hash, encryptedData, version) {
        version = typeof version === "number" ? version : VBF.Version;
        switch (version) {
            case 1:
                return VBF.CreateV1(hash, encryptedData);
            case 2:
                return VBF.CreateV2(hash, encryptedData);
        }
    },

    /**
     * Creates a VBF1 file with the specified encryptedData
     * @param {BufferSource} hash
     * @param {BufferSource} encryptedData
     * @param {number} version
     * @returns {Uint8Array} VBF formatted file
     */
    CreateV1: function (hash, encryptedData) {
        let upload_payload = new Uint8Array(37 + encryptedData.byteLength);

        let created = new ArrayBuffer(4);
        new DataView(created).setUint32(0, parseInt(new Date().getTime() / 1000), true);

        upload_payload[0] = 1; //blob version
        upload_payload.set(new Uint8Array(hash), 1);
        upload_payload.set(new Uint8Array(created), hash.byteLength + 1);
        upload_payload.set(new Uint8Array(encryptedData), 37);

        return upload_payload;
    },
    
    /**
     * Creates a VBF2 file with the specified encryptedData
     * @param {BufferSource} hash
     * @param {BufferSource} encryptedData
     * @param {number} version
     * @returns {Uint8Array} VBF formatted file
     */
    CreateV2: function (hash, encryptedData) {
        let header_len = 12;
        let upload_payload = new Uint8Array(header_len + encryptedData.byteLength + hash.byteLength);

        let created = new ArrayBuffer(4);
        new DataView(created).setUint32(0, parseInt(new Date().getTime() / 1000), true);

        upload_payload.set(new Uint8Array([0x02, 0x4f, 0x49, 0x44, 0xf0, 0x9f, 0x90, 0xb1]), 0);
        upload_payload.set(new Uint8Array(created), 8);
        upload_payload.set(new Uint8Array(encryptedData), header_len);
        upload_payload.set(new Uint8Array(hash), header_len + encryptedData.byteLength);

        return upload_payload;
    },

    /**
     * Creates the header part of VBF_V2
     * @param {BufferSource} hash
     * @param {BufferSource} encryptedData
     * @param {number} version
     * @returns {Uint8Array} VBF2 header
     */
    CreateV2Start: function () {
        let header_len = 12;
        let start = new Uint8Array(header_len);

        let created = new ArrayBuffer(4);
        new DataView(created).setUint32(0, parseInt(new Date().getTime() / 1000), true);

        start.set(new Uint8Array([0x02, 0x4f, 0x49, 0x44, 0xf0, 0x9f, 0x90, 0xb1]), 0);
        start.set(new Uint8Array(created), 8);

        return start;
    },

    /**
     * Returns the encrypted part of the VBF blob
     * @param {number} version 
     * @param {ArrayBuffer} blob 
     */
    GetEncryptedPart: function (version, blob) {
        switch (version) {
            case 1:
                return blob.slice(37);
            case 2:
                return blob.slice(12, blob.byteLength - 32);
        }
    },

    /**
     * Parses the header of the raw file
     * @param {ArrayBuffer} data - Raw data from the server
     * @returns {*} The header 
     */
    Parse: function (data) {
        let version = new Uint8Array(data)[0];
        if (version === 1) {
            let hmac = data.slice(1, 33);
            let uploaded = new DataView(data.slice(33, 37)).getUint32(0, true);

            return {
                version,
                hmac,
                uploaded,
                magic: null
            };
        } else if (version === 2) {
            let magic = data.slice(1, 8);
            let hmac = data.slice(data.byteLength - 32);
            let uploaded = new DataView(data.slice(8, 12)).getUint32(0, true);

            return {
                version,
                hmac,
                uploaded,
                magic
            };
        }
    }
};

export { VBF };