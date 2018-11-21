<?php
    class FileStore {
        private $UploadFolder;

        public function __construct($path) {
            $this->UploadFolder = $path;
        }

        public function SetFileStats($info) : void {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $info->FileId;

            $redis->hMSet($file_key, array(
                'views' => $info->Views,
                'lastview' => $info->LastView
            ));
        }

        public function GetFileStats($id) : object {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $id;

            $public_file_info = $redis->hMGet($file_key, array('views', 'lastview'));
            return (object)array(
                "views" => ($public_file_info["views"] !== False ? $public_file_info["views"] : 0),
                "lastview" => ($public_file_info["lastview"] !== False ? $public_file_info["lastview"] : 0)
            );
        }

        public function GetRelativeFilePath($id) : string {
            return "$this->UploadFolder/$id";
        }

        public function GetAbsoluteFilePath($id) : string {
            return "$_SERVER[DOCUMENT_ROOT]/$this->UploadFolder/$id";
        }

        public function GetFileInfo($id) : ?FileInfo {
            $file_path = $this->GetAbsoluteFilePath($id);
            if($this->FileExists($id)) {
                $stats = $this->GetFileStats($id);
                $file_stat = stat($file_path);

                $file = new FileInfo();
                $file->FileId = $id;
                $file->Views = intval($stats->views);
                $file->LastView = intval($stats->lastview);
                $file->Size = $file_stat["size"];
                $file->Uploaded = $file_stat["ctime"];
                
                return $file;
            }
            return NULL;
        }

        public function FileExists($id) : bool {
            $file_path = $this->GetAbsoluteFilePath($id);
            return file_exists($file_path);
        }

        public function StoreFile($file, $id) {
            $file_path = $this->GetAbsoluteFilePath($id);

            $input = fopen($file, "rb");
            $fout = fopen($file_path, 'wb+');
            stream_copy_to_stream($input, $fout);
            fclose($fout);
            fclose($input);
        }
    }
?>