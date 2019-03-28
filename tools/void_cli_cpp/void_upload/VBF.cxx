#pragma warning(disable:4996)
#include "VBF.h"
#include "Util.h"

#include <ctime>
#include <cryptopp/osrng.h>

int vbf_init(VBF_CTX* ctx) {
	ctx->hmac_ctx = new HMAC_DGST();

	CryptoPP::AutoSeededRandomPool prng;
	prng.GenerateBlock(ctx->key, ENC_ALGO::DEFAULT_KEYLENGTH);
	prng.GenerateBlock(ctx->iv, ENC_ALGO::BLOCKSIZE);
	ctx->hmac_ctx->SetKey(ctx->key, ENC_ALGO::DEFAULT_KEYLENGTH);

	if(ctx->mode == VBFMODE::ENCRYPT) {
		ctx->aes_enc_ctx = new CryptoPP::CBC_Mode<ENC_ALGO>::Encryption();
		ctx->aes_enc_ctx->SetKeyWithIV(ctx->key, ENC_ALGO::DEFAULT_KEYLENGTH, ctx->iv);
	} else {
		ctx->aes_dec_ctx = new CryptoPP::CBC_Mode<ENC_ALGO>::Decryption();
		ctx->aes_dec_ctx->SetKeyWithIV(ctx->key, ENC_ALGO::DEFAULT_KEYLENGTH, ctx->iv);
	}
	return 1;
}

VBFHeader vbf_make_header() {
	VBFHeader vheader;
	memcpy(vheader.magic, VOID_MAGIC, MAGIC_LEN);
	vheader.uploaded = (uint32_t)std::time(0);

	return vheader;
}

int vbf_set_key(VBF_CTX* ctx, unsigned char* key, unsigned char* iv) {
	memcpy(ctx->key, key, ENC_ALGO::DEFAULT_KEYLENGTH);
	memcpy(ctx->iv, iv, ENC_ALGO::BLOCKSIZE);

	if (ctx->mode == VBFMODE::ENCRYPT) {
		ctx->aes_enc_ctx->SetKeyWithIV(ctx->key, ENC_ALGO::DEFAULT_KEYLENGTH, ctx->iv);
	}
	else {
		ctx->aes_dec_ctx->SetKeyWithIV(ctx->key, ENC_ALGO::DEFAULT_KEYLENGTH, ctx->iv);
	}

	return 1;
}

int vbf_encrypt_file(VBF_CTX* ctx, const char* filename, FILE* in, FILE* out) {
	int bsize = ENC_ALGO::BLOCKSIZE * 1024;

	unsigned char* kh = to_hex(ctx->key, 16);
	unsigned char* ih = to_hex(ctx->iv, 16);
	fprintf(stdout, "Encrypting %s with key %s and iv %s\n", filename, kh, ih);
	free(kh);
	free(ih);

	unsigned int offset;
	vbf_buf i;
	i.len = bsize;
	i.buf = (unsigned char*)malloc(i.len);

	vbf_buf o;
	o.len = bsize;
	o.buf = (unsigned char*)malloc(o.len);

	bool start = true;
	for (;;) {
		offset = 0;

		if (feof(in)) {
			break;
		}
		else {
			if (start) {
				fseek(in, 0, SEEK_END);
				long flen = ftell(in);
				rewind(in);

				VBFPayloadHeader h;
				h.len = flen;
				h.mime = "application/x-msdownload";
				h.filename = filename;

				o.len = i.len -= ENC_ALGO::BLOCKSIZE - sizeof(VBFHeader); //reduce by 1 block to allow space for header
				vbf_start_buffer(ctx, &h, &i, offset);

				fprintf(stdout, "Using header: %s\n", i.buf + sizeof(VBFHeader) + sizeof(uint16_t));
			}

			int nread = fread(i.buf + offset, 1, i.len - offset, in);
			if (nread != i.len - offset) {
				//end
				if (start) {
					i.len = nread - sizeof(VBFHeader);
					i.buf += sizeof(VBFHeader);
					o.buf += sizeof(VBFHeader);
				}
				else {
					i.len = nread;
				}

				vbf_encrypt_na_end(ctx, &i, offset, &o);

				unsigned char* hh = to_hex(o.buf + (o.len - HMAC_DGST::DIGESTSIZE), HMAC_DGST::DIGESTSIZE);
				fprintf(stdout, "HMAC is: %s\n", hh);
				free(hh);

				if (start) {
					i.buf -= sizeof(VBFHeader);
					o.buf -= sizeof(VBFHeader);
					memcpy(i.buf, o.buf, sizeof(VBFHeader));
				}
			}
			else {
				if (start) {
					vbf_encrypt_na_start(ctx, &i, offset, &o);
				}
				else {
					vbf_encrypt_na(ctx, &i, offset, &o);
				}
			}

			if (!fwrite(o.buf, 1, o.len, out)) {
				return 0; //write problem
			}
			else if (start) {
				o.len = i.len += ENC_ALGO::BLOCKSIZE - sizeof(VBFHeader);
			}
		}
		start = false;
	}

	free(i.buf);
	free(o.buf);
	return 1;
}

int vbf_decrypt_file(VBF_CTX* ctx, FILE* in, FILE* out) {
	return 0;
}

int vbf_start_buffer(VBF_CTX* ctx, VBFPayloadHeader* header, vbf_buf* outBuf, unsigned int& offset) {
	if (ctx->mode != VBFMODE::ENCRYPT) {
		return 0;
	}
	if ((outBuf->len - sizeof(VBFHeader)) % ENC_ALGO::BLOCKSIZE != 0) {
		return 0;
	}

	//28 chars being the json wrapping around these values
	uint16_t json_len = 28 + strlen(header->filename) + strlen(header->mime) + findn(header->len);

	//buffer is too small
	if (outBuf->len < sizeof(VBFHeader) + 2 + json_len) {
		return 0;
	}

	//put vbf header
	VBFHeader vheader = vbf_make_header();

	//copy header to output
	memcpy(outBuf->buf, &vheader, sizeof(VBFHeader));
	memcpy(outBuf->buf + sizeof(VBFHeader), &json_len, sizeof(uint16_t));

	//by using sizeof(VBFHeader) + sizeof(uint16_t) you can find the start of the json string and print it
	sprintf((char*)outBuf->buf + sizeof(VBFHeader) + sizeof(uint16_t), "{\"name\":\"%s\",\"mime\":\"%s\",\"len\":%ld}", header->filename, header->mime, header->len);
	
	offset = sizeof(VBFHeader) + 2 + json_len;
	return 1;
}

int vbf_encrypt_start(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out) {
	out->len = in->len;
	out->buf = (unsigned char*)malloc(out->len);

	return vbf_encrypt_na_start(ctx, in, offset, out);
}

int vbf_encrypt_na_start(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out) {
	//copy the header to the output
	memcpy(out->buf, in->buf, sizeof(VBFHeader));
	ctx->aes_enc_ctx->ProcessData(out->buf + sizeof(VBFHeader), in->buf + sizeof(VBFHeader), in->len - sizeof(VBFHeader));
	ctx->hmac_ctx->Update(in->buf + offset, in->len - offset);

	return 1;
}

int vbf_encrypt(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out) {
	out->len = in->len;
	out->buf = (unsigned char*)malloc(out->len);

	return vbf_encrypt_na(ctx, in, offset, out);
}

int vbf_encrypt_na(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out) {
	ctx->aes_enc_ctx->ProcessData(out->buf, in->buf, in->len);
	ctx->hmac_ctx->Update(in->buf + offset, in->len - offset);

	return 1;
}

int vbf_encrypt_end(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out) {
	int padding = (ENC_ALGO::BLOCKSIZE - (in->len % ENC_ALGO::BLOCKSIZE));
	out->len = in->len + padding + HMAC_DGST::DIGESTSIZE;
	out->buf = (unsigned char*)malloc(out->len);

	return vbf_encrypt_end(ctx, in, offset, out);
}

int vbf_encrypt_na_end(VBF_CTX* ctx, vbf_buf* in, size_t offset, vbf_buf* out) {
	//Add PKCS#7 Padding
	int padding = (ENC_ALGO::BLOCKSIZE - (in->len % ENC_ALGO::BLOCKSIZE));
	memset(in->buf + in->len, padding, padding);

	ctx->aes_enc_ctx->ProcessData(out->buf, in->buf, in->len + padding);
	ctx->hmac_ctx->Update(in->buf + offset, in->len - offset);
	ctx->hmac_ctx->Final(ctx->hmac);

	//copy hmac to output
	memcpy(out->buf + in->len + padding, ctx->hmac, HMAC_DGST::DIGESTSIZE);
	out->len = in->len + padding + HMAC_DGST::DIGESTSIZE;

	return 1;
}

