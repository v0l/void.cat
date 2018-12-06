<?php
    class Download implements RequestHandler {
        private $Config;
        private $Fs;

        public function __construct() {
            Config::LoadConfig(array("upload_folder", "download_captcha_check"));
        }

        public function HandleRequest() : void {
            $this->Fs = new FileStore(Config::$Instance->upload_folder);
            if(isset($_REQUEST["id"])){
                $id = $_REQUEST["id"];

                if($this->Fs->FileExists($id)){
                    $this->StartDownload($id);
                } else {
                    http_response_code(404);
                    exit();
                }
            } else {
                http_response_code(404);
                exit();
            }
        }

        function StartDownload($id){
            $abuse = new Abuse();
            $tracking = new Tracking();

            header("Cache-Control: private");
            header("Access-Control-Allow-Origin: " . $_SERVER["HTTP_ORIGIN"]);
            header("Access-Control-Allow-Method: GET");

            $abuse->CheckDownload($id);
            $tracking->TrackDownload($this->Fs, $id);

            //allow embeded header from preflight check
            if($_SERVER["REQUEST_METHOD"] === "GET") {
                $this->InternalNginxRedirect($this->Fs->GetRelativeFilePath($id), 604800);
            }
        }

        function InternalNginxRedirect($location, $expire){
            header("Content-Type: application/octet-stream");
            header("X-Accel-Redirect: /" . $location);
        }
    }
?>