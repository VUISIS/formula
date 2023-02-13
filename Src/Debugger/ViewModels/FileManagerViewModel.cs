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
    private MainWindow win;
    private FormulaProgram formulaProgram;
    private TextBlock consoleOutput;
    
    public FileManagerViewModel(MainWindow mwin, FormulaProgram program)
    {
        win = mwin;
        formulaProgram = program;

        Items = new ObservableCollection<Node>();
        ItemsSource = new ObservableCollection<Node>();
        SelectedItems = new ObservableCollection<Node>();

        consoleOutput = win.Get<CommandConsoleView>("CommandInputView")
                           .Get<TextBlock>("ConsoleOutput");
    }

    public ObservableCollection<Node> Items { get; }
    public ObservableCollection<Node> ItemsSource { get; set;  }
    public ObservableCollection<Node> SelectedItems { get; }

    public void LoadFormulaFileCmd()
    {
        if (SelectedItems.Count > 0)
        {
            Uri? uri = null;
            if (Utils.LastDirectory != null &&
                Utils.LastDirectory.TryGetUri(out uri))
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
            
                var txt = consoleOutput.Text;
                if(txt == null || txt.Length <= 0)
                {
                    consoleOutput.Text += "[]> ";
                }
            
                consoleOutput.Text += "load " + Path.Join(uri.AbsolutePath, SelectedItems[0].Header);
                consoleOutput.Text += "\n";
                consoleOutput.Text += formulaProgram.GetConsoleOutput();
            }
        }
    }

}