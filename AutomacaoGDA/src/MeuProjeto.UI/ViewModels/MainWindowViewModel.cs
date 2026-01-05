using System.Collections.ObjectModel;
using Avalonia.Threading;
using MeuProjeto.Core.Services;
using MeuProjeto.Infrastructure;
using MeuProjeto.UI.Services;

namespace MeuProjeto.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    public MainWindowViewModel()
    {
        var connectorFactory = new DbConnectorFactory();

        AppState = new AppState();
        var configManager = new AppSettingsConfigManager();
        var connectionProvider = new UiConnectionConfigProvider(configManager, AppState);
        _databaseService = new DatabaseService(connectionProvider, connectorFactory);
        ConfiguracoesViewModel = new ConfiguracoesViewModel(connectionProvider, configManager, AppState);
        DataCleanupViewModel = new DataCleanupViewModel(_databaseService, AppState);
        ResetAcquisitionViewModel = new ResetAcquisitionViewModel(_databaseService, AppState);
        StockCopyViewModel = new StockCopyViewModel(_databaseService, AppState);

        var logService = new LogService();
        var s3Service = new S3Service();
        var creditUploadService = new CreditUploadService(_databaseService, logService);
        CreditUploadViewModel = new CreditUploadViewModel(_databaseService, creditUploadService, s3Service, AppState, logService);

        Pages = new ObservableCollection<NavigationItem>
        {
            new("Limpar Operacao", DataCleanupViewModel),
            new("Resetar Aquisicao", ResetAcquisitionViewModel),
            new("Copiar Stock", StockCopyViewModel),
            new("Subir Creditos", CreditUploadViewModel),
            new("Configuracoes", ConfiguracoesViewModel)
        };
        SelectedPage = Pages.FirstOrDefault();

        CarregarOperacoesCommand = new AsyncRelayCommand(CarregarOperacoesAsync);

        _ = ConfiguracoesViewModel.CarregarAsync().ContinueWith(_ =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (AppState.Conexoes.Count > 0)
                {
                    AppState.ConexaoSelecionada ??= AppState.Conexoes[0];
                }
            });
        });

        AppState.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AppState.ConexaoSelecionada))
            {
                _ = CarregarOperacoesAsync();
            }
        };
    }

    public AppState AppState { get; }
    public ConfiguracoesViewModel ConfiguracoesViewModel { get; }
    public DataCleanupViewModel DataCleanupViewModel { get; }
    public ResetAcquisitionViewModel ResetAcquisitionViewModel { get; }
    public StockCopyViewModel StockCopyViewModel { get; }
    public CreditUploadViewModel CreditUploadViewModel { get; }
    public ObservableCollection<NavigationItem> Pages { get; }

    private NavigationItem? _selectedPage;
    public NavigationItem? SelectedPage
    {
        get => _selectedPage;
        set => SetProperty(ref _selectedPage, value);
    }

    public AsyncRelayCommand CarregarOperacoesCommand { get; }

    private async Task CarregarOperacoesAsync()
    {
        var ambiente = AppState.ConexaoSelecionada?.Ambiente ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ambiente))
        {
            AppState.OperacoesStatus = "Selecione um ambiente.";
            return;
        }

        try
        {
            AppState.OperacoesStatus = "Carregando operacoes...";
            var lista = await _databaseService.ListarOperacoesAtivas(ambiente);
            AppState.Operacoes.Clear();
            foreach (var item in lista)
            {
                AppState.Operacoes.Add(item);
            }

            AppState.OperacaoSelecionada = AppState.Operacoes.Count > 0 ? AppState.Operacoes[0] : null;
            AppState.OperacoesStatus = $"{AppState.Operacoes.Count} operacao(oes) ativas.";
            AppState.BancoStatusMensagem = string.Empty;
            AppState.BancoStatusVisivel = false;
        }
        catch (Exception ex)
        {
            AppState.OperacoesStatus = ex.Message;
            AppState.BancoStatusMensagem = "Nao foi possivel conectar com o banco de dados.";
            AppState.BancoStatusVisivel = true;
        }
    }
}
