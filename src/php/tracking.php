<?php
    class Tracking {
        public function TrackDownload($id) : void {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $id;

            if(!$this->IsRangeRequest()) {
                $redis->hIncrBy($file_key, 'views', 1);
                $redis->hSet($file_key, 'lastview', time());
            }

            $this->SendMatomoEvent();
        }

        function IsRangeRequest() : bool {
            if(isset($_SERVER['HTTP_RANGE'])) {
                $rby = explode('=', $_SERVER['HTTP_RANGE']);
                $rbv = explode('-', $rby[1]);
                if($rbv[0] != '0'){
                    return True;
                }
            }

            return False;
        }

        function SendMatomoEvent() : void {
            $msg = "?" . http_build_query(array(
                "idsite" => 1,
                "rec" => 1,
                "apiv" => 1,
                "_id" => isset($_COOKIE["VC:UID"]) ? $_COOKIE["VC:UID"] : uniqid(),
                "url" => (isset($_SERVER['HTTPS']) ? "https" : "http") . "://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]",
                "cip" => _UIP,
                "ua" => isset($_SERVER["HTTP_USER_AGENT"]) ? $_SERVER["HTTP_USER_AGENT"] : "",
                "urlref" => isset($_SERVER["HTTP_REFERER"]) ? $_SERVER["HTTP_REFERER"] : ""
            ));
            
            StaticRedis::$Instance->publish('ga-page-view-matomo', $msg);
        }
    }
?>