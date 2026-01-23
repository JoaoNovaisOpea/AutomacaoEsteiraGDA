# AutomacaoGDA

Aplicacao desktop multiplataforma (Windows/macOS) em C#/.NET 8 com Avalonia UI para executar SQL, gerenciar ambientes e copiar dados entre bancos.

## Estrutura

```
AutomacaoGDA/
├── src/
│   ├── AutomacaoGDA.UI/               # Aplicacao Avalonia
│   │   ├── Views/
│   │   │   ├── MainWindow.axaml
│   │   │   ├── MainWindow.axaml.cs
│   │   │   ├── ConfiguracoesView.axaml
│   │   │   ├── ConfiguracoesView.axaml.cs
│   │   │   ├── SqlExecutorView.axaml
│   │   │   └── SqlExecutorView.axaml.cs
│   │   ├── ViewModels/
│   │   │   ├── MainWindowViewModel.cs
│   │   │   ├── ConfiguracoesViewModel.cs
│   │   │   └── SqlExecutorViewModel.cs
│   │   ├── App.axaml
│   │   ├── App.axaml.cs
│   │   └── Program.cs
│   │
│   ├── AutomacaoGDA.Core/             # Regras de negocio
│   │   ├── Models/
│   │   │   └── ConexaoConfig.cs
│   │   ├── Interfaces/
│   │   │   └── IDatabaseService.cs
│   │   └── Services/
│   │       └── DatabaseService.cs
│   │
│   ├── AutomacaoGDA.Infrastructure/   # Acesso ao banco de dados
│   │   ├── SqlServer/
│   │   │   └── SqlServerConnector.cs
│   │   └── DbConfigManager.cs
│   │
├── tests/
│   └── AutomacaoGDA.Tests/
│
└── README.md
```

## Camadas e responsabilidades

- **UI (Avalonia)**: telas, binds e comandos. `MainWindow` organiza abas e define o ambiente ativo. `ConfiguracoesView` salva strings de conexao. `SqlExecutorView` executa SQL e mostra resultados em tabela.
- **Core**: modelos e regras. `ConexaoConfig` guarda ambiente e provider. `DatabaseService` orquestra consultas/comandos e copia entre ambientes.
- **Infrastructure**: persistencia local das configuracoes (`DbConfigManager`) e implementacao SQL Server (`SqlServerConnector`).

## Persistencia de conexoes

As configuracoes sao salvas em `appsettings.json` dentro do projeto UI:

- `src/AutomacaoGDA.UI/appsettings.json`

## Copiar dados

O metodo `CopiarDadosEntreAmbientes` executa o `selectSql` no ambiente de origem, e para cada linha executa o `insertSql` no destino usando parametros com o mesmo nome das colunas.

Exemplo:

```
SELECT Id, Nome FROM Clientes

INSERT INTO Clientes (Id, Nome) VALUES (@Id, @Nome)
```

## Build e execucao

Requisitos: **.NET SDK 8**.

### Windows

```
dotnet restore
dotnet build
dotnet run --project src/AutomacaoGDA.UI/AutomacaoGDA.UI.csproj
```

### macOS

```
dotnet restore
dotnet build
dotnet run --project src/AutomacaoGDA.UI/AutomacaoGDA.UI.csproj
```

## Melhorias futuras (sugestao)

- Adicionar implementacoes PostgreSQL/MySQL no `DbConnectorFactory`.
- Validacao de SQL e preview de dados antes de copiar.
- Transacoes e bulk insert para copiar dados em massa.

## Executar para gerar o dmg, so que tem q fazer na pasta TesteGda
- DOTNET_BIN=/Users/Opea/.dotnet/dotnet ./scripts/build-macos-dmg.sh

## Executar para gerar o exe, so que tem q fazer na pasta TesteGda
- /Users/Opea/.dotnet/dotnet publish /Users/Opea/Documents/OutrosCodigos/TestesGda/AutomacaoGDA/src/AutomacaoGDA.UI/AutomacaoGDA.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
