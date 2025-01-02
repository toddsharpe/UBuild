#include <stdio.h>

extern int GetVersion();

int main()
{
    printf("Hello world2\n");
    printf("Version: %d\n", GetVersion());
}