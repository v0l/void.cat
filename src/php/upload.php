<?php
    class UploadResponse {
        public $status = 0;
        public $msg;
        public $id;
        public $sync;
    }

    class Upload implements RequestHandler {
        private $isMultipart = False;

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
            $rsp = new UploadResponse();
            $file_size = $_SERVER["CONTENT_LENGTH"];

            if($file_size > Config::$Instance->max_upload_size){
                $rsp->status = 1;
                $rsp->msg = "File is too large";
            } else {
                $bf = BlobFile::LoadHeader("php://input");

                if($bf != null){
                    //save upload
                    $id = $this->SaveUpload($bf);

                    //sync to other servers 
                    $rsp->sync = $this->SyncFileUpload($id);
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

        function SyncFileUpload($id) : array {
            $redis = StaticRedis::$Instance;
            $sync_hosts = $redis->sMembers(REDIS_PREFIX . 'sync-hosts');
            if($sync_hosts !== False) {
                $fs = new FileStore(Config::$Instance->upload_folder);

                $status_codes = [];
                foreach($sync_hosts as $host) {
                    $status_codes[] = Sync::SyncFile($id, $fs->GetAbsoluteFilePath($id), $host);
                }

                return $status_codes;
                /*
                $sync_threads = array();
                foreach($sync_hosts as $host) {
                    $new_thread = new SyncThread($id, $fs->GetAbsoluteFilePath($id), $host);
                    $new_thread->start();
                    $sync_threads[] = $new_thread;
                }

                foreach($sync_threads as $thread) {
                    while($thread->isRunning()) {
                        usleep(100);
                    }
                    $thread->join();
                }*/
            }

            return array();
        }

        function SaveUpload($bf) : string {
            $id = gmp_strval(gmp_init("0x" . hash(Config::$Instance->public_hash_algo, $bf->Hash)), 62);

            $fs = new FileStore(Config::$Instance->upload_folder);
            $fs->StoreFile("php://input", $id);

            return $id;
        }
    }
?>