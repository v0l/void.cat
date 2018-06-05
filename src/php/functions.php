<?php
	include_once("config.php");
	
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
		return json_decode(curl_text($url));	
	}
	
	function curl_text($url) 
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
		$val = curl_text("https://btgexp.com/ext/getaddress/" . $addr);
		return (object) [
			"balance" => floatval($val),
			"txns" => 0
		];
	}
	
	function GetAddrInfo_ZEC($addr)
	{
		$val = curl_text("https://api.zcha.in/v2/mainnet/accounts/" . $addr);
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
?>
