<?php
    class FileStore {
        public function SetPublicFileInfo($info) : void {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $info->PublicHash;

            $redis->hMSet($file_key, array(
                'path' => $info->Path,
                'views' => $info->Views,
                'uploaded' => $info->Uploaded,
                'lastview' => $info->LastView,
                'size' => $info->Size,
                'hash' => $info->Hash
            ));
        }

        public function GetPublicFileInfo($public_hash) : ?FileInfo {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $public_hash;

            $public_file_info = $redis->hMGet($file_key, array('path', 'hash', 'views', 'uploaded', 'lastview', 'size'));
            if($public_file_info['path'] != False){
                $file = new FileInfo();
                $file->PublicHash = $public_hash;
                $file->Hash = $public_file_info['hash'];
                $file->Path = $public_file_info['path'];
                $file->Views = intval($public_file_info['views']);
                $file->Uploaded = intval($public_file_info['uploaded']);
                $file->LastView = intval($public_file_info['lastview']);
                $file->Size = intval($public_file_info['size']);

                return $file;
            } 

            return NULL;
        }

        public function FileExists($public_hash) : Boolean {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $public_hash;
            return $redis->hExists($file_key, 'path');
        }
    }
?>