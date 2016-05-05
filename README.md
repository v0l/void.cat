# baba
Simple file upload with statistics

## Features 

 * Async uploads
 * View counter
 * Copy/Paste uploads
 * Drag&Drop uploads
 * File browser uploads
 * Eye pain while reading logo text
 * Random background colors
 
## Screenshots

![screenshot1](http://shit.host/d37c6bcb25b42d8493d43634a12ee6e2b6241f8aa33eb3b5b55c7552f90c1b65/baba0.PNG)
![screenshot2](http://shit.host/4e6e7c4598533d2e29b1b10d14600333c9fae901ff477b5f05ad8fcfadc080c2/baba1.PNG)
![screenshot3](http://shit.host/bf544fd2b1cc9f32b4556062c7bb77bd64647211c134e7d3811fbd8b43707ca6/baba2.PNG)

## Roadmap 

See issues.


##Install

### Requirements 

 * nginx (or other)
 * php5
 * php5-mysql
 * mysql-server
 
### Setup

Start by configuring your ```config.php``` with details for you mysql server.

Next import the sql script to create the table

```
cat db.sql | mysql -p -D baba
```

Next you need to add a rule to you webserver to use index.php for 404 errors, below is an example for nginx

```
location / {
	try_files $uri index.php?hash=$uri;
}
```

If this is not setup correctly your file links will not work.


Another thing you will need to do is adjust the max post size in PHP and nginx, for nginx you add the following:

```
client_max_body_size 512M;
```

Or whatever you want to the max file size to be.

In ```php.ini``` change the following:

```
memory_limit = 512M
post_max_size = 512M
```

You will need to set the memory limit to the same size as your desired max file size since the file is stored in memory while reading from the client. 

```post_max_size``` is the size you will see on the home page.

Finally make sure the PHP process has access to the directory where files will be saved.

The default directory is ```out``` in the root of the site. To set this up do the following.

```
mkdir out
mkdir out/thumbs
chown www-data:www-data out -R
chmod 770 out -R
```

Make sure to reset php5 and your webserver so settings apply

Run composer

```
php composer.phar install
```

## License 

Whats that? 