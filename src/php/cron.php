<?php
    include_once("init.php");

    if(StaticRedis::Connect()) {
        echo "Connected to redis..\n";

        Config::LoadConfig(array("upload_folder"));

        if(StaticRedis::$IsConnectedToSlave == False) {
            echo "Runing master node tasks..\n";
            $fs = new FileStore(Config::$Instance->upload_folder, $_SERVER["cron_root"]);
            Stats::Collect($fs);
        }
    }
?>