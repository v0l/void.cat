<?php

function XFastDownload($location, $filename, $mimeType = 'application/octet-stream')
{
	$url = "https://www.google-analytics.com/collect";
	$payload = "v=1&tid=UA-73200448-1&cid=" . session_id() . "&t=pageview&dh=" . $_SERVER['HTTP_HOST'] . "&dp=" . urlencode($_SERVER['REQUEST_URI']) . "&uip=" . $_SERVER['REMOTE_ADDR'] . "&ua=" . urlencode($_SERVER["HTTP_USER_AGENT"]) . "&dr=" . urlencode($_SERVER["HTTP_REFERER"]);

	$ch = curl_init();

	curl_setopt($ch, CURLOPT_URL, $url);
	curl_setopt($ch, CURLOPT_POST, 1);
	curl_setopt($ch, CURLOPT_POSTFIELDS, $payload);
	
	curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
	curl_exec($ch);
	curl_close ($ch);
	
	$expire = 604800;
	
	header("X-Accel-Redirect: $location");
	header("Cache-Control: public, max-age=$expire");
	header("Content-type: $mimeType");
	header('Content-Disposition: inline; filename="' . $filename . '"');
}
?>