using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AutomacaoGDA.UI.ViewModels;
using AutomacaoGDA.UI.Views;

namespace AutomacaoGDA.UI;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel();
            desktop.MainWindow = new MainWindow(viewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
