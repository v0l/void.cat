<?php
	require_once('db.php');

	$response = array(
		"status" => 0,
		"msg" => null,
		"hash" => null,
		"publichash" => null,
		"link" => null,
		"mime" => null,
		"filename" => null,
		"upload" => json_encode($_FILES)
	);
	
	$isMultipart = strpos($_SERVER['CONTENT_TYPE'], 'multipart/form-data') !== False;
	
	//check input size is large enough
	$maxsizeM = ini_get('post_max_size');
	$maxsize = (int)(str_replace('M', '', $maxsizeM) * 1024 * 1024);
	$fsize = $isMultipart ? $_FILES['files']['size'][0] : (int)$_SERVER['CONTENT_LENGTH'];
	$mime = $isMultipart ? $_FILES['files']['type'][0] : (strlen($_SERVER['CONTENT_TYPE']) > 0 ? $_SERVER['CONTENT_TYPE'] : _DEFAULT_TYPE);
	$fname = $isMultipart ? $_FILES['files']['name'][0] : $_GET["filename"];
	
	if($fsize > $maxsize)
	{
		$response["msg"] = "File too big";
	}
	else
	{
		$tmpf = 0;
		
		if ($isMultipart === False) 
		{
			$source = isset($_GET["remote"]) ? $_GET["remote"] : "php://input";
			
			$rawf = fopen($source, 'rb');
			
			if(isset($_GET["remote"])){
				$meta_data = stream_get_meta_data($rawf);
				foreach($meta_data["wrapper_data"] as $hd){
					if(strpos($hd, "Content-Type") === 0){
						$nt = explode(": ", $hd);
						$mime = $nt[1];
						if(strpos($mime, ";") > 0){
							$ms = explode(";", $mime);
							$mime = $ms[0];
						}
					}else if(strpos($hd, "Content-Disposition") === 0){
						$nn = explode("filename=", $hd);
						$fname = str_replace("\"", "", $nn[1]);
					}
				}
				
				if($fname == "remote"){
					//parse url if no content-disposition is set
					preg_match('@^.*\/(.*\.[a-zA-Z0-9]{0,4})@i', $_GET["remote"], $matches);
					if(count($matches) > 0){
						$fname = $matches[1];
					}
				}
			}
			
			$tmpf = fopen("php://temp", 'rb+');
			stream_copy_to_stream($rawf, $tmpf);
			rewind($tmpf);
			fclose($rawf);
		}
		else 
		{
			$f = $_FILES['files'];
			$tmpf = fopen($f['tmp_name'][0], 'rb');
		}
		
		//Connect to db
		$db = new DB();
		
		//get file hash
		$hc = hash_init('sha256');
		hash_update_stream($hc, $tmpf);
		$fh = hash_final($hc);
		$response["hash"] = $fh;
		rewind($tmpf);
		
		$response["filename"] = $fname;
		
		//check for dupes
		$f_e = $db->Exists256($fh);
		if($f_e->hash160 != NULL)
		{
			//file already exists
			$response["status"] = 200;
			$response["publichash"] = $f_e->hash160;
			$response["link"] = _SITEURL . $f_e->hash160;
			$response["mime"] = $f_e->mime;
			
			if($isMultipart) {
				$response["success"] = true;
				$response["files"] = array(array("url" => $response["link"]));
			}
		}
		else
		{
			/*
			//file does not exist
			//check clamav
			include_once('clamav.php');
			$sr = ScanStream($tmpf, $fsize);
			if($sr !== "stream: OK"){
				$discord_data = array("content" => $sr);
				include("discord.php");
			}else{
				$response["clamav"] = $sr;
			}*/
			
			//check with VT (max upload is 32MB)
			/*if($fsize < 32 * 1000 * 1000) {
				$redis = new Redis();
				$redis->pconnect(_REDIS_SERVER);
		
				include_once('virustotal.php');
				$vtr = CheckVirusTotalCached($redis, $fh);
				if($vtr != null && isset($vtr->response_code) && $vtr->response_code == 0) {
					$sr = ScanFile($tmpf);
					$discord_data = array("content" => $sr->verbose_msg);
					include("discord.php");
				}
			}*/
			
			//generate public hash
			$phc = hash_init('ripemd160');
			hash_update($phc, $fh);
			$ph = hash_final($phc);
			$response["publichash"] = $ph;
			//save to disk
			$op = _FILEPATH . $ph;
			$fo = fopen($op, 'wb+');
			if($fo !== False){
				stream_copy_to_stream($tmpf, $fo);
				fclose($fo);
				
				//save to db
				$f_e = new FileUpload();
				$f_e->hash160 = $ph;
				$f_e->hash256 = $fh;
				$f_e->mime = $mime;
				$f_e->size = filesize($op);
				$f_e->path = $op;
				$f_e->filename = $fname;
				
				$db->InsertFile($f_e);
				$discord_data = array("content" => _SITEURL . '#' . $f_e->hash160);
				include("discord.php");
				
				$response["status"] = 200;
				$response["link"] = _SITEURL . $f_e->hash160; 
				$response["mime"] = $mime;
				
				if($isMultipart) {
					$response["success"] = true;
					$response["files"] = array(array("url" => $response["link"]));
				}
			}else{
				$response["status"] = 500;
				$response["msg"] = "Server error!";
			}
		}
		//close streams
		fclose($tmpf);
	}
	
	//return response
	header('Content-Type: application/json');
	echo json_encode($response);
?>  
