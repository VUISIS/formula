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
    
    public ObservableCollection<Node> Items { get; }
}