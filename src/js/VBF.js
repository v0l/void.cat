const VBF = {
    Version: 2,

    Create: function (hash, encryptedData, version) {
        version = typeof version === "number" ? version : VBF.Version;
        switch (version) {
            case 1:
                return VBF.CreateV1(hash, encryptedData);
            case 2:
                return VBF.CreateV2(hash, encryptedData);
        }
    },

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

    CreateV2: function (hash, encryptedData) {
        let upload_payload = new Uint8Array(9 + encryptedData.byteLength + hash.byteLength);

        let created = new ArrayBuffer(4);
        new DataView(created).setUint32(0, parseInt(new Date().getTime() / 1000), true);

        upload_payload[0] = 2; //blob version
        upload_payload.set(new TextEncoder().encode("VOID"), 1);
        upload_payload.set(new Uint8Array(created), 5);
        upload_payload.set(new Uint8Array(encryptedData), 9);
        upload_payload.set(new Uint8Array(hash), 9 + encryptedData.byteLength);

        return upload_payload;
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
                return blob.slice(9, blob.byteLength - 32);
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
            let magic = data.slice(1, 5);
            let hmac = data.slice(data.byteLength - 32);
            let uploaded = new DataView(data.slice(5, 9)).getUint32(0, true);

            return {
                version,
                hmac,
                uploaded,
                magic
            };
        }
    }
};