<?php
	include("config.php");
	
	header("Content-Type: text/json"); 
	echo json_encode(array("result" => file_exists(_FILEPATH . $_GET["hash"])));
?>