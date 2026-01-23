# Guia de Distribuição - AutomacaoGDA macOS

## Como Gerar o DMG para Distribuição

Execute o script de build a partir do diretório raiz do repositório:

```bash
DOTNET_BIN=/Users/Opea/.dotnet/dotnet ./scripts/build-macos-dmg.sh
```

O DMG será gerado em:
```
AutomacaoGDA/src/AutomacaoGDA.UI/bin/Release/net8.0/osx-arm64/publish/AutomacaoGDA.dmg
```

## O que o DMG Contém

Quando o usuário abrir o DMG, verá:

1. **AutomacaoGDA.app** - O aplicativo
2. **Instalar AutomacaoGDA.command** - Instalador automático (RECOMENDADO)
3. **Applications** - Link simbólico para a pasta /Applications
4. **LEIA-ME.txt** - Instruções de instalação

## Instruções para o Usuário Final

Envie estas instruções junto com o DMG:

---

### Como Instalar o AutomacaoGDA no macOS

**MÉTODO 1 - INSTALADOR AUTOMÁTICO (Mais Fácil)**

1. Abra o arquivo `AutomacaoGDA.dmg`
2. Duplo-clique em **"Instalar AutomacaoGDA.command"**
3. Se aparecer um aviso de segurança:
   - Clique com botão direito (ou Control+clique) no arquivo
   - Selecione "Abrir"
   - Confirme que deseja abrir
4. Siga as instruções que aparecerem no Terminal
5. Pronto! O AutomacaoGDA estará instalado em /Applications

**MÉTODO 2 - INSTALAÇÃO MANUAL**

1. Abra o arquivo `AutomacaoGDA.dmg`
2. Arraste `AutomacaoGDA.app` para a pasta `Applications`
3. Abra o **Terminal** (Aplicativos > Utilitários > Terminal)
4. Cole este comando e pressione Enter:
   ```bash
   xattr -cr /Applications/AutomacaoGDA.app
   ```
5. Agora você pode abrir o AutomacaoGDA normalmente

**Primeira Execução**

- Se mesmo após a instalação aparecer um aviso de segurança ao abrir:
  - Vá em **Preferências do Sistema > Privacidade e Segurança**
  - Clique em "Abrir Mesmo Assim"

---

## Limitações Conhecidas

- O aplicativo **não está assinado** com certificado Apple Developer
- O macOS Gatekeeper pode bloquear na primeira execução
- O instalador automático resolve esse problema removendo o atributo de quarentena
- Para distribuição comercial/em larga escala, recomenda-se:
  - Conta Apple Developer ($99 USD/ano)
  - Code signing com certificado
  - Notarização pela Apple

## Troubleshooting

### Erro: "AutomacaoGDA está danificado e não pode ser aberto"

**Causa:** O macOS bloqueou o app porque não está assinado.

**Solução:** Use o instalador automático ou execute manualmente:
```bash
xattr -cr /Applications/AutomacaoGDA.app
```

### Erro: "Instalador não pode ser aberto porque é de um desenvolvedor não identificado"

**Solução:**
1. Clique com botão direito no instalador
2. Selecione "Abrir"
3. Confirme que deseja abrir

### App não abre após instalação

**Solução:**
1. Vá em Preferências do Sistema > Privacidade e Segurança
2. Na seção "Segurança", clique em "Abrir Mesmo Assim"
3. Ou execute no Terminal:
   ```bash
   chmod +x /Applications/AutomacaoGDA.app/Contents/MacOS/*
   ```
