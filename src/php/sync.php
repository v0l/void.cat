<?php
    class Sync implements RequestHandler {
        public function __construct(){
            Config::LoadConfig(array("upload_folder"));
            ini_set('enable_post_data_reading', 0);
        }

        public function HandleRequest() : void {
            
            if(isset($_SERVER["HTTP_X_FILE_ID"])) {
                $id = $_SERVER["HTTP_X_FILE_ID"];
                $fs = new FileStore(Config::$Instance->upload_folder);
                if(!$fs->FileExists($id)) {
                    //resolve the hostnames to ips
                    $redis = StaticRedis::$Instance;
                    $sync_hosts = $redis->sMembers(REDIS_PREFIX . 'sync-hosts');
                    
                    $sync_hosts_ips = array();
                    foreach($sync_hosts as $host) {
                        $sync_hosts_ips[] = gethostbyname($host);
                    }

                    //check the ip of the host submitting the file for sync
                    if(in_array(USER_IP, $sync_hosts_ips)) {
                        $fs->StoreFile("php://input", $id);
                    } else {
                        http_response_code(401);
                    }
                }
            } else {
                http_response_code(400);
            }
        }

        public static function SyncFile($id, $filename, $host) : void {
            $ch = curl_init();
            curl_setopt($ch, CURLOPT_URL, "https://$host/sync");
            curl_setopt($ch, CURLOPT_POST, 1);
            curl_setopt($ch, CURLOPT_POSTFIELDS, file_get_contents($filename));
            curl_setopt($ch, CURLOPT_HTTPHEADER, array(
                "Content-Type: application/octet-stream",
                "X-File-Id: " . $id
            ));
            curl_exec($ch);
            curl_close ($ch);
        }
    }

?>