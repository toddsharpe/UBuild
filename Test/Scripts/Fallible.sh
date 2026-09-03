#!/bin/bash
#
# A pre-build step whose output compiles, unless UBUILD_TEST_BREAK is set and it does not.
set -e

OUT="$1"
mkdir -p "$OUT"
if [ -n "${UBUILD_TEST_BREAK:-}" ]; then
	echo "this is not c" > "$OUT/fallible.c"
else
	echo "const char* Fallible() { return \"$2\"; }" > "$OUT/fallible.c"
fi
