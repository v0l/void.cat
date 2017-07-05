<?php
	session_start();
	
	//check for view param otherwise return file
	$hash = isset($_GET["hash"]) ? substr($_GET["hash"], strrpos($_GET["hash"], '/') + 1) : null;
	if(!isset($_GET["v"]) && $hash != null)
	{
		include_once('db.php');

		$db = new DB();
		$f = $db->GetFile($hash);
		if($f->id != 0){
			include_once('download2.php');
			XFastDownload(_UPLOADDIR . $f->hash160, $f->filename, $f->mime);
			
			if(!isset($_SERVER['HTTP_RANGE'])){
				$db->AddView($f->hash160);
			}
		}
		
		exit;
	}
?>
<!DOCTYPE html>
<html prefix="og: http://ogp.me/ns#">
	<head>
		<?php 
			$f = null;
			if($hash != null){
				include_once('db.php');
				
				$db = new DB();
				$f = $db->GetFile($hash);
			}
			$title = 'void.cat';
			$maxsizeM = ini_get('post_max_size');
			$maxsize = (int)(str_replace('M', '', $maxsizeM) * 1024 * 1024);
			echo "<script>var max_upload_size = " . $maxsize . ";</script>";
		?>
		<title><?= $title . ($f != null ? ' - ' . $f->filename : '') ?></title>
		<meta charset="utf-8">
		<meta name="viewport" content="width=device-width, initial-scale=1">
		<meta name="keywords" content="baba,file,host,upload,free">
		<meta name="description" content="Free file host">
		<?php
			if($hash != null){
				if($f->id != 0){
					echo "<meta property=\"og:title\" content=\"" . $f->filename . "\" />";
					echo "<meta property=\"og:site_name\" content=\"" . $title . "\" />";
					
					$content_url = _SITEURL . $f->hash160; 
					if(strpos($f->mime, "image/") === 0) {
						echo "<meta property=\"og:image:url\" content=\"" . $content_url . "\" />";
						echo "<meta property=\"og:image:type\" content=\"" . $f->mime . "\" />";
					}else if(strpos($f->mime, "audio/") === 0) {
						echo "<meta property=\"og:audio\" content=\"" . $content_url . "\" />";
						echo "<meta property=\"og:audio:type\" content=\"" . $f->mime . "\" />";
					}else if(strpos($f->mime, "video/") === 0) {
						echo "<meta property=\"og:video\" content=\"" . $content_url . "\" />";
						echo "<meta property=\"og:video:type\" content=\"" . $f->mime . "\" />";
						
						$ld = array(
							"@context" => "http://schema.org",
							"@type" => "VideoObject",
							"name" => $f->filename,
							"description" => $f->filename . " Video",
							"thumbnailUrl" => $content_url . "&thumb",
							"uploadDate" => $f->created,
							"contentUrl" => $content_url . "&v",
							"embedUrl" => $content_url,
							"interactionCount" => $f->views
						);
						
						echo "<script type=\"application/ld+json\">" . json_encode($ld) . "</script>";
					}
				}
			}
		?>
		
		<link rel="stylesheet" href="public/main.css" />
	</head>
	
	<body>
		<div id="main">
			<?php include_once('config.php'); ?>
			<div id="header" onclick="window.location.href = '<?php echo _SITEURL; ?>';"><?= $title ?></div>
			<?php 
				if($hash != null){
					if($f->id != 0){
						$db->AddView($f->hash160);
						
						if(strpos($f->mime, "image/") === 0) {
							require_once('views/image.php');
						}else if(strpos($f->mime, "audio/") === 0) {
							require_once('views/audio.php');
						}else if(strpos($f->mime, "video/") === 0) {
							require_once('views/video.php');
						}else {
							require_once('views/default.php');
						}
						
						require_once('views/stats.php');
					}else{
						echo "<h1>File Not Found :/</h1>";
					}
				}else{
						echo "<div id=\"uploads\" style=\"display: none\"></div><div id=\"upload\">Drop Files < " . $maxsizeM . "</div>";
				}
			?>
			<div id="history">
				<h3>Your Uploads</h3>
				<small>History is saved in <a style="display: initial; padding: initial; margin: initial;" href="https://www.w3schools.com/html/html5_webstorage.asp">localStorage</a> <b style="cursor: pointer; user-select: none;" onclick="localStorage.setItem('history', ''); window.location.reload();">(clear)</b></small>
			</div>
			<div id="footer">
				<a href="https://github.com/v0l/void.cat">Github</a>
				| <a href="https://twitter.com/chkn10deez">Twitter</a>
				| Hosting: <?php echo explode("\t", exec("du -sh " . _FILEPATH))[0]; ?>
				<br/><small>Files expire in 30 days if not viewed</small>
			</div>
		</div>
		<script src="public/main.js"></script>
		<script>
			var h = loadHistory();
			var hl = document.querySelector('#history');
			for(var x = h.length - 1; x >= 0; x--) {
				var hx = h[x];
				var nh = document.createElement('a');
				nh.href =  hx.link + '&v';
				nh.target = '_blank'
				nh.text = (hx.filename === null ? 'clipboard' : hx.filename);
				
				hl.appendChild(nh);
			}
		</script>
	</body>
</html>
