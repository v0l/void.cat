<?php
    class UploadResponse {
        public $status = 0;
        public $msg;
        public $id;
        public $sync;
    }

    class Upload implements RequestHandler {
        private $IsMultipart = False;

        public function __construct() {
            Config::LoadConfig(array('max_upload_size', 'upload_folder', 'public_hash_algo'));
            
            //set php params
            set_time_limit(1200);
            ini_set('post_max_size', Config::$Instance->max_upload_size);
            ini_set('upload_max_filesize', Config::$Instance->max_upload_size);
            ini_set('memory_limit', Config::$Instance->max_upload_size);
            ini_set('enable_post_data_reading', 0);

            //check upload dir exists
            if(!file_exists("$_SERVER[DOCUMENT_ROOT]/" . Config::$Instance->upload_folder)){
                mkdir("$_SERVER[DOCUMENT_ROOT]/" . Config::$Instance->upload_folder);
            }
        }

        public function HandleRequest() : void {
            if(isset($_SERVER["HTTP_ORIGIN"])) {
                header("Access-Control-Allow-Origin: " . $_SERVER["HTTP_ORIGIN"]);
                header("Access-Control-Allow-Method: POST,OPTIONS");
				header("Access-Control-Allow-Headers: Content-Type");
            }

            $rsp = new UploadResponse();
            $file_size = $_SERVER["CONTENT_LENGTH"];

            if($file_size > Config::$Instance->max_upload_size){
                $rsp->status = 1;
                $rsp->msg = "File is too large";
            } else {
                $auth = new Auth();
                $token = $auth->GetBearerToken();

                if($token !== null) {
                    if($auth->CheckApiToken($token)) {
                        $id = $this->SaveLegacyUpload();

                        //sync to other servers 
                        if($id !== null) {
                            $rsp->sync = $this->SyncFileUpload($id);
                            $rsp->status = 200;
                            $rsp->id = $id;
                        } else {
                            $rsp->status = 3;
                            $rsp->msg = "Legacy upload error";
                        }
                    } else {
                        http_response_code(403);
                        exit();
                    }
                } else {
                    $read_from = "php://input";
                    $bf = BlobFile::LoadHeader($read_from);

                    if($bf != null){
                        //save upload
                        $id = $this->SaveUpload($bf, $read_from);

                        //sync to other servers 
                        if($id == null) {
                            $rsp->status = 4;
                            $rsp->msg = "Invalid VBF or file already exists";
                        } else {
                            $rsp->sync = $this->SyncFileUpload($id);
                            $rsp->status = 200;
                            $rsp->id = $id;
                        }
                    } else {
                        $rsp->status = 2;
                        $rsp->msg = "Invalid file header";
                    }
                }
            }
            header('Content-Type: application/json');
            echo json_encode($rsp);
        }

        function SyncFileUpload($id) : array {
            $redis = StaticRedis::ReadOp();
            $sync_hosts = $redis->sMembers(REDIS_PREFIX . 'sync-hosts');
            if($sync_hosts !== False) {
                $fs = new FileStore(Config::$Instance->upload_folder);

                $status_codes = [];
                foreach($sync_hosts as $host) {
                    $status_codes[] = Sync::SyncFile($id, $fs->GetAbsoluteFilePath($id), $host);
                }

                return $status_codes;
            }

            return array();
        }

        function SaveUpload($bf, $rf) : ?string {
            $fs = new FileStore(Config::$Instance->upload_folder);
            switch($bf->Version) {
                case 1:
                    return $fs->StoreV1File($bf, $rf);
                case 2:
                    return $fs->StoreV2File($bf, $rf);
            }
            return null;
        }

        function SaveLegacyUpload() : ?string {
            if(isset($_SERVER["HTTP_X_LEGACY_FILENAME"])){
                $hash = hash_file("sha256", "php://input");
                $id = gmp_strval(gmp_init("0x" . hash(Config::$Instance->public_hash_algo, $hash)), 62);

                $fs = new FileStore(Config::$Instance->upload_folder);
                $fs->StoreFile(fopen("php://input", "rb"), $id);

                $info = new FileInfo();
                $info->FileId = $id;
                $info->LegacyFilename = $_SERVER["HTTP_X_LEGACY_FILENAME"];
                $info->LegacyMime = isset($_SERVER["CONTENT_TYPE"]) ? $_SERVER["CONTENT_TYPE"] : "application/octet-stream";

                $fs->SetAsLegacyFile($info);
                return $id;
            }
            return null;
        }

        public static function GetUploadHost() : string {
            $cont = geoip_continent_code_by_name(USER_IP);
            if($cont === False){
                $cont = "EU";
            }

            $redis = StaticRedis::ReadOp();
            $map = $redis->hGetAll(REDIS_PREFIX . "upload-region-mapping");
            if($map !== False && isset($map[$cont])) {
                return $map[$cont];
            } else {
                return $_SERVER["HTTP_HOST"];
            }
        }
    }
?>