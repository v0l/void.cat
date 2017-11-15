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
	
	function GAPageView(){
		GACollect(array(
			"t" => "pageview",
			"dh" => $_SERVER['HTTP_HOST'],
			"dp" => urlencode($_SERVER['REQUEST_URI']),
			"uip" => $_SERVER['REMOTE_ADDR'],
			"ua" => urlencode(isset($_SERVER["HTTP_USER_AGENT"]) ? $_SERVER["HTTP_USER_AGENT"] : ""),
			"dr" => urlencode(isset($_SERVER["HTTP_REFERER"]) ? $_SERVER["HTTP_REFERER"] : "")
		));
	}
	
	function GAEvent($cat, $act) {
		GACollect(array(
			"t" => "event",
			"ec" => $cat,
			"ea" => $act
		));
	}
?>