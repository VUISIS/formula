using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;

using Avalonia;
using ReactiveUI;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace Debugger.Views;

internal class MainWindowViewModel : ReactiveObject
{
    private MainWindow win;
    private IStorageFolder? lastSelectedDirectory;
    private Node? root;
    private FormulaProgram formulaProgram;
    private Button? loadButton;
    
    public MainWindowViewModel(MainWindow mainWin)
    {
        win = mainWin;

        formulaProgram = new FormulaProgram();
        
        Items = new ObservableCollection<Node>();
        ItemsSource = new ObservableCollection<Node>();
        SelectedItems = new ObservableCollection<Node>();

        loadButton = win.Get<Button>("LoadButton");
    }

    public ObservableCollection<Node>? Items { get; }
    public ObservableCollection<Node>? ItemsSource { get; set;  }
    public ObservableCollection<Node>? SelectedItems { get; }

    public void LoadFormulaFileCmd()
    {
        if (SelectedItems != null &&
            SelectedItems.Count > 0)
        {
            Uri? uri = null;
            if (lastSelectedDirectory != null &&
                lastSelectedDirectory.TryGetUri(out uri))
            {
                if(!formulaProgram.ExecuteCommand("load " + Path.Join(uri.AbsolutePath, SelectedItems[0].Header)))
                {
                    Console.WriteLine("Command Failed");
                }
                win.Get<TextBlock>("ConsoleOutput").Text += "[]> load " + Path.Join(uri.AbsolutePath, SelectedItems[0].Header) + "\n" + formulaProgram.GetConsoleOutput();
            }
        }
    }
    
    public async void OpenCmd()
    {
        var file = await GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "File",
            AllowMultiple = false,
            SuggestedStartLocation = lastSelectedDirectory,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("formula")
                {
                    Patterns = new List<string>(){"*.4ml"}
                }
            }
        });

        var formulaFile = file.FirstOrDefault();
        if (formulaFile != null &&
            Items != null &&
            ItemsSource != null)
        {
            lastSelectedDirectory = await formulaFile.GetParentAsync();
            root = new Node(formulaFile.Name);
            
            ItemsSource.Clear();
            Items.Clear();
            Items.Add(root);
            
            Uri? uri = null;
            if (lastSelectedDirectory != null &&
                lastSelectedDirectory.TryGetUri(out uri))
            {
                if(!formulaProgram.ExecuteCommand("load " + Path.Join(uri.AbsolutePath, formulaFile.Name)))
                {
                    Console.WriteLine("Command Failed");
                }
                win.Get<TextBlock>("ConsoleOutput").Text += "[]> load " + Path.Join(uri.AbsolutePath, formulaFile.Name) + "\n" + formulaProgram.GetConsoleOutput();
            }
        }
    }
    
    private IStorageProvider GetStorageProvider()
    {
        return GetTopLevel().StorageProvider;
    }
    
    TopLevel GetTopLevel() => win.GetVisualRoot() as TopLevel ?? throw new NullReferenceException("Invalid Owner");

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
            root = new Node(lastSelectedDirectory.Name);
            var items = await lastSelectedDirectory.GetItemsAsync();
            foreach (var file in items)
            {
                if (file.Name.EndsWith(".4ml"))
                {
                    var child = new Node(root, file.Name);
                    root.AddItem(child);
                }
            }

            if (root.Children.Count > 0 &&
                Items != null &&
                ItemsSource != null)
            {
                Items.Clear();
                ItemsSource.Clear();
                
                Items.Add(root);
                ItemsSource = root.Children;
                
                if (loadButton != null)
                {
                    loadButton.IsEnabled = true;
                }
            }
        }
    }
    
    public void ExitCmd() 
    {
        win.Close();
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

        public Node? Parent;
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