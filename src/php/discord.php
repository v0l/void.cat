<?php

    class Discord {
        public static function SendPublic($msg) : void {
            self::CallWebhook(Config::$Instance->discord_webhook_pub, $msg);
        }

        private static function CallWebhook($url, $data) : void {
            self::CurlPost($url, json_encode($data));
        }

        private static function CurlPost($url, $data) : ?string {
            $ch = curl_init();
            curl_setopt($ch, CURLOPT_URL, $url);
            curl_setopt($ch, CURLOPT_POST, true);
            curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
            curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
            curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 0);
            
            $result = curl_exec($ch);
            curl_close($ch);
            return $result;	
        }
    }

?>