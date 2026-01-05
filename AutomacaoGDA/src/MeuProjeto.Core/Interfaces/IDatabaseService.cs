using System.Data;

namespace MeuProjeto.Core.Interfaces;

public interface IDatabaseService
{
    Task<DataTable> ExecutarConsulta(string sql, string ambiente);
    Task<int> ExecutarComando(string sql, string ambiente);
    Task<IReadOnlyList<Models.OperationInfo>> ListarOperacoesAtivas(string ambiente);
    Task CopiarOperationStock(
        Guid operacaoOrigemId,
        string ambienteOrigem,
        Guid operacaoDestinoId,
        string ambienteDestino,
        IProgress<long>? progress = null);
}
