<?php
    class Download implements RequestHandler {
        private $Config;

        public function HandleRequest() : void {
            $this->Config = Config::MGetConfig(array('upload_folder'));
            
            if($this->Config->upload_folder == FALSE){
                $this->Config->upload_folder = Upload::$UploadFolderDefault; 
            }

            $fs = new FileStore($this->Config->upload_folder);
            if(isset($_REQUEST["id"])){
                $id = $_REQUEST["id"];

                $file_info = $fs->GetPublicFileInfo($id);
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
            $this->InternalNginxRedirect($this->Config->upload_folder . '/' . $file_info->FileId, 604800);
        }

        function InternalNginxRedirect($location, $expire){
            //var_dump($location);
            header("X-Accel-Redirect: /" . $location);
			//header("Cache-Control: public, max-age=$expire");
        }
    }
?>