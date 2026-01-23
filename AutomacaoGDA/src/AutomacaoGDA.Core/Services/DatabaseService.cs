using System.Data;
using AutomacaoGDA.Core.Interfaces;

namespace AutomacaoGDA.Core.Services;

public class DatabaseService : IDatabaseService
{
    private readonly IConnectionConfigProvider _configProvider;
    private readonly IDbConnectorFactory _connectorFactory;

    public DatabaseService(IConnectionConfigProvider configProvider, IDbConnectorFactory connectorFactory)
    {
        _configProvider = configProvider;
        _connectorFactory = connectorFactory;
    }

    public async Task<DataTable> ExecutarConsulta(string sql, string ambiente)
    {
        var connector = await GetConnectorAsync(ambiente);
        var connectionString = await GetConnectionStringAsync(ambiente);

        return await connector.ExecuteQueryAsync(connectionString, sql);
    }

    public async Task<int> ExecutarComando(string sql, string ambiente)
    {
        var connector = await GetConnectorAsync(ambiente);
        var connectionString = await GetConnectionStringAsync(ambiente);

        return await connector.ExecuteCommandAsync(connectionString, sql);
    }

    public async Task<IReadOnlyList<Models.OperationInfo>> ListarOperacoesAtivas(string ambiente)
    {
        var connector = await GetConnectorAsync(ambiente);
        var connectionString = await GetConnectionStringAsync(ambiente);
        const string sql = """
            SELECT Id, FundName, Status
            FROM Operation
            WHERE DeletedOn IS NULL
            ORDER BY FundName
            """;

        var table = await connector.ExecuteQueryAsync(connectionString, sql);
        var results = new List<Models.OperationInfo>();
        foreach (DataRow row in table.Rows)
        {
            var idValue = row["Id"];
            var fundNameValue = row["FundName"];
            var statusValue = row["Status"];

            var info = new Models.OperationInfo
            {
                Id = idValue is Guid guid ? guid : Guid.Parse(idValue?.ToString() ?? Guid.Empty.ToString()),
                FundName = fundNameValue?.ToString() ?? string.Empty,
                Status = statusValue?.ToString()
            };
            results.Add(info);
        }

        return results;
    }

    public async Task CopiarOperationStock(
        Guid operacaoOrigemId,
        string ambienteOrigem,
        Guid operacaoDestinoId,
        string ambienteDestino,
        IProgress<long>? progress = null)
    {
        var origemConnector = await GetConnectorAsync(ambienteOrigem);
        var destinoConnector = await GetConnectorAsync(ambienteDestino);
        var origemConnectionString = await GetConnectionStringAsync(ambienteOrigem);
        var destinoConnectionString = await GetConnectionStringAsync(ambienteDestino);

        var sql = $"SELECT * FROM OperationStock WHERE OperationId = '{operacaoOrigemId}'";
        var table = await origemConnector.ExecuteQueryAsync(origemConnectionString, sql);

        if (table.Columns.Contains("Id"))
        {
            table.Columns.Remove("Id");
        }

        if (!table.Columns.Contains("OperationId"))
        {
            throw new InvalidOperationException("Coluna OperationId nao encontrada em OperationStock.");
        }

        foreach (DataRow row in table.Rows)
        {
            row["OperationId"] = operacaoDestinoId;
        }

        var deleteSql = $"DELETE FROM OperationStock WHERE OperationId = '{operacaoDestinoId}'";
        await destinoConnector.ExecuteCommandAsync(destinoConnectionString, deleteSql);
        await destinoConnector.BulkInsertAsync(destinoConnectionString, "OperationStock", table, progress);
    }

    private async Task<string> GetConnectionStringAsync(string ambiente)
    {
        var connectionString = await _configProvider.GetConnectionStringAsync(ambiente);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Nenhuma string de conexao configurada para '{ambiente}'.");
        }

        return connectionString;
    }

    private async Task<IDbConnector> GetConnectorAsync(string ambiente)
    {
        var provider = await _configProvider.GetProviderAsync(ambiente) ?? Models.DatabaseProvider.SqlServer;
        return _connectorFactory.GetConnector(provider);
    }
}
