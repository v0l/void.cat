<?php
	function ScanStream($res, $slen) {
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

	
	if(isset($_GET["check_test"])) {
		
		header("Content-Type: application/json");
		
		include_once("config.php");
		include_once("db.php");
		$redis = new Redis();
		$redis->pconnect(_REDIS_SERVER);
		$db = new DB();
		
		$f = $db->GetFile($_GET["check_test"]);
		ScanStream(fopen($f->path, 'r'), $f->size);
	}
?>