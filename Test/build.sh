#!/bin/bash
#
# Builds the fixture with the Release UBuild, which is the one CI drives it with.

set -e

UBUILD=../Src/UBuild/bin/Release/net9.0/UBuild
if [ ! -x "$UBUILD" ]; then
	echo "build the builder first: dotnet build -c Release Src/UBuild/UBuild.csproj" >&2
	exit 2
fi

"$UBUILD" build --project ${1:-ALL} --verbose --toolchain ${2:-All}
