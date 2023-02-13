using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Debugger.ViewModels;

namespace Debugger.Views;

public partial class InferenceRulesView : UserControl
{
    public InferenceRulesView()
    {
        InitializeComponent();

        DataContext = new InferenceRulesViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}