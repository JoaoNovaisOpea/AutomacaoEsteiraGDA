using MeuProjeto.Core.Models;

namespace MeuProjeto.Core.Interfaces;

public interface IDbConnectorFactory
{
    IDbConnector GetConnector(DatabaseProvider provider);
}
