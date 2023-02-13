using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;

using Debugger.Views;
using Debugger.Windows;

namespace Debugger.ViewModels;

internal class CommandConsoleViewModel : ReactiveObject
{
    private readonly MainWindow? mainWindow;
    private readonly FormulaProgram? formulaProgram;
    private readonly AutoCompleteBox commandInput;
    private readonly TextBlock commandOutput;
    public CommandConsoleViewModel(MainWindow win, FormulaProgram program)
    {
        mainWindow = win;
        formulaProgram = program;

        var commandInputView = mainWindow.Get<CommandConsoleView>("CommandInputView");
        commandInput = commandInputView.Get<AutoCompleteBox>("CommandInput");
        commandOutput = commandInputView.Get<TextBlock>("ConsoleOutput");
        commandInput.KeyDown += InputKey;
    }

    private void InputKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            RunCmd();
        }
    }
    
    public void RunCmd()
    {
        if (mainWindow != null &&
            formulaProgram != null)
        {
            if (commandInput.Text != null &&
                commandInput.Text.Length > 0)
            {
                if(!formulaProgram.ExecuteCommand(commandInput.Text))
                {
                    System.Console.WriteLine("Command Failed");
                    return;
                }
            
                var txt = commandOutput.Text;
                if(txt == null || txt.Length <= 0)
                {
                    commandOutput.Text += "[]> ";
                }

                commandOutput.Text += commandInput.Text;
                if (commandInput.Text.StartsWith("unload ") ||
                    commandInput.Text.StartsWith("load ") ||
                    commandInput.Text.StartsWith("l ") ||
                    commandInput.Text.StartsWith("ul "))
                {
                    commandOutput.Text += "\n";
                }
                commandOutput.Text += formulaProgram.GetConsoleOutput();
            }
        }
    }
}