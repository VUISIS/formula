using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Debugger.ViewModels;

namespace Debugger.Views;

public partial class SolverView : UserControl
{
    public SolverView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AddConstraintClick(object sender, RoutedEventArgs e)
    {
        //add the constraint
    }

    private void SolveConstraintsClick(object sender, RoutedEventArgs e)
    {
        //solve constraints
    }

    private void GenerateNextSolutionClick(object sender, RoutedEventArgs e)
    {
        //generate the next solution
    }
}