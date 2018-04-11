<?php
	include_once("config.php");
	
	spl_autoload_register(function ($class_name) {
		include 'src/php/' . $class_name . '.php';
	});
	
	$up = new Upload(0, CFG_IPFS, CFG_IPFS_PORT);
	
	header("Content-Type: application/json");
	
	$ripfs = $up->Process();
	$ipfs = json_decode($ripfs[0]);
	$rsp = array(
		"url" => "https://ipfs.io/ipfs/" . $ipfs->Hash,
		"rsp" => $ipfs,
		"raw" => $ripfs
	);
	
	echo json_encode($rsp);
?>