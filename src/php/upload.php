<?php

class Upload {
	
	private $db;
	private $ipfs;
	
	function __construct($redis, $ipfs_host, $ipfs_port) {
			$this->db = $redis;
			$this->ipfs = new IPFS($ipfs_host, $ipfs_port);
	}
	
	public function ProcessMultipart() {
		$infi = $_FILES['files']['tmp_name'][0];
		$name = $_FILES['files']['name'][0];
		$size = $_FILES['files']['size'][0];
		$mime = $_FILES['files']['type'][0];
		return $this->ipfs->add(fopen($infi, "rb"), $name, $size, $mime);	
	}
	
	public function ProcessRawfile() {
		$infi = fopen("php://input", "rb");
		$mime = strlen($_SERVER['CONTENT_TYPE']) > 0 ? $_SERVER['CONTENT_TYPE'] : CFG_DEFAULT_TYPE;
		return $this->ipfs->add($infi, $mime);	
	}
	
	public function Process() {
		$isMulti = strpos($_SERVER['CONTENT_TYPE'], 'multipart/form-data') !== False;
		
		if($isMulti == True) {
			return $this->ProcessMultipart();
		}else {
			return $this->ProcessRawfile();
		}	
	}
}