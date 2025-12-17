#!/bin/bash

# Configuration
APP_ID="com.stremio.Stremio"
DEST_DIR="$HOME/.local/share/applications"
DEST_FILE="$DEST_DIR/$APP_ID.desktop"

# --- DYNAMIC CONFIGURATION ---
# Fetch the target URL
REPO_URL_SOURCE="https://raw.githubusercontent.com/aayushrautela/stremio-neo-installer/main/target_url"

echo "Stremio Custom Launch Configuration"
echo "-----------------------------------"
echo "Fetching configuration from repository..."

# Fetch URL, trim newlines/spaces
TARGET_URL=$(curl -s "$REPO_URL_SOURCE" | tr -d '\n' | tr -d '\r')

# Verify a valid URL
if [[ -z "$TARGET_URL" || "$TARGET_URL" != http* ]]; then
    echo "[ERROR] Could not fetch a valid URL from repository."
    echo "Checked: $REPO_URL_SOURCE"
    echo "Response was: $TARGET_URL"
    exit 1
fi

echo "[INFO] Target URL found: $TARGET_URL"
# -----------------------------

# Locations for the Flatpak export (System vs User install)
SYSTEM_SOURCE="/var/lib/flatpak/exports/share/applications/$APP_ID.desktop"
USER_SOURCE="$HOME/.local/share/flatpak/exports/share/applications/$APP_ID.desktop"

# 1. Verify Stremio Installation
if ! command -v flatpak &> /dev/null; then
    echo "[ERROR] Flatpak is not installed."
    exit 1
fi

if flatpak list | grep -q "$APP_ID"; then
    echo "[OK] Stremio Flatpak is installed."
else
    echo "[ERROR] Stremio (com.stremio.Stremio) is not installed via Flatpak."
    echo "Please install it first and try again."
    exit 1
fi

# 2. Locate the source desktop file
if [ -f "$SYSTEM_SOURCE" ]; then
    SOURCE_FILE="$SYSTEM_SOURCE"
    echo "[INFO] Found system installation source."
elif [ -f "$USER_SOURCE" ]; then
    SOURCE_FILE="$USER_SOURCE"
    echo "[INFO] Found user installation source."
else
    echo "[ERROR] Could not locate the .desktop file in standard Flatpak directories."
    exit 1
fi

# 3. Create local applications directory if missing
if [ ! -d "$DEST_DIR" ]; then
    mkdir -p "$DEST_DIR"
    echo "[INFO] Created directory: $DEST_DIR"
fi

# 4. Copy the file to the local directory
cp "$SOURCE_FILE" "$DEST_FILE"
if [ $? -eq 0 ]; then
    echo "[INFO] Successfully copied desktop file to user directory."
else
    echo "[ERROR] Failed to copy desktop file."
    exit 1
fi

# 5. Modify the Exec line
NEW_EXEC="Exec=/usr/bin/flatpak run --branch=beta --arch=x86_64 --command=sh com.stremio.Stremio -c 'stremio --url=\"$TARGET_URL\"'"

sed -i "s|^Exec=.*|$NEW_EXEC|" "$DEST_FILE"

if [ $? -eq 0 ]; then
    echo "[OK] Desktop file modified successfully."
else
    echo "[ERROR] Failed to modify the desktop file."
    exit 1
fi

# 6. Update the database
update-desktop-database "$DEST_DIR" 2>/dev/null
echo "[INFO] Desktop database updated."

echo "-----------------------------------"
echo "CONFIGURATION COMPLETE"
echo "Success! Stremio is now set to launch: $TARGET_URL"
echo "To ensure the changes take effect, please LOG OUT and LOG BACK IN (desktop environment)."
echo "-----------------------------------"