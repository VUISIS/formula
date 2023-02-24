using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Debugger.Views;

public partial class Domain4MLView : UserControl
{
    public Domain4MLView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}