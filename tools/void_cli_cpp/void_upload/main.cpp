#include <ctime>

#include <cryptopp/osrng.h>
#include <cryptopp/hmac.h>
#include <cryptopp/sha.h>
#include <cryptopp/aes.h>
#include <cryptopp/ccm.h>

#include <cxxopts.hpp>
#include <curl/curl.h>
#include <nlohmann/json.hpp>

#define HMAC_DGST CryptoPP::HMAC<CryptoPP::SHA256>
#define ENC_ALGO CryptoPP::AES

#pragma warning(disable:4996)

const char MAGIC[] = "VOID";

typedef struct {
	const char* filename;
	bool headerSent;
	bool hmacSent;
	FILE* file;
	FILE* vbf_dump;

	//HMAC-SHA256
	HMAC_DGST *hmac_ctx;
	unsigned char hmac[HMAC_DGST::DIGESTSIZE];

	//AES128-CBC
	CryptoPP::CBC_Mode<ENC_ALGO>::Encryption *aes_ctx;
	unsigned char key[ENC_ALGO::DEFAULT_KEYLENGTH];
	unsigned char iv[ENC_ALGO::BLOCKSIZE];
} upload_state;

void print_sys_error() {
#ifdef WIN32
	LPSTR msg = 0;
	FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_STRING | FORMAT_MESSAGE_FROM_SYSTEM, (LPCVOID)&errno, NULL, NULL, msg, 1024, NULL);
	fprintf(stderr, msg);
#endif
}

static const char ht[] = "0123456789abcdef";
static inline unsigned char* to_hex(unsigned char* buf, size_t len) {
	unsigned char* ret = (unsigned char*)malloc((len * 2) + 1);
	memset(ret, 0, (len * 2) + 1);
	for (unsigned int x = 0; x < len; x++) {
		ret[x * 2] = ht[buf[x] >> 4];
		ret[(x * 2) + 1] = ht[buf[x] & 0x0F];
	}
	return ret;
}

int init_upload_state(upload_state* st) {
	st->hmac_ctx = new HMAC_DGST();
	st->aes_ctx = new CryptoPP::CBC_Mode<ENC_ALGO>::Encryption();

	CryptoPP::AutoSeededRandomPool prng;
	prng.GenerateBlock(st->key, ENC_ALGO::DEFAULT_KEYLENGTH);
	prng.GenerateBlock(st->iv, ENC_ALGO::BLOCKSIZE);

	st->aes_ctx->SetKeyWithIV(st->key, ENC_ALGO::DEFAULT_KEYLENGTH, st->iv);
	st->hmac_ctx->SetKey(st->key, ENC_ALGO::DEFAULT_KEYLENGTH);

	return 0;
}

static int curl_write(void *ptr, size_t size, size_t nmemb, void *stream) {
	upload_state* state = (upload_state*)stream;

	std::string json;
	json.assign((char*)ptr, size * nmemb);

	fprintf(stdout, "Got response for file %s: %s\n", state->filename, json.c_str());
	nlohmann::json json_parsed = nlohmann::json::parse(json);
	if (json_parsed["status"].get<int>() == 200) {
		std::cout << "https://v3.void.cat/#" << json_parsed["id"].get<std::string>() << ":" << to_hex(state->key, ENC_ALGO::DEFAULT_KEYLENGTH) << ":" << to_hex(state->iv, ENC_ALGO::BLOCKSIZE) << std::endl;
	}
	return size * nmemb;
}

static int curl_read(void *ptr, size_t size, size_t nmemb, void *stream) {
	upload_state* state = (upload_state*)stream;

	long fpos = ftell(state->file);
	//if its the start of the file create our header and send that first
	if (fpos == 0) {
		fseek(state->file, 0, SEEK_END);
		long len = ftell(state->file);
		rewind(state->file);

		nlohmann::json header = {
			{ "name", state->filename },
			{ "mime", "application/octet-stream" },
			{"len", len }
		};
		std::string header_json = header.dump();
		fprintf(stdout, "Using header: %s\n", header_json.c_str());

		unsigned char* buff = (unsigned char*)ptr;

		//put vbf header
		uint32_t time = (uint32_t)std::time(0);
		buff[0] = 0x02;
		memcpy(buff + 1, MAGIC, 4);
		memcpy(buff + 5, &time, 4);
		buff += 9;

		state->headerSent = true;
		int target_size = (size * nmemb) - 9;
		int actual_size = target_size - (target_size % ENC_ALGO::BLOCKSIZE);

		uint16_t header_len = header_json.size();
		unsigned char* enc_buffer = (unsigned char*)malloc(actual_size + ENC_ALGO::BLOCKSIZE);
		memset(enc_buffer, 0, actual_size);
		memcpy(enc_buffer, &header_len, 2);
		memcpy(enc_buffer + 2, header_json.data(), header_json.size());

		int header_size = 2 + header_json.size();
		int nread = fread(enc_buffer + header_size, 1, actual_size - header_size, state->file);
		if (nread != actual_size - header_size) {
			actual_size = nread + 2 + header_json.size();
		}

		fprintf(stdout, "Encrypting %s with key %s and iv %s\n", state->filename, to_hex(state->key, 16), to_hex(state->iv, 16));
		state->aes_ctx->ProcessData(buff, enc_buffer, actual_size);
		state->hmac_ctx->Update(enc_buffer + header_size, nread);
		free(enc_buffer);

		if (state->vbf_dump != 0) {
			fwrite(buff, 1, nread + header_size, state->vbf_dump);
		}
		return 9 + nread + header_size;
	}
	else {
		if (feof(state->file)) {
			return 0;
		}
		else {
			unsigned char* buff = (unsigned char*)ptr;
			int target_size = size * nmemb;
			int actual_size = target_size - (target_size % ENC_ALGO::BLOCKSIZE);
			unsigned char* enc_buffer = (unsigned char*)malloc(actual_size + ENC_ALGO::BLOCKSIZE + HMAC_DGST::DIGESTSIZE);
			memset(enc_buffer, 0, actual_size);

			int nread = fread(enc_buffer, 1, actual_size, state->file);
			if (nread != actual_size) {
				//Finalize the hmac
				state->hmac_ctx->Update(enc_buffer, nread);
				state->hmac_ctx->Final(state->hmac);

				//Add PKCS#7 Padding
				int padding = (ENC_ALGO::BLOCKSIZE - (nread % ENC_ALGO::BLOCKSIZE));
				int finalLen = nread + padding;
				memset(enc_buffer + nread, padding, padding);
				state->aes_ctx->ProcessData(buff, enc_buffer, finalLen);

				//copy the hmac to the output
				memcpy(buff + finalLen, state->hmac, HMAC_DGST::DIGESTSIZE);
				fprintf(stdout, "%s hmac is: %s\n", state->filename, to_hex(state->hmac, HMAC_DGST::DIGESTSIZE));

				//write to dump
				if (state->vbf_dump != 0) {
					fwrite(ptr, 1, HMAC_DGST::DIGESTSIZE + finalLen, state->vbf_dump);
				}

				free(enc_buffer);
				return HMAC_DGST::DIGESTSIZE + finalLen;
			}
			else {
				//update
				state->hmac_ctx->Update(enc_buffer, nread);
				state->aes_ctx->ProcessData(buff, enc_buffer, nread);

				//write to dump
				if (state->vbf_dump != 0) {
					fwrite(ptr, 1, nread, state->vbf_dump);
				}

				free(enc_buffer);
				return nread;
			}
		}
	}

	return 0;
}

int uploadFile(std::string file, bool verbose = false) {
	upload_state* ustate = (upload_state*)malloc(sizeof(upload_state));
	memset(ustate, 0, sizeof(upload_state));

	if (init_upload_state(ustate) != 0) {
		return 1;
	}
	ustate->file = fopen(file.c_str(), "rb");
	if (!ustate->file) {
		print_sys_error();
		return 1;
	}

#ifdef WIN32
	size_t lpos = file.find_last_of("\\");
#else
	size_t lpos = file.find_last_of("/");
#endif
	std::string filename = file.substr(lpos + 1);
	ustate->filename = filename.c_str();

	ustate->vbf_dump = fopen(file.append(".vbf").c_str(), "wb+");
	if (!ustate->vbf_dump) {
		print_sys_error();
		return 1;
	}

	CURL *curl;
	CURLcode res;

	fprintf(stdout, "Starting upload for %s\n", ustate->filename);

	curl_global_init(CURL_GLOBAL_DEFAULT);
	curl = curl_easy_init();
	if (curl) {
		curl_easy_setopt(curl, CURLOPT_UPLOAD, 1L);
		curl_easy_setopt(curl, CURLOPT_URL, "https://v3.void.cat/upload");
		curl_easy_setopt(curl, CURLOPT_READFUNCTION, curl_read);
		curl_easy_setopt(curl, CURLOPT_READDATA, ustate);
		curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, curl_write);
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
}

int main(int argc, char* argv[]) {
	cxxopts::Options options("void_upload", "Upload tool for void.cat");
	options.add_options()
		("f,file", "Upload file name", cxxopts::value<std::string>())
		("u,url", "Url to download", cxxopts::value<std::string>())
		("v,verbose", "Verbose logs");
	try {
		auto args_res = options.parse(argc, argv);

		if (args_res.count("file") > 0) {
			uploadFile(args_res["file"].as<std::string>(), args_res["verbose"].as<bool>());
		}
		else {
			fprintf(stderr, "file/url must be specified\n");
		}
	}
	catch (cxxopts::OptionException ex) {
		fprintf(stderr, "%s\n", ex.what());
		return 1;
	}
	catch (std::exception ex) {
		fprintf(stderr, "%s\n", ex.what());
		return 1;
	}

	return 0;
}