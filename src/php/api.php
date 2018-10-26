<?php
    class ApiResponse {
        public $ok = false;
        public $msg;
        public $data;
        public $cmd;
    }

    class Api implements RequestHandler {
        public function __construct(){
            ini_set('enable_post_data_reading', 0);
        }

        public function HandleRequest() : void {
            $cmd = json_decode(file_get_contents("php://input"));

            $rsp = new ApiResponse();
            $rsp->cmd = $cmd;

            $fs = new FileStore();

            switch($cmd->cmd){
                case "file_info":{
                    $rsp->ok = true;
                    $rsp->data = $fs->GetPublicFileInfo($cmd->hash); 
                    break;
                }
            }

            header('Content-Type: application/json');
            echo json_encode($rsp);
        }

    }

?>