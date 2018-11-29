<?php
    class Stats {
        public $Files;
        public $Size;
        public $Transfer_24h;

        private static $AllTransferStatsKey = REDIS_PREFIX . "stats-transfer-all:";
        private static $GeneralStatsKey = REDIS_PREFIX . "stats-general";

        public static function Get() : Stats {
            $redis = StaticRedis::ReadOp();

            //calculate 24hr transfer stats
            $tx_24h = 0;
            $now = time();
            for($x = 0; $x < 24; $x += 1) {
                $stat_key = date("YmdH", $now - (60 * 60 * $x));
                $val = $redis->get(self::$AllTransferStatsKey . $stat_key);
                if($val != False){
                    $tx_24h += intval($val);
                }
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
            self::AddAllTransfer($size);
        }

        public static function AddAllTransfer($size) : void {
            $redis = StaticRedis::WriteOp();
            $stat_member = date("YmdH");
            $redis->incrBy(self::$AllTransferStatsKey . $stat_member, $size);
            $redis->setTimeout(self::$AllTransferStatsKey . $stat_member, 2592000); //store 30 days only
        }

        public static function Collect($fs) : void {
            $redis = StaticRedis::WriteOp();

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