using AutomacaoGDA.Core.Interfaces;

namespace AutomacaoGDA.UI.ViewModels;

public class ResetAcquisitionViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly AppState _appState;
    private string _acquisitionIdText = string.Empty;
    private string _scriptGerado = string.Empty;
    private string _status = string.Empty;
    private string _batchIdResult = string.Empty;

    public ResetAcquisitionViewModel(IDatabaseService databaseService, AppState appState)
    {
        _databaseService = databaseService;
        _appState = appState;
        ExecutarResetCommand = new AsyncRelayCommand(ExecutarResetAsync);
        BuscarBatchIdCommand = new AsyncRelayCommand(BuscarBatchIdAsync);
    }

    public string AcquisitionIdText
    {
        get => _acquisitionIdText;
        set
        {
            if (SetProperty(ref _acquisitionIdText, value))
            {
                UpdateScript();
                RaisePropertyChanged(nameof(PodeExecutar));
            }
        }
    }

    public string ScriptGerado
    {
        get => _scriptGerado;
        set => SetProperty(ref _scriptGerado, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string BatchIdResult
    {
        get => _batchIdResult;
        set => SetProperty(ref _batchIdResult, value);
    }

    public AsyncRelayCommand ExecutarResetCommand { get; }
    public AsyncRelayCommand BuscarBatchIdCommand { get; }

    public bool PodeExecutar => TryGetAcquisitionId(out _);

    private async Task ExecutarResetAsync()
    {
        var conexao = _appState.ConexaoSelecionada;
        if (conexao is null)
        {
            Status = "Selecione um ambiente.";
            return;
        }

        if (conexao.IsProduction)
        {
            Status = "BLOQUEADO: Nao e permitido resetar aquisicao em ambiente de producao.";
            return;
        }

        if (!TryGetAcquisitionId(out var acquisitionId))
        {
            return;
        }

        var ambiente = conexao.Ambiente;

        try
        {
            var script = BuildScript(acquisitionId);
            ScriptGerado = script;
            var rows = await _databaseService.ExecutarComando(script, ambiente);
            Status = $"Reset executado. Linhas afetadas: {rows}.";
            _appState.BancoStatusMensagem = string.Empty;
            _appState.BancoStatusVisivel = false;
        }
        catch (Exception ex)
        {
            Status = ex.Message;
            _appState.BancoStatusMensagem = "Nao foi possivel conectar com o banco de dados.";
            _appState.BancoStatusVisivel = true;
        }
    }

    private void UpdateScript()
    {
        if (!TryGetAcquisitionId(out var acquisitionId))
        {
            return;
        }

        ScriptGerado = BuildScript(acquisitionId);
        Status = "Script pronto para execucao.";
        RaisePropertyChanged(nameof(PodeExecutar));
    }

    private bool TryGetAcquisitionId(out Guid acquisitionId)
    {
        acquisitionId = Guid.Empty;
        var text = AcquisitionIdText.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            ScriptGerado = string.Empty;
            Status = "Informe o ID da aquisicao.";
            return false;
        }

        if (!Guid.TryParse(text, out acquisitionId))
        {
            ScriptGerado = string.Empty;
            Status = "ID de aquisicao invalido.";
            return false;
        }

        return true;
    }

    private static string BuildScript(Guid acquisitionId) => $@"
DECLARE @Id varchar(max) = ('{acquisitionId}');
UPDATE Acquisition
SET Status = 'ANALYZING', AllGlobalCriteriaProcessed = 0, AllBatchsReceived = 1, AllBatchsProcessed = 1
WHERE ID = @Id;

UPDATE AcquisitionBatch
SET Status = 'PENDING', StatusMessage = NULL
WHERE AcquisitionId = @Id;

UPDATE AcquisitionRight
SET Status = 'PENDING', CostAmount = 0, ExpenseCostAmount = 0, HedgeCostAmount = 0, StatusMessage = NULL
WHERE AcquisitionBatchId IN (
    SELECT AB.Id
    FROM AcquisitionBatch AS AB WITH (NOLOCK)
    INNER JOIN Acquisition AS A WITH (NOLOCK) ON A.Id = AB.AcquisitionId
    WHERE AB.DeletedOn IS NULL
      AND A.DeletedOn IS NULL
      AND A.Id = @Id
);

UPDATE AcquisitionRightInstallment
SET CostAmount = 0, ExpenseCostAmount = 0, HedgeCostAmount = 0
WHERE AcquisitionRightId IN (
    SELECT AR.Id
    FROM AcquisitionRight AR
    INNER JOIN AcquisitionBatch AS AB ON AR.AcquisitionBatchId = AB.Id
    INNER JOIN Acquisition AS A WITH (NOLOCK) ON A.Id = AB.AcquisitionId
    WHERE AB.DeletedOn IS NULL
      AND A.DeletedOn IS NULL
      AND A.Id = @Id
);
".Trim();

    private async Task BuscarBatchIdAsync()
    {
        var conexao = _appState.ConexaoSelecionada;
        if (conexao is null)
        {
            BatchIdResult = "Selecione um ambiente.";
            return;
        }

        if (!TryGetAcquisitionId(out var acquisitionId))
        {
            BatchIdResult = "Informe um ID de aquisicao valido.";
            return;
        }

        var ambiente = conexao.Ambiente;

        try
        {
            BatchIdResult = "Buscando...";
            var query = $"SELECT Id FROM AcquisitionBatch WHERE AcquisitionId = '{acquisitionId}'";
            var result = await _databaseService.ExecutarConsulta(query, ambiente);

            if (result.Rows.Count == 0)
            {
                BatchIdResult = "Nenhum lote encontrado para esta aquisicao.";
            }
            else if (result.Rows.Count == 1)
            {
                var batchId = result.Rows[0]["Id"].ToString();
                BatchIdResult = $"ID do Lote: {batchId}";
            }
            else
            {
                var batchIds = string.Join(", ", result.Rows.Cast<System.Data.DataRow>().Select(r => r["Id"].ToString()));
                BatchIdResult = $"Multiplos lotes encontrados ({result.Rows.Count}): {batchIds}";
            }

            _appState.BancoStatusMensagem = string.Empty;
            _appState.BancoStatusVisivel = false;
        }
        catch (Exception ex)
        {
            BatchIdResult = $"Erro: {ex.Message}";
            _appState.BancoStatusMensagem = "Nao foi possivel conectar com o banco de dados.";
            _appState.BancoStatusVisivel = true;
        }
    }
}
