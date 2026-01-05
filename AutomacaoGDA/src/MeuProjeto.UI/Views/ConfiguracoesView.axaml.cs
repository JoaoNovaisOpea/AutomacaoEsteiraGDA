using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MeuProjeto.Core.Models;
using MeuProjeto.UI.ViewModels;

namespace MeuProjeto.UI.Views;

public partial class ConfiguracoesView : UserControl
{
    public ConfiguracoesView()
    {
        InitializeComponent();
    }

    private async void OnEditarAmbiente(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ConfiguracoesViewModel vm || vm.ConexaoSelecionada is null)
        {
            return;
        }

        await AbrirDialogAsync(vm.ConexaoSelecionada);
    }

    private async void OnEditarAmbienteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control || control.DataContext is not ConexaoConfig config)
        {
            return;
        }

        await AbrirDialogAsync(config);
    }

    private async Task AbrirDialogAsync(ConexaoConfig config)
    {
        var owner = this.GetVisualRoot() as Window;
        var dialog = new EnvironmentConfigDialog(config);

        if (owner is null)
        {
            await dialog.ShowDialog<bool>(new Window());
        }
        else
        {
            await dialog.ShowDialog<bool>(owner);
        }
    }
}
