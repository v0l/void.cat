<?php
    class Stats {
        public $Files;
        public $Size;
        public $Transfer_24h;

        private static $AllTransferStatsKey = REDIS_PREFIX . "stats-transfer-all";
        private static $GeneralStatsKey = REDIS_PREFIX . "stats-general";

        public static function Get() : Stats {
            $redis = StaticRedis::$Instance;

            //calculate 24hr transfer stats
            $tx_24h_array = $redis->zRange(self::$AllTransferStatsKey, 0, 24, true); //stats are 1hr interval
            $tx_24h = 0;
            foreach($tx_24h_array as $tx_key => $tx_bytes) {
                $tx_24h += $tx_bytes;
            }

            //get general stats
            $general = (object)$redis->hMGet(self::$GeneralStatsKey, array("files", "size"));

            $ret = new Stats();
            $ret->Transfer_24h = $tx_24h;
            $ret->Files = intval($general->files !== False ? $general->files : 0);
            $ret->Size = intval($general->size !== False ? $general->size : 0);

            return $ret;
        }

        public static function TrackTransfer($id, $size) : void {
            //$redis = StaticRedis::$Instance;
            self::AddAllTransfer($size);
        }

        public static function AddAllTransfer($size) : void {
            $redis = StaticRedis::$Instance;
            $stat_member = date("YmdH");
            $redis->zIncrBy(self::$AllTransferStatsKey, $size, $stat_member);
        }

        public static function Collect($fs) : void {
            $redis = StaticRedis::$Instance;

            $files = $fs->ListFiles();
            $total_size = 0;
            foreach($files as $file) {
                $total_size += filesize($file);
            }

            $redis->hMSet(self::$GeneralStatsKey, array(
                "files" => count($files),
                "size" => $total_size 
            ));
            
            //tick from cron job to create keys for every hour
            //if no downloads are happening we will be missing keys
            //this will prevent inaccurate reporting
            self::AddAllTransfer(0); 
        }
    }
?>