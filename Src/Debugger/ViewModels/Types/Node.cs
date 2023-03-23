using System.Collections.ObjectModel;

namespace Debugger.ViewModels.Types;

public class Node
{
    public Node(string folderName, int index, int id = -1)
    {
        Header = folderName;
        Index = index+1;
        Children = new ObservableCollection<Node>();
        Id = id;
        Display = Index + ": " + folderName;
    }

    public Node(Node parent, string fileName, int index, int id = -1)
    {
        Children = new ObservableCollection<Node>();
        Parent = parent;
        Header = fileName;
        Index = index+1;
        Id = id;
        Display = Index + ": " + fileName;
    }

    public Node? Parent;
    public string Header { get; set; }
    public int Id { get; }
    public int Index { get; }
    public string Display { get; }
    public ObservableCollection<Node> Children { get; }
    public void AddItem(Node child) => Children.Add(child);
    public void RemoveItem(Node child) => Children.Remove(child);
    public override string ToString() => Header;
}