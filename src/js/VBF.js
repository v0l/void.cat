const VBF = {
    Version: 1,
    HeaderSize: 37,

    Create: function (hash, encryptedData) {
        //upload the encrypted file data
        let upload_payload = new Uint8Array(VBF.HeaderSize + encryptedData.byteLength);

        let created = new ArrayBuffer(4);
        new DataView(created).setUint32(0, parseInt(new Date().getTime() / 1000), true);

        upload_payload[0] = VBF.Version; //blob version
        upload_payload.set(new Uint8Array(hash), 1);
        upload_payload.set(new Uint8Array(created), hash.byteLength + 1);
        upload_payload.set(new Uint8Array(encryptedData), VBF.HeaderSize);

        return upload_payload;
    },

    /**
     * Parses the header of the raw file
     * @param {ArrayBuffer} data - Raw data from the server
     * @returns {*} The header 
     */
    Parse: function (data) {
        let version = new Uint8Array(data)[0];
        let hmac = data.slice(1, 33);
        let uploaded = new DataView(data.slice(33, 37)).getUint32(0, true);

        return {
            version,
            hmac,
            uploaded
        };
    }
};