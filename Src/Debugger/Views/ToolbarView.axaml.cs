using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Debugger.Views;

public partial class ToolbarView : UserControl
{
    public ToolbarView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}