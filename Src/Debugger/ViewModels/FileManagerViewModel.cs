using ReactiveUI;
using Avalonia.Input;
using Avalonia.Controls;

using System;
using System.IO;
using System.Collections.ObjectModel;

using Debugger.Windows;
using Debugger.ViewModels.Types;
using Debugger.ViewModels.Helpers;
using Debugger.Views;

namespace Debugger.ViewModels;


internal class FileManagerViewModel : ReactiveObject
{
    private readonly MainWindow? mainWindow;
    private readonly FormulaProgram? formulaProgram;
    private readonly TextBlock? consoleOutput;
    private readonly TextBlock? fileOutput;
    
    public FileManagerViewModel(MainWindow win, FormulaProgram program)
    {
        mainWindow = win;
        formulaProgram = program;

        Items = new ObservableCollection<Node>();
        ItemsSource = new ObservableCollection<Node>();
        SelectedItems = new ObservableCollection<Node>();
        
        fileOutput = mainWindow.Get<Domain4MLView>("DomainView")
                              .Get<TextBlock>("FileOutput");

        consoleOutput = mainWindow.Get<CommandConsoleView>("CommandInputView")
                                  .Get<TextBlock>("ConsoleOutput");
    }

    public ObservableCollection<Node> Items { get; }
    public ObservableCollection<Node> ItemsSource { get; set;  }
    public ObservableCollection<Node> SelectedItems { get; }

    public void LoadFormulaFileCmd()
    {
        if (formulaProgram != null &&
            consoleOutput != null &&
            SelectedItems.Count > 0)
        {
            Uri? uri = null;
            if (Utils.LastDirectory != null &&
                Utils.LastDirectory.TryGetUri(out uri))
            {
                var txt = consoleOutput.Text;
                if(txt == null || txt.Length <= 0)
                {
                    consoleOutput.Text += "[]> ";
                }
                
                if(!formulaProgram.ExecuteCommand("unload *"))
                {
                    consoleOutput.Text += "ERROR: Command failed.";
                    return;
                }
            
                formulaProgram.ClearConsoleOutput();

                var file = Path.Join(uri.AbsolutePath, SelectedItems[0].Header);
            
                if(!formulaProgram.ExecuteCommand("load " + file))
                {
                    consoleOutput.Text += "ERROR: Command failed.";
                    return;
                }

                if (fileOutput != null)
                {
                    fileOutput.Text = Utils.OpenFileText(file);
                }

                consoleOutput.Text += "load " + file;
                consoleOutput.Text += "\n";
                consoleOutput.Text += formulaProgram.GetConsoleOutput();
            }
        }
    }

}