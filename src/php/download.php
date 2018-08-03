<?php
	include_once('config.php');
	include_once('functions.php');
	
	$redis = new Redis();
	$redis->pconnect(_REDIS_SERVER);
	
	ga_page_view($redis);

	$hash = substr($_SERVER["REQUEST_URI"], 1);
	$hashKey = _UIP . ':' . $hash;

	if(_IS_LB_HOST == False && count(_LB_HOSTS) > 0) {
		$has_cache = $redis->sIsMember("VC:DL:LB", $hash);
		if($has_cache == False) {
			$lb_hash_cache = True;
			foreach(_LB_HOSTS as $lbh) {
				$lb_x = json_decode(curl_get($lbh . "/hasfile.php?hash=" . $hash, array("Host: " . _LB_HOSTNAME)));
				if($lb_x->result == False){
					$lb_hash_cache = False;
					break;
				}
			}
			
			if($lb_hash_cache == True){
				$redis->sadd("VC:DL:LB", $hash);
				header("location: https://" . _LB_HOSTNAME . "/" . $hash);
				exit();
			}
		} else {
			header("location: https://" . _LB_HOSTNAME . "/" . $hash);
			exit();
		}
	}
	
	$refr = isset($_SERVER["HTTP_REFERER"]) ? $_SERVER["HTTP_REFERER"] : False;
	if($refr != False){
		$rh = parse_url($refr)["host"];
		if(in_array($rh, _BLOCK_REFERER)){
			header("Content-Type: text/html");
			echo file_get_contents("empty.html");
			//http_response_code(403);
			exit();
		}
		
		if(_IS_LB_HOST == False && $rh != "void.cat") {
			//redirect to view page from hotlink
			header("location: /#" . $hash);
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
			/*$cfbk = 'VC:CF:BLOCK';
			if(_CLOUDFLARE_API_KEY != 'API_KEY' && $redis->sIsMember($cfbk, _UIP) == False){
				$redis->sadd($cfbk, _UIP);
				include_once('cloudflare.php');
				AddFirewallRule(_UIP);
			}*/
			header('location: /');
			exit();
		}else if($dlCounter >= _DL_CAPTCHA){
			//redirect for captcha check
			$redis->incr($hashKey);
			ga_event("Captcha", "Hit");
			header('location: ' . _SITEURL . '?dl#' . $hash);
			exit();
		}
	}else{
		$redis->setEx($hashKey, _CAPTCHA_DL_EXPIRE, 0);
		$dlCounter = 0;
	}
	
	include_once('db.php');
	$db = new DB();
	
	//try to guess the hash if the link was truncated with '...'
	if(strpos($hash, "...") !== false) {
		$nh = str_replace("...", "%", $hash);
		$gh = $db->GuessHash($nh);
		if($gh !== null) {
			header('location: ' . _SITEURL . $gh);
			exit();
		}
	}
	
	$f = $db->GetFile($hash);
	if($f->hash160 != NULL){
		$vtr = CheckVirusTotalCached($redis, $f->hash256, $f->hash160);
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
