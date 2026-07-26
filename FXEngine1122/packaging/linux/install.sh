#!/usr/bin/env bash
# Installs XephyreFX for the current user: copies the app to ~/.local/share/xephyrefx
# and registers a desktop entry so it shows up in your normal app launcher, same as
# anything installed via a package manager.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_DIR="$HOME/.local/share/xephyrefx"

mkdir -p "$INSTALL_DIR"
cp -r "$SCRIPT_DIR"/app/* "$INSTALL_DIR"/
chmod +x "$INSTALL_DIR/XephyreFX"

mkdir -p "$HOME/.local/share/applications"
sed "s#Exec=XephyreFX#Exec=$INSTALL_DIR/XephyreFX#" "$SCRIPT_DIR/xephyrefx.desktop" \
  > "$HOME/.local/share/applications/xephyrefx.desktop"

echo "Installed to $INSTALL_DIR"
echo "XephyreFX should now appear in your application launcher."
echo "(If it doesn't show up immediately, log out/in or run: update-desktop-database ~/.local/share/applications)"
