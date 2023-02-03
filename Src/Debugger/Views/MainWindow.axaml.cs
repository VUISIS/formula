using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using ReactiveUI;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace FormulaDebuggerGUI.Views;

internal class MainWindowViewModel : ReactiveObject
{
    private MainWindow _win;
    private IStorageFolder? lastSelectedDirectory = null;
    private Node _root = new Node("null");
    
    public MainWindowViewModel(MainWindow win)
    {
        _win = win;
        
        Items = new ObservableCollection<Node>();
        ItemsSource = new ObservableCollection<Node>();
        SelectedItems = new ObservableCollection<Node>();
    }
    
    public ObservableCollection<Node>? Items { get; }
    public ObservableCollection<Node>? ItemsSource { get; set;  }
    public ObservableCollection<Node>? SelectedItems { get; }

    public async void OpenCmd()
    {
        await Task.Delay(2000);
    }
    
    private IStorageProvider GetStorageProvider()
    {
        return GetTopLevel().StorageProvider;
    }
    
    TopLevel GetTopLevel() => _win.GetVisualRoot() as TopLevel ?? throw new NullReferenceException("Invalid Owner");

    public async void OpenFolderCmd()
    {
        var folders = await GetStorageProvider().OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Folder",
            SuggestedStartLocation = lastSelectedDirectory,
            AllowMultiple = false
        });

        lastSelectedDirectory = folders.FirstOrDefault();
        if (lastSelectedDirectory != null)
        {
            _root = new Node(lastSelectedDirectory.Name);
            if(Items != null)
                Items.Add(_root);
            var items = await lastSelectedDirectory.GetItemsAsync();
            foreach (var file in items)
            {
                var child = new Node(_root, file.Name);
                _root.AddItem(child);
            }

            ItemsSource = _root.Children;
        }
    }
    
    public void ExitCmd() 
    {
        _win.Close();
    }
    
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

        public Node? Parent { get; }
        public string Header { get; }
        public ObservableCollection<Node> Children { get; }
        public void AddItem(Node child) => Children.Add(child);
        public void RemoveItem(Node child) => Children.Remove(child);
        public override string ToString() => Header;
    }
}

public partial class MainWindow : Window
{    
    public MainWindow()
    {
        this.InitializeComponent();
        
        this.AttachDevTools();

        this.DataContext = new MainWindowViewModel(this);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}