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
