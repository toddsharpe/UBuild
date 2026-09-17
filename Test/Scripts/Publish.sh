#!/bin/bash
#
# A project post-build step: records what it was handed, and fails when UBUILD_TEST_PUBLISH_FAIL is set.
set -e

if [ -n "${UBUILD_TEST_PUBLISH_FAIL:-}" ]; then
	echo "publish refused" >&2
	exit 1
fi

mkdir -p "$OutputExeDir"
printf 'arg=%s\nProject=%s\nExes=%s\nOutputExeDir=%s\n' "$1" "$Project" "$Exes" "$OutputExeDir" > "$OutputExeDir/$Project.published"
