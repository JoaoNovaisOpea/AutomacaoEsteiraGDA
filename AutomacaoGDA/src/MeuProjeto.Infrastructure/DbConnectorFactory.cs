using MeuProjeto.Core.Interfaces;
using MeuProjeto.Core.Models;
using MeuProjeto.Infrastructure.SqlServer;

namespace MeuProjeto.Infrastructure;

public class DbConnectorFactory : IDbConnectorFactory
{
    private readonly SqlServerConnector _sqlServerConnector = new();

    public IDbConnector GetConnector(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer => _sqlServerConnector,
        _ => _sqlServerConnector
    };
}
