<?php
    class Download implements RequestHandler {
        public function HandleRequest() : void {
            $fs = new FileStore();
            if(isset($_REQUEST["hash"])){
                $hash = $_REQUEST["hash"];

                $file_info = $fs->GetPublicFileInfo($hash);
                if($file_info != NULL){
                    $this->StartDownload($file_info);
                } else {
                    http_response_code(404);
                    exit();
                }
            } else {
                http_response_code(404);
                exit();
            }
        }

        function StartDownload($file_info){
            $abuse = new Abuse();
            $tracking = new Tracking();

            //pass to nginx to handle download
            $this->InternalNginxRedirect($file_info->Path, 604800);
        }

        function InternalNginxRedirect($location, $expire){
            //var_dump($location);
            header("X-Accel-Redirect: /" . $location);
			//header("Cache-Control: public, max-age=$expire");
        }
    }
?>