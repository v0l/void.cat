<?php

    class StaticRedis {
        public static $Instance = NULL;
        public static $MasterInstance = NULL;

        public static function ReadOp() : object {
            return self::$Instance;
        }

        public static function WriteOp() : object {
            if(self::$MasterInstance != NULL){
                return self::$MasterInstance;
            } else {
                return self::$Instance;
            }
        }

        public static function Connect() : bool {
            self::$Instance = new Redis();
            $con = self::$Instance->pconnect(REDIS_CONFIG);
            if($con){
                $rep = self::$Instance->info("REPLICATION");
                if($rep["role"] == "slave"){
                    self::$MasterInstance = new Redis();
                    $mcon = self::$MasterInstance->pconnect($rep["master_host"], $rep["master_port"]);
                    return $con && $mcon;
                }
            }
            return $con;
        }


    }
?>