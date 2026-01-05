using Avalonia.Controls;
using Avalonia.Interactivity;
using MeuProjeto.Core.Models;

namespace MeuProjeto.UI.Views;

public partial class EnvironmentConfigDialog : Window
{
    public EnvironmentConfigDialog()
    {
        InitializeComponent();
    }

    public EnvironmentConfigDialog(ConexaoConfig config) : this()
    {
        DataContext = config;
    }

    private void OnFecharClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
