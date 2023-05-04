using ReactiveUI;

using System.Collections.ObjectModel;

using Debugger.ViewModels.Types;

namespace Debugger.ViewModels;

public class ConstraintsViewModel : ReactiveObject
{
    public ConstraintsViewModel()
    {
        DirectConstraintsItems = new ObservableCollection<Node>();
        PosConstraintsItems = new ObservableCollection<Node>();
        NegConstraintsItems = new ObservableCollection<Node>();
        FlatConstraintsItems = new ObservableCollection<Node>();
    }
    
    public void ClearAll()
    {
        DirectConstraintsItems.Clear();
        PosConstraintsItems.Clear();
        NegConstraintsItems.Clear();
        FlatConstraintsItems.Clear();
    }
    
    public ObservableCollection<Node> DirectConstraintsItems { get; }
    public ObservableCollection<Node> PosConstraintsItems { get; }
    public ObservableCollection<Node> NegConstraintsItems { get; }
    public ObservableCollection<Node> FlatConstraintsItems { get; }
}