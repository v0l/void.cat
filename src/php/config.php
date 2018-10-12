<?php
    class Config {
        public static $Instance = NULL;
        
        public static function LoadConfig() {
            self::$Instance = json_decode(file_get_contents("settings.json"));
        }
    }
?>