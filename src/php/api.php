<?php
    class ApiResponse {
        public $ok = false;
        public $msg;
        public $data;
        public $cmd;
    }

    class Api implements RequestHandler {

        public function __construct(){
            Config::LoadConfig(array('max_upload_size', 'upload_folder', 'recaptcha_site_key', 'recaptcha_secret'));

            ini_set('enable_post_data_reading', 0);
        }

        public function HandleRequest() : void {
            $cmd = json_decode(file_get_contents("php://input"));
            
            $rsp = new ApiResponse();
            $rsp->cmd = $cmd;

            $fs = new FileStore(Config::$Instance->upload_folder);

            switch($cmd->cmd){
                case "site_info": {
                    $rsp->ok = true;
                    $rsp->data = array(
                        "max_upload_size" => Config::$Instance->max_upload_size,
                        "basic_stats" => Stats::Get(),
                        "upload_host" => Upload::GetUploadHost(),
                        "geoip_info" => geoip_database_info(),
                        "host" => gethostname()
                    );
                    break;
                }
                case "file_info": {
                    $rsp->ok = true;
                    $rsp->data = $fs->GetFileInfo($cmd->id); 
                    break;
                }
                case 'captcha_info': {
                    if(isset(Config::$Instance->recaptcha_site_key) && Config::$Instance->recaptcha_site_key !== False && isset(Config::$Instance->recaptcha_secret) && Config::$Instance->recaptcha_secret !== False) {
                        $rsp->ok = true;
                        $rsp->data = array(
                            "site_key" => Config::$Instance->recaptcha_site_key
                        );
                    } 
                    break;
                }
                case "verify_captcha_rate_limit": {
                    $abuse = new Abuse();
                    $rsp->data = $abuse->VerifyCaptcha($cmd->token);
                    if($rsp->data !== null && $rsp->data->success) {
                        $abuse->ResetRateLimits($cmd->id);
                        $rsp->ok = true;
                    }
                    break;
                }
            }

            header('Content-Type: application/json');
            echo json_encode($rsp);
        }
    }
?>