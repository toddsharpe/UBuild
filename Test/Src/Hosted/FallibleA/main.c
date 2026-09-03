#include <stdio.h>

extern const char* Fallible();

int main()
{
    printf("FallibleA: %s\n", Fallible());
}
