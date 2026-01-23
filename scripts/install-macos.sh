#!/usr/bin/env bash
set -e

APP_NAME="AutomacaoGDA"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_SOURCE="$SCRIPT_DIR/$APP_NAME.app"
INSTALL_DIR="/Applications"

echo "=========================================="
echo "  Instalador - $APP_NAME"
echo "=========================================="
echo ""

# Verifica se o app existe no diretório do DMG
if [[ ! -d "$APP_SOURCE" ]]; then
    echo "❌ Erro: $APP_NAME.app não encontrado"
    echo "   Certifique-se de que o DMG foi montado corretamente."
    exit 1
fi

echo "📦 Copiando $APP_NAME para /Applications..."
# Remove versão antiga se existir
if [[ -d "$INSTALL_DIR/$APP_NAME.app" ]]; then
    echo "   Removendo versão anterior..."
    rm -rf "$INSTALL_DIR/$APP_NAME.app"
fi

# Copia o app
cp -R "$APP_SOURCE" "$INSTALL_DIR/"

echo "🔓 Removendo restrições de segurança do macOS..."
# Remove atributo de quarentena (resolve o erro "está danificado")
xattr -cr "$INSTALL_DIR/$APP_NAME.app"

# Torna o executável realmente executável
chmod +x "$INSTALL_DIR/$APP_NAME.app/Contents/MacOS/"*

echo ""
echo "✅ Instalação concluída com sucesso!"
echo ""
echo "Você pode encontrar o $APP_NAME em:"
echo "   /Applications/$APP_NAME.app"
echo ""
echo "Ou buscar por '$APP_NAME' no Spotlight (Cmd+Space)"
echo ""
echo "=========================================="
echo ""

# Pergunta se deseja abrir o app agora
read -p "Deseja abrir o $APP_NAME agora? (s/n): " -n 1 -r
echo ""
if [[ $REPLY =~ ^[SsYy]$ ]]; then
    echo "🚀 Abrindo $APP_NAME..."
    open "$INSTALL_DIR/$APP_NAME.app"
fi
