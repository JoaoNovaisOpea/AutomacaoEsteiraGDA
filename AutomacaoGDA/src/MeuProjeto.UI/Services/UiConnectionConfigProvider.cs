using MeuProjeto.Core.Interfaces;
using MeuProjeto.Core.Models;
using MeuProjeto.UI.ViewModels;

namespace MeuProjeto.UI.Services;

public class UiConnectionConfigProvider : IConnectionConfigProvider
{
    private readonly AppSettingsConfigManager _configManager;
    private readonly AppState _appState;

    public UiConnectionConfigProvider(AppSettingsConfigManager configManager, AppState appState)
    {
        _configManager = configManager;
        _appState = appState;
    }

    public async Task<IReadOnlyList<ConexaoConfig>> GetAllAsync()
    {
        if (_appState.Conexoes.Count > 0)
        {
            return _appState.Conexoes;
        }

        var configs = await _configManager.GetAllAsync();
        _appState.Conexoes.Clear();
        foreach (var config in configs)
        {
            _appState.Conexoes.Add(config);
        }

        return _appState.Conexoes;
    }

    public async Task SaveAllAsync(IEnumerable<ConexaoConfig> configs)
    {
        var list = configs.ToList();
        _appState.Conexoes.Clear();
        foreach (var config in list)
        {
            _appState.Conexoes.Add(config);
        }

        await _configManager.SaveAllAsync(list);
    }

    public async Task<string?> GetConnectionStringAsync(string ambiente)
    {
        var all = await GetAllAsync();
        var ambienteNormalizado = ambiente.Trim();
        return all.FirstOrDefault(c => string.Equals(c.Ambiente?.Trim(), ambienteNormalizado, StringComparison.OrdinalIgnoreCase))?.ConnectionString;
    }

    public async Task<DatabaseProvider?> GetProviderAsync(string ambiente)
    {
        var all = await GetAllAsync();
        var ambienteNormalizado = ambiente.Trim();
        return all.FirstOrDefault(c => string.Equals(c.Ambiente?.Trim(), ambienteNormalizado, StringComparison.OrdinalIgnoreCase))?.Provider;
    }
}
