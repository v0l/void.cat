 * Nginx >= 1.13.0
 * php7.0-fpm & php7.0-dev
 * Redis >= 4.0.0
 * phpredis >= (develop branch)
 
```
cat src/db.sql | mysql -D YOUR_DB -p
```

```
location ~ "^\/[0-9a-z]{40}$" {
	try_files $uri /src/php/download.php;
}
```