using System;
using ReactiveUI;
using Avalonia.Controls;
using Avalonia.Input;

using System.Collections.ObjectModel;

using Debugger.ViewModels.Types;
using Debugger.Windows;
using Debugger.Views;

using Microsoft.Formula.API;

namespace Debugger.ViewModels;

public class CurrentTermsViewModel : ReactiveObject
{
    private readonly FormulaProgram formulaProgram;
    private readonly ConstraintsViewModel? constraintsViewModel;
    public CurrentTermsViewModel(MainWindow window, FormulaProgram program)
    {
        formulaProgram = program;
        
        CurrentTermItems = new ObservableCollection<Node>();
        CurrentTermSelectedItems = new ObservableCollection<Node>();
        
        constraintsViewModel = window.Get<ConstraintsView>("ConstrView").DataContext as ConstraintsViewModel;

        var currentTermsBox = window.Get<CurrentTermsView>("TermsView").Get<ListBox>("CurrentTerms");
        currentTermsBox.DoubleTapped += CurrentTermsDoubleTap;
    }

    private void CurrentTermsDoubleTap(object? sender, TappedEventArgs e)
    {
        if (CurrentTermSelectedItems.Count > 0 &&
            CurrentTermSelectedItems[0].Id > -1 &&
            constraintsViewModel != null)
        {
            var terms = formulaProgram.FormulaPublisher.GetLeastFixedPointConstraints();
            
            constraintsViewModel.DirectConstraintsItems.Clear();
            constraintsViewModel.PosConstraintsItems.Clear();
            constraintsViewModel.NegConstraintsItems.Clear();
            constraintsViewModel.FlatConstraintsItems.Clear();

            foreach (var constraints in terms[CurrentTermSelectedItems[0].Id][ConstraintKind.Direct])
            {
                var node = new Node(constraints);
                constraintsViewModel.DirectConstraintsItems.Add(node);
            }
            
            foreach(var constraints in terms[CurrentTermSelectedItems[0].Id][ConstraintKind.Positive])
            {
                var node = new Node(constraints);
                constraintsViewModel.PosConstraintsItems.Add(node);
            }
            
            foreach(var constraints in terms[CurrentTermSelectedItems[0].Id][ConstraintKind.Negative])
            {
                var node = new Node(constraints);
                constraintsViewModel.NegConstraintsItems.Add(node);
            }
            
            foreach(var constraints in terms[CurrentTermSelectedItems[0].Id][ConstraintKind.Flattened])
            {
                var node = new Node(constraints);
                constraintsViewModel.FlatConstraintsItems.Add(node);
            }
        }
    }

    public void ClearAll()
    {
        CurrentTermItems.Clear();
        CurrentTermSelectedItems.Clear();
    }
    
    public ObservableCollection<Node> CurrentTermItems { get; }
    public ObservableCollection<Node> CurrentTermSelectedItems { get; }
}