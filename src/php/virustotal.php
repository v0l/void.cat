<?php
	function CheckVirusTotal($h256) {
		$post = array('apikey' => _VIRUSTOTAL_KEY, 'resource' => $h256);
		$ch = curl_init();
		curl_setopt($ch, CURLOPT_URL, 'https://www.virustotal.com/vtapi/v2/file/report');
		curl_setopt($ch, CURLOPT_POST,1);
		curl_setopt($ch, CURLOPT_ENCODING, 'gzip,deflate');
		curl_setopt($ch, CURLOPT_USERAGENT, "gzip, void.cat virus check");
		curl_setopt($ch, CURLOPT_RETURNTRANSFER ,true);
		curl_setopt($ch, CURLOPT_POSTFIELDS, $post);
		 
		$result = curl_exec ($ch);
		curl_close ($ch);
		
		$vtr = json_decode($result, true);
		
		if($vtr["response_code"] == 1 && $vtr["positives"] > 0){
			$discord_data = array("content" => "[VIRUS DETECTED] " . $vtr["permalink"]);
			include_once("discord.php");
		}
		
		return $vtr;
	}
	
	function ScanFile($res) {
		$ch = curl_init();
		$bnd_id = "---------------------------735323031399963166993862150";
		
		$post_data = "--$bnd_id\r\nContent-Disposition: form-data; name='apikey'\r\n\r\n" . _VIRUSTOTAL_KEY . "\r\n--$bnd_id\r\nContent-Disposition: form-data; name='file'; filename='binary'\r\nContent-Type: application/octet-stream\r\n\r\n" . stream_get_contents($res) . "\r\n--$bnd_id--";
		$options = array(
			CURLOPT_URL => "https://www.virustotal.com/vtapi/v2/file/scan",
			CURLOPT_RETURNTRANSFER => true,
			CURLOPT_ENCODING => "gzip,deflate",
			CURLOPT_USERAGENT => "gzip, void.cat virus check",
			CURLOPT_VERBOSE => true,
			CURLOPT_HTTPHEADER => array(
				"Content-Type: multipart/form-data; boundary=$bnd_id"
			),
			CURLOPT_POST => true,
			CURLOPT_POSTFIELDS => $post_data
		);
		curl_setopt_array($ch, $options);
		
		$result = curl_exec($ch);
		$status_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
		curl_close($ch);
		
		if($status_code != 200) {
			return json_encode(array('response_code' => 0, 'verbose_msg' => 'Error, got status code: ' . $status_code), true);
		} else {
			return json_decode($result, true);
		}
	}
	
	function CheckVirusTotalCached($redis, $h256) {
		$vr = $redis->get("VC:VT:" . $h256);
		if($vr != FALSE) {
			return json_decode($vr);
		}else {
			$vtr = CheckVirusTotal($h256);
			$redis->setEx("VC:VT:" . $h256, 60 * 60 * 24, json_encode($vtr));
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
	
	if(isset($_GET["check_test"])) {
		
		header("Content-Type: application/json");
		
		include_once("config.php");
		include_once("db.php");
		$redis = new Redis();
		$redis->pconnect(_REDIS_SERVER);
		$db = new DB();
		
		$f = $db->GetFile($_GET["check_test"]);
		if($f) {
			$vtr = CheckVirusTotalCached($redis, $f->hash256);
			echo json_encode($vtr);
			if($vtr != null && isset($vtr->response_code) && $vtr->response_code == 0) {
				$sr = ScanFile(fopen($f->path, 'r'));
				echo json_encode($sr);
			}
		}
	}
?>