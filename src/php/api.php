<?php
    class ApiResponse {
        public $ok = false;
        public $msg;
        public $data;
    }

    class Api implements RequestHandler {
        public function __construct(){
            ini_set('enable_post_data_reading', 0);
        }

        public function HandleRequest() : void {
            $cmd = json_decode(file_get_contents("php://input"));

            $rsp = new ApiResponse();

            header('Content-Type: application/json');
            echo json_encode($rsp);
        }
        
        public function GetStats() {
            
        }
    }

?>