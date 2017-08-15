<?php
	include_once('config.php');
	include_once('file.php');
	
	class DB {
		function __construct()
		{
			$this->error = null;
			$this->mysqli = new mysqli(_DB_HOST, _DB_USER, _DB_PASS, _DB_DATABASE);
			
			if ($this->mysqli->connect_errno) {
				$this->error = "Failed to connect to MySQL: (" . $mysqli->connect_errno . ") " . $mysqli->connect_error;
			}
		}
		
		function __destruct()
		{
			if($this->error == null)
			{
				$this->mysqli->close(); 
			}
		}
		
		function Exists256($hash)
		{
			$res = new FileUpload();
			
			$stmt = $this->mysqli->prepare("select id, hash160, hash256, mime, path, filename, views, created, expire from files where hash256 = ? limit 1");
			if($stmt)
			{
				$stmt->bind_param("s", $hash);
				$stmt->execute();
				$stmt->bind_result($res->id, $res->hash160, $res->hash256, $res->mime, $res->path, $res->filename, $res->views, $res->created, $res->expire);
				$stmt->fetch();
				$stmt->close();
			}
			
			return $res;
		}
		
		function GetFile($hash)
		{
			$res = new FileUpload();
			
			$stmt = $this->mysqli->prepare("select id, hash160, hash256, mime, path, filename, views, created, expire from files where hash160 = ? limit 1");
			if($stmt)
			{
				$stmt->bind_param("s", $hash);
				$stmt->execute();
				$stmt->bind_result($res->id, $res->hash160, $res->hash256, $res->mime, $res->path, $res->filename, $res->views, $res->created, $res->expire);
				$stmt->fetch();
				$stmt->close();
			}
			
			return $res;
		}

		function GetFiles()
		{
			$res = array();

			$stmt = $this->mysqli->prepare("select id, hash160, hash256, mime, path, filename, views, created, expire from files");
			if($stmt)
			{
				$stmt->execute();
				$stmt->bind_result($id, $hash160, $hash256, $mime, $path, $filename, $views, $created, $expire);
				while($stmt->fetch()){
					$nf = new FileUpload();
					$nf->id = $id;
					$nf->hash160 = $hash160;
					$nf->hash256 = $hash256;
					$nf->mime = $mime;
					$nf->path = $path;
					$nf->filename = $filename;
					$nf->views = $views;
					$nf->created = $created;
					$nf->expire = $expire;
					
					array_push($res, $nf);
				}
				$stmt->close();
			}

			return $res;
		}
		
		function InsertFile($f)
		{
			$stmt = $this->mysqli->prepare("insert into files(hash160, hash256, mime, path, filename, expire) values(?,?,?,?,?, DATE_ADD(NOW(), INTERVAL " . _FILE_EXPIRE_TIME . " DAY))");
			if($stmt)
			{
				$stmt->bind_param("sssss", $f->hash160, $f->hash256, $f->mime, $f->path, $f->filename);
				$stmt->execute();
				$stmt->close();
			}
		}
		function DeleteFile($f)
		{
			$stmt = $this->mysqli->prepare("delete from files where id = ?");
			if($stmt)
			{
				$stmt->bind_param("d", $f->id);
				$stmt->execute();
				$stmt->close();
			}
		}
		function AddView($hash160)
		{
			$stmt = $this->mysqli->prepare("update files set views = views + 1, expire = DATE_ADD(NOW(), INTERVAL " . _FILE_EXPIRE_TIME . " DAY) where hash160 = ?");
			if($stmt)
			{
				$stmt->bind_param("s", $hash160);
				$stmt->execute();
				$stmt->close();
			}
		}
		function GetExpiredFiles()
		{
			$res = array();

			$stmt = $this->mysqli->prepare("select id, hash160, hash256, mime, path, filename, views, created, expire from files where expire < CURRENT_TIMESTAMP");
			if($stmt)
			{
				$stmt->execute();
				$stmt->bind_result($id, $hash160, $hash256, $mime, $path, $filename, $views, $created, $expire);
				while($stmt->fetch()){
					$nf = new FileUpload();
					$nf->id = $id;
					$nf->hash160 = $hash160;
					$nf->hash256 = $hash256;
					$nf->mime = $mime;
					$nf->path = $path;
					$nf->filename = $filename;
					$nf->views = $views;
					$nf->created = $created;
					$nf->expire = $expire;
					
					array_push($res, $nf);
				}
				$stmt->close();
			}

			return $res;
		}
	};
?>
