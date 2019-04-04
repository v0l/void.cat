<?php
    include_once("init.php");

    if(StaticRedis::Connect()) {
        echo "Connected to redis..\n";

        Config::LoadConfig(array("upload_folder", "discord_webhook_pub"));
        $fs = new FileStore(Config::$Instance->upload_folder, $_SERVER["cron_root"]);

        //delete expired files
        $pmsg = "`" . gethostname() . ":\n";
        $redis = StaticRedis::ReadOp();
        $deleted = false;
        foreach($fs->ListFiles() as $file) {
            $id = basename($file);
            $file_key = REDIS_PREFIX . $id;
            $lv = $redis->hGet($file_key, "lastview");
            $expire = time() - (30 * 24 * 60 * 60);

            //use the file upload timestamp if there is no view data recorded
            //if the file upload time is greater than the current timestamp, mark as old (!!abuse!!)
            //this will also force legacy file uploads with no views to be deleted (header will always fail to load)
            if($lv === false) {
                $file_header = BlobFile::LoadHeader($file);
                if($file_header !== null && $file_header->Uploaded <= time()){
                    $lv = $file_header->Uploaded;
                } else {
                    //cant read file header or upload timestamp is invalid, mark as old
                    $lv = 0;
                }
            }

            if($lv !== false && intval($lv) < $expire) {
                $nmsg = "Deleting expired file: " . $id . " (lastview=" . date("Y-m-d h:i:s", intval($lv)) . ")\n";
                if(strlen($pmsg) + strlen($nmsg) >= 2000){
                    //send to discord public hook
                    $pmsg = $pmsg . "`";
                    Discord::SendPublic(array(
                        "content" => $pmsg
                    ));
                    $pmsg = "`";
                }
                $pmsg = $pmsg . $nmsg;
                unlink($file);
                $deleted = true;
            }
        }

        //send last message if any
        if(strlen($pmsg) > 0 && $deleted) {
            $pmsg = $pmsg . "`";
            Discord::SendPublic(array(
                "content" => $pmsg
            ));
        }

        if(StaticRedis::$IsConnectedToSlave == False) {
            echo "Runing master node tasks..\n";
            Stats::Collect($fs);
        }
    }
?>