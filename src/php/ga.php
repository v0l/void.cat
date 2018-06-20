<?php
	include_once('config.php');
	
	function GACollect($p) {
		$url = "https://www.google-analytics.com/collect";
		$p["v"] = "1";
		$p["tid"] = _GA_SITE_CODE;
		$p["cid"] = session_id();
		
		$ch = curl_init();

		curl_setopt($ch, CURLOPT_URL, $url);
		curl_setopt($ch, CURLOPT_POST, 1);
		curl_setopt($ch, CURLOPT_POSTFIELDS, http_build_query($p));
	
		curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
		curl_exec($ch);
		curl_close ($ch);
	}
	
	function GAPageView($redis){
		$msg = http_build_query(array(
			"v" => "1",
			"tid" => _GA_SITE_CODE,
			"cid" => isset($_COOKIE["VC:UID"]) ? $_COOKIE["VC:UID"] : uniqid(),
			"t" => "pageview",
			"dh" => $_SERVER['HTTP_HOST'],
			"dp" => $_SERVER['REQUEST_URI'],
			"uip" => _UIP,
			"ua" => isset($_SERVER["HTTP_USER_AGENT"]) ? $_SERVER["HTTP_USER_AGENT"] : "",
			"dr" => isset($_SERVER["HTTP_REFERER"]) ? $_SERVER["HTTP_REFERER"] : ""
		));
		
		$redis->publish('ga-page-view', $msg);
	}
	
	function GAEvent($cat, $act) {
		GACollect(array(
			"t" => "event",
			"ec" => $cat,
			"ea" => $act
		));
	}
?>