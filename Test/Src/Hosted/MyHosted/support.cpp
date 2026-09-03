#include "version.h"

//BASE_VERSION comes from the env-wide include dir, VERSION_BUMP from the env-wide defines.
extern "C" int GetVersion()
{
    return BASE_VERSION + VERSION_BUMP;
}
