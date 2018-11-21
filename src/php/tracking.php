<?php
    class TrackingEvent {

    }

    class Tracking {
        public static function CreateEventFromDownload(){
            return new TrackingEvent();
        }

        public function TrackDownload($id) {
            $redis = StaticRedis::$Instance;
            $file_key = REDIS_PREFIX . $id;

            if(!$this->IsRangeRequest()) {
                $redis->hIncrBy($file_key, 'views', 1);
                $redis->hSet($file_key, 'lastview', time());
            }
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
    }
?>