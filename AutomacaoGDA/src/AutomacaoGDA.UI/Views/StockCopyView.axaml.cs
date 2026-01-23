using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AutomacaoGDA.UI.ViewModels;

namespace AutomacaoGDA.UI.Views;

public partial class StockCopyView : UserControl
{
    public StockCopyView()
    {
        InitializeComponent();
    }

    private async void OnCopiarStockClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StockCopyViewModel vm || !vm.PodeCopiar)
        {
            return;
        }

        var owner = this.GetVisualRoot() as Window;
        var dialog = new ConfirmDialog("Confirmar copia",
            "Deseja realmente copiar o OperationStock entre os ambientes?");

        var confirmado = owner is null
            ? await dialog.ShowDialog<bool>(new Window())
            : await dialog.ShowDialog<bool>(owner);

        if (confirmado)
        {
            vm.CopiarStockCommand.Execute(null);
        }
    }
}
