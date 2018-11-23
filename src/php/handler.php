<?php
    include_once("init.php");

    //Startup
    if(StaticRedis::Connect() == True) {
        Tracking::SendMatomoEvent();
        
        if(isset($_REQUEST["h"])) {
            $handler_name = $_REQUEST["h"];
            if(file_exists($handler_name . '.php')){
                $handler = new $handler_name();
                if($handler instanceof RequestHandler){
                    $handler->HandleRequest();
                    exit();
                }
            }
        }
        //var_dump($_REQUEST);
        http_response_code(400);
        exit();
    } else {
        http_response_code(500);
        exit();
    }
?>