using AutomacaoGDA.Core.Interfaces;
using AutomacaoGDA.Core.Models;
using AutomacaoGDA.Infrastructure.SqlServer;

namespace AutomacaoGDA.Infrastructure;

public class DbConnectorFactory : IDbConnectorFactory
{
    private readonly SqlServerConnector _sqlServerConnector = new();

    public IDbConnector GetConnector(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer => _sqlServerConnector,
        _ => _sqlServerConnector
    };
}
