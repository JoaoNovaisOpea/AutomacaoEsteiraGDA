using System.Text.Json;
using AutomacaoGDA.Core.Interfaces;
using AutomacaoGDA.Core.Models;

namespace AutomacaoGDA.UI.Services;

public class AppSettingsConfigManager : IConnectionConfigProvider
{
    private const string FileName = "appsettings.json";
    private readonly string _filePath;
    private readonly string _defaultFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private AppSettingsModel? _cache;

    public AppSettingsConfigManager()
    {
        _defaultFilePath = Path.Combine(AppContext.BaseDirectory, FileName);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, "AutomacaoGDA");
        Directory.CreateDirectory(configDir);
        _filePath = Path.Combine(configDir, FileName);
    }

    public async Task<IReadOnlyList<ConexaoConfig>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_cache is not null)
            {
                return _cache.Connections;
            }

            _cache = await LoadInternalAsync();
            return _cache.Connections;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAllAsync(IEnumerable<ConexaoConfig> configs)
    {
        await _lock.WaitAsync();
        try
        {
            var list = configs.ToList();
            _cache ??= await LoadInternalAsync();
            _cache.Connections = list;
            await SaveInternalAsync(_cache);
        }
        finally
        {
            _lock.Release();
        }
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

    public async Task<string> GetBearerTokenAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _cache ??= await LoadInternalAsync();
            return _cache.BearerToken ?? string.Empty;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveBearerTokenAsync(string token)
    {
        await _lock.WaitAsync();
        try
        {
            _cache ??= await LoadInternalAsync();
            _cache.BearerToken = token.Trim();
            await SaveInternalAsync(_cache);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<AppSettingsModel> LoadInternalAsync()
    {
        if (!File.Exists(_filePath))
        {
            if (File.Exists(_defaultFilePath))
            {
                var seedSettings = await LoadFromFileAsync(_defaultFilePath);
                await SaveInternalAsync(seedSettings);
                return seedSettings;
            }

            var defaultSettings = CreateDefaults();
            await SaveInternalAsync(defaultSettings);
            return defaultSettings;
        }

        var settings = await LoadFromFileAsync(_filePath);

        if (settings.Connections.Count == 0)
        {
            settings.Connections = CreateDefaults().Connections;
            await SaveInternalAsync(settings);
        }

        return settings;
    }

    private async Task SaveInternalAsync(AppSettingsModel settings)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, settings, options);
    }

    private static async Task<AppSettingsModel> LoadFromFileAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var settings = await JsonSerializer.DeserializeAsync<AppSettingsModel>(stream);
        settings ??= new AppSettingsModel();
        settings.Connections ??= new List<ConexaoConfig>();
        settings.BearerToken ??= string.Empty;
        return settings;
    }

    private static AppSettingsModel CreateDefaults() =>
        new()
        {
            Connections = new List<ConexaoConfig>
            {
                new ConexaoConfig { Ambiente = "Teste" },
                new ConexaoConfig { Ambiente = "Homologacao" },
                new ConexaoConfig { Ambiente = "Producao" }
            },
            BearerToken = string.Empty
        };

    private sealed class AppSettingsModel
    {
        public List<ConexaoConfig> Connections { get; set; } = new();
        public string? BearerToken { get; set; }
    }
}
