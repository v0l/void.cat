```
location ~ "\/[0-9a-z]{40}$" {
	try_files $uri /src/php/download.php;
}
```