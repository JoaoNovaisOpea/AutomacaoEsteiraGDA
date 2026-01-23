using Avalonia.Controls;
using Avalonia.Interactivity;
using AutomacaoGDA.Core.Models;

namespace AutomacaoGDA.UI.Views;

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
