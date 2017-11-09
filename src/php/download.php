<?php
	session_start();
	include_once('config.php');
	
	function XFastDownload($location, $filename, $mimeType = 'application/octet-stream')
	{
		global $validRequest;
		if($validRequest)
		{
			$url = "https://www.google-analytics.com/collect";
			$payload = "v=1&tid=" . _GA_CODE . "&cid=" . session_id() . "&t=pageview&dh=" . $_SERVER['HTTP_HOST'] . "&dp=" . urlencode($_SERVER['REQUEST_URI']) . "&uip=" . $_SERVER['REMOTE_ADDR'] . "&ua=" . urlencode($_SERVER["HTTP_USER_AGENT"]) . "&dr=" . urlencode(isset($_SERVER["HTTP_REFERER"]) ? $_SERVER["HTTP_REFERER"] : "");

			$ch = curl_init();

			curl_setopt($ch, CURLOPT_URL, $url);
			curl_setopt($ch, CURLOPT_POST, 1);
			curl_setopt($ch, CURLOPT_POSTFIELDS, $payload);
		
			curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
			curl_exec($ch);
			curl_close ($ch);
		}
	
		$expire = 604800;
	
		header("X-Accel-Redirect: $location");
		header("Cache-Control: public, max-age=$expire");
		header("Content-type: $mimeType");
		header('Content-Disposition: inline; filename="' . $filename . '"');
	}

	$hash = substr($_SERVER["REQUEST_URI"], 1);
	$hashKey = $_SERVER['REMOTE_ADDR'] . ':' . $hash;

	$range_start = 0;
	$range_end = 999;
	if(isset($_SERVER['HTTP_RANGE'])){
		$rby = explode('=', $_SERVER['HTTP_RANGE']);
		$rbv = explode('-', $rby[1]);
		if($rbv[0] != ''){
			$range_start = $rbv[0];
		}
		if($rbv[1] != ''){
			$range_end = $rbv[1];
		}
	}
	
	$validRequest = ($range_start == 0);
	$redis = new Redis();
	$redis->connect(_REDIS_SERVER);
	
	$dlCounter = $redis->get($hashKey);
	if($dlCounter != FALSE) {
		if($dlCounter >= _DL_CAPTCHA){
			//redirect for captcha check
			$redis->close();
			header('location: ' . _SITEURL . '?dl#' . $hash);
			exit();
		}
	}else{
		$redis->setEx($hashKey, _CAPTCHA_DL_EXPIRE, 0);
	}
	
	include_once('db.php');
	$db = new DB();
	$f = $db->GetFile($hash);
	if($f->hash160 != NULL){
		XFastDownload(_UPLOADDIR . $f->hash160, $f->filename, $f->mime);
		
		if($validRequest){
			$db->AddView($f->hash160);
			$redis->incr($hashKey);
		}
	}
	
	$redis->close();
?>