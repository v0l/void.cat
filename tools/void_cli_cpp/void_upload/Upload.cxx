#pragma warning(disable:4996)
#include "Upload.h"
#include "Util.h"

#include <curl/curl.h>
#include <nlohmann/json.hpp>

static int init_upload_state(upload_state* st) {
	st->ctx = (VBF_CTX*)malloc(sizeof(VBF_CTX));
	st->ctx->mode = VBFMODE::ENCRYPT;
	
	st->bi = (vbf_buf*)malloc(sizeof(vbf_buf));
	st->bo = (vbf_buf*)malloc(sizeof(vbf_buf));

	st->bi->len = ENC_ALGO::BLOCKSIZE * 1024;
	st->bi->buf = (unsigned char*)malloc(st->bi->len);

	vbf_init(st->ctx);
	return 1;
}

static int free_upload_state(upload_state* st) {
	free(st->bi->buf);
	free(st->bi);
	free(st->bo);
	free(st);

	return 1;
}

static int curl_upload_write(void *ptr, size_t size, size_t nmemb, void *stream) {
	upload_state* state = (upload_state*)stream;

	((char*)ptr)[1 + (size * nmemb)] = 0; //null terminate the json
	fprintf(stdout, "Got response for file %s: %s\n", state->filename, (char*)ptr);
	nlohmann::json json_parsed = nlohmann::json::parse(nlohmann::detail::input_adapter((char*)ptr, size * nmemb));
	if (json_parsed["status"].get<int>() == 200) {
		unsigned char* kh = to_hex(state->ctx->key, ENC_ALGO::DEFAULT_KEYLENGTH);
		unsigned char* ih = to_hex(state->ctx->iv, ENC_ALGO::BLOCKSIZE);
		fprintf(stdout, "https://%s/#%s:%s:%s\n", state->upload_host, json_parsed["id"].get<std::string>().c_str(), kh, ih);
		free(kh);
		free(ih);
	}
	else {
		fprintf(stderr, "Upload failed: %s\n", json_parsed["msg"].get<std::string>().c_str());
	}

	return (size * nmemb);
}

static int curl_upload_read(void *ptr, size_t size, size_t nmemb, void *stream) {
	upload_state* state = (upload_state*)stream;

	unsigned int offset = 0;
	int target_size = min(size * nmemb, state->bi->len);

	//clamp the buffer len to whichever is smaller
	state->bi->len = target_size - (target_size % ENC_ALGO::BLOCKSIZE); 

	//set our output buffer to the curl buffer
	state->bo->len = state->bi->len;
	state->bo->buf = (unsigned char*)ptr;

	bool start = ftell(state->file) == 0;
	if (feof(state->file)) {
		return 0;
	}
	else {
		if (start) {
			fseek(state->file, 0, SEEK_END);
			long flen = ftell(state->file);
			rewind(state->file);

			VBFPayloadHeader h;
			h.len = flen;
			h.mime = "application/x-msdownload";
			h.filename = state->filename;

			state->bo->len = state->bi->len -= ENC_ALGO::BLOCKSIZE - sizeof(VBFHeader); //reduce by 1 block to allow space for header
			vbf_start_buffer(state->ctx, &h, state->bi, offset);

			fprintf(stdout, "Using header: %s\n", state->bi->buf + sizeof(VBFHeader) + sizeof(uint16_t));
			unsigned char* kh = to_hex(state->ctx->key, 16);
			unsigned char* ih = to_hex(state->ctx->iv, 16);
			fprintf(stdout, "Encrypting %s with key %s and iv %s\n", state->filename, kh, ih);
			free(kh);
			free(ih);
		}

		int nread = fread(state->bi->buf + offset, 1, state->bi->len - offset, state->file);
		if (nread != state->bi->len - offset) {
			if (start) {
				offset -= sizeof(VBFHeader);
				state->bi->buf += sizeof(VBFHeader);
				state->bo->buf += sizeof(VBFHeader);
			}
			state->bi->len = offset + nread;

			vbf_encrypt_na_end(state->ctx, state->bi, offset, state->bo);

			if (start) {
				offset += sizeof(VBFHeader);
				state->bi->buf -= sizeof(VBFHeader);
				state->bo->buf -= sizeof(VBFHeader);
				state->bo->len += sizeof(VBFHeader);
				memcpy(state->bo->buf, state->bi->buf, sizeof(VBFHeader));
			}

			unsigned char* hh = to_hex(state->bo->buf + (state->bo->len - HMAC_DGST::DIGESTSIZE), HMAC_DGST::DIGESTSIZE);
			fprintf(stdout, "HMAC is: %s\n", hh);
			free(hh);
		}
		else {
			if (start) {
				vbf_encrypt_na_start(state->ctx, state->bi, offset, state->bo);
			}
			else {
				vbf_encrypt_na(state->ctx, state->bi, offset, state->bo);
			}
		}

		int rlen = state->bo->len;
		if (start) {
			state->bo->len = state->bi->len += ENC_ALGO::BLOCKSIZE - sizeof(VBFHeader);
		}
		return rlen;
	}

	return 0;
}

int uploadFile(const char* file, const char* hostname, bool verbose) {
	upload_state* ustate = (upload_state*)malloc(sizeof(upload_state));
	memset(ustate, 0, sizeof(upload_state));

	if (!init_upload_state(ustate)) {
		return 1;
	}
	ustate->file = fopen(file, "rb");
	if (!ustate->file) {
		print_sys_error();
		return 1;
	}

#ifdef WIN32
	const char* fname = strrchr(file, '\\') + 1;
#else
	const char* fname = strrchr(file, '//') + 1;
#endif
	ustate->filename = fname;
	ustate->upload_host = hostname;

	CURL *curl;
	CURLcode res;

	fprintf(stdout, "Starting upload for %s\n", ustate->filename);
	char url[512];
	sprintf(url, "http://%s/upload", ustate->upload_host);

	curl_global_init(CURL_GLOBAL_DEFAULT);
	curl = curl_easy_init();
	if (curl) {
		curl_easy_setopt(curl, CURLOPT_UPLOAD, 1L);
		curl_easy_setopt(curl, CURLOPT_URL, url);
		curl_easy_setopt(curl, CURLOPT_POST, 1L);
		curl_easy_setopt(curl, CURLOPT_READFUNCTION, curl_upload_read);
		curl_easy_setopt(curl, CURLOPT_READDATA, ustate);
		curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, curl_upload_write);
		curl_easy_setopt(curl, CURLOPT_WRITEDATA, ustate);

		if (verbose) {
			curl_easy_setopt(curl, CURLOPT_VERBOSE, 1L);
		}
		curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, 0L);

		res = curl_easy_perform(curl);
		if (res != CURLE_OK) {
			fprintf(stderr, "curl_easy_perform() failed: %s\n", curl_easy_strerror(res));
		}
		curl_easy_cleanup(curl);
	}

	free_upload_state(ustate);
}
