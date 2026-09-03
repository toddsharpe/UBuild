#include <stdio.h>

extern int GetVersion();
extern int GetSubVersion();

int main()
{
    //GetVersion and GetSubVersion come from the base exe's sources, FROM_BASE from its defines
    printf("Extended: %d\n", GetVersion() + GetSubVersion() + FROM_BASE + FROM_DERIVED);
}
