<?php
	include_once("config.php");
	include_once("functions.php");
		
	$uri_format = array(
		"BTC" => "bitcoin:%s",
		"BCH" => "bitcoincash:%s",
		"BTG" => "bitcoingold:%s",
		"DASH" => "dash:%s",
		"LTC" => "litecoin:%s",
		"ZEC" => "zcash:%s",
		"ETH" => "ethereum:%s",
		"EOS" => "ethereum:%s",
		"TRX" => "ethereum:%s",
		"ETC" => "ethereum:%s&id=61",
		"XEM" => "nem:%s"
	);
	
	$redis = new Redis();
	$redis->pconnect(_REDIS_SERVER);
	
	$inf = array();
	$inf_cache = $redis->get("tip_info_cache");
	if($inf_cache == false) {
		$inf = GetAllAddrInfo(_TIP_ADDRS);
		$redis->setEx("tip_info_cache", 3600, json_encode($inf));
	}else{
		$inf = json_decode($inf_cache);
	}
?>
<!doctype html>
<html>
	<head>
		<meta name="viewport" content="width=device-width, initial-scale=1">
		<title>Tips</title>
		<style>
			html, body {
				padding: 0;
				margin: 0;
				font-family: Arial;
				font-size: 12px;
			}
			
			div.body {
				width: 720px;
				margin-left: auto;
				margin-right: auto;
				margin-top: 10px;
				
				border-radius: 10px;
				border: 1px solid #888;
				overflow:hidden;
				padding: 10px;
			}
			
			div.body div.tip-row {
				margin-top: 10px;
				overflow: hidden;
				padding: 10px;
				
				background-color: #ccc;
				border-radius: 3px;
				border: 1px solid #555;
				
				line-height: 24px;
			}
			
			div.body div.tip-row img {
				float: left;
				height: 24px;
				margin-right: 10px;
			}
			
			div.body div.tip-row div {
				float: left;
			}
			div.body div.tip-row div.bal {
				float: right;
			}
			
			@media screen and (max-width: 720px) {
				div.body {
					width: auto;
					margin: 0;
				}
				div.body div.tip-row div.bal {
					display: none;
				}
			}
		</style>
	</head>
	<body>
		<div class="body">
			<p>Tips help me get drunk, please consider tipping if you like the service I am currenly paying all the server bills myself.</p>
			<?php
				foreach($inf as $addr) 
				{
					$addr_name = (isset($uri_format[strtoupper($addr->currency)]) ? ("<a href=\"" . sprintf($uri_format[$addr->currency], $addr->address) . "\">" . $addr->address . "</a>") : $addr->address);
					echo "<div class=\"tip-row\"><img src=\"/src/img/" . strtolower($addr->currency) . ".png\"/><div class=\"addr\">" . $addr_name . "</div><div class=\"bal\">" . strtoupper($addr->currency) . " " . number_format($addr->balance, 8) . "</div></div>";
				}
			?>
		</div>
	</body>
</html>