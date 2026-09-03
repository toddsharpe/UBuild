#include <stdio.h>
#include "stamp.h"

extern const char* Stamp();

int main()
{
    //One from the generated header, one from the generated source
    printf("Stamped: %s %s\n", STAMP, Stamp());
}
