<?php
    class Download implements RequestHandler {

        function __construct(){
            $this->Abuse = new Abuse();
            $this->Tracking = new Tracking();
            $this->FileStore = new FileStore();
        }
        
        public function HandleRequest() : void {
            if(isset($_REQUEST["hash"])){
                $hash = $_REQUEST["hash"];

                $file_info = $this->FileStore->GetPublicFileInfo($hash);
                if($file_info != NULL){
                    var_dump($file_info);
                } else {
                    http_response_code(404);
                    exit();
                }
            } else {
                http_response_code(404);
                exit();
            }
        }

        function StartDownload(){
            
        }
    }
?>