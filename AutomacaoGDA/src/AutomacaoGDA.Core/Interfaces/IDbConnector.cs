using System.Data;

namespace AutomacaoGDA.Core.Interfaces;

public interface IDbConnector
{
    Task<DataTable> ExecuteQueryAsync(string connectionString, string sql);
    Task<int> ExecuteCommandAsync(string connectionString, string sql, IDictionary<string, object?>? parameters = null);
    Task BulkInsertAsync(
        string connectionString,
        string tableName,
        DataTable dataTable,
        IProgress<long>? progress = null,
        int notifyAfter = 5000);
}
