<?php
	function ln_query($method, $params = NULL){
		$sock = fsockopen(_LN_RPC_FILE);
		if($sock) {
			fwrite($sock, json_encode(array("jsonrpc" => "2.0", "method" => $method, "params" => $params, "id" => 1)) . "\n");
			$rsp = fgets($sock);
			fclose($sock);
			return json_decode($rsp);
		}
		return NULL;
	}
	
	function curl_json_get($url) 
	{
		return json_decode(curl_get($url));	
	}
	
	function curl_get($url) 
	{
		$ch = curl_init();
		curl_setopt($ch, CURLOPT_URL, $url);
		curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
		curl_setopt($ch, CURLOPT_USERAGENT, _CURL_USER_AGENT);
		curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 0);
		
		$result = curl_exec($ch);
		curl_close($ch);
		return $result;	
	}
	
	function curl_post($url, $data) 
	{
		$ch = curl_init();
		curl_setopt($ch, CURLOPT_URL, $url);
		curl_setopt($ch, CURLOPT_POST, true);
		curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
		curl_setopt($ch, CURLOPT_USERAGENT, _CURL_USER_AGENT);
		curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
		curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 0);
		
		$result = curl_exec($ch);
		curl_close($ch);
		return $result;	
	}
	
	function GetAllAddrInfo($addrs) 
	{
		$ret = array();
		
		foreach($addrs as $cur => $addr) 
		{
			$f = "GetAddrInfo_" . $cur;
			if(is_callable($f)) {
				$val = call_user_func($f, $addr);
				if($val) 
				{
					array_push($ret, (object) [
						"currency" => $cur,
						"address" => $addr,
						"balance" => $val->balance,
						"txns" => $val->txns
					]);
				}
			}
		}
		
		return $ret;
	}
	
	function GetAddrInfo_BTC($addr) 
	{
		$val = curl_json_get("https://api.blockcypher.com/v1/btc/main/addrs/" . $addr . "/balance");
		return (object) [
			"balance" => $val->final_balance * SAT,
			"txns" => $val->final_n_tx
		];
	}
	
	function GetAddrInfo_LTC($addr)
	{
		$val = curl_json_get("https://api.blockcypher.com/v1/ltc/main/addrs/" . $addr . "/balance");
		return (object) [
			"balance" => $val->final_balance,
			"txns" => $val->final_n_tx
		];
	}
	
	function GetAddrInfo_DASH($addr)
	{
		$val = curl_json_get("https://api.blockcypher.com/v1/dash/main/addrs/" . $addr . "/balance");
		return (object) [
			"balance" => $val->final_balance,
			"txns" => $val->final_n_tx
		];
	}
	
	function GetAddrInfo_BCH($addr) 
	{
		$val = curl_json_get("https://bitcoincash.blockexplorer.com/api/addr/" . $addr . "/balance");
		return (object) [
			"balance" => $val->balance,
			"txns" => $val->txApperances,
			"new_addr" => $val->addrStr
		];
	}
	
	function GetAddrInfo_ETH_ERC20($contract, $addr)
	{
		$val = curl_json_get("https://api.etherscan.io/api?module=account&action=tokenbalance&contractaddress=" . $contract . "&address=" . $addr . "&tag=latest&apikey=" . _ETHERSCAN_API_KEY);
		return (object) [
			"balance" => $val->response,
			"txns" => 0
		];
	}
	
	function GetAddrInfo_EOS($addr)
	{
		return GetAddrInfo_ETH_ERC20("0x86fa049857e0209aa7d9e616f7eb3b3b78ecfdb0", $addr);
	}
	
	function GetAddrInfo_TRX($addr)
	{
		return GetAddrInfo_ETH_ERC20("0xf230b790e05390fc8295f4d3f60332c93bed42e2", $addr);
	}
	
	function GetAddrInfo_ETH($addr)
	{
		$val = curl_json_get("https://api.etherscan.io/api?module=account&action=balance&address=" . $addr . "&tag=latest&apikey=" . _ETHERSCAN_API_KEY);
		return (object) [
			"balance" => $val->response,
			"txns" => 0
		];
	}
	
	function GetAddrInfo_ETC($addr)
	{
		$val = curl_json_get("https://etcchain.com/api/v1/getAddressBalance?address=" . $addr);
		return (object) [
			"balance" => $val->balance,
			"txns" => 0
		];
	}
	
	function GetAddrInfo_BTG($addr)
	{
		$val = curl_get("https://btgexp.com/ext/getaddress/" . $addr);
		return (object) [
			"balance" => floatval($val),
			"txns" => 0
		];
	}
	
	function GetAddrInfo_ZEC($addr)
	{
		$val = curl_get("https://api.zcha.in/v2/mainnet/accounts/" . $addr);
		return (object) [
			"balance" => $val->balance,
			"txns" => $val->recvCount
		];
	}
	
	function GetAddrInfo_XEM($addr)
	{
		//pick a random node to query
		$nodes = curl_json_get("https://nodeexplorer.com/api_openapi_version");
		$api = array_rand($nodes->nodes);
		
		$val = curl_json_get("http://" . $nodes->nodes[$api] . "/account/get?address=" . $addr);
		return (object) [
			"balance" => $val->account->balance,
			"txns" => 0
		];
	}
	
	function GetBTCPrice()
	{
		$val = curl_json_get("https://api.coinmarketcap.com/v2/ticker/1/");
		return $val->data->quotes->USD->price;
	}
	
	function call_webhook($url, $data) {
		curl_post($url, json_encode($data));
	}
	
	function send_pub_discord_msg($data) {
		call_webhook(_DISCORD_WEBHOOK_PUB, $data);
	}
	
	function send_discord_msg($data) {
		call_webhook(_DISCORD_WEBHOOK, $data);
	}
	
	function ga_collect($p) {
		$url = "https://www.google-analytics.com/collect";
		$p["v"] = "1";
		$p["tid"] = _GA_SITE_CODE;
		$p["cid"] = session_id();
		
		curl_post($url, http_build_query($p));
	}
	
	function ga_page_view($redis){
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
	
	function ga_event($cat, $act) {
		ga_collect(array(
			"t" => "event",
			"ec" => $cat,
			"ea" => $act
		));
	}
	
	function clamav_scan_stream($res, $slen) {
		$socket = socket_create(AF_INET, SOCK_STREAM, SOL_TCP);
		socket_connect($socket, '127.0.0.1', 3310);
		
		$cs = 1 * 1000 * 1000; //1MB chunk size
		$offset = 0;
		
		socket_write($socket, "zINSTREAM\0");
        while ($chunk = fread($res, $cs)) {
            $size = pack('N', strlen($chunk));
            socket_write($socket, $size);
            socket_write($socket, $chunk);
        }
        socket_write($socket, pack('N', 0));
		rewind($res);
		
		$response = null;
		do {
			$data = socket_read($socket, 128);
			if($data === "") {
				break;
			}
			$response .= $data;
			
			if(substr($response, -1) === "\0"){
				break;
			}
		}while(true);
		
		return substr($response, 0, -1);
	}
	
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
			send_pub_discord_msg($discord_data);
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
			if($vtr["response_code"] == 1 && $vtr["positives"] > 0) {
				$redis->set("VC:VT:" . $h256, json_encode($vtr));
			} else {
				$redis->setEx("VC:VT:" . $h256, 60 * 60 * 24, json_encode($vtr));
			}
			
			return $vtr;
		}
	}
	
	function AddFirewallRule($ip) {
		$post = array(
			'mode' => 'challenge',
			'configuration' => array(
				'target' => 'ip',
				'value' => $ip
			),
			'notes' => 'blocked by: ' . $_SERVER['SERVER_NAME']
		);
		
		$ch = curl_init();
		curl_setopt($ch, CURLOPT_URL, 'https://api.cloudflare.com/client/v4/zones/' . _CLOUDFLARE_ZONE . '/firewall/access_rules/rules');
		curl_setopt($ch, CURLOPT_POST,1);
		curl_setopt($ch, CURLOPT_RETURNTRANSFER ,true);
		curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($post));
		curl_setopt($ch, CURLOPT_HTTPHEADER, array(
			'Content-Type: application/json',
			'X-Auth-Email: ' . _CLOUDFLARE_API_EMAIL,
			'X-Auth-Key: ' . _CLOUDFLARE_API_KEY
		));
		$result = curl_exec ($ch);
		curl_close ($ch);
		
		$cfr = json_decode($result, true);
		
		if($cfr['success'] == True){
			send_discord_msg(array("content" => "[IP BLOCKED] " . $ip));
		}else {
			send_discord_msg(array("content" => "[IP BLOCK ERROR] " . $ip . "\n```json\n" . $result . "\n```"));
		}
		
		return $cfr;
	}
?>
