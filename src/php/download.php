<?php
	session_start();
	include_once('config.php');
	include_once('ga.php');
	
	$hash = substr($_SERVER["REQUEST_URI"], 1);
	$hashKey = $_SERVER['REMOTE_ADDR'] . ':' . $hash;

	$refr = isset($_SERVER["HTTP_REFERER"]) ? $_SERVER["HTTP_REFERER"] : False;
	if($refr != False){
		$rh = parse_url($refr)["host"];
		if(in_array($rh, _BLOCK_REFERER)){
			http_response_code(403);
			exit();
		}
	}
	
	//block certain bots from counting views (to stop files never expiring)
	$validRequest = True;
	$ua = isset($_SERVER["HTTP_USER_AGENT"]) ? $_SERVER["HTTP_USER_AGENT"] : "";
	if(preg_match('/.*(\(.*\))/i', $ua, $matches) == 1)
	{
		$opts = array();
		if(strpos($matches[1], ';') != False){
			$opts = explode(';', $matches[1]);
		}else{
			$opts[0] = $matches[1];
		}
		
		foreach($opts as $opt){
			if(in_array(trim($opt), _UA_NO_VIEW)){
				$validRequest = False;
				break;
			}
		}
	}
	
	$redis = new Redis();
	$redis->connect(_REDIS_SERVER);
	
	$dlCounter = $redis->get($hashKey);
	if($dlCounter != FALSE) {
		if($dlCounter >= _DL_CAPTCHA){
			//redirect for captcha check
			$redis->close();
			GAEvent("Captcha", "Hit");
			header('location: ' . _SITEURL . '?dl#' . $hash);
			exit();
		}
	}else{
		$redis->setEx($hashKey, _CAPTCHA_DL_EXPIRE, 0);
		$dlCounter = 0;
	}
	
	include_once('db.php');
	$db = new DB();
	$f = $db->GetFile($hash);
	if($f->hash160 != NULL){
		$expire = 604800;
		$location = _UPLOADDIR . $f->hash160;
		$mimeType = $f->mime;
		$filename = $f->filename;
		
		header("X-Accel-Redirect: $location");
		header("Cache-Control: public, max-age=$expire");
		header("Content-type: $mimeType");
		header('Content-Disposition: inline; filename="' . $filename . '"');
		
		if($validRequest){
			GAPageView();
			$db->AddView($f->hash160);
			$redis->incr($hashKey);
		}
	}else{
		http_response_code(404);
	}
	
	$redis->close();
?>
