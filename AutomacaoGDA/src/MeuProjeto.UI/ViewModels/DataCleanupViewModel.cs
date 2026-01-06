using MeuProjeto.Core.Interfaces;

namespace MeuProjeto.UI.ViewModels;

public class DataCleanupViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly AppState _appState;
    private bool _limparStock;
    private bool _limparGrupoEconomico;
    private string _scriptGerado = string.Empty;
    private string _status = string.Empty;
    private string _statusCor = "#222222";

    public DataCleanupViewModel(IDatabaseService databaseService, AppState appState)
    {
        _databaseService = databaseService;
        _appState = appState;
        ExecutarLimpezaCommand = new AsyncRelayCommand(ExecutarLimpezaAsync);
        LimparStock = true;
        LimparGrupoEconomico = true;
        _appState.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AppState.OperacaoSelecionada))
            {
                UpdateScript();
            }
        };
        UpdateScript();
    }

    public bool LimparStock
    {
        get => _limparStock;
        set
        {
            if (SetProperty(ref _limparStock, value))
            {
                UpdateScript();
            }
        }
    }

    public bool LimparGrupoEconomico
    {
        get => _limparGrupoEconomico;
        set
        {
            if (SetProperty(ref _limparGrupoEconomico, value))
            {
                UpdateScript();
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

    public string StatusCor
    {
        get => _statusCor;
        set => SetProperty(ref _statusCor, value);
    }

    public bool PodeExecutarLimpeza => _appState.OperacaoSelecionada is not null;

    public AsyncRelayCommand ExecutarLimpezaCommand { get; }

    private async Task ExecutarLimpezaAsync()
    {
        var conexao = _appState.ConexaoSelecionada;
        if (conexao is null)
        {
            Status = "Selecione um ambiente.";
            return;
        }

        if (conexao.IsProduction)
        {
            Status = "BLOQUEADO: Nao e permitido executar limpeza em ambiente de producao.";
            StatusCor = "#C81E1E";
            return;
        }

        var operacao = _appState.OperacaoSelecionada;
        if (operacao is null)
        {
            Status = "Selecione uma operacao.";
            return;
        }

        var ambiente = conexao.Ambiente;

        try
        {
            var script = BuildScript(operacao.Id, LimparStock, LimparGrupoEconomico);
            ScriptGerado = script;
            var rows = await _databaseService.ExecutarComando(script, ambiente);
            Status = $"Limpeza executada. Linhas afetadas: {rows}.";
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
        var operacao = _appState.OperacaoSelecionada;
        if (operacao is null)
        {
            ScriptGerado = string.Empty;
            Status = "Selecione uma operacao.";
            StatusCor = "#C81E1E";
            RaisePropertyChanged(nameof(PodeExecutarLimpeza));
            return;
        }

        ScriptGerado = BuildScript(operacao.Id, LimparStock, LimparGrupoEconomico);
        Status = "Script pronto para execucao.";
        StatusCor = "#222222";
        RaisePropertyChanged(nameof(PodeExecutarLimpeza));
    }

    private static string BuildScript(Guid operationId, bool limparStock, bool limparGrupoEconomico)
    {
        var script = $@"
DECLARE @OperationIds TABLE (Id UNIQUEIDENTIFIER);

INSERT INTO @OperationIds VALUES
    ('{operationId}');

DECLARE @AcquisitionIds TABLE (Id UNIQUEIDENTIFIER);

INSERT INTO @AcquisitionIds
SELECT Id FROM Acquisition WHERE OperationId IN (SELECT Id FROM @OperationIds);

DELETE FROM AcquisitionHistory
WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds);

DELETE FROM AcquisitionRightInstallment
WHERE AcquisitionRightId IN (
    SELECT Id
    FROM AcquisitionRight
    WHERE AcquisitionBatchId IN (
        SELECT Id
        FROM AcquisitionBatch
        WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds)
    )
);

DELETE FROM AcquisitionRightParticipant
WHERE AcquisitionRightId IN (
    SELECT Id
    FROM AcquisitionRight
    WHERE AcquisitionBatchId IN (
        SELECT Id
        FROM AcquisitionBatch
        WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds)
    )
);

DELETE FROM AcquisitionRightAdditionalParticipant
WHERE AcquisitionRightId IN (
    SELECT Id
    FROM AcquisitionRight
    WHERE AcquisitionBatchId IN (
        SELECT Id
        FROM AcquisitionBatch
        WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds)
    )
);

DELETE FROM AcquisitionRightSpecificField
WHERE AcquisitionRightId IN (
    SELECT Id
    FROM AcquisitionRight
    WHERE AcquisitionBatchId IN (
        SELECT Id
        FROM AcquisitionBatch
        WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds)
    )
);

DELETE FROM AcquisitionRight
WHERE AcquisitionBatchId IN (
    SELECT Id
    FROM AcquisitionBatch
    WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds)
);

DELETE FROM AcquisitionBatch
WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds);

DELETE FROM AcquisitionParticipant
WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds);

DELETE FROM AcquisitionDisbursementPayment
WHERE AcquisitionDisbursementId IN (
    SELECT Id
    FROM AcquisitionDisbursement
    WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds)
);

DELETE FROM AcquisitionDisbursement
WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds);

DELETE FROM ClosePackage
WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds);

DELETE FROM AcquisitionApprover
WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds);

DELETE FROM AcquisitionEligibility
WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds);

DELETE FROM AcquisitionRemittanceLog
WHERE AcquisitionRemittanceId IN (
    SELECT Id
    FROM AcquisitionRemittance
    WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds)
);

DELETE FROM AcquisitionRemittance
WHERE AcquisitionId IN (SELECT Id FROM @AcquisitionIds);

DELETE FROM Acquisition
WHERE Id IN (SELECT Id FROM @AcquisitionIds);

DELETE FROM OperationDashboard
WHERE OperationId IN (SELECT Id FROM @OperationIds);

DELETE FROM OperationBalanceHistory
WHERE OperationId IN (SELECT Id FROM @OperationIds);

DELETE FROM OperationNetWorthHistory
WHERE OperationId IN (SELECT Id FROM @OperationIds);

DELETE FROM ReceivableCenterSnapshot
WHERE OperationId IN (SELECT Id FROM @OperationIds);
";

        if (limparStock)
        {
            script += @"
DELETE FROM OperationStockSummaryEconomicGroup
WHERE OperationStockSummaryId IN (
    SELECT Id
    FROM OperationStockSummary
    WHERE OperationId IN (SELECT Id FROM @OperationIds)
);

DELETE FROM OperationStockSummaryAgent
WHERE OperationStockSummaryId IN (
    SELECT Id
    FROM OperationStockSummary
    WHERE OperationId IN (SELECT Id FROM @OperationIds)
);

DELETE FROM OperationStockSummary
WHERE OperationId IN (SELECT Id FROM @OperationIds);

DELETE FROM OperationStock WHERE OperationId IN (SELECT Id FROM @OperationIds);
";
        }

        if (limparGrupoEconomico)
        {
            script += @"
DELETE FROM OperationEconomicGroupHistory
WHERE OperationEconomicGroupId IN (
    SELECT Id
    FROM OperationEconomicGroup
    WHERE OperationId IN (SELECT Id FROM @OperationIds) AND UserName = 'KAFKA'
);

DELETE FROM OperationAgentEconomicGroup
WHERE OperationEconomicGroupId IN (
    SELECT Id
    FROM OperationEconomicGroup
    WHERE OperationId IN (SELECT Id FROM @OperationIds) AND UserName = 'KAFKA'
);

DELETE FROM OperationEconomicGroup
WHERE OperationId IN (SELECT Id FROM @OperationIds) AND UserName = 'KAFKA';
";
        }

        return script.Trim();
    }
}
