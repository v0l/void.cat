<?php
    class FileStore {
        public function GetPublicFileInfo($public_hash) : FileInfo {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $public_hash;

            $public_file_info = $redis->hMGet($file_key, array('path', 'views', 'uploaded', 'lastview', 'size'));
            if($public_file_info['path'] != False){
                $file = new FileInfo();
                $file->Path = $public_file_info['path'];
                $file->Views = $public_file_info['views'];
                $file->Uploaded = $public_file_info['uploaded'];
                $file->LastView = $public_file_info['lastview'];
                $file->Size = $public_file_info['size'];

                return $file;
            } 

            return NULL;
        }

        public function FileExists($public_hash) {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $public_hash;
            return $redis->hExists($file_key, 'path');
        }
    }
?>