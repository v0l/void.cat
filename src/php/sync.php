<?php
    class Sync implements RequestHandler {
        public function __construct(){
            ini_set('enable_post_data_reading', 0);
        }

        public function HandleRequest() : void {
            
        }
    }

?>