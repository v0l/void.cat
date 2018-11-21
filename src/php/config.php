<?php
    class Config {
        public static $Instance;

        public static function GetConfig($config_name) {
            $redis = StaticRedis::$Instance;
            return $redis->hGet(REDIS_PREFIX . 'config', $config_name);
        }

        public static function MGetConfig($config_name) {
            $redis = StaticRedis::$Instance;
            return (object)$redis->hMGet(REDIS_PREFIX . 'config', $config_name);
        }

        public static function LoadConfig($config_name){
            self::$Instance = self::MGetConfig($config_name);

            //set defaults
            if(!isset(self::$Instance->upload_folder) || self::$Instance->upload_folder == False) {
                self::$Instance->upload_folder = "out";
            }
            if(!isset(self::$Instance->public_hash_algo) || self::$Instance->public_hash_algo == False) {
                self::$Instance->public_hash_algo = "ripemd160";
            }
            if(!isset(self::$Instance->max_upload_size) || self::$Instance->max_upload_size == False) {
                self::$Instance->max_upload_size = 104857600; //100MiB is the default upload size
            }
            if(!isset(self::$Instance->download_captcha_check) || self::$Instance->download_captcha_check == False) {
                self::$Instance->download_captcha_check = 10;
            }
        }
    }
?>