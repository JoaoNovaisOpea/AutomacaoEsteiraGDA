using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MeuProjeto.UI.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;

        OkButton.Click += OnOkClick;
        CancelButton.Click += OnCancelClick;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
