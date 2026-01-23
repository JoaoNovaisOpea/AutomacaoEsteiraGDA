using System.Data;
using Microsoft.Data.SqlClient;
using AutomacaoGDA.Core.Interfaces;

namespace AutomacaoGDA.Infrastructure.SqlServer;

public class SqlServerConnector : IDbConnector
{
    public async Task<DataTable> ExecuteQueryAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    public async Task<int> ExecuteCommandAsync(string connectionString, string sql, IDictionary<string, object?>? parameters = null)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        if (parameters is not null)
        {
            foreach (var (key, value) in parameters)
            {
                command.Parameters.AddWithValue(key, value ?? DBNull.Value);
            }
        }

        return await command.ExecuteNonQueryAsync();
    }

    public async Task BulkInsertAsync(
        string connectionString,
        string tableName,
        DataTable dataTable,
        IProgress<long>? progress = null,
        int notifyAfter = 5000)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, null)
        {
            DestinationTableName = tableName,
            BatchSize = 5000,
            BulkCopyTimeout = 0
        };

        foreach (DataColumn column in dataTable.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        if (progress is not null)
        {
            bulkCopy.NotifyAfter = notifyAfter;
            bulkCopy.SqlRowsCopied += (_, args) => progress.Report(args.RowsCopied);
        }

        await bulkCopy.WriteToServerAsync(dataTable);
    }
}
