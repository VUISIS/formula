using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using Debugger.ViewModels.Helpers;
using Debugger.Views;
using Debugger.Windows;
using Debugger.ViewModels.Types;
using Microsoft.Formula.API;
using Microsoft.Formula.Common.Terms;

namespace Debugger.ViewModels;

internal class CommandConsoleViewModel : ReactiveObject
{
    private readonly MainWindow? mainWindow;
    private readonly FormulaProgram? formulaProgram;
    private readonly AutoCompleteBox? commandInput;
    private readonly TextBlock? commandOutput;
    private readonly InferenceRulesViewModel? inferenceRulesViewModel;
    private readonly TermsConstraintsViewModel? termsConstraintsViewModel;
    
    public CommandConsoleViewModel(MainWindow win, FormulaProgram program)
    {
        mainWindow = win;
        formulaProgram = program;

        var commandInputView = mainWindow.Get<CommandConsoleView>("CommandInputView");
        commandInput = commandInputView.Get<AutoCompleteBox>("CommandInput");
        if (commandInput != null)
        {
            commandInput.KeyDown += InputKey;
        }
        
        commandOutput = commandInputView.Get<TextBlock>("ConsoleOutput");

        termsConstraintsViewModel = mainWindow.Get<TermsConstraintsView>("TermsAndConstraintsView").DataContext as TermsConstraintsViewModel;
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
                if (commandOutput.Text == null ||
                    commandOutput.Text.Length <= 0)
                {
                    commandOutput.Text += "[]> ";
                }

                if (commandInput.Text.StartsWith("load") ||
                    commandInput.Text.StartsWith("l"))
                {
                    if (!formulaProgram.ExecuteCommand("unload *"))
                    {
                        commandOutput.Text += "ERROR: Command failed.";
                        return;
                    }

                    formulaProgram.ClearConsoleOutput();
                }

                if (!formulaProgram.ExecuteCommand(commandInput.Text))
                {
                    commandOutput.Text += "ERROR: Command failed.";
                    return;
                }

                commandOutput.Text += commandInput.Text;

                foreach (var cmd in Utils.InputCommands)
                {
                    if (commandInput.Text.StartsWith(cmd))
                    {
                        commandOutput.Text += "\n";
                    }
                }

                var output = formulaProgram.GetConsoleOutput();
                var solveResult = formulaProgram.FormulaPublisher.WaitForCompletion();
                if ((commandInput.Text.StartsWith("solve") ||
                     commandInput.Text.StartsWith("sl")) &&
                    inferenceRulesViewModel != null &&
                    termsConstraintsViewModel != null)
                {
                    if (solveResult != null)
                    {
                        inferenceRulesViewModel.Items.Clear();

                        var coreRules = formulaProgram.FormulaPublisher.GetCoreRules();
                        foreach (var rule in coreRules)
                        {
                            var rn = new Node(rule);
                            inferenceRulesViewModel.Items.Add(rn);
                        }

                        var rules = formulaProgram.FormulaPublisher.GetLeastFixedPointTerms();
                        var constraints = formulaProgram.FormulaPublisher.GetLeastFixedPointConstraints();
                        var flag = true;
                        foreach (var term in rules)
                        {
                            var tw = new StringWriter();
                            var cancelToken = new CancellationToken();
                            var envParams = new EnvParams();
                            term.PrintTerm(tw,cancelToken,envParams);
                            var keyId = 0;
                            if (term.Symbol != null)
                            {
                                keyId = term.Symbol.Id;
                                var node = new Node(tw.ToString(), keyId);
                                termsConstraintsViewModel.CurrentTermItems.Add(node);
                            }

                            if (flag)
                            {
                                var directConsts = constraints[keyId][ConstraintKind.Direct];
                                foreach (var v in directConsts)
                                {
                                    var dirn = new Node(v);
                                    termsConstraintsViewModel.DirectConstraintsItems.Add(dirn);
                                }
                            
                                var posConsts = constraints[keyId][ConstraintKind.Positive];
                                foreach (var v in posConsts)
                                {
                                    var posn = new Node(v);
                                    termsConstraintsViewModel.PosConstraintsItems.Add(posn);
                                }
                            
                                var negConsts = constraints[keyId][ConstraintKind.Negative];
                                foreach (var v in negConsts)
                                {
                                    var negn = new Node(v);
                                    termsConstraintsViewModel.NegConstraintsItems.Add(negn);
                                }
                            
                                var flatConsts = constraints[keyId][ConstraintKind.Flattened];
                                foreach (var v in flatConsts)
                                {
                                    var flatn = new Node(v);
                                    termsConstraintsViewModel.FlatConstraintsItems.Add(flatn);
                                }

                                flag = false;
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