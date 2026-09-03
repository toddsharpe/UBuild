#include <stdio.h>

//Exits non-zero so `UBuild Run` has something to report
int main()
{
    printf("Failing on purpose\n");
    return 1;
}
