<?php
    include_once("init.php");

    StaticRedis::Connect();
    Config::LoadConfig(array("upload_folder"));
    var_dump($_SERVER);

    $fs = new FileStore(Config::$Instance->upload_folder, $_SERVER["cron_root"]);

    echo "Loading stats for: " . $fs->GetUploadDirAbsolute() . "\n";
    //echo "\n\t" . implode("\n\t", $fs->ListFiles()) . "\n";

    Stats::Collect($fs);
?>