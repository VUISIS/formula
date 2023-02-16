using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Debugger.ViewModels;

namespace Debugger.Views;

public partial class TermsConstraintsView : UserControl
{
    public TermsConstraintsView()
    {
        InitializeComponent();

        DataContext = new TermsConstraintsViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}