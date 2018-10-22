<?php
    class UploadResponse {
        public $status = 0;
        public $msg;
        public $pub_hash;
    }

    class Upload implements RequestHandler {

        private $isMultipart = False;
        private $MaxUploadSize = 104857600; //100MiB is the default upload size
        private $UploadPath = NULL;
        private $PublicHashAlgo = "ripemd160";

        public function __construct(){
            $cfg = Config::MGetConfig(array('max_size', 'upload_path', 'public_hash_algo'));
            
            if($cfg["max_size"] != False){
                $this->MaxUploadSize = $cfg["max_size"];
            }

            if($cfg["upload_path"] != False){
                $this->UploadPath = $cfg["upload_path"];
            } else {
                $this->UploadPath = $_SERVER["DOCUMENT_ROOT"] . "/out";
            }

            if($cfg["public_hash_algo"] != False){
                $this->PublicHashAlgo = $cfg["public_hash_algo"];
            }

            //set php params
            set_time_limit(1200);
            ini_set('post_max_size', $this->MaxUploadSize);
            ini_set('upload_max_filesize', $this->MaxUploadSize);
            ini_set('memory_limit', $this->MaxUploadSize);
            ini_set('enable_post_data_reading', 0);

            //check upload dir exists
            if(!file_exists($this->UploadPath)){
                mkdir($this->UploadPath);
            }
        }

        public function HandleRequest() : void {
            $rsp = new UploadResponse();
            $file_size = $_SERVER["CONTENT_LENGTH"];

            if($file_size > $this->MaxUploadSize){
                $rsp->status = 1;
                $rsp->msg = "File is too large";
            } else {
                $input = fopen("php://input", "rb");
                $bf = BlobFile::LoadHeader($input);

                if($bf != null){
                    //generate public hash
                    $pub_hash = hash($this->PublicHashAlgo, $bf->Hash);

                    //sync to other servers 
                    $this->SyncFileUpload($input);

                    $rsp->status = 200;
                    $rsp->pub_hash = $pub_hash;
                } else {
                    $rsp->status = 2;
                    $rsp->msg = "Invalid file header";
                }
            }
            header('Content-Type: application/json');
            echo json_encode($rsp);
        }

        function SyncFileUpload() {

        }
    }
?>