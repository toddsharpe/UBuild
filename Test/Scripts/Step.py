#!/usr/bin/env python3
#
# A post-build step in Python: pins the toolchain environment and $OutFile the same way Step.sh does.
import os, sys

print(f"pystep arg: {sys.argv[1]}")
print(f"pystep objcopy: {os.environ['ObjCopy']}")
out = os.environ["OutFile"]
assert os.path.isfile(out)
with open(out + ".pystep", "w") as f:
	f.write("pystep ok\n")
