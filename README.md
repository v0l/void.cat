 
Setup
===

 * Nginx
 * php-fpm
 * php-redis 
 * php-curl
 * php-gmp
 * Redis
 
 Nginx handler
 ====
```
location ~* "^\/([0-9a-z]{27})$" {
		try_files $uri /src/php/handler.php?h=download&id=$1;
}
```

Void Binary File Format (VBF2)
===
*All numbers are little endian*

| Name | Size | Description |
|---|---|---|
| version | 1 byte unsigned number | Binary file format version |
| magic | 4 bytes | "VOID" encoded to UTF8 string |
| uploaded | 4 byte unsigned number | Unix timestamp of when the upload started |
| payload | EOF - 32 bytes | The encrypted payload |
| hash | 32 bytes HMAC-SHA265 | The HMAC of the unencrypted file* |

*\* Using the encryption key as the HMAC key*

VBF Payload Format
====
| Name | Size | Description |
|---|---|---|
| header_length | 2 byte unsigned number | Length of the header section |
| header | {header_length} UTF8 String | The header json |
| file | >EOF | The file |
