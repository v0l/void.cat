<?php
	function CheckVirusTotal($h256) {
		$post = array('apikey' => _VIRUSTOTAL_KEY, 'resource' => $h256);
		$ch = curl_init();
		curl_setopt($ch, CURLOPT_URL, 'https://www.virustotal.com/vtapi/v2/file/report');
		curl_setopt($ch, CURLOPT_POST,1);
		curl_setopt($ch, CURLOPT_ENCODING, 'gzip,deflate'); // please compress data
		curl_setopt($ch, CURLOPT_USERAGENT, "gzip, void.cat virus check");
		curl_setopt($ch, CURLOPT_RETURNTRANSFER ,true);
		curl_setopt($ch, CURLOPT_POSTFIELDS, $post);
		 
		$result = curl_exec ($ch);
		$status_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
		curl_close ($ch);
		
		$vtr = json_decode($result, true);
		
		if($vtr["response_code"] == 1 && $vtr["positives"] > 0){
			$discord_data = array("content" => "[VIRUS DETECTED] " . $vtr["permalink"]);
			include_once("discord.php");
		}
		
		return $vtr;
	}
	
	function CheckVirusTotalCached($redis, $h256) {
		$vr = $redis->get("VC:VT:" . $h256);
		if($vr != FALSE) {
			return json_decode($vr);
		}else {
			$vtr = CheckVirusTotal($h256);
			$redis->set("VC:VT:" . $h256, json_encode($vtr));
			return $vtr;
		}
	}
	
	if(isset($_GET["hash"])) {
		include_once("config.php");
		$redis = new Redis();
		$redis->pconnect(_REDIS_SERVER);
		header("Content-Type: application/json");
		echo json_encode(CheckVirusTotalCached($redis, $_GET["hash"]));
	}
?>