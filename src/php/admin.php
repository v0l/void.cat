<?php

    class Admin implements RequestHandler {
        public function HandleRequest() : void {
            include(dirname(__FILE__) . "/../admin/index.html");
        }
    }

?>