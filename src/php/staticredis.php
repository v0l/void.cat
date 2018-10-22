<?php

    class StaticRedis {
        public static $Instance = NULL;

        public static function Connect(){
            self::$Instance = new Redis();
            return self::$Instance->pconnect(REDIS_CONFIG);
        }
    }
?>