<?php
    class UploadResponse {
        public $status = 0;
        public $msg;
        public $pub_hash;
    }

    class Upload implements RequestHandler {
        public static $UploadFolderDefault = "out";

        private $isMultipart = False;
        private $MaxUploadSize = 104857600; //100MiB is the default upload size
        private $UploadFolder = NULL;
        private $PublicHashAlgo = "ripemd160";

        public function __construct(){
            $cfg = Config::MGetConfig(array('max_size', 'upload_folder', 'public_hash_algo'));
            
            if($cfg["max_size"] != False){
                $this->MaxUploadSize = $cfg["max_size"];
            }

            if($cfg["upload_folder"] != False){
                $this->UploadFolder = $cfg["upload_folder"];
            } else {
                $this->UploadFolder = self::$UploadFolderDefault;
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
            if(!file_exists("$_SERVER[DOCUMENT_ROOT]/$this->UploadFolder")){
                mkdir("$_SERVER[DOCUMENT_ROOT]/$this->UploadFolder");
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

                    //save upload
                    $this->SaveUpload($input, $bf->Hash, $pub_hash);

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

        function SaveUpload($input, $hash, $pub_hash) {
            $fs = new FileStore();
            $file_path = "$this->UploadFolder/$pub_hash";

            $fi = new FileInfo();
            $fi->PublicHash = $pub_hash;
            $fi->Hash = $hash;
            $fi->Path = $file_path;
            $fi->Uploaded = time();
            $fi->LastView = time();
            $fi->Views = 0;

            $fout = fopen("$_SERVER[DOCUMENT_ROOT]/$file_path", 'wb+');
            $fi->Size = stream_copy_to_stream($input, $fout);
            fclose($fout);

            $fs->SetPublicFileInfo($fi);
        }
    }
?>