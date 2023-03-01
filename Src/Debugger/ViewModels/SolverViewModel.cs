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
    private readonly string[] constraintIems = new string[] { "=", "<", "<=", ">", ">=" };
    public SolverViewModel()
    {
        VariableItems = new ObservableCollection<Node>();
        AllConstraintsItems = new ObservableCollection<Node>();
        SolutionItems = new ObservableCollection<Node>();
        CounterExampleItems = new ObservableCollection<Node>();
        ConstraintItems = new ObservableCollection<Node>();
        
        foreach(var item in constraintIems)
        {
            ConstraintItems.Add(new Node(item));
        }
        
        AddedConstraint = new string("");
    }

    public void ClearAll()
    {
        VariableItems.Clear();
        AllConstraintsItems.Clear();
        SolutionItems.Clear();
    }

    public ObservableCollection<Node> AllConstraintsItems { get; }
    public ObservableCollection<Node> SolutionItems { get; }
    public ObservableCollection<Node> CounterExampleItems { get; }
    public ObservableCollection<Node> ConstraintItems { get; }
    public ObservableCollection<Node> VariableItems { get; }
    public string AddedConstraint { get; }
}