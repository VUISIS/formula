using System;
using ReactiveUI;

using System.Collections.ObjectModel;

using Debugger.ViewModels.Types;

namespace Debugger.ViewModels;

internal class InferenceRulesViewModel : ReactiveObject
{
    public InferenceRulesViewModel()
    {
        Items = new ObservableCollection<Node>();
    }
    
    public void ClearAll()
    {
        Items.Clear();
    }
    
    public ObservableCollection<Node> Items { get; }
}