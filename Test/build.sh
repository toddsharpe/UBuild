#
#!/bin/bash
#

set -e

../latest.sh Build --project ${1:-ALL} --verbose --toolchain ${2:-All}
