<!doctype html>
<html>
	<head>
		<meta name="viewport" content="width=device-width, initial-scale=1">
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
			
			div.body pre {
				word-wrap: break-word;
				margin: 10px;
				padding: 10px;
				border: 1px solid #aaa;
				border-radius: 3px;
				background-color: #eee;
				white-space: normal;
			}
			
			div.body img.qr {
				width: 300px;
				margin-left: auto;
				margin-right: auto; 
				display: block;
			}
			
			@media screen and (max-width: 720px) {
				html, body {
					font-size: 20px;
				}
				
				div.body {
					width: auto;
					margin: 0;
				}
				
				div.body img.qr {
					width: 100%;
				}
			}
		</style>
	</head>
	<body>
		<div class="body">
			<?php
				include_once("config.php");
				include_once("functions.php");
				
				if(!isset($_GET["label"])) {
					$id = uniqid();
					$inv = ln_query("invoice", array("any", $id, "void.cat tip"));
					
					if(isset($inv->result)) {
						header("location: /src/php/lightning-tip.php?label=" . $id);
					} else {
						echo "<pre>" . json_encode($inv) . "</pre>";
					}
				} else {
					$id = $_GET["label"];
					$inv = ln_query("listinvoices", array($id));
					if(isset($inv->result) && isset($inv->result->invoices[0])) {
						$i = $inv->result->invoices[0];
						
						echo "<pre>" . $i->bolt11 . "</pre>";
						
						$cmd = "/usr/local/bin/myqr lightning:" . $i->bolt11 . " -n " . $id . ".png -c -d /tmp/ 2>&1";						
						$qr = shell_exec($cmd);
						$img_b64 = base64_encode(file_get_contents(substr(explode(", ", substr(explode("\n", $qr)[1], 1, -1))[3], 1, -1)));
						
						echo "<img class=\"qr\" src=\"data:image/png;base64," . $img_b64 . "\"/>";
					} else {
						echo "<pre>" . json_encode($inv) . "</pre>";
					}
				}
			?>
		</div>
	</body>
</html>