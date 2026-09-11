#include <stdio.h>

extern int GetVersion();
extern int GetSubVersion();

int main()
{
    //Two bases up: GetVersion, GetSubVersion and FROM_BASE are Base's, FROM_MIDDLE is Middle's
    printf("Chained: %d\n", GetVersion() + GetSubVersion() + FROM_BASE + FROM_MIDDLE + FROM_CHAINED);
}
