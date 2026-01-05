using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using MeuProjeto.Core.Interfaces;
using MeuProjeto.Core.Models;

namespace MeuProjeto.UI.ViewModels;

public class StockCopyViewModel : ViewModelBase
{
    private static readonly HttpClient HttpClient = new();
    private readonly IDatabaseService _databaseService;
    private readonly AppState _appState;
    private ConexaoConfig? _ambienteOrigem;
    private ConexaoConfig? _ambienteDestino;
    private OperationInfo? _operacaoOrigem;
    private OperationInfo? _operacaoDestino;
    private string _status = string.Empty;
    private int _totalRegistros;
    private long _copiados;
    private bool _isCopiando;
    private bool _executarSumarizacao = true;

    public StockCopyViewModel(IDatabaseService databaseService, AppState appState)
    {
        _databaseService = databaseService;
        _appState = appState;
        CopiarStockCommand = new AsyncRelayCommand(CopiarStockAsync);
        AtualizarOperacoesOrigemCommand = new AsyncRelayCommand(AtualizarOperacoesOrigemAsync);
        AtualizarOperacoesDestinoCommand = new AsyncRelayCommand(AtualizarOperacoesDestinoAsync);
    }

    public ObservableCollection<ConexaoConfig> Conexoes => _appState.Conexoes;

    public ObservableCollection<OperationInfo> OperacoesOrigem { get; } = new();
    public ObservableCollection<OperationInfo> OperacoesDestino { get; } = new();

    public ConexaoConfig? AmbienteOrigem
    {
        get => _ambienteOrigem;
        set
        {
            if (SetProperty(ref _ambienteOrigem, value))
            {
                RaisePropertyChanged(nameof(PodeCopiar));
                _ = AtualizarOperacoesOrigemAsync();
            }
        }
    }

    public ConexaoConfig? AmbienteDestino
    {
        get => _ambienteDestino;
        set
        {
            if (SetProperty(ref _ambienteDestino, value))
            {
                RaisePropertyChanged(nameof(PodeCopiar));
                _ = AtualizarOperacoesDestinoAsync();
            }
        }
    }

    public OperationInfo? OperacaoOrigem
    {
        get => _operacaoOrigem;
        set
        {
            if (SetProperty(ref _operacaoOrigem, value))
            {
                RaisePropertyChanged(nameof(PodeCopiar));
            }
        }
    }

    public OperationInfo? OperacaoDestino
    {
        get => _operacaoDestino;
        set
        {
            if (SetProperty(ref _operacaoDestino, value))
            {
                RaisePropertyChanged(nameof(PodeCopiar));
            }
        }
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool PodeCopiar =>
        _ambienteOrigem is not null &&
        _ambienteDestino is not null &&
        _operacaoOrigem is not null &&
        _operacaoDestino is not null;

    public int TotalRegistros
    {
        get => _totalRegistros;
        set => SetProperty(ref _totalRegistros, value);
    }

    public long Copiados
    {
        get => _copiados;
        set => SetProperty(ref _copiados, value);
    }

    public bool IsCopiando
    {
        get => _isCopiando;
        set => SetProperty(ref _isCopiando, value);
    }

    public bool ExecutarSumarizacao
    {
        get => _executarSumarizacao;
        set => SetProperty(ref _executarSumarizacao, value);
    }

    public string ProgressoTexto => TotalRegistros == 0
        ? string.Empty
        : $"Copiadas {Copiados:n0} de {TotalRegistros:n0}";

    public AsyncRelayCommand CopiarStockCommand { get; }
    public AsyncRelayCommand AtualizarOperacoesOrigemCommand { get; }
    public AsyncRelayCommand AtualizarOperacoesDestinoCommand { get; }

    private async Task AtualizarOperacoesOrigemAsync()
    {
        if (_ambienteOrigem is null)
        {
            OperacoesOrigem.Clear();
            OperacaoOrigem = null;
            return;
        }

        try
        {
            Status = "Carregando operacoes de origem...";
            var lista = await _databaseService.ListarOperacoesAtivas(_ambienteOrigem.Ambiente);
            OperacoesOrigem.Clear();
            foreach (var item in lista)
            {
                OperacoesOrigem.Add(item);
            }

            OperacaoOrigem = OperacoesOrigem.Count > 0 ? OperacoesOrigem[0] : null;
            Status = string.Empty;
            SetBancoOk();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
            SetBancoErro();
        }
    }

    private async Task AtualizarOperacoesDestinoAsync()
    {
        if (_ambienteDestino is null)
        {
            OperacoesDestino.Clear();
            OperacaoDestino = null;
            return;
        }

        try
        {
            Status = "Carregando operacoes de destino...";
            var lista = await _databaseService.ListarOperacoesAtivas(_ambienteDestino.Ambiente);
            OperacoesDestino.Clear();
            foreach (var item in lista)
            {
                OperacoesDestino.Add(item);
            }

            OperacaoDestino = OperacoesDestino.Count > 0 ? OperacoesDestino[0] : null;
            Status = string.Empty;
            SetBancoOk();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
            SetBancoErro();
        }
    }

    private async Task CopiarStockAsync()
    {
        if (_ambienteOrigem is null || _ambienteDestino is null)
        {
            Status = "Selecione ambiente de origem e destino.";
            return;
        }

        if (_operacaoOrigem is null || _operacaoDestino is null)
        {
            Status = "Selecione operacoes de origem e destino.";
            return;
        }

        try
        {
            Status = "Copiando OperationStock...";
            Copiados = 0;
            TotalRegistros = 0;
            IsCopiando = true;
            RaisePropertyChanged(nameof(ProgressoTexto));

            var previewSql = $"SELECT COUNT(*) FROM OperationStock WHERE OperationId = '{_operacaoOrigem.Id}'";
            var totalTable = await _databaseService.ExecutarConsulta(previewSql, _ambienteOrigem.Ambiente);
            if (totalTable.Rows.Count > 0 && int.TryParse(totalTable.Rows[0][0]?.ToString(), out var total))
            {
                TotalRegistros = total;
            }

            var progress = new Progress<long>(rows =>
            {
                Copiados = rows;
                RaisePropertyChanged(nameof(ProgressoTexto));
            });

            await _databaseService.CopiarOperationStock(
                _operacaoOrigem.Id,
                _ambienteOrigem.Ambiente,
                _operacaoDestino.Id,
                _ambienteDestino.Ambiente,
                progress);
            Copiados = TotalRegistros > 0 ? TotalRegistros : Copiados;
            if (ExecutarSumarizacao)
            {
                Status = "Copia concluida. Iniciando sumarizacao...";
                SetBancoOk();
                await DispararSumarizacaoAsync();
            }
            else
            {
                Status = "Copia concluida.";
                SetBancoOk();
            }
        }
        catch (Exception ex)
        {
            Status = ex.Message;
            SetBancoErro();
        }
        finally
        {
            IsCopiando = false;
            RaisePropertyChanged(nameof(ProgressoTexto));
        }
    }

    private async Task DispararSumarizacaoAsync()
    {
        if (_ambienteDestino is null)
        {
            return;
        }

        var baseUrl = _ambienteDestino.UrlBaseOriginacao?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Status = "Copia concluida. Base API do destino nao configurada.";
            return;
        }

        var url = $"{baseUrl.TrimEnd('/')}/aatestworkers";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        var bearerToken = _appState.BearerToken?.Trim();
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            const string bearerPrefix = "Bearer ";
            if (bearerToken.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                bearerToken = bearerToken[bearerPrefix.Length..].Trim();
            }

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }
        }

        using var form = new MultipartFormDataContent
        {
            { new StringContent(string.Empty), "IsAdmin" },
            { new StringContent(string.Empty), "InsertBatchWithProcessLambda" },
            { new StringContent(string.Empty), "FileUrl" },
            { new StringContent(string.Empty), "UserName" },
            { new StringContent("true"), "OnlyRecalculateStockSummary" },
            { new StringContent("STOCK"), "WorkerType" },
            { new StringContent(string.Empty), "Id" },
            { new StringContent(string.Empty), "TotalBatchs" }
        };

        request.Content = form;

        using var response = await HttpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            Status = "Sumarizacao disparada com sucesso.";
            return;
        }

        Status = $"Falha ao disparar sumarizacao ({(int)response.StatusCode}).";
    }

    private void SetBancoOk()
    {
        _appState.BancoStatusMensagem = string.Empty;
        _appState.BancoStatusVisivel = false;
    }

    private void SetBancoErro()
    {
        _appState.BancoStatusMensagem = "Nao foi possivel conectar com o banco de dados.";
        _appState.BancoStatusVisivel = true;
    }
}
