<?php
    class Info implements RequestHandler {
        public function HandleRequest() : void {
            phpinfo();
        }
    }
?>