<?php
	if(_DISCORD_WEBHOOK != 'DISCORD_HOOK_URL') 
	{
		$curl = curl_init(_DISCORD_WEBHOOK);
		curl_setopt($curl, CURLOPT_CUSTOMREQUEST, "POST");
		curl_setopt($curl, CURLOPT_POSTFIELDS, json_encode($discord_data));
		curl_setopt($curl, CURLOPT_RETURNTRANSFER, true);
		curl_exec($curl);
	}
?>