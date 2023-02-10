using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Debugger.ViewModels;

namespace Debugger.Views;

public partial class SolverView : UserControl
{
    public SolverView()
    {
        InitializeComponent();

        this.DataContext = new SolverViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}