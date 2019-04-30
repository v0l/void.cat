<?php
    class SyncThread extends Thread {
        private $Destination;
        private $FilePath;
        private $Id;

        public function __constrct($id, $filepath, $host){
            $this->Id = $id;
            $this->FilePath = $filepath;
            $this->Destination = $host;
        }

        public function run() {
            Sync::SyncFile($this->Id, $this->FilePath, $this->Destination);
        }
    }

?>