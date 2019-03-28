#include "Util.h"

#include <memory>

short findn(unsigned long num)
{
	if (num < 10UL)
		return 1;
	if (num < 100UL)
		return 2;
	if (num < 1000UL)
		return 3;
	if (num < 10000UL)
		return 4;
	if (num < 100000UL)
		return 5;
	if (num < 1000000UL)
		return 6;
	if (num < 10000000UL)
		return 7;
	if (num < 100000000UL)
		return 8;
	if (num < 1000000000UL)
		return 9;
	if (num < 10000000000UL)
		return 10;
	if (num < 100000000000UL)
		return 11;
	if (num < 1000000000000UL)
		return 12;
	if (num < 10000000000000UL)
		return 13;
	if (num < 100000000000000UL)
		return 14;
	if (num < 1000000000000000UL)
		return 15;
	if (num < 10000000000000000UL)
		return 16;
	if (num < 100000000000000000UL)
		return 17;
	if (num < 1000000000000000000UL)
		return 18;
	if (num < 10000000000000000000UL)
		return 19;
}

const char ht[] = "0123456789abcdef";
unsigned char* to_hex(unsigned char* buf, size_t len) {
	unsigned char* ret = (unsigned char*)malloc((len * 2) + 1);
	memset(ret, 0, (len * 2) + 1);
	for (unsigned int x = 0; x < len; x++) {
		ret[x * 2] = ht[buf[x] >> 4];
		ret[(x * 2) + 1] = ht[buf[x] & 0x0F];
	}
	return ret;
}

void print_sys_error() {
#ifdef WIN32
	LPSTR msg = 0;
	FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_STRING | FORMAT_MESSAGE_FROM_SYSTEM, (LPCVOID)&errno, NULL, NULL, msg, 1024, NULL);
	fprintf(stderr, msg);
#endif
}
