#pragma warning(disable:4996)
#include "Util.h"
#include "Upload.h"

#include <argtable2.h>

#define CLI_VERSION "void_util v0.1"

struct arg_lit *verb, *help;
struct arg_file *upload, *pack;
struct arg_str *download, *host;
struct arg_end *end;

void *argtable[] = {
	help = arg_lit0("h", "help", "Displays this help message."),
	verb = arg_lit0("v", "verbose", "Verbose CURL output"),
	upload = arg_file0("u", "upload", "<file>", "Uploads a file"),
	download = arg_str0("d", "download", "<url>", "Downloads a file"),
	pack = arg_file0("p", "pack", "<file>", "Packs a file into VBF format \"<file>.vbf\""),
	host = arg_str0(NULL, "host", "<hostname>", "Sets custom server hostname for uploading"),
	end = arg_end(20),
};

int main(int argc, char* argv[]) {
	int nerrors = arg_parse(argc, argv, argtable);

	bool missingMode = upload->count == 0 && download->count == 0 && pack->count == 0;
	if (help->count > 0 || nerrors > 0 || missingMode)
	{
		if (nerrors > 0) {
			arg_print_errors(stdout, end, CLI_VERSION);
		}
		fprintf(stdout, "Usage: %s", CLI_VERSION);
		arg_print_syntax(stdout, argtable, "\n");
		arg_print_glossary(stdout, argtable, "  %-25s %s\n");
		goto exit;
	}

	if (upload->count > 0) {
		const char* fn = upload->filename[0];
		const char* hn = host->count > 0 ? host->sval[0] : "v3.void.cat";
		bool verbose = verb->count > 0;
		uploadFile(fn, hn, verbose);
		goto exit;
	}

	if (pack->count > 0) {
		const char* fn = pack->filename[0];
		char* fo = (char*)malloc(strlen(fn) + 5);
		sprintf(fo, "%s.vbf", fn);

		VBF_CTX* ctx = (VBF_CTX*)malloc(sizeof(VBF_CTX));
		ctx->mode = VBFMODE::ENCRYPT;
		vbf_init(ctx);
	
		FILE *ffi, *ffo;
		ffi = fopen(fn, "rb");
		ffo = fopen(fo, "wb+");

		if (!ffi || !ffo) {
			fprintf(stderr, "IO error\n");
		}
		else {
#ifdef _MSC_VER 
			const char* fname = strrchr(fn, '\\') + 1;
#else
			const char* fname = strrchr(fn, '//') + 1;
#endif
			vbf_encrypt_file(ctx, fname, ffi, ffo);

			fprintf(stdout, "Done!");
			fflush(ffo);
			fclose(ffi);
			fclose(ffo);
		}

		free(fo);
		free(ctx);
	}
exit:
	arg_freetable(argtable, sizeof(argtable) / sizeof(argtable[0]));
	return 0;
}