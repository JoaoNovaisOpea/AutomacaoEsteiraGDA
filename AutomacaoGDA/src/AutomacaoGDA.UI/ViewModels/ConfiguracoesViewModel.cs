using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutomacaoGDA.Core.Interfaces;
using AutomacaoGDA.Core.Models;
using AutomacaoGDA.UI.Services;

namespace AutomacaoGDA.UI.ViewModels;

public class ConfiguracoesViewModel : ViewModelBase
{
    private readonly IConnectionConfigProvider _configProvider;
    private readonly AppSettingsConfigManager _configManager;
    private readonly AppState _appState;
    private string _statusMessage = string.Empty;
    private ConexaoConfig? _conexaoSelecionada;
    private string _novoAmbiente = string.Empty;
    private string _bearerToken = string.Empty;

    public ConfiguracoesViewModel(IConnectionConfigProvider configProvider, AppSettingsConfigManager configManager, AppState appState)
    {
        _configProvider = configProvider;
        _configManager = configManager;
        _appState = appState;
        Conexoes = appState.Conexoes;
        SalvarCommand = new AsyncRelayCommand(SalvarAsync);
        AdicionarCommand = new AsyncRelayCommand(AdicionarAsync, PodeAdicionar);
        RemoverCommand = new AsyncRelayCommand(RemoverAsync, () => ConexaoSelecionada is not null);
    }

    public ObservableCollection<ConexaoConfig> Conexoes { get; }

    public AsyncRelayCommand SalvarCommand { get; }
    public AsyncRelayCommand AdicionarCommand { get; }
    public AsyncRelayCommand RemoverCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ConexaoConfig? ConexaoSelecionada
    {
        get => _conexaoSelecionada;
        set
        {
            if (!SetProperty(ref _conexaoSelecionada, value))
            {
                return;
            }

            RemoverCommand.RaiseCanExecuteChanged();
        }
    }

    public IReadOnlyList<DatabaseProvider> Providers { get; } = Enum.GetValues<DatabaseProvider>();

    public string NovoAmbiente
    {
        get => _novoAmbiente;
        set
        {
            if (!SetProperty(ref _novoAmbiente, value))
            {
                return;
            }

            AdicionarCommand.RaiseCanExecuteChanged();
        }
    }

    public string BearerToken
    {
        get => _bearerToken;
        set
        {
            if (SetProperty(ref _bearerToken, value))
            {
                _appState.BearerToken = NormalizeBearerToken(value);
            }
        }
    }

    public async Task CarregarAsync()
    {
        await _configProvider.GetAllAsync();
        BearerToken = await _configManager.GetBearerTokenAsync();

        if (_appState.ConexaoSelecionada is null && Conexoes.Count > 0)
        {
            _appState.ConexaoSelecionada = Conexoes[0];
        }
    }

    private async Task SalvarAsync()
    {
        await _configProvider.SaveAllAsync(Conexoes);
        var normalizedToken = NormalizeBearerToken(BearerToken);
        BearerToken = normalizedToken;
        await _configManager.SaveBearerTokenAsync(normalizedToken);
        StatusMessage = "Configuracoes salvas com sucesso.";
    }

    private Task AdicionarAsync()
    {
        var novo = new ConexaoConfig
        {
            Ambiente = NovoAmbiente.Trim()
        };

        Conexoes.Add(novo);
        ConexaoSelecionada = novo;
        StatusMessage = string.Empty;
        NovoAmbiente = string.Empty;
        return Task.CompletedTask;
    }

    private Task RemoverAsync()
    {
        if (ConexaoSelecionada is null)
        {
            return Task.CompletedTask;
        }

        var toRemove = ConexaoSelecionada;
        Conexoes.Remove(toRemove);
        ConexaoSelecionada = Conexoes.FirstOrDefault();
        StatusMessage = string.Empty;
        return Task.CompletedTask;
    }

    private static string NormalizeBearerToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var trimmed = token.Trim();
        const string bearerPrefix = "Bearer ";
        if (trimmed.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed[bearerPrefix.Length..].Trim();
        }

        return trimmed;
    }

    private bool PodeAdicionar()
    {
        return !string.IsNullOrWhiteSpace(NovoAmbiente);
    }
}
