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

            $abuse->CheckDownload($id);
            $tracking->TrackDownload($id);

            //allow embeded header from preflight check
            if($_SERVER["REQUEST_METHOD"] === "OPTIONS"){
                header("Access-Control-Allow-Origin: *");
                header("Access-Control-Allow-Headers: x-void-embeded");
            } else {
                if(isset($_SERVER['HTTP_X_VOID_EMBEDED'])) {
                    $this->SendFile($this->Fs->GetAbsoluteFilePath($id), 604800);
                } else {
                    $this->InternalNginxRedirect($this->Fs->GetRelativeFilePath($id), 604800);
                }
            }
        }

        function SendFile($location, $expire){
            header("Access-Control-Allow-Headers: x-void-embeded");
            header("Access-Control-Allow-Origin: *");
            header("Access-Control-Allow-Method: GET");
            header("Content-Type: application/octet-stream");
            header('Content-Length: ' . filesize($location));
            flush();
            readfile($location);
            exit();
        }

        function InternalNginxRedirect($location, $expire){
            header("Access-Control-Allow-Origin: *"); //this doesnt seem to work
            header("Content-Type: application/octet-stream");
            header("X-Accel-Redirect: /" . $location);
        }
    }
?>