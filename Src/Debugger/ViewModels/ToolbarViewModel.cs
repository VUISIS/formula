using Avalonia.Controls;
using ReactiveUI;

using System;
using System.IO;

using Debugger.ViewModels.Helpers;
using Debugger.Views;
using Debugger.Windows;
using Debugger.ViewModels.Types;

namespace Debugger.ViewModels;

internal class ToolbarViewModel : ReactiveObject
{
    private FileManagerViewModel? fileManagerModel;
    private MainWindow? mainWindow;
    private FormulaProgram? formulaProgram;
    private TextBlock? consoleOutput;

    public ToolbarViewModel(MainWindow win, FormulaProgram program)
    {
        mainWindow = win;

        formulaProgram = program;

        consoleOutput = mainWindow.Get<CommandConsoleView>("CommandInputView")
                                  .Get<TextBlock>("ConsoleOutput");
        Console.WriteLine(consoleOutput);
        fileManagerModel = mainWindow.Get<FileManagerView>("FileTreeView").DataContext as FileManagerViewModel;
        Console.WriteLine(fileManagerModel);
    }
    
    public async void OpenCmd()
    {
        if (mainWindow != null &&
            fileManagerModel != null &&
            formulaProgram != null &&
            consoleOutput != null)
        {
            var file = await Utils.GetFile(mainWindow);
            if(file != null)
            {
                Utils.LastDirectory = await file.GetParentAsync();
                var root = new Node(file.Name);
            
                fileManagerModel.ItemsSource.Clear();
                fileManagerModel.Items.Clear();
                fileManagerModel.Items.Add(root);
            
                Uri? uri = null;
                if (Utils.LastDirectory != null &&
                    Utils.LastDirectory.TryGetUri(out uri))
                {
                    if(!formulaProgram.ExecuteCommand("load " + Path.Join(uri.AbsolutePath, file.Name)))
                    {
                        Console.WriteLine("Command Failed");
                        return;
                    }
                
                    var txt = consoleOutput.Text;
                    if(txt == null || txt.Length <= 0)
                    {
                        consoleOutput.Text += "[]> ";
                    }
 
                    consoleOutput.Text += "load " + Path.Join(uri.AbsolutePath, file.Name); 
                    consoleOutput.Text += "\n";
                    consoleOutput.Text += formulaProgram.GetConsoleOutput();
                }
            }
        }
    }
    
    public async void OpenFolderCmd()
    {
        Console.WriteLine(mainWindow);
        Console.WriteLine(fileManagerModel);
        if (mainWindow != null &&
            fileManagerModel != null)
        {
            var folder = await Utils.GetFolder(mainWindow);
            if(folder != null)
            {
                var root = new Node(folder.Name);
                var items = await folder.GetItemsAsync();
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
                    fileManagerModel.Items.Clear();
                    fileManagerModel.ItemsSource.Clear();
                
                    fileManagerModel.Items.Add(root);
                    fileManagerModel.ItemsSource = root.Children;
                }
            }
        }
    }
    
    public void ExitCmd() 
    {
        if (mainWindow != null)
        {
            mainWindow.Close();
        }
    }
}