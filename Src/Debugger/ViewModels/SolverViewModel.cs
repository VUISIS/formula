using ReactiveUI;
using System.Collections.ObjectModel;

using Debugger.Views;
using Debugger.ViewModels.Types;
using Debugger.Windows;
using Microsoft.Formula.API;
using Avalonia.Interactivity;

namespace Debugger.ViewModels;

internal class SolverViewModel : ReactiveObject
{
    public SolverViewModel()
    {
        SymbolicVariablesItems = new ObservableCollection<Node>();
        AllConstraintsItems = new ObservableCollection<Node>();
        SolutionItems = new ObservableCollection<Node>();

    }

    public void ClearAll()
    {
        SymbolicVariablesItems.Clear();
        AllConstraintsItems.Clear();
        SolutionItems.Clear();
    }

    public ObservableCollection<Node> SymbolicVariablesItems { get; }
    public ObservableCollection<Node> AllConstraintsItems { get; }
    public ObservableCollection<Node> SolutionItems { get; }
    public string AddedConstraint { get; }
}