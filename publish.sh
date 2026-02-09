#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$SCRIPT_DIR/src/ClaudeTokens/ClaudeTokens.csproj"
APP_NAME="Claude Tokens"
APP_BUNDLE="$SCRIPT_DIR/publish/$APP_NAME.app"
RID="osx-arm64"

echo "Building Claude Tokens..."
export PATH="$HOME/.dotnet:$PATH"

dotnet publish "$PROJECT" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=false \
  -o "$SCRIPT_DIR/publish/bin"

echo "Creating app bundle..."
rm -rf "$APP_BUNDLE"
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy published files into the bundle
cp -R "$SCRIPT_DIR/publish/bin/"* "$APP_BUNDLE/Contents/MacOS/"

# Copy Info.plist
cp "$SCRIPT_DIR/src/ClaudeTokens/Info.plist" "$APP_BUNDLE/Contents/Info.plist"

# Generate .icns from icon.png
ICON_SRC="$SCRIPT_DIR/src/ClaudeTokens/Assets/icon.png"
if [ -f "$ICON_SRC" ]; then
  ICONSET="$SCRIPT_DIR/publish/app.iconset"
  mkdir -p "$ICONSET"

  # Use sips to resize the icon to required sizes
  for SIZE in 16 32 128 256 512; do
    sips -z $SIZE $SIZE "$ICON_SRC" --out "$ICONSET/icon_${SIZE}x${SIZE}.png" >/dev/null 2>&1
    DOUBLE=$((SIZE * 2))
    sips -z $DOUBLE $DOUBLE "$ICON_SRC" --out "$ICONSET/icon_${SIZE}x${SIZE}@2x.png" >/dev/null 2>&1
  done

  iconutil -c icns "$ICONSET" -o "$APP_BUNDLE/Contents/Resources/app.icns" 2>/dev/null || true
  rm -rf "$ICONSET"
fi

# Clean up loose publish folder
rm -rf "$SCRIPT_DIR/publish/bin"

echo ""
echo "Done! App bundle created at:"
echo "  $APP_BUNDLE"
echo ""
echo "To install, drag it to /Applications or run:"
echo "  cp -R \"$APP_BUNDLE\" /Applications/"
