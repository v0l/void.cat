<?php
	function AddFirewallRule($ip) {
		$post = array(
			'mode' => 'challenge',
			'configuration' => array(
				'target' => 'ip',
				'value' => $ip
			),
			'notes' => 'void.cat auto block'
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
			$discord_data = array("content" => "[IP BLOCKED] " . $ip);
			include_once("discord.php");
		}
		
		return $cfr;
	}

	if(isset($_GET["ip"])) {
		include_once("config.php");
		var_dump(AddFirewallRule($_GET["ip"]));
	}
?>