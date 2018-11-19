<?php
    class Config {
        public static function GetConfig($config_name) {
            $redis = StaticRedis::$Instance;
            return $redis->hGet(REDIS_PREFIX . 'config', $config_name);
        }

        public static function MGetConfig($config_name) {
            $redis = StaticRedis::$Instance;
            return (object)$redis->hMGet(REDIS_PREFIX . 'config', $config_name);
        }
    }
?>