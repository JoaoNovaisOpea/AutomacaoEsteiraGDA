using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AutomacaoGDA.UI.ViewModels;

namespace AutomacaoGDA.UI.Views;

public partial class ResetAcquisitionView : UserControl
{
    public ResetAcquisitionView()
    {
        InitializeComponent();
    }

    private async void OnExecutarResetClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ResetAcquisitionViewModel vm || !vm.PodeExecutar)
        {
            return;
        }

        var owner = this.GetVisualRoot() as Window;
        var dialog = new ConfirmDialog("Confirmar reset",
            "Deseja realmente executar o reset da aquisicao?");

        var confirmado = owner is null
            ? await dialog.ShowDialog<bool>(new Window())
            : await dialog.ShowDialog<bool>(owner);

        if (confirmado)
        {
            vm.ExecutarResetCommand.Execute(null);
        }
    }
}
