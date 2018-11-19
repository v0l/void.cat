<?php
    class BlobFile {
        public $Version;
        public $Hash;
        public $Uploaded;
        
        public static function LoadHeader() : ?BlobFile {
            $input = fopen("php://input", "rb");
            $header = fread($input, 37); //1 version byte + 32 byte hash (64 hex digits) + 4 byte timestamp
            fclose($input);

            $header_data = unpack("C1version/H64hash256/Vuploaded", $header);
            if($header_data["version"] == 1){
                $bf = new BlobFile();
                $bf->Version = $header_data["version"];
                $bf->Hash = $header_data["hash256"];
                $bf->Uploaded = $header_data["uploaded"];
                return $bf;
            }

            return null;
        }
    }
?>