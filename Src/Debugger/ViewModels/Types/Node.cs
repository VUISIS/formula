using System.Collections.ObjectModel;

namespace Debugger.ViewModels.Types;

public class Node
{
    public Node(string folderName)
    {
        Header = folderName;
        Children = new ObservableCollection<Node>();
    }

    public Node(Node parent, string fileName)
    {
        Children = new ObservableCollection<Node>();
        Parent = parent;
        Header = fileName;
    }

    public Node? Parent;
    public string Header { get; }
    public ObservableCollection<Node> Children { get; }
    public void AddItem(Node child) => Children.Add(child);
    public void RemoveItem(Node child) => Children.Remove(child);
    public override string ToString() => Header;
}