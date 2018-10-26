 
Setup
===

 * Nginx
 * php-fpm
 * php-redis 
 * php-curl
 * Redis
 
 Nginx handler
 ====
```
location ~ "^\/([0-9a-z\.]{36,40})$" {
	try_files $uri /src/php/handler.php?h=download&hash=$1;
}
```

Void Binary File Format (VBF)
===
| Name | Type | Description |
|---|---|---|
| version | uint8_t | Binary file format version |
| hash | 32 byte hash | The hash of the unencrypted file |
| payload | >EOF | The encrypted payload |

---
VBF Payload Format

| Name | Type | Description |
|---|---|---|
| header_length | uint16_t | Length of the header section |
| header | string | The header json |
| file | >EOF | The file |
