<?php
    class FileStore {
        private $UploadFolder;

        public function __construct($path) {
            if($path !== FALSE){
                $this->UploadFolder = $path;
            } else {
                $this->UploadFolder = Upload::$UploadFolderDefault;
            }
        }

        public function SetPublicFileInfo($info) : void {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $info->FileId;

            $redis->hMSet($file_key, array(
                'views' => $info->Views,
                'lastview' => $info->LastView
            ));
        }

        public function GetPublicFileInfo($id) : ?FileInfo {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $id;

            $public_file_info = $redis->hMGet($file_key, array('views', 'lastview'));
            if($public_file_info['views'] != False){
                $file_stat = stat("$_SERVER[DOCUMENT_ROOT]/$this->UploadFolder/$id");

                $file = new FileInfo();
                $file->FileId = $id;
                $file->Views = intval($public_file_info['views']);
                $file->LastView = intval($public_file_info['lastview']);
                $file->Size = $file_stat["size"];
                $file->Uploaded = $file_stat["ctime"];
                
                return $file;
            } 

            return NULL;
        }

        public function FileExists($id) : Boolean {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $id;
            return $redis->hExists($file_key, 'views');
        }
    }
?>