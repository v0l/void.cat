<?php
/**
* Reads the requested portion of a file and sends its contents to the client with the appropriate headers.
* 
* This HTTP_RANGE compatible read file function is necessary for allowing streaming media to be skipped around in.
* 
* @param string $location
* @param string $filename
* @param string $mimeType
* @return void
* 
* @link https://groups.google.com/d/msg/jplayer/nSM2UmnSKKA/Hu76jDZS4xcJ
* @link http://php.net/manual/en/function.readfile.php#86244
*/
function smartReadFile($location, $filename, $mimeType = 'application/octet-stream')
{
	if (!file_exists($location))
	{
		header ("HTTP/1.1 404 Not Found");
		return;
	}

	$size	= filesize($location);
	$ftime  = filemtime($location);
	$time	= date('r', $ftime);

	$fm		= @fopen($location, 'rb');
	if (!$fm)
	{
		header ("HTTP/1.1 505 Internal server error");
		return;
	}

	$begin	= 0;
	$end	= $size - 1;

	if (isset($_SERVER['HTTP_RANGE']))
	{
		if (preg_match('/bytes=\h*(\d+)-(\d*)[\D.*]?/i', $_SERVER['HTTP_RANGE'], $matches))
		{
			$begin	= intval($matches[1]);
			if (!empty($matches[2]))
			{
				$end	= intval($matches[2]);
			}
		}
	}

	if (isset($_SERVER['HTTP_RANGE']))
	{
		header('HTTP/1.1 206 Partial Content');
	}
	else
	{
		header('HTTP/1.1 200 OK');
	}

	$expire = 604800;
	header("Content-Type: $mimeType");
	header("Cache-Control: public, max-age=$expire");
	header("Accept-Ranges: bytes");
	header("Content-Length: " . (($end - $begin) + 1));
	if (isset($_SERVER['HTTP_RANGE']))
	{
		header("Content-Range: bytes $begin-$end/$size");
	}
	header("Content-Disposition: inline; filename=$filename");
	header("Content-Transfer-Encoding: binary");
	header("Last-Modified: $time");
	header("Expires: " . date('r', strtotime("+$expire seconds")));

	$cur	= $begin;
	fseek($fm, $begin, 0);

	while(!feof($fm) && $cur <= $end && (connection_status() == 0))
	{
		print fread($fm, min(1024 * 16, ($end - $cur) + 1));
		$cur += 1024 * 16;
	}
}

?>
