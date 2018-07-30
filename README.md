 * Nginx >= 1.13.0
 * php7.0-fpm & php7.0-dev
 * Redis >= 4.0.0
 * phpredis >= (develop branch)
 
```
cat src/db.sql | mysql -D YOUR_DB -p
```

```
location ~ "^\/[0-9a-z\.]{36,40}$" {
	try_files $uri /src/php/download.php;
}
```

```
fastcgi_read_timeout 1200;
```

For lightning tips i recommend using `socat` to connect to the rpc file.
```
socat TCP-LISTEN:9737,bind=127.0.0.1,reuseaddr,fork,range=127.0.0.0/8 UNIX-CLIENT:/root/.lightning/lightning-rpc 1> /var/log/socat-lightning-log 2>&1 &
```