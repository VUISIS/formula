using System;
using ReactiveUI;
using Avalonia.Controls;
using Avalonia.Input;

using System.Collections.ObjectModel;

using Debugger.ViewModels.Types;
using Debugger.Windows;
using Debugger.Views;

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
       if(CurrentTermSelectedItems.Count > 0 &&
          CurrentTermSelectedItems[0].Id > -1 &&
          constraintsViewModel != null)
        {
            var posConstraints = formulaProgram.FormulaPublisher.GetPosConstraints();
            var negConstraints = formulaProgram.FormulaPublisher.GetNegConstraints();
            var dirConstraints = formulaProgram.FormulaPublisher.GetDirConstraints();
            var flatConstraints = formulaProgram.FormulaPublisher.GetFlatConstraints();
            
            constraintsViewModel.ClearAll();

            if (dirConstraints.ContainsKey(CurrentTermSelectedItems[0].Id))
            {
                foreach (var constraints in dirConstraints[CurrentTermSelectedItems[0].Id])
                {
                    var node = new Node(constraints, constraintsViewModel.DirectConstraintsItems.Count);
                    constraintsViewModel.DirectConstraintsItems.Add(node);
                }
            }

            if (posConstraints.ContainsKey(CurrentTermSelectedItems[0].Id))
            {
                foreach (var constraints in posConstraints[CurrentTermSelectedItems[0].Id])
                {
                    var node = new Node(constraints, constraintsViewModel.PosConstraintsItems.Count);
                    constraintsViewModel.PosConstraintsItems.Add(node);
                }
            }

            if (negConstraints.ContainsKey(CurrentTermSelectedItems[0].Id))
            {
                foreach (var constraints in negConstraints[CurrentTermSelectedItems[0].Id])
                {
                    var node = new Node(constraints, constraintsViewModel.NegConstraintsItems.Count);
                    constraintsViewModel.NegConstraintsItems.Add(node);
                }
            }

            if (flatConstraints.ContainsKey(CurrentTermSelectedItems[0].Id))
            {
                foreach (var constraints in flatConstraints[CurrentTermSelectedItems[0].Id])
                {
                    var node = new Node(constraints, constraintsViewModel.FlatConstraintsItems.Count);
                    constraintsViewModel.FlatConstraintsItems.Add(node);
                }
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