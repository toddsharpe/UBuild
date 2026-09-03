#!/bin/bash
#
# A pre-build step: writes a header and a source naming the toolchain and exe it was run for.
set -e

OUT="$1"
mkdir -p "$OUT"
echo "#define STAMP \"$2\"" > "$OUT/stamp.h"
echo "const char* Stamp() { return \"$2\"; }" > "$OUT/stamp.c"
