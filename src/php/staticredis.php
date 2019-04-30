<?php

    class StaticRedis {
        public static $Instance = null;
        public static $MasterInstance = null;
        public static $IsConnectedToSlave = false;

        public static function ReadOp() : object {
            return self::$Instance;
        }

        public static function WriteOp() : object {
            if(self::$MasterInstance != null){
                return self::$MasterInstance;
            } else {
                return self::$Instance;
            }
        }

        public static function Connect() : bool {
            self::$Instance = new Redis();
            $con = self::$Instance->pconnect(REDIS_CONFIG);
            if($con){
                $rep = self::$Instance->info();
                if($rep["role"] == "slave"){
                    self::$IsConnectedToSlave = true;
                    self::$MasterInstance = new Redis();
                    $mcon = self::$MasterInstance->pconnect($rep["master_host"], $rep["master_port"]);
                    return $con && $mcon;
                }
            }
            return $con;
        }
    }
?>