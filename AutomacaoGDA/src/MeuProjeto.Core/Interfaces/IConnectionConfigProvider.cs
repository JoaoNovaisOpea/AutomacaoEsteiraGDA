using MeuProjeto.Core.Models;

namespace MeuProjeto.Core.Interfaces;

public interface IConnectionConfigProvider
{
    Task<IReadOnlyList<ConexaoConfig>> GetAllAsync();
    Task SaveAllAsync(IEnumerable<ConexaoConfig> configs);
    Task<string?> GetConnectionStringAsync(string ambiente);
    Task<DatabaseProvider?> GetProviderAsync(string ambiente);
}
