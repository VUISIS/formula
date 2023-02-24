using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Debugger.Views;

public partial class ConstraintsView : UserControl
{
    public ConstraintsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}