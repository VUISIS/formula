using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Debugger.Views;

public partial class CurrentTermsView : UserControl
{
    public CurrentTermsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}