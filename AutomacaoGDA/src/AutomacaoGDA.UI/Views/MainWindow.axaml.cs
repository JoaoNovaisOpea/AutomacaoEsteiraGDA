using Avalonia.Controls;
using AutomacaoGDA.UI.ViewModels;

namespace AutomacaoGDA.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
