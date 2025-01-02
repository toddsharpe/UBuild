#include <stdio.h>

extern int GetVersion();
extern int GetSubVersion();
extern int GetRev();

int main()
{
    printf("Hello world\n");
    printf("Version: %d\n", GetVersion());
    printf("Version: %d\n", GetSubVersion());
    printf("Version: %d\n", GetRev());
}