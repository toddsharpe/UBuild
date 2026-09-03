#include <stdio.h>

extern const char* Fallible();

int main()
{
    printf("FallibleB: %s\n", Fallible());
}
