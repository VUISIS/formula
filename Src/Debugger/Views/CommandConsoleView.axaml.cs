using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Debugger.ViewModels;

namespace Debugger.Views;

public partial class CommandConsoleView : UserControl
{
    public CommandConsoleView()
    {
        InitializeComponent();

        this.DataContext = new CommandConsoleViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}