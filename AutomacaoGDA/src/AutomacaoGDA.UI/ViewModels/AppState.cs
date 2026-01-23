using System.Collections.ObjectModel;
using AutomacaoGDA.Core.Models;

namespace AutomacaoGDA.UI.ViewModels;

public class AppState : ViewModelBase
{
    private ConexaoConfig? _conexaoSelecionada;
    private OperationInfo? _operacaoSelecionada;
    private string _operacoesStatus = string.Empty;
    private string _bancoStatusMensagem = string.Empty;
    private bool _bancoStatusVisivel;
    private string _bearerToken = string.Empty;

    public ObservableCollection<ConexaoConfig> Conexoes { get; } = new();
    public ObservableCollection<OperationInfo> Operacoes { get; } = new();

    public ConexaoConfig? ConexaoSelecionada
    {
        get => _conexaoSelecionada;
        set
        {
            if (SetProperty(ref _conexaoSelecionada, value))
            {
                RaisePropertyChanged(nameof(AmbienteSelecionado));
            }
        }
    }

    public string AmbienteSelecionado => ConexaoSelecionada?.Ambiente ?? "Nenhum";

    public OperationInfo? OperacaoSelecionada
    {
        get => _operacaoSelecionada;
        set => SetProperty(ref _operacaoSelecionada, value);
    }

    public string OperacoesStatus
    {
        get => _operacoesStatus;
        set => SetProperty(ref _operacoesStatus, value);
    }

    public string BancoStatusMensagem
    {
        get => _bancoStatusMensagem;
        set => SetProperty(ref _bancoStatusMensagem, value);
    }

    public bool BancoStatusVisivel
    {
        get => _bancoStatusVisivel;
        set => SetProperty(ref _bancoStatusVisivel, value);
    }

    public string BearerToken
    {
        get => _bearerToken;
        set => SetProperty(ref _bearerToken, value);
    }
}
