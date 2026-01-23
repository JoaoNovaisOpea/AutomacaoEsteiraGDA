using AutomacaoGDA.Core.Models;

namespace AutomacaoGDA.Core.Interfaces;

public interface IDbConnectorFactory
{
    IDbConnector GetConnector(DatabaseProvider provider);
}
