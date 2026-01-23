using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AutomacaoGDA.Core.Interfaces;
using AutomacaoGDA.UI.Views;

namespace AutomacaoGDA.UI.ViewModels;

public class CreditUploadViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ICreditUploadService _creditUploadService;
    private readonly IS3Service _s3Service;
    private readonly AppState _appState;
    private readonly ILogService _logService;

    private string _status = "Pronto para processar arquivos.";
    private int _progressStep;
    private int _totalSteps = 10;
    private bool _isProcessing;
    private List<string> _selectedFilePaths = new();
    private string _selectedFilesText = "Nenhum arquivo selecionado.";
    private string _logFilePath = string.Empty;

    public CreditUploadViewModel(
        IDatabaseService databaseService,
        ICreditUploadService creditUploadService,
        IS3Service s3Service,
        AppState appState,
        ILogService logService)
    {
        _databaseService = databaseService;
        _creditUploadService = creditUploadService;
        _s3Service = s3Service;
        _appState = appState;
        _logService = logService;

        _logFilePath = _logService.GetLogFilePath();

        SelectFilesCommand = new AsyncRelayCommand(SelectFilesAsync, () => !IsProcessing);
        ProcessUploadCommand = new AsyncRelayCommand(ProcessUploadAsync, CanProcess);
        OpenLogCommand = new AsyncRelayCommand(OpenLogAsync);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public int ProgressStep
    {
        get => _progressStep;
        set => SetProperty(ref _progressStep, value);
    }

    public int TotalSteps
    {
        get => _totalSteps;
        set => SetProperty(ref _totalSteps, value);
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (SetProperty(ref _isProcessing, value))
            {
                SelectFilesCommand.RaiseCanExecuteChanged();
                ProcessUploadCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedFilesText
    {
        get => _selectedFilesText;
        set => SetProperty(ref _selectedFilesText, value);
    }

    public string LogFilePath
    {
        get => _logFilePath;
        set => SetProperty(ref _logFilePath, value);
    }

    public AsyncRelayCommand SelectFilesCommand { get; }
    public AsyncRelayCommand ProcessUploadCommand { get; }
    public AsyncRelayCommand OpenLogCommand { get; }

    private bool CanProcess()
    {
        return !IsProcessing
               && _selectedFilePaths.Count > 0
               && _appState.ConexaoSelecionada != null
               && _appState.OperacaoSelecionada != null;
    }

    private async Task SelectFilesAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null)
            {
                Status = "Erro: Janela principal não encontrada.";
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Selecionar arquivos JSON",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON Files")
                    {
                        Patterns = new[] { "*.json" }
                    }
                }
            });

            if (files.Count > 0)
            {
                _selectedFilePaths = files.Select(f => f.Path.LocalPath).ToList();
                SelectedFilesText = $"{_selectedFilePaths.Count} arquivo(s) selecionado(s): {string.Join(", ", _selectedFilePaths.Select(Path.GetFileName))}";
                Status = $"{_selectedFilePaths.Count} arquivo(s) selecionado(s).";
                ProcessUploadCommand.RaiseCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            Status = $"Erro ao selecionar arquivos: {ex.Message}";
        }
    }

    private async Task OpenLogAsync()
    {
        try
        {
            if (File.Exists(_logFilePath))
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _logFilePath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            else
            {
                Status = "Arquivo de log ainda não foi criado.";
            }
        }
        catch (Exception ex)
        {
            Status = $"Erro ao abrir log: {ex.Message}";
        }
    }

    private async Task ProcessUploadAsync()
    {
        try
        {
            IsProcessing = true;
            ProgressStep = 0;

            _logService.ClearLog();
            _logService.Log("========== INICIANDO PROCESSO DE UPLOAD ==========");
            _logService.Log($"Arquivos selecionados: {_selectedFilePaths.Count}");

            var conexao = _appState.ConexaoSelecionada;
            var operacao = _appState.OperacaoSelecionada;

            if (conexao == null)
            {
                Status = "Erro: Nenhuma conexão selecionada.";
                return;
            }

            if (conexao.IsProduction)
            {
                Status = "BLOQUEADO: Nao e permitido subir creditos em ambiente de producao.";
                _logService.LogError("Operacao bloqueada: ambiente de producao selecionado.");
                return;
            }

            if (operacao == null)
            {
                Status = "Erro: Nenhuma operação selecionada.";
                return;
            }

            if (_selectedFilePaths.Count == 0)
            {
                Status = "Nenhum arquivo selecionado. Operação abortada.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_appState.BearerToken))
            {
                Status = "Erro: Bearer Token não configurado. Configure nas Configurações.";
                _logService.LogError("Bearer Token está vazio ou não configurado!");
                return;
            }

            _logService.Log($"Bearer Token configurado: {_appState.BearerToken.Substring(0, Math.Min(20, _appState.BearerToken.Length))}...");

            Status = "Autenticando via OAuth...";
            ProgressStep = 1;

            var oauthResponse = await _creditUploadService.AuthenticateAsync(
                conexao.UrlLogin,
                conexao.ClientId,
                conexao.ClientSecret);

            var oauthToken = oauthResponse.AccessToken;
            _logService.Log($"OAuth token obtido (primeiros 20 chars): {oauthToken.Substring(0, Math.Min(20, oauthToken.Length))}...");

            Status = $"OAuth concluído. Token obtido: {oauthResponse.TokenType}";
            await Task.Delay(500);

            Status = "Criando cessão...";
            ProgressStep = 2;

            if (string.IsNullOrWhiteSpace(conexao.CedenteId))
            {
                Status = "Erro: CedenteId não configurado nas configurações do ambiente.";
                return;
            }

            var externalId = $"AQ-{DateTime.Now:yyyyMMddHHmmss}";

            _logService.Log("Usando OAuth token para criar cessão (UrlBase)");
            var assignmentResponse = await _creditUploadService.CreateAssignmentAsync(
                conexao.UrlBase,
                oauthToken,
                operacao.Id,
                conexao.CedenteId,
                externalId);

            var assignmentId = assignmentResponse.Id;
            Status = $"Cessão criada: {assignmentId} (Status: {assignmentResponse.Status})";
            await Task.Delay(500);

            for (int i = 0; i < _selectedFilePaths.Count; i++)
            {
                var filePath = _selectedFilePaths[i];
                var fileName = Path.GetFileName(filePath);

                Status = $"Processando arquivo {i + 1}/{_selectedFilePaths.Count}: {fileName}";
                ProgressStep = 3 + (i * 4 / Math.Max(_selectedFilePaths.Count, 1));

                var originalJson = await File.ReadAllTextAsync(filePath);

                Status = $"Substituindo AssignmentId em: {fileName}";
                var modifiedJson = await _creditUploadService.ReplaceAssignmentIdInJsonAsync(
                    originalJson,
                    assignmentId);

                Status = $"Enviando para S3: {fileName}";
                var s3Url = await _s3Service.UploadJsonFileAsync(
                    modifiedJson,
                    fileName,
                    "Adaptador2_API",
                    conexao.RegionS3Integracao,
                    conexao.AccessKeyIDIntegracao,
                    conexao.SecretAccessKeyIntegracao,
                    conexao.RootBucketIntegracao);

                var httpUrl = string.IsNullOrWhiteSpace(conexao.UrlBaseS3)
                    ? s3Url
                    : $"{conexao.UrlBaseS3.TrimEnd('/')}/Adaptador2_API/{fileName}";

                Status = $"Gerando batch de aquisição: {fileName}";
                _logService.Log("Usando BearerToken para TriggerWorker (UrlBaseOriginacao)");
                await _creditUploadService.TriggerWorkerAsync(
                    conexao.UrlBaseOriginacao,
                    _appState.BearerToken,
                    httpUrl,
                    "ACQUISITION_INTEGRATION",
                    assignmentId.ToString());

                await Task.Delay(1000);
            }

            Status = "Aguardando criação de batches...";
            _logService.Log("Aguardando criação de batches (2 segundos)...");
            await Task.Delay(2000);

            Status = "Consultando IDs de batch...";
            ProgressStep = 8;
            _logService.Log($"Consultando IDs de batch para AssignmentId: {assignmentId}");

            var batchIds = await _creditUploadService.GetBatchIdsAsync(
                conexao.Ambiente,
                assignmentId);

            _logService.Log($"Primeira consulta retornou {batchIds.Count} batch(es)");

            if (batchIds.Count == 0)
            {
                Status = "Aviso: Nenhum batch encontrado. Aguardando mais tempo...";
                _logService.Log("Nenhum batch encontrado. Aguardando mais 3 segundos...");
                await Task.Delay(3000);
                batchIds = await _creditUploadService.GetBatchIdsAsync(conexao.Ambiente, assignmentId);
                _logService.Log($"Segunda consulta retornou {batchIds.Count} batch(es)");
            }

            Status = $"Encontrados {batchIds.Count} batch(es). Processando...";
            _logService.Log($"Total de batches encontrados: {batchIds.Count}");
            ProgressStep = 9;

            foreach (var batchId in batchIds)
            {
                Status = $"Processando batch: {batchId}";
                _logService.Log("Usando BearerToken para processar batch (UrlBaseOriginacao)");
                await _creditUploadService.TriggerWorkerAsync(
                    conexao.UrlBaseOriginacao,
                    _appState.BearerToken,
                    string.Empty,
                    "ACQUISITION_DATA_PROCESSING",
                    batchId.ToString());
                await Task.Delay(1000);
            }

            ProgressStep = 10;
            Status = "Processamento concluído. Verificando se deseja fechar a cessão...";

            var shouldClose = await ShowConfirmDialogAsync();

            if (shouldClose)
            {
                Status = "Fechando cessão...";
                _logService.Log("Usando BearerToken para fechar cessão (UrlBaseOriginacao)");
                await _creditUploadService.CloseAssignmentAsync(
                    conexao.UrlBaseOriginacao,
                    _appState.BearerToken,
                    assignmentId,
                    batchIds.Count);
                Status = $"✓ Cessão {assignmentId} fechada com sucesso!";
            }
            else
            {
                Status = $"✓ Upload concluído. Cessão {assignmentId} não foi fechada.";
            }
        }
        catch (Exception ex)
        {
            Status = $"Erro: {ex.Message}";
            _logService.LogError("Erro no processo de upload", ex);
            ProgressStep = 0;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task<bool> ShowConfirmDialogAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null)
            {
                return false;
            }

            var dialog = new ConfirmDialog("Fechar cessão?", "Deseja fechar a cessão agora?");
            var result = await dialog.ShowDialog<bool?>(topLevel);
            return result ?? false;
        }
        catch
        {
            return false;
        }
    }
}
