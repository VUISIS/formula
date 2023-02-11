//using System;
//using System.Linq;
//using System.Collections.ObjectModel;
//using System.Collections.Generic;
//using System.IO;
using System.Runtime.InteropServices;

//using Avalonia;

using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
//using Avalonia.Input;
//using Avalonia.Platform.Storage;
//using Avalonia.VisualTree;

using Debugger.ViewModels;
using Debugger.Views;
using ReactiveUI;

namespace Debugger.Windows;

/*internal class MainWindowViewModel : ReactiveObject
{
    private MainWindow win;
    private IStorageFolder? lastSelectedDirectory;
    private Node? root;
    private FormulaProgram formulaProgram;

    public MainWindowViewModel(MainWindow mainWin)
    {
        win = mainWin;

        formulaProgram = new FormulaProgram();
        
        Items = new ObservableCollection<Node>();
        ItemsSource = new ObservableCollection<Node>();
        SelectedItems = new ObservableCollection<Node>();
        
        var fileManagerPanel = win.Get<TreeView>("FileManagerPanel");
        fileManagerPanel.DoubleTapped += FileDoubleTap;
        
        var cmdInput = win.Get<AutoCompleteBox>("CommandInput");
        cmdInput.KeyDown += InputKey;
    }

    private void InputKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            RunCmd();
        }
    }
    
    private void FileDoubleTap(object? sender, TappedEventArgs e)
    {
        LoadFormulaFileCmd();
    }

    public ObservableCollection<Node> Items { get; }
    public ObservableCollection<Node> ItemsSource { get; set;  }
    public ObservableCollection<Node> SelectedItems { get; }

    public void LoadFormulaFileCmd()
    {
        if (SelectedItems.Count > 0)
        {
            Uri? uri = null;
            if (lastSelectedDirectory != null &&
                lastSelectedDirectory.TryGetUri(out uri))
            {
                if(!formulaProgram.ExecuteCommand("unload *"))
                {
                    Console.WriteLine("Command Failed");
                    return;
                }
                
                formulaProgram.ClearConsoleOutput();
                
                if(!formulaProgram.ExecuteCommand("load " + Path.Join(uri.AbsolutePath, SelectedItems[0].Header)))
                {
                    Console.WriteLine("Command Failed");
                    return;
                }
                
                var txt = win.Get<TextBlock>("ConsoleOutput").Text;
                if(txt == null || txt.Length <= 0)
                {
                    win.Get<TextBlock>("ConsoleOutput").Text += "[]> ";
                }
                
                win.Get<TextBlock>("ConsoleOutput").Text += "load " + Path.Join(uri.AbsolutePath, SelectedItems[0].Header);
                win.Get<TextBlock>("ConsoleOutput").Text += "\n";
                win.Get<TextBlock>("ConsoleOutput").Text += formulaProgram.GetConsoleOutput();
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
        if (formulaFile != null)
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
                    return;
                }
                
                var txt = win.Get<TextBlock>("ConsoleOutput").Text;
                if(txt == null || txt.Length <= 0)
                {
                    win.Get<TextBlock>("ConsoleOutput").Text += "[]> ";
                }
 
                win.Get<TextBlock>("ConsoleOutput").Text += "load " + Path.Join(uri.AbsolutePath, formulaFile.Name); 
                win.Get<TextBlock>("ConsoleOutput").Text += "\n";
                win.Get<TextBlock>("ConsoleOutput").Text += formulaProgram.GetConsoleOutput();
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

            if (root.Children.Count > 0)
            {
                Items.Clear();
                ItemsSource.Clear();
                
                Items.Add(root);
                ItemsSource = root.Children;
            }
        }
    }
}*/

public partial class MainWindow : Window
{    
    private readonly FormulaProgram formulaProgram;
    public MainWindow()
    {
        formulaProgram = new FormulaProgram();
        
        InitializeComponent();
        
        DataContext = new MainWindowViewModel();

        var bar = this.Get<DockPanel>("TopBar");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            bar.Margin = new Thickness(0.0,25.0,0.0,20.0);
        }
        else
        {
            bar.Margin = new Thickness(0.0,0.0,0.0,20.0);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        var tbv = this.Get<ToolbarView>("TopToolbarView");
        SetViewWindow(tbv, new ToolbarViewModel(this));
        
        var civ = this.Get<CommandConsoleView>("CommandInputView");
        SetViewWindow(civ, new CommandConsoleViewModel(this, formulaProgram));
    }

    private void SetViewWindow(UserControl uc, ReactiveObject obj)
    {
        uc.DataContext = obj;
    }
}