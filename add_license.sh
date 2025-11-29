#!/bin/bash

# Текст лицензии
HEADER="// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.
"

TEMP_FILE=$(mktemp)

find . -name "*.cs" -not -path "*/bin/*" -not -path "*/obj/*" | while read -r file; do
    if ! grep -q "Copyright (C) 2024-2025 ZenonEl" "$file"; then
        echo "Adding header to: $file"
        echo "$HEADER" > "$TEMP_FILE"
        cat "$file" >> "$TEMP_FILE"
        mv "$TEMP_FILE" "$file"
    else
        echo "Skipping (already exists): $file"
    fi
done

rm "$TEMP_FILE"
echo "Done!"