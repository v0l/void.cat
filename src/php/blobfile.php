<?php
    class BlobFile {
        public $Version;
        public $Hash;
        public $Uploaded;
        
        public static function LoadHeader($path) : ?BlobFile {
            $input = fopen($path, "rb");
            $version = ord(fread($input, 1));
            //error_log($version);

            $bf = new BlobFile();
            if($version == 1) {
                $header = fread($input, 36); //+32 byte hash (64 hex digits) + 4 byte timestamp
                fclose($input);
                $header_data = unpack("H64hash256/Vuploaded", $header);

                $bf->Version = 1;
                $bf->Hash = $header_data["hash256"];
                $bf->Uploaded = $header_data["uploaded"];
                return $bf;
            } elseif($version == 2) {
                $header = fread($input, 11); //+7 magic bytes + 4 byte timestamp
                $header_data = unpack("H14magic/Vuploaded", $header);
                fclose($input);

                //error_log("Magic is: " . $header_data["magic"]);
                if($header_data["magic"] == "4f4944f09f90b1") { //OID🐱 as hex (UTF-8)
                    $bf->Version = 2;
                    $bf->Uploaded = $header_data["uploaded"];
                    return $bf;
                }
            } else {
                fclose($input);
            }

            return null;
        }
    }
?>