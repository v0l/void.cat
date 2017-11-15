<?php
    include("db.php");

    echo 'Cleaning files...';

    $db = new DB();
    $fl = $db->GetExpiredFiles();

    foreach($fl as $f) {
	if(unlink($f->path)) {
	    $db->DeleteFile($f);
	    echo 'Deleted file: ' . $f->filename . ' (' . $f->hash160 . ') \n';
	    $del[] = $f->hash160;
	}else{
	    echo 'Cant delete file ' . $f->path . ' \n';
	}
    }

    if(count($fl) > 0){
    	$discord_data = array("content" => 'Deleted ' . count($fl) . ' expired files. `' . implode("` `", $del) . '`');
    	include('discord.php');
    }
?>
