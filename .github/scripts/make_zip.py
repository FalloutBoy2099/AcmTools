#!/usr/bin/env python3
"""Creates a zip whose entries keep their Unix permissions, on any operating system.

    python make_zip.py OUTPUT.zip SOURCE=NAME_IN_ZIP[:x] ...

Add ":x" after the name to store the entry as executable (mode 0755), so that the
extracted file runs directly on Linux and macOS.
"""

import os
import sys
import time
import zipfile


def main():
    if len(sys.argv) < 3:
        sys.exit(__doc__)

    output = sys.argv[1]
    if os.path.exists(output):
        os.remove(output)

    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for spec in sys.argv[2:]:
            source, _, name = spec.partition("=")
            executable = name.endswith(":x")
            name = name[:-2] if executable else name
            info = zipfile.ZipInfo(name or os.path.basename(source), time.localtime(os.path.getmtime(source))[:6])
            info.compress_type = zipfile.ZIP_DEFLATED
            info.create_system = 3  # Unix, so that the permissions below are honoured
            info.external_attr = (0o100755 if executable else 0o100644) << 16
            with open(source, "rb") as f:
                archive.writestr(info, f.read())

    print("%s: %d bytes" % (output, os.path.getsize(output)))


if __name__ == "__main__":
    main()