<?php

    class FileStore {
        function __construct(){
            $this->Redis = StaticRedis::$Instance;
            $this->Db = Db::$Instance;
        }

        public function GetFileInfo($h160) {
            
        }
    }
?>