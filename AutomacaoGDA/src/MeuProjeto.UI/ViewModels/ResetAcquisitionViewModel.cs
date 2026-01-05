using MeuProjeto.Core.Interfaces;

namespace MeuProjeto.UI.ViewModels;

public class ResetAcquisitionViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly AppState _appState;
    private string _acquisitionIdText = string.Empty;
    private string _scriptGerado = string.Empty;
    private string _status = string.Empty;

    public ResetAcquisitionViewModel(IDatabaseService databaseService, AppState appState)
    {
        _databaseService = databaseService;
        _appState = appState;
        ExecutarResetCommand = new AsyncRelayCommand(ExecutarResetAsync);
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

    public AsyncRelayCommand ExecutarResetCommand { get; }

    public bool PodeExecutar => TryGetAcquisitionId(out _);

    private async Task ExecutarResetAsync()
    {
        var ambiente = _appState.ConexaoSelecionada?.Ambiente ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ambiente))
        {
            Status = "Selecione um ambiente.";
            return;
        }

        if (!TryGetAcquisitionId(out var acquisitionId))
        {
            return;
        }

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
}
