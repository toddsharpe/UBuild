#!/bin/bash
#
# A post-build step: pins the toolchain environment, $OutFile, and an argument holding a colon.
set -e

echo "step arg: $1"
echo "step objcopy: $ObjCopy"
test -f "$OutFile"
echo "step ok" > "$OutFile.step"
