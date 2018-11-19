 
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

Void Binary File Format (VBF)
===
| Name | Type | Description |
|---|---|---|
| version | uint8_t | Binary file format version |
| hash | SHA256 hash | The hash of the unencrypted file |
| uploaded | uint32_t | Timestamp of when the upload started |
| payload | >EOF | The encrypted payload |

---
VBF Payload Format

| Name | Type | Description |
|---|---|---|
| header_length | uint16_t | Length of the header section |
| header | string | The header json |
| file | >EOF | The file |
