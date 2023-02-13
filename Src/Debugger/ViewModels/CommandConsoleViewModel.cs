using System;
using Avalonia.Controls;
using Avalonia.Input;
using Debugger.ViewModels.Types;
using ReactiveUI;

using Debugger.Views;
using Debugger.Windows;

namespace Debugger.ViewModels;

internal class CommandConsoleViewModel : ReactiveObject
{
    private readonly MainWindow? mainWindow;
    private readonly FormulaProgram? formulaProgram;
    private readonly AutoCompleteBox? commandInput;
    private readonly TextBlock? commandOutput;
    private readonly InferenceRulesViewModel? inferenceRulesViewModel;
    
    public CommandConsoleViewModel(MainWindow win, FormulaProgram program)
    {
        mainWindow = win;
        formulaProgram = program;

        var commandInputView = mainWindow.Get<CommandConsoleView>("CommandInputView");
        commandInput = commandInputView.Get<AutoCompleteBox>("CommandInput");
        commandOutput = commandInputView.Get<TextBlock>("ConsoleOutput");

        if (commandInput != null)
        {
            commandInput.KeyDown += InputKey;
        }
        
        inferenceRulesViewModel = mainWindow.Get<InferenceRulesView>("SolverRulesView").DataContext as InferenceRulesViewModel;
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
            formulaProgram != null &&
            commandInput != null &&
            commandOutput != null)
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
                    commandInput.Text.StartsWith("ul ") ||
                    commandInput.Text.StartsWith("help") ||
                    commandInput.Text.StartsWith("h") ||
                    commandInput.Text.StartsWith("solve ") ||
                    commandInput.Text.StartsWith("sl "))
                {
                    commandOutput.Text += "\n";
                }
                commandOutput.Text += formulaProgram.GetConsoleOutput();

                if ((commandInput.Text.StartsWith("solve ") ||
                    commandInput.Text.StartsWith("sl ")) &&
                    inferenceRulesViewModel != null)
                {
                    inferenceRulesViewModel.Items.Clear();

                    var pc = formulaProgram.GetPositiveConstraints();
                    var nc = formulaProgram.GetNegativeConstraints();
                    
                    Console.WriteLine("Pos Constraints");
                    Console.WriteLine(nc);
                    
                    Console.WriteLine("Neg Constraints");
                    Console.WriteLine(nc);
                    
                    foreach (var term in pc)
                    {
                        var node = new Node(term.ToString());
                        inferenceRulesViewModel.Items.Add(node);
                    }
                    
                    foreach (var term in nc)
                    {
                        var node = new Node(term.ToString());
                        inferenceRulesViewModel.Items.Add(node);
                    }
                }
            }
        }
    }
}