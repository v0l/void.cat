<?php
    class Abuse {
        public function CheckDownload($id) {
            $redis = StaticRedis::WriteOp();
            $key = REDIS_PREFIX . "uvc:" . USER_IP;

            $views = $redis->hGet($key, $id);
            if($views !== False) {
                if($views >= Config::$Instance->download_captcha_check * 2) {

                } else if($views >= Config::$Instance->download_captcha_check) {
                    http_response_code(429); // Too many requests, tell the client to do captcha check
                    exit();
                }
            }

            $redis->hIncrBy($key, $id, 1);
        }

        public function VerifyCaptcha($token) : ?object {
            if(isset(Config::$Instance->recaptcha_secret)) {
                $ch = curl_init();
                curl_setopt($ch, CURLOPT_URL, 'https://www.google.com/recaptcha/api/siteverify');
                curl_setopt($ch, CURLOPT_POST, 1);
                curl_setopt($ch, CURLOPT_POSTFIELDS, array(
                    "secret" => Config::$Instance->recaptcha_secret,
                    "response" => $token,
                    "remoteip" => USER_IP
                ));
            
                curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
                $crsp = json_decode(curl_exec($ch));
                curl_close ($ch);

                return $crsp;
            }
            return null;
        }

        public function ResetRateLimits($id) {
            $redis = StaticRedis::WriteOp();
            $key = REDIS_PREFIX . "uvc:" . USER_IP;
            $redis->hSet($key, $id, 0);
        }
    }
?>