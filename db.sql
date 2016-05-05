CREATE TABLE `files` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `hash160` varchar(40) DEFAULT NULL,
  `hash256` varchar(64) DEFAULT NULL,
  `mime` varchar(64) DEFAULT NULL,
  `path` varchar(512) DEFAULT NULL,
  `filename` varchar(255) DEFAULT NULL,
  `views` int(11) DEFAULT 0 NULL,
  `created` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `hs160` (`hash160`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=54 DEFAULT CHARSET=latin1
