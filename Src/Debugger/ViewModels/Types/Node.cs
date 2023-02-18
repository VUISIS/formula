using System.Collections.ObjectModel;

namespace Debugger.ViewModels.Types;

public class Node
{
    public Node(string folderName, int id = -1)
    {
        Header = folderName;
        Children = new ObservableCollection<Node>();
        Id = id;
    }

    public Node(Node parent, string fileName, int id = -1)
    {
        Children = new ObservableCollection<Node>();
        Parent = parent;
        Header = fileName;
        Id = id;
    }

    public Node? Parent;
    public string Header { get; }
    public int Id { get; }
    public ObservableCollection<Node> Children { get; }
    public void AddItem(Node child) => Children.Add(child);
    public void RemoveItem(Node child) => Children.Remove(child);
    public override string ToString() => Header;
}