CREATE TABLE `files` (
  `hash160` varchar(40) NOT NULL,
  `hash256` varchar(64) NOT NULL,
  `filename` varchar(255) NOT NULL,
  `mime` varchar(64) NOT NULL,
  `size` int(11) NOT NULL,
  `path` varchar(512) NOT NULL,
  `views` int(11) DEFAULT 0 NULL,
  `isAdminFile` bit(1) DEFAULT 0 NULL,
  `uploaded` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `lastview` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`hash160`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8