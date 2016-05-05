<?php 
	$audio_url = _SITEURL . $f->hash160; 
?>
<audio controls style="width: 100%">
	<source src="<?php echo $audio_url; ?>" type="<?php echo $f->mime; ?>">
</audio>