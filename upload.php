<?php
	include('db.php');
	
	$response = array(
		"status" => 0,
		"msg" => null,
		"hash" => null,
		"publichash" => null,
		"link" => null,
		"mime" => null,
		"filename" => null
	);
	
	//check input size is large enough
	$maxsizeM = ini_get('post_max_size');
	$maxsize = (int)(str_replace('M', '', $maxsizeM) * 1024 * 1024);
	$fsize = (int)$_SERVER['CONTENT_LENGTH'];
	$mime = strlen($_SERVER['CONTENT_TYPE']) > 0 ? $_SERVER['CONTENT_TYPE'] : _DEFAULT_TYPE;
	$fname = $_GET["filename"];
	
	if($fsize > $maxsize)
	{
		$response["msg"] = "File size larger than " . $maxsizeM;
	}
	else
	{
		$rawf = fopen('php://input', 'rb');
		$tmpf = fopen('php://temp', 'rb+');
		stream_copy_to_stream($rawf, $tmpf);
		rewind($tmpf);
		
		//Connect to db
		$db = new DB();
		
		//get file hash
		$hc = hash_init('sha256');
		hash_update_stream($hc, $tmpf);
		$fh = hash_final($hc);
		$response["hash"] = $fh;
		rewind($tmpf);
		
		//check for dupes
		$f_e = $db->Exists256($fh);
		if($f_e->id != 0)
		{
			//file already exists
			$response["publichash"] = $f_e->hash160;
			$response["mime"] = $f_e->mime;
		}
		else
		{
			//file does not exist
			//generate public hash
			$phc = hash_init('ripemd160');
			hash_update($phc, $fh);
			$ph = hash_final($phc);
			$response["publichash"] = $ph;

			//save to disk
			$op = _FILEPATH . $ph;
			$fo = fopen($op, 'wb+');
			stream_copy_to_stream($tmpf, $fo);
			fclose($fo);
			
			//save to db
			$f_e = new FileUpload();
			$f_e->hash160 = $ph;
			$f_e->hash256 = $fh;
			$f_e->mime = $mime;
			$f_e->path = $op;
			$f_e->filename = $fname;
			
			$db->InsertFile($f_e);

			//update sitemap
			//include("gensitemap.php");
		}

		//close streams
		fclose($rawf);
		fclose($tmpf);
		
		$response["status"] = 200;
		$response["link"] = _SITEURL . $f_e->hash160;
		$response["mime"] = $mime;
	}
	
	//return response
	header('Content-Type: application/json');
	echo json_encode($response);
?>  
