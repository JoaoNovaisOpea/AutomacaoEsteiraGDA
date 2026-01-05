using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MeuProjeto.UI.ViewModels;

namespace MeuProjeto.UI.Views;

public partial class DataCleanupView : UserControl
{
    public DataCleanupView()
    {
        InitializeComponent();
    }

    private async void OnExecutarLimpezaClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DataCleanupViewModel vm || !vm.PodeExecutarLimpeza)
        {
            return;
        }

        var owner = this.GetVisualRoot() as Window;
        var dialog = new ConfirmDialog("Confirmar limpeza",
            "Deseja realmente executar a limpeza? Essa acao e irreversivel.");

        var confirmado = owner is null
            ? await dialog.ShowDialog<bool>(new Window())
            : await dialog.ShowDialog<bool>(owner);

        if (confirmado)
        {
            vm.ExecutarLimpezaCommand.Execute(null);
        }
    }
}
