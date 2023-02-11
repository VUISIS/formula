using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Debugger.Views;

public partial class CommandConsoleView : UserControl
{
    public CommandConsoleView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}