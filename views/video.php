<?php 
	$audio_url = _SITEURL . $f->hash160; 
?>
<video controls>
	<source src="<?php echo $audio_url; ?>" type="<?php echo $f->mime; ?>">
	Your browser does not support the video tag.
</video>