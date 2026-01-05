# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AutomacaoGDA is a cross-platform (Windows/macOS) desktop application built with C#/.NET 8 and Avalonia UI. It automates database operations including SQL execution, environment management, and data copying between database environments.

## Key Commands

### Development
```bash
# Working directory for all commands
cd AutomacaoGDA

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run application
dotnet run --project src/MeuProjeto.UI/MeuProjeto.UI.csproj

# Build specific configuration
dotnet build -c Release
```

### Distribution Builds

#### macOS DMG
```bash
# From repository root, run:
DOTNET_BIN=/Users/Opea/.dotnet/dotnet ./scripts/build-macos-dmg.sh

# The script supports parameters:
# ./scripts/build-macos-dmg.sh [APP_NAME] [RUNTIME_ID] [EXEC_NAME] [SELF_CONTAINED]
# Defaults: AutomacaoGDA, osx-arm64, MeuProjeto.UI, true
```

#### Windows EXE
```bash
dotnet publish AutomacaoGDA/src/MeuProjeto.UI/MeuProjeto.UI.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

## Architecture

This application follows a three-layer architecture with dependency injection:

### Layer Responsibilities

**MeuProjeto.UI** (Presentation)
- Avalonia UI application using MVVM pattern
- ViewModels inherit from `ViewModelBase` and implement `INotifyPropertyChanged`
- `AppState` is a shared singleton holding global state (selected connection, operation, status messages)
- Main views: `MainWindow` (navigation container), `ConfiguracoesView` (connection settings), `StockCopyView` (data copy), `ResetAcquisitionView`, `DataCleanupView`
- Configuration persisted in `src/MeuProjeto.UI/appsettings.json`

**MeuProjeto.Core** (Business Logic)
- `DatabaseService`: orchestrates all database operations via `IDatabaseService`
- `ConexaoConfig`: model holding environment configuration (connection string, URLs, API credentials, S3 settings, database provider)
- `OperationInfo`: represents operations from the database (Id, FundName, Status)
- Interfaces: `IDatabaseService`, `IDbConnector`, `IDbConnectorFactory`, `IConnectionConfigProvider`

**MeuProjeto.Infrastructure** (Data Access)
- `DbConnectorFactory`: creates appropriate database connectors based on `DatabaseProvider` enum
- `SqlServerConnector`: implements `IDbConnector` for SQL Server operations (currently only SQL Server is supported)
- Handles connection string retrieval and persistence

### Key Patterns

**Environment Selection Flow**
1. User selects environment from `ConexaoConfig` list in `AppState.ConexaoSelecionada`
2. `DatabaseService` retrieves connection string via `IConnectionConfigProvider.GetConnectionStringAsync(ambiente)`
3. Gets appropriate `IDbConnector` from `DbConnectorFactory` based on `DatabaseProvider`
4. Executes operation on selected environment

**Data Copy Between Environments**
- `DatabaseService.CopiarOperationStock()` handles copying data between environments
- Deletes all records from `Id` column before copying
- Updates `OperationId` to destination operation ID
- Executes SELECT on source, transforms DataTable, then bulk inserts to destination
- Supports progress reporting via `IProgress<long>`

**Configuration Storage**
- All environment configurations stored in `appsettings.json` as array under `"Connections"`
- Each `ConexaoConfig` includes: database connection, API URLs, OAuth credentials, S3 settings
- `BearerToken` stored at root level for API authentication
- File is copied to output directory on build (`CopyToOutputDirectory: PreserveNewest`)

## Technology Stack

- **.NET 8.0** with nullable reference types enabled
- **Avalonia 11.0.6** (cross-platform UI framework)
- **Avalonia.Themes.Fluent** for modern UI styling
- **Avalonia.Controls.DataGrid** for tabular data display
- **SQL Server** as primary database (extensible to PostgreSQL/MySQL via `IDbConnector`)
- **Implicit usings** enabled across all projects

## Important Notes

- Solution file: `AutomacaoGDA/AutomacaoGDA.sln`
- Only SQL Server is currently implemented; PostgreSQL/MySQL support requires implementing `IDbConnector`
- The application uses compiled bindings by default (`AvaloniaUseCompiledBindingsByDefault: true`)
- Connection strings in `appsettings.json` contain actual credentials - handle with care
- macOS build script automatically detects project location (handles both root and nested structure)
