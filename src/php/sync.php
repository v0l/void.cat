<?php
    class Sync implements RequestHandler {
        public function __construct(){
            Config::LoadConfig(array('max_upload_size', 'upload_folder'));
			
			set_time_limit(1200);
            ini_set('post_max_size', Config::$Instance->max_upload_size);
            ini_set('upload_max_filesize', Config::$Instance->max_upload_size);
            ini_set('memory_limit', Config::$Instance->max_upload_size);
            ini_set('enable_post_data_reading', 0);
        }

        public function HandleRequest() : void {
            if(isset($_SERVER["HTTP_X_FILE_ID"])) {
                $id = $_SERVER["HTTP_X_FILE_ID"];
                $fs = new FileStore(Config::$Instance->upload_folder);
                if(!$fs->FileExists($id)) {
                    //resolve the hostnames to ips
                    $redis = StaticRedis::ReadOp();
                    $sync_hosts = $redis->sMembers(REDIS_PREFIX . 'sync-hosts');

                    $sync_hosts_ips = array();
                    foreach($sync_hosts as $host) {
                        $sync_hosts_ips[] = gethostbyname($host);
                    }

                    //check the ip of the host submitting the file for sync
                    if(in_array(USER_IP, $sync_hosts_ips)) {
                        $fs->StoreFile(fopen("php://input", "rb"), $id);
                        http_response_code(201);
                    } else {
                        http_response_code(401);
                    }
                } else {
                    http_response_code(200);
                }
            } else {
                http_response_code(400);
            }
        }

        public static function SyncFile($id, $filename, $host) : int {
            $ch = curl_init();
            curl_setopt($ch, CURLOPT_URL, "https://$host/sync");
            curl_setopt($ch, CURLOPT_POST, 1);
            curl_setopt($ch, CURLOPT_POSTFIELDS, file_get_contents($filename));
            curl_setopt($ch, CURLOPT_HTTPHEADER, array(
                "Content-Type: application/octet-stream",
                "X-File-Id: " . $id
            ));
            curl_exec($ch);
            $status = curl_getinfo($ch, CURLINFO_RESPONSE_CODE);
            curl_close ($ch);

            return intval($status);
        }
    }

?>
