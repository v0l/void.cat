<?php
    include_once("init.php");

    StaticRedis::Connect();
    Config::LoadConfig(array("upload_folder"));
    
    $fs = new FileStore(Config::$Instance->upload_folder, "/usr/local/nginx/html");

    echo "Loading stats for: " . $fs->GetUploadDirAbsolute() . "\n";
    //echo "\n\t" . implode("\n\t", $fs->ListFiles()) . "\n";

    Stats::Collect($fs);
?>