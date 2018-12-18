<?php
    class Download implements RequestHandler {
        private $Config;
        private $Fs;

        public function __construct() {
            Config::LoadConfig(array("upload_folder", "download_captcha_check"));
        }

        public function HandleRequest() : void {
            $ref = isset($_SERVER["HTTP_REFERER"]) ? $_SERVER["HTTP_REFERER"] : null;
            $id = isset($_REQUEST["id"]) ? $_REQUEST["id"] : null;

            if($ref === null && $id !== null) {
                header("location: /#$id");
            } else if($id !== null) {
                $this->Fs = new FileStore(Config::$Instance->upload_folder);
                if($this->Fs->FileExists($id)) {
                    $this->StartDownload($id, $this->Fs->GetFileInfo($id));
                } else {
                    http_response_code(404);
                }
            } else {
                http_response_code(404);
            }
        }

        function StartDownload($id, $info) : void {
            $abuse = new Abuse();
            $tracking = new Tracking();

            header("Cache-Control: private");
            if(isset($_SERVER["HTTP_ORIGIN"])) {
                header("Access-Control-Allow-Origin: " . $_SERVER["HTTP_ORIGIN"]);
                header("Access-Control-Allow-Method: GET");
            }

            $abuse->CheckDownload($id);
            $tracking->TrackDownload($this->Fs, $id);

            if($_SERVER["REQUEST_METHOD"] === "GET") {
                $this->InternalNginxRedirect($this->Fs->GetRelativeFilePath($id), 604800, $info);
            }
        }

        function InternalNginxRedirect($location, $expire, $info) : void {
            header("X-Accel-Redirect: /" . $location);
            if($info->IsLegacyUpload) {
                header("Content-Type: $info->LegacyMime");
                header("Content-Disposition: inline; filename=\"$info->LegacyFilename\"");
            } else {
                header("Content-Type: application/octet-stream");
            }
        }
    }
?>