<?php

    class StaticRedis {
        public static $Instance = NULL;

        public static function Connect(){
            $cfg = Config::$Instance;
            self::$Instance = new Redis();
            return self::$Instance->pconnect($cfg->redis);
        }
    }
?>