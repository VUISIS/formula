using ReactiveUI;
using Avalonia.Controls;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Debugger.ViewModels.Types;
using Debugger.Views;
using Debugger.Windows;

namespace Debugger.ViewModels;

internal class SolverViewModel : ReactiveObject
{
    private readonly string[] constraintIems = new string[] { "=", "<", "<=", ">", ">=" };
    private readonly TextBox? inputExpression;
    private readonly FormulaProgram formulaProgram;
    private List<Task> tasks = new List<Task>();
    private readonly CommandConsoleViewModel? commandConsoleViewModel;
    public SolverViewModel(MainWindow window, FormulaProgram program)
    {
        formulaProgram = program;
        
        VariableItems = new ObservableCollection<Node>();
        AllConstraintsItems = new ObservableCollection<Node>();
        SolutionItems = new ObservableCollection<Node>();
        CounterExampleItems = new ObservableCollection<Node>();
        ConstraintItems = new ObservableCollection<Node>();
        
        foreach(var item in constraintIems)
        {
            ConstraintItems.Add(new Node(item));
        }
        
        SelectedVariable = new Node("");
        SelectedConstraint = new Node("");

        inputExpression = window.Get<SolverView>("SolverCommandView").Get<TextBox>("InputExpression");
        commandConsoleViewModel = window.Get<CommandConsoleView>("CommandInputView").DataContext as CommandConsoleViewModel;
    }

    public void ClearAll()
    {
        VariableItems.Clear();
        AllConstraintsItems.Clear();
        SolutionItems.Clear();
        CounterExampleItems.Clear();
    }

    public ObservableCollection<Node> AllConstraintsItems { get; }
    public ObservableCollection<Node> SolutionItems { get; }
    public ObservableCollection<Node> CounterExampleItems { get; }
    public ObservableCollection<Node> ConstraintItems { get; }
    public ObservableCollection<Node> VariableItems { get; }
    public Node SelectedVariable { get; }
    public Node SelectedConstraint { get; }
    public void AddConstraint()
    {
        if (inputExpression != null)
        {
            decimal outNum;
            if (decimal.TryParse(inputExpression.Text, out outNum))
            {
                Console.WriteLine(outNum);
            }
        }
    }

    public void GenerateNextSolution()
    {
    }
}