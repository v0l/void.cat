<?php
    class BlobFile {
        public $Version;
        public $Hash;

        public static function LoadHeader($stream) : ?BlobFile {
            $header = fread($stream, 33); //1 version byte + 32 byte hash (64 hex digits)
            rewind($stream);

            $header_data = unpack("C1version/H64hash256", $header);
            if($header_data["version"] == 1){
                $bf = new BlobFile();
                $bf->Version = $header_data["version"];
                $bf->Hash = $header_data["hash256"];
                return $bf;
            }

            return null;
        }
    }
?>