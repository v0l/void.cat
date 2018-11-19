<?php
    class UploadResponse {
        public $status = 0;
        public $msg;
        public $id;
    }

    class Upload implements RequestHandler {
        public static $UploadFolderDefault = "out";

        private $Config;
        private $isMultipart = False;
        private $MaxUploadSize = 104857600; //100MiB is the default upload size
        private $UploadFolder = NULL;
        private $PublicHashAlgo = "ripemd160";

        public function __construct() {
            $this->Config = Config::MGetConfig(array('max_size', 'upload_folder', 'public_hash_algo'));
            
            if($this->Config->max_size !== False){
                $this->MaxUploadSize = $this->Config->max_size;
            }

            if($this->Config->upload_folder !== False){
                $this->UploadFolder = $this->Config->upload_folder;
            } else {
                $this->UploadFolder = self::$UploadFolderDefault;
            }

            if($this->Config->public_hash_algo !== False){
                $this->PublicHashAlgo = $this->Config->public_hash_algo;
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
                $bf = BlobFile::LoadHeader();

                if($bf != null){
                    //save upload
                    $id = $this->SaveUpload($bf);

                    //sync to other servers 
                    $this->SyncFileUpload($id);

                    $rsp->status = 200;
                    $rsp->id = $id;
                } else {
                    $rsp->status = 2;
                    $rsp->msg = "Invalid file header";
                }
            }
            header('Content-Type: application/json');
            echo json_encode($rsp);
        }

        function SyncFileUpload($id) : void {

        }

        function SaveUpload($bf) : string {
            $id = gmp_strval(gmp_init("0x" . hash($this->PublicHashAlgo, $bf->Hash)), 62);

            $fs = new FileStore($this->UploadFolder);
            $file_path = "$this->UploadFolder/$id";

            $fi = new FileInfo();
            $fi->FileId = $id;
            $fi->LastView = time();
            $fi->Views = 1;

            $input = fopen("php://input", "rb");
            $fout = fopen("$_SERVER[DOCUMENT_ROOT]/$file_path", 'wb+');
            $fi->Size = stream_copy_to_stream($input, $fout);
            fclose($fout);
            fclose($input);

            $fs->SetPublicFileInfo($fi);

            return $id;
        }
    }
?>