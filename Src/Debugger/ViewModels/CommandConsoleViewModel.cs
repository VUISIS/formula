using Avalonia.Controls;
using Avalonia.Input;
using Debugger.ViewModels.Types;
using ReactiveUI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using Debugger.ViewModels.Helpers;
using Debugger.Views;
using Debugger.Windows;
using Microsoft.Formula.Common.Terms;

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
                if (!formulaProgram.ExecuteCommand(commandInput.Text))
                {
                    System.Console.WriteLine("Command Failed");
                    return;
                }

                if (commandOutput.Text == null ||
                    commandOutput.Text.Length <= 0)
                {
                    commandOutput.Text += "[]> ";
                }

                commandOutput.Text += commandInput.Text;

                var reg = new Regex(@"^[a-z]+");
                MatchCollection matches = reg.Matches(commandInput.Text);
                if (Utils.InputCommands.Contains(matches[0].Value))
                {
                    commandOutput.Text += "\n";
                }

                var output = formulaProgram.GetConsoleOutput();
                var solveResult = formulaProgram.FormulaPublisher.WaitForCompletion();
                if ((matches[0].Value.Equals("solve") ||
                    matches[0].Value.Equals("sl")) &&
                    inferenceRulesViewModel != null)
                {
                    if (solveResult != null)
                    {
                        inferenceRulesViewModel.Items.Clear();

                        var constraintTerms = formulaProgram.FormulaPublisher.GetConstraintTerms();
                        foreach (var term in constraintTerms)
                        {
                            var sym = term.Symbol as UserSymbol;
                            if (sym != null &&
                                sym.IsAutoGen)
                            {
                                var cancelToken = new CancellationToken();
                                var tw = new StringWriter();
                                foreach (var n in sym.Definitions)
                                {
                                    n.Print(tw, cancelToken, formulaProgram.GetParameters());
                                    Console.WriteLine("Defs");
                                    Console.WriteLine(tw.ToString());
                                }
                                tw.Flush();
                                term.PrintTerm(tw, cancelToken, formulaProgram.GetParameters());
                                Console.WriteLine("Node");
                                Console.WriteLine(tw.ToString());
                                var node = new Node(sym.PrintableName);
                                inferenceRulesViewModel.Items.Add(node);
                            }
                        }

                        var withoutEnd = output.Replace("[]> ", "");
                        commandOutput.Text += withoutEnd;
                        commandOutput.Text += "Solve result task completed.";
                        commandOutput.Text += "\n";
                        commandOutput.Text += formulaProgram.FormulaPublisher.GetResultTimeString();
                        commandOutput.Text += "\n\n";
                        commandOutput.Text += "[]>";
                        return;
                    }

                    var removeCursor = output.Replace("[]>", "");
                    commandOutput.Text += removeCursor;
                    commandOutput.Text += "Solve result task timed out.";
                    commandOutput.Text += "\n";
                    commandOutput.Text += "30s";
                    commandOutput.Text += "\n\n";
                    commandOutput.Text += "[]>";
                    return;
                }

                commandOutput.Text += output;
            }
        }
    }
}