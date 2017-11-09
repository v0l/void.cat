<?php
	session_start();
	
	require_once('config.php');

	$body = file_get_contents('php://input');
	$c = json_decode($body);
	$rsp = array(
		"input" => $c
	);

	switch($c->cmd){
		case "config":
		{
			require_once("db.php");
			
			$db = new DB();
			$rsp["stats"] = $db->GetStats();
			
			$maxsizeM = ini_get('post_max_size');
			$maxsize = (int)(str_replace('M', '', $maxsizeM) * 1000 * 1000);
			$rsp["maxsize"] = $maxsize;
			$rsp["expire"] = _FILE_EXPIRE_TIME;
			break;
		}
		case "file": 
		{
			require_once("db.php");
			
			$db = new DB();
			$fi = $db->GetFile($c->hash);
			if($fi->hash160 != NULL)
			{
				unset($fi->path); //block internal path value
				$fi->url = _SITEURL . $fi->hash160;
				$rsp["file"] = $fi;
				
				$hashKey = $_SERVER['REMOTE_ADDR'] . ':' . $fi->hash160;
				
				$redis = new Redis();
				$redis->connect(_REDIS_SERVER);
				
				$dlCounter = $redis->get($hashKey);
				if($dlCounter != False && $dlCounter >= _DL_CAPTCHA) {
					$rsp["captcha"] = True;
				}
				
				$redis->close();
			}
			break;
		}
		case "captcha_config": 
		{
			$rsp["cap_key"] = _CAPTCHA_KEY;
			$rsp["cap_dl"] = _DL_CAPTCHA;
			break;
		}
		case "captcha_verify":
		{
			$redis = new Redis();
			$redis->connect(_REDIS_SERVER);
			
			$hashKey = $_SERVER['REMOTE_ADDR'] . ':' . $c->hash;
			
			$dlCounter = $redis->get($hashKey);
			if($dlCounter != FALSE) {
				$ch = curl_init();

				curl_setopt($ch, CURLOPT_URL, 'https://www.google.com/recaptcha/api/siteverify');
				curl_setopt($ch, CURLOPT_POST, 1);
				curl_setopt($ch, CURLOPT_POSTFIELDS, 'secret=' . _CAPTCHA_SECRET . '&response=' . $c->token . '&remoteip=' . $_SERVER['REMOTE_ADDR']);
			
				curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
				$crsp = json_decode(curl_exec($ch));
				curl_close ($ch);
				
				if($crsp->success == True){
					$dlCounter = 0;
					$redis->setEx($hashKey, _CAPTCHA_DL_EXPIRE, 0);
					$rsp["ok"] = True;
				}else{
					$rsp["ok"] = False;
				}
			}else{
				$rsp["ok"] = True;
			}
			
			$redis->close();
			break;
		}
	}

	header('Content-Type: application/json');
	echo json_encode($rsp);
?>