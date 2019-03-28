#pragma once

#include <cryptopp/aes.h>
#include <cryptopp/ccm.h>
#include <cryptopp/hmac.h>
#include <cryptopp/sha.h>

#define VOID_MAGIC "\x02OID\xf0\x9f\x90\xb1"
#define MAGIC_LEN 8
#define HMAC_DGST CryptoPP::HMAC<CryptoPP::SHA256>
#define ENC_ALGO CryptoPP::AES

typedef enum {
	ENCRYPT,
	DECRYPT
} VBFMODE;

typedef struct {
	char magic[8];
	uint32_t uploaded;
} VBFHeader;

typedef struct {
	const char* filename;
	const char* mime;
	long len;
} VBFPayloadHeader;

typedef struct {
	VBFMODE mode;

	//HMAC-SHA256
	HMAC_DGST* hmac_ctx;
	unsigned char hmac[HMAC_DGST::DIGESTSIZE];

	//AES128-CBC
	CryptoPP::CBC_Mode<ENC_ALGO>::Encryption* aes_enc_ctx;
	CryptoPP::CBC_Mode<ENC_ALGO>::Decryption* aes_dec_ctx;
	unsigned char key[ENC_ALGO::DEFAULT_KEYLENGTH];
	unsigned char iv[ENC_ALGO::BLOCKSIZE];
} VBF_CTX;

//Hints to use vbf_start_buffer for encryption/decryption
typedef struct {
	unsigned char* buf;
	size_t len;
} vbf_buf;

//CTX must be already allocated with the mode set to the mode you wish to use for this context
int vbf_init(VBF_CTX* ctx);
VBFHeader vbf_make_header();
int vbf_set_key(VBF_CTX* ctx, unsigned char* key, unsigned char* iv);
int vbf_encrypt_file(VBF_CTX* ctx, const char* filename, FILE* in, FILE* out);
int vbf_decrypt_file(VBF_CTX* ctx, FILE* in, FILE* out);

//This is only for encryption
//Puts the headers at the start of the buffer returning the offset of the payload data
//In VBF2 encryption starts after sizeof(VBFHeader) bytes
int vbf_start_buffer(VBF_CTX* ctx, VBFPayloadHeader* header, vbf_buf* outBuf, unsigned int& offset);

//encrypts the payload part of the in buffer create with vbf_start_buffer
//allocates the buffer on out
int vbf_encrypt_start(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out);
int vbf_encrypt_na_start(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out);

//encrypts the entire input and creates the output buffer
int vbf_encrypt(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out);
int vbf_encrypt_na(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out);

//in buf must have enough space to add padding, len must be the end of the data
//offset is only needed if your buffer contains the entire payload
int vbf_encrypt_end(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out);
int vbf_encrypt_na_end(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out);