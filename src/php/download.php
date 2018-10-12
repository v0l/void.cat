<?php
    class Download implements RequestHandler {

        function __construct(){
            $this->Redis = StaticRedis::$Instance;
            $this->Config = Config::$Instance;
            $this->Db = Db::$Instance;

            $this->Abuse = new Abuse();
            $this->Tracking = new Tracking();
            $this->FileStore = new FileStore();

            $this->Redis->pconnect($this->Config->Redis);
        }
        
        public function HandleRequest() {
            if(isset($_REQUEST["hash"])){
                $hash = $_REQUEST["hash"];

                $fi = $this->FileStore->GetFileInfo($hash);
            } else {
                http_response_code(404);
                exit();
            }
        }

        function StartDownload($req){

        }
    }
?>