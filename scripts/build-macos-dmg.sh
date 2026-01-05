#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET_BIN="${DOTNET_BIN:-dotnet}"
APP_NAME="${1:-AutomacaoGDA}"
RUNTIME_ID="${2:-osx-arm64}"
EXEC_NAME="${3:-MeuProjeto.UI}"
SELF_CONTAINED="${4:-true}"

PROJECT_FILE="$ROOT_DIR/src/MeuProjeto.UI/MeuProjeto.UI.csproj"
if [[ ! -f "$PROJECT_FILE" && -f "$ROOT_DIR/AutomacaoGDA/src/MeuProjeto.UI/MeuProjeto.UI.csproj" ]]; then
  ROOT_DIR="$ROOT_DIR/AutomacaoGDA"
  PROJECT_FILE="$ROOT_DIR/src/MeuProjeto.UI/MeuProjeto.UI.csproj"
fi

if [[ ! -f "$PROJECT_FILE" ]]; then
  echo "Erro: projeto nao encontrado. Esperado em:"
  echo "  $PROJECT_FILE"
  exit 1
fi

PUBLISH_DIR="$ROOT_DIR/src/MeuProjeto.UI/bin/Release/net8.0/${RUNTIME_ID}/publish"
APP_DIR="$PUBLISH_DIR/${APP_NAME}.app/Contents"

echo "Publishing for ${RUNTIME_ID}..."
"$DOTNET_BIN" publish "$PROJECT_FILE" -c Release -r "${RUNTIME_ID}" --self-contained "${SELF_CONTAINED}"

echo "Building .app bundle..."
rm -rf "$PUBLISH_DIR/${APP_NAME}.app"
mkdir -p "$APP_DIR/MacOS" "$APP_DIR/Resources"
cp -R "$PUBLISH_DIR/"* "$APP_DIR/MacOS/"

cat > "$APP_DIR/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key><string>${EXEC_NAME}</string>
  <key>CFBundleIdentifier</key><string>com.automacaogda</string>
  <key>CFBundleName</key><string>${APP_NAME}</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleVersion</key><string>1.0.0</string>
  <key>CFBundleShortVersionString</key><string>1.0.0</string>
</dict>
</plist>
EOF

echo "Creating DMG..."
hdiutil create -volname "${APP_NAME}" \
  -srcfolder "$PUBLISH_DIR/${APP_NAME}.app" \
  -ov -format UDZO \
  "$PUBLISH_DIR/${APP_NAME}.dmg"

echo "Done."
echo "App:  $PUBLISH_DIR/${APP_NAME}.app"
echo "DMG:  $PUBLISH_DIR/${APP_NAME}.dmg"
