using ReactiveUI;

using System.Collections.ObjectModel;

using Debugger.ViewModels.Types;

namespace Debugger.ViewModels;

internal class TermsConstraintsViewModel : ReactiveObject
{
    public TermsConstraintsViewModel()
    {
        CurrentTermItems = new ObservableCollection<Node>();
    }
    
    public ObservableCollection<Node> CurrentTermItems { get; }
}