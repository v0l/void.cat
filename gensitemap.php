<?php
	require_once('sitemap-php/Sitemap.php');
	require_once('db.php');

	$sitemap = new Sitemap('https://example.com');
	$sitemap->setPath('sitemap/');
	$sitemap->addItem('/', '1.0');

	$db = new DB();
	$links = $db->GetFiles();

	foreach($links as $f){
		$url = '/' . $f->hash160 . '&v';
		$sitemap->addItem($url, '0.8', 'daily');
	}

	$sitemap->createSitemapIndex('https://example.com/sitemap/', 'Today');
?>
