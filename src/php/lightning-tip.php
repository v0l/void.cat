<!doctype html>
<html>
	<head>
		<title>⚡ Tip! ⚡</title>
		<style>
			html, body {
				padding: 0;
				margin: 0;
				font-family: Arial;
				font-size: 12px;
			}
			
			div.body {
				width: 720px;
				margin-left: auto;
				margin-right: auto;
				margin-top: 10px;
				
				border-radius: 10px;
				border: 1px solid #888;
				overflow:hidden;
				padding: 10px;
			}
			
		</style>
	</head>
	<body>
		<div class="body">
			<?php
				include_once("config.php");
				include_once("functions.php");
				
				$id = uniqid();
				$inv = ln_query("invoice", array("any", $id, "void.cat tip"));
				
				if(isset($inv->result)) {
					echo "wip...";
					
					echo "<pre>" . $inv->result->bolt11 . "</pre>";
					
					$cmd = "/usr/local/bin/myqr lightning:" . $inv->result->bolt11 . " -n " . $id . ".png -c -d /tmp/ 2>&1";
					
					$qr = shell_exec($cmd);
					$img_b64 = base64_encode(file_get_contents(substr(explode(", ", substr(explode("\n", $qr)[1], 1, -1))[3], 1, -1)));
					
					echo "<img style=\"width: 300px\" src=\"data:image/png;base64," . $img_b64 . "\"/>";
				}else{
					echo "<pre>" . json_encode($inv) . "</pre>";
				}
			?>
		</div>
	</body>
</html>