#pragma once

#include "VBF.h"

typedef struct {
	const char* filename;
	const char* upload_host;
	FILE* file;
	VBF_CTX* ctx;
	vbf_buf* bi;
	vbf_buf* bo;
} upload_state;

int uploadFile(const char* file, const char* hostname, bool verbose);