using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;

using System;
using System.IO;
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
    
    public CommandConsoleViewModel(MainWindow win, 
                                   FormulaProgram program, 
                                   TermsConstraintsViewModel termsConstraintsModel,
                                   InferenceRulesViewModel inferenceModel)
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

        termsConstraintsViewModel = termsConstraintsModel;
        inferenceRulesViewModel = inferenceModel;
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
                
                if (!formulaProgram.ExecuteCommand(commandInput.Text))
                {
                    commandOutput.Text += "ERROR: Command failed.";
                    return;
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
                    inferenceRulesViewModel != null &&
                    termsConstraintsViewModel != null)
                {
                    if (solveResult != null)
                    {
                        inferenceRulesViewModel.Items.Clear();

                        var rules = formulaProgram.FormulaPublisher.GetLeastFixedPointTerms();
                        var firstTermSet = false;
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
                            
                            var constraints = formulaProgram.FormulaPublisher.GetLeastFixedPointConstraints();
                            foreach (var val in constraints[keyId])
                            {
                                foreach (var data in val.Value)
                                {
                                    var n = new Node(data);
                                    inferenceRulesViewModel.Items.Add(n);
                                    
                                    if (!firstTermSet)
                                    {
                                        switch (val.Key)
                                        {
                                            case ConstraintKind.Direct:
                                                var dirn = new Node(data);
                                                termsConstraintsViewModel.DirectConstraintsItems.Add(dirn);
                                                break;
                                            case ConstraintKind.Positive:
                                                var posn = new Node(data);
                                                termsConstraintsViewModel.PosConstraintsItems.Add(posn);
                                                break;
                                            case ConstraintKind.Negative:
                                                var negn = new Node(data);
                                                termsConstraintsViewModel.NegConstraintsItems.Add(negn);
                                                break;
                                        }

                                        firstTermSet = true;
                                    }
                                }
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