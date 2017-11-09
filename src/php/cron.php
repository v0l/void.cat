<?php
    include("db.php");

    echo 'Cleaning files...';

    $db = new DB();
    $fl = $db->GetExpiredFiles();

    foreach($fl as $f) {
	if(unlink($f->path)) {
	    $db->DeleteFile($f);
	    echo 'Deleted file: ' . $f->filename . ' (' . $f->hash160 . ')\n';
	}else{
	    echo 'Cant delete file ' . $f->hash160 . '\n';
	}
    }

    if(count($fl) > 0){
    	$discord_data = array("content" => 'Deleted ' . count($fl) . ' expired files.');
    	include('discord.php');
    }
?>