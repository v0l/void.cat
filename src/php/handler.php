<?php
    //Startup
    Config::LoadConfig();
    StaticRedis::Connect();
    Db::Connect();

    if(isset($_REQUEST["h"])) {
        $hf = $_REQUEST["h"];
        if(file_exists($h)){
            $hc = new $hf();
            if($hc instanceof RequestHandler){
                $hc->HandleRequest();
            }
        }
    }
?>