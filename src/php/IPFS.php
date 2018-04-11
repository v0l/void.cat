<?php

class IPFS {
	private $url;

	function __construct($ip = "localhost", $port = "5001") {
		$this->url = "http://$ip:$port";
	} 
	
	public function add($file, $name, $size, $mime) {
		$bnd_id = uniqid();
		
		$ch = curl_init();
		$options = array(
			CURLOPT_URL => $this->url . "/api/v0/add?pin=false",
			CURLOPT_BINARYTRANSFER => True,
			CURLOPT_RETURNTRANSFER => True,
			CURLOPT_HEADER => False,
			CURLOPT_TIMEOUT => 5,
			CURLOPT_HTTPHEADER => array(
				"Content-Type: multipart/form-data; boundary=$bnd_id"
			),
			CURLOPT_POST => True,
			CURLOPT_POSTFIELDS => "--$bnd_id\r\nContent-Type: $mime\r\nContent-Disposition: file; \r\n\r\n" . stream_get_contents($file) . "\r\n--$bnd_id"
		);
		curl_setopt_array($ch, $options);
		$output = curl_exec($ch);
		curl_close($ch);
		
		return explode("\n", $output);
	}
}

