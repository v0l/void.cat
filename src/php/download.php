<?php
	include_once('config.php');
	include_once('ga.php');
	
	$redis = new Redis();
	$redis->pconnect(_REDIS_SERVER);
	
	GAPageView($redis);

	$hash = substr($_SERVER["REQUEST_URI"], 1);
	$hashKey = _UIP . ':' . $hash;

	$refr = isset($_SERVER["HTTP_REFERER"]) ? $_SERVER["HTTP_REFERER"] : False;
	if($refr != False){
		$rh = parse_url($refr)["host"];
		if(in_array($rh, _BLOCK_REFERER)){
			http_response_code(403);
			exit();
		}
	}
	
	//check is range request
	$is_non_range = True;
	if(isset($_SERVER['HTTP_RANGE'])){
		$rby = explode('=', $_SERVER['HTTP_RANGE']);
		$rbv = explode('-', $rby[1]);
		if($rbv[0] != '0'){
			$is_non_range = False;
		}
	}
	
	//block certain bots from counting views (to stop files never expiring)
	$isCrawlBot = False;
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
				$isCrawlBot = True;
				break;
			}
		}
	}
	
	$dlCounter = $redis->get($hashKey);
	if($dlCounter != FALSE) {
		if($dlCounter >= _DL_CAPTCHA * 2){
			$cfbk = 'VC:CF:BLOCK';
			if(_CLOUDFLARE_API_KEY != 'API_KEY' && $redis->sIsMember($cfbk, _UIP) == False){
				$redis->sadd($cfbk, _UIP);
				include_once('cloudflare.php');
				AddFirewallRule(_UIP);
			}
			exit();
		}else if($dlCounter >= _DL_CAPTCHA){
			//redirect for captcha check
			$redis->incr($hashKey);
			GAEvent("Captcha", "Hit");
			header('location: ' . _SITEURL . '?dl#' . $hash);
			exit();
		}
	}else{
		$redis->setEx($hashKey, _CAPTCHA_DL_EXPIRE, 0);
		$dlCounter = 0;
	}
	
	include_once('db.php');
	include_once('virustotal.php');
	$db = new DB();
	$f = $db->GetFile($hash);
	if($f->hash160 != NULL){
		$vtr = CheckVirusTotalCached($redis, $f->hash256);
		if($vtr != null && isset($vtr->positives) && $vtr->positives > 1) {
			http_response_code(404);
		}else {
			$expire = 604800;
			$location = _UPLOADDIR . $f->hash160;
			$mimeType = $f->mime;
			$filename = $f->filename;
			
			header("X-Accel-Redirect: $location");
			header("Cache-Control: public, max-age=$expire");
			header("Content-type: $mimeType");
			header('Content-Disposition: inline; filename="' . $filename . '"');
			
			if(!$isCrawlBot && $is_non_range){
				$db->AddView($f->hash160);
				$redis->incr($hashKey);
			}
		}
	}else{
		http_response_code(404);
	}
?>
