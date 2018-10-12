<?php

    class Db {
        public static $Instance = NULL;
        public static $Error = NULL;

        public static function Connect() {
            $cfg = Config::$Instance;
            self::$Instance = new mysqli($cfg->db_host, $cfg->db_user, $cfg->db_password, $cfg->db_database);

            if (mysqli_connect_errno()) {
                self::$Error = mysqli_connect_error();
                return FALSE;
            }
            return TRUE;
        }
    }
?>