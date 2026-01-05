namespace MeuProjeto.UI.ViewModels;

public class NavigationItem
{
    public NavigationItem(string title, object viewModel)
    {
        Title = title;
        ViewModel = viewModel;
    }

    public string Title { get; }
    public object ViewModel { get; }
}
