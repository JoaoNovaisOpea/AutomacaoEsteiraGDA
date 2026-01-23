#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET_BIN="${DOTNET_BIN:-dotnet}"
APP_NAME="${1:-AutomacaoGDA}"
RUNTIME_ID="${2:-osx-arm64}"
EXEC_NAME="${3:-AutomacaoGDA.UI}"
SELF_CONTAINED="${4:-true}"

PROJECT_FILE="$ROOT_DIR/src/AutomacaoGDA.UI/AutomacaoGDA.UI.csproj"
if [[ ! -f "$PROJECT_FILE" && -f "$ROOT_DIR/AutomacaoGDA/src/AutomacaoGDA.UI/AutomacaoGDA.UI.csproj" ]]; then
  ROOT_DIR="$ROOT_DIR/AutomacaoGDA"
  PROJECT_FILE="$ROOT_DIR/src/AutomacaoGDA.UI/AutomacaoGDA.UI.csproj"
fi

if [[ ! -f "$PROJECT_FILE" ]]; then
  echo "Erro: projeto nao encontrado. Esperado em:"
  echo "  $PROJECT_FILE"
  exit 1
fi

PUBLISH_DIR="$ROOT_DIR/src/AutomacaoGDA.UI/bin/Release/net8.0/${RUNTIME_ID}/publish"
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

echo "Preparing DMG contents..."
DMG_TEMP="$PUBLISH_DIR/dmg-contents"
rm -rf "$DMG_TEMP"
mkdir -p "$DMG_TEMP"

# Copia o .app para a pasta temporária
cp -R "$PUBLISH_DIR/${APP_NAME}.app" "$DMG_TEMP/"

# Copia o script de instalação
INSTALL_SCRIPT="$ROOT_DIR/../scripts/install-macos.sh"
if [[ ! -f "$INSTALL_SCRIPT" ]]; then
  INSTALL_SCRIPT="$ROOT_DIR/scripts/install-macos.sh"
fi

if [[ -f "$INSTALL_SCRIPT" ]]; then
  cp "$INSTALL_SCRIPT" "$DMG_TEMP/Instalar ${APP_NAME}.command"
  chmod +x "$DMG_TEMP/Instalar ${APP_NAME}.command"
  echo "  ✓ Script de instalação incluído"
else
  echo "  ⚠ Script de instalação não encontrado: $INSTALL_SCRIPT"
fi

# Cria link simbólico para /Applications (padrão de DMGs do macOS)
ln -s /Applications "$DMG_TEMP/Applications"

# Cria arquivo README com instruções
cat > "$DMG_TEMP/LEIA-ME.txt" <<'READMEEOF'
========================================
    Instalação - AutomacaoGDA
========================================

OPÇÃO 1 - INSTALADOR AUTOMÁTICO (RECOMENDADO):
  1. Duplo-clique em "Instalar AutomacaoGDA.command"
  2. Se aparecer aviso de segurança, clique com botão direito > Abrir
  3. Siga as instruções na tela

OPÇÃO 2 - INSTALAÇÃO MANUAL:
  1. Arraste AutomacaoGDA.app para a pasta Applications
  2. Abra o Terminal e execute:
     xattr -cr /Applications/AutomacaoGDA.app
  3. Agora pode abrir o AutomacaoGDA normalmente

========================================

Em caso de problemas, entre em contato com o suporte.
READMEEOF

echo "Creating DMG..."
hdiutil create -volname "${APP_NAME}" \
  -srcfolder "$DMG_TEMP" \
  -ov -format UDZO \
  "$PUBLISH_DIR/${APP_NAME}.dmg"

# Limpa pasta temporária
rm -rf "$DMG_TEMP"

echo "Done."
echo "App:  $PUBLISH_DIR/${APP_NAME}.app"
echo "DMG:  $PUBLISH_DIR/${APP_NAME}.dmg"
