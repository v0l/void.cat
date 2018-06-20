<?php
	include_once("config.php");
	include_once("functions.php");
	
	$tips = ln_query("listinvoices", array())->result;
	$sp = GetBTCPrice();
?>
<!doctype html>
<html>
	<head>
		<title>Tip List</title>
		<style>
			html, body {
				margin: 0;
				padding: 0;
				font-family: Arial;
				font-size: 14px;
			}
			
			table {
				border-collapse: collapse;
			}
			
			th,td {
				border: 1px solid #333;
				padding: 5px;
			}
		</style>
	</head>
	<body>
		<h3>BTC price: $<?php echo $sp; ?></h3>
		<table>
			<thead>
				<tr>
					<th>id</th>
					<th>msatoshi</th>
					<th>USD</th>
					<th>status</th>
					<th>paid</th>
				</tr>
			</thead>
			<tbody>
			<?php
				$total = 0;
				
				foreach($tips->invoices as $inv) 
				{
					$col = "";
					switch($inv->status) {
						case "paid": $col = "#00ff00"; break;
						case "unpaid": $col = "#ffb100"; break;
					}
					
					$val = isset($inv->msatoshi_received) ? $inv->msatoshi_received : 0;
					if($inv->status === "paid") {
						$total += $val;
					}
					echo "<tr style=\"background-color: " . $col . ";\"><td>" . $inv->label . "</td><td>" . number_format($val, 8) . "</td><td>" . number_format($val * $sp * MSAT, 4) . "</td><td>" . $inv->status . "</td><td>" . (isset($inv->paid_at) ? date('Y/m/d H:i:s', $inv->paid_at) : "") . "</td></tr>";
				}
			?>
			</tbody>
		</table>
		<?php echo "<h3>Total: $" . number_format($total * $sp * MSAT, 4) . " (BTC " . number_format($total * MSAT, 8) . ")</h3>"; ?>
	</body>
</html>
