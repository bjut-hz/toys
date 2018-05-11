#include <iostream>
using namespace std;

#include "memmgr.h"
#include "complex.h"
#define _CRTDBG_MAP_ALLOC     
#include <crtdbg.h> 
int main(int argc, char* argv[]) {
	//_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);
	//_CrtSetBreakAlloc(174);
	gMemoryManager = new MemoryManager();

	Complex* array[ 1000 ];
	for (int i = 0; i < 500; i++) {
		for (int j = 0; j < 1000; j++) {
			array[ j ] = new Complex(i, j);
		}
		for (int j = 0; j < 1000; j++) {
			delete array[ j ];
		}
	}
	delete gMemoryManager;

	_CrtDumpMemoryLeaks();
	return 0;
}
