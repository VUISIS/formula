using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ReactiveUI;

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Debugger.ViewModels.Helpers;
using Debugger.Views;
using Debugger.Windows;
using Material.Icons.Avalonia;
using Microsoft.Formula.API;
using Microsoft.Formula.Common;
using Node = Debugger.ViewModels.Types.Node;

namespace Debugger.ViewModels;

internal class CommandConsoleViewModel : ReactiveObject
{
    private readonly MainWindow? mainWindow;
    private readonly FormulaProgram formulaProgram;
    private readonly AutoCompleteBox? commandInput;
    private readonly TextBlock? commandOutput;
    private readonly InferenceRulesViewModel? inferenceRulesViewModel;
    private readonly CurrentTermsViewModel? termsViewModel;
    private readonly ConstraintsViewModel? constraintsViewModel;
    private readonly SolverViewModel? solverViewModel;
    private readonly TextBlock? fileOutput;
    private readonly TextBlock? solutionOut;
    private List<Task> tasks = new List<Task>();
    private readonly Button? startButton;
    private readonly Button? initButton;
    private readonly Button? executeButton;
    private readonly Button? genButton;
    private readonly Button? solutionButton;
    private static int solutionNum = 0;

    public CommandConsoleViewModel(MainWindow win, FormulaProgram program)
    {
        mainWindow = win;
        formulaProgram = program;

        commandInput = mainWindow.Get<AutoCompleteBox>("CommandInput");
        if (commandInput != null)
        {
            commandInput.KeyDown += InputKey;
        }
        
        fileOutput = mainWindow.Get<Domain4MLView>("DomainView")
                              .Get<TextBlock>("FileOutput");
        
        var commandInputView = mainWindow.Get<CommandConsoleView>("CommandInputView");
        commandOutput = commandInputView.Get<TextBlock>("ConsoleOutput");

        termsViewModel = mainWindow.Get<CurrentTermsView>("TermsView").DataContext as CurrentTermsViewModel;
        constraintsViewModel = mainWindow.Get<ConstraintsView>("ConstrView").DataContext as ConstraintsViewModel;
        inferenceRulesViewModel = mainWindow.Get<InferenceRulesView>("SolverRulesView").DataContext as InferenceRulesViewModel;
        solverViewModel = mainWindow.Get<SolverView>("SolverCommandView").DataContext as SolverViewModel;
        solutionOut = mainWindow.Get<SolverView>("SolverCommandView").Get<TextBlock>("SolutionOutput");
        startButton = mainWindow.Get<SolverView>("SolverCommandView").Get<Button>("SolveButton");
        startButton.Click += StartSolve;
        initButton = mainWindow.Get<SolverView>("SolverCommandView").Get<Button>("InitButton");
        initButton.Click += InitSolve;
        executeButton = mainWindow.Get<SolverView>("SolverCommandView").Get<Button>("ExecuteButton");
        executeButton.Click += ExecuteSolve;
        genButton = mainWindow.Get<SolverView>("SolverCommandView").Get<Button>("GenSolButton");
        genButton.Click += GenerateSolve;
        solutionButton = mainWindow.Get<SolverView>("SolverCommandView").Get<Button>("SolutionButton");
        solutionButton.Click += GetSolution;
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
            commandInput != null &&
            commandOutput != null)
        {
            if (commandInput.Text != null &&
                commandInput.Text.Length > 0)
            {
                if (commandInput.Text.StartsWith("load"))
                {
                    if (mainWindow != null)
                    {
                        mainWindow.Get<Ellipse>("Iterator").IsVisible = true;
                    }
                    
                    var cmdTask = new Task(() => LoadCommand(commandInput.Text));
                    cmdTask.Start();
                    tasks.Add(cmdTask);
                }
                else if (commandInput.Text.StartsWith("solve"))
                {
                    ClearAll(false);

                    if (initButton != null)
                    {
                        initButton.IsEnabled = true;
                    }
                    
                    formulaProgram.ClearConsoleOutput();
                    
                    if (mainWindow != null)
                    {
                        mainWindow.Get<Ellipse>("Iterator").IsVisible = true;
                    }
            
                    if (commandOutput != null)
                    {
                        if (commandOutput.Text == null ||
                            commandOutput.Text.Length <= 0)
                        {
                            commandOutput.Text += "[]> ";
                        }
                    }

                    if (!formulaProgram.ExecuteCommand("tunload *"))
                    {
                        if (commandOutput != null)
                        {
                            commandOutput.Text += "ERROR: tunload * failed to execute.";
                        }
                    }
                        
                    formulaProgram.ClearConsoleOutput();

                    var commandExecuteTask = new Task(() => ExecuteCommand(commandInput.Text));
                    commandExecuteTask.Start();
                    tasks.Add(commandExecuteTask);
                }
                else
                {
                    if (commandInput.Text.StartsWith("reload") ||
                        commandInput.Text.StartsWith("rl"))
                    {
                        ReloadCommand();
                        commandOutput.Text += "\n";
                    }
                    
                    if (mainWindow != null)
                    {
                        mainWindow.Get<Ellipse>("Iterator").IsVisible = true;
                    }
            
                    if (commandOutput != null)
                    {
                        if (commandOutput.Text == null ||
                            commandOutput.Text.Length <= 0)
                        {
                            commandOutput.Text += "[]> ";
                        }
                    }

                    var commandExecuteTask = new Task(() => ExecuteCommand(commandInput.Text));
                    commandExecuteTask.Start();
                    tasks.Add(commandExecuteTask);
                }
            }
        }
    }

    private void ReloadCommand()
    {
        ClearAll(true);
        SetConstraintPanelEnabled(false);
        
        var fileOut = Utils.OpenFileText(Utils.LoadedFile);
        if (fileOutput != null)
        {
            fileOutput.Text = fileOut;
        }
    }

    private void LoadCommand(string input)
    {
        if (!formulaProgram.ExecuteCommand("unload *"))
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (commandOutput != null)
                {
                    commandOutput.Text += "ERROR: " + input + " failed to execute.";
                }
            }, DispatcherPriority.Background);
            return;
        }

        formulaProgram.ClearConsoleOutput();

        var outP = "";
        if (input.StartsWith("load"))
        {
            outP = input.Replace("load ", "");
        }
        else if (input.StartsWith("l"))
        {
            outP = input.Replace("l ", "");
        }

        Utils.LoadedFile = outP;

        var fileOut = Utils.OpenFileText(outP);
        if (!formulaProgram.ExecuteCommand(input))
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (commandOutput != null)
                {
                    commandOutput.Text += "ERROR: " + input + " failed to execute.";
                }
            }, DispatcherPriority.Background);
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (inferenceRulesViewModel != null &&
                termsViewModel != null &&
                constraintsViewModel != null)
            {
                constraintsViewModel.ClearAll();
                termsViewModel.ClearAll();
                inferenceRulesViewModel.ClearAll();
                SetConstraintPanelEnabled(false);
                ClearAll(true);
            }
            
            if (fileOutput != null)
            {
                fileOutput.Text = fileOut;
            }
            
            if (commandOutput != null)
            {
                commandOutput.Text += input;
                commandOutput.Text += "\n";
                commandOutput.Text += formulaProgram.GetConsoleOutput();
            }
            
            if (mainWindow != null)
            {
                mainWindow.Get<Ellipse>("Iterator").IsVisible = false;
            }
        }, DispatcherPriority.Background);
    }

    private void ExecuteCommand(string input)
    {
        if (!formulaProgram.ExecuteCommand(input))
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (commandOutput != null)
                {
                    commandOutput.Text += "ERROR: " + input + " failed to execute.";
                }
            }, DispatcherPriority.Background);
            return;
        }
        
        Dispatcher.UIThread.Post(() =>
        {
            if (commandOutput != null)
            {
                commandOutput.Text += input;

                foreach (var cmd in Utils.InputCommands)
                {
                    if (input != null &&
                        input.StartsWith(cmd))
                    {
                        commandOutput.Text += "\n";
                    }
                }
                
                if (input != null && 
                    input.EndsWith("debugger"))
                {
                    if (solutionButton != null &&
                        genButton != null)
                    {
                        solutionButton.IsEnabled = false;
                        genButton.IsEnabled = true;
                    }
                    
                    if (solutionOut != null)
                    {
                        solutionOut.Text = formulaProgram.FormulaPublisher.GetExtractOutput();
                    }
                }
                else
                {
                    commandOutput.Text += formulaProgram.GetConsoleOutput();
                }
            }

            if (mainWindow != null)
            {
                mainWindow.Get<Ellipse>("Iterator").IsVisible = false;
            }
        }, DispatcherPriority.Background);
    }

    private void SolverInit()
    {
        var solveResult = formulaProgram.FormulaPublisher.GetSolverResult();
        solveResult.Init();

        var coreRules = formulaProgram.FormulaPublisher.GetCoreRules();
        var varFacts = formulaProgram.FormulaPublisher.GetVarFacts();
        var rules = formulaProgram.FormulaPublisher.GetCurrentTerms();
        var posConstraints = formulaProgram.FormulaPublisher.GetPosConstraints();
        var negConstraints = formulaProgram.FormulaPublisher.GetNegConstraints();
        var dirConstraints = formulaProgram.FormulaPublisher.GetDirConstraints();
        var flatConstraints = formulaProgram.FormulaPublisher.GetFlatConstraints();
        Dispatcher.UIThread.Post(() =>
        {
            if (commandOutput != null &&
                termsViewModel != null &&
                constraintsViewModel != null &&
                inferenceRulesViewModel != null &&
                solverViewModel != null)
            {
                inferenceRulesViewModel.ClearAll();

                foreach (var rulePair in coreRules)
                {
                    foreach (var rule in rulePair.Value)
                    {
                        var rn = new Node(rule);
                        inferenceRulesViewModel.Items.Add(rn);
                    }
                }
                
                if (varFacts.Count > 0)
                {
                    solverViewModel.ClearAll();
                    
                    foreach (var varFact in varFacts)
                    {
                        var n = new Node(varFact.Value, varFact.Key);
                        solverViewModel.VariableItems.Add(n);
                    }
                    
                    SetConstraintPanelEnabled(true);
                }

                termsViewModel.ClearAll();
                constraintsViewModel.ClearAll();

                var flag = true;
                foreach (var term in rules)
                {
                    var node = new Node(term.Value, term.Key);
                    termsViewModel.CurrentTermItems.Add(node);

                    if (flag)
                    {
                        if (dirConstraints.ContainsKey(term.Key))
                        {
                            foreach (var v in dirConstraints[term.Key])
                            {
                                var dirn = new Node(v, term.Key);
                                constraintsViewModel.DirectConstraintsItems.Add(dirn);
                            }
                        }

                        if (posConstraints.ContainsKey(term.Key))
                        {
                            foreach (var v in posConstraints[term.Key])
                            {
                                var posn = new Node(v, term.Key);
                                constraintsViewModel.PosConstraintsItems.Add(posn);
                            }
                        }

                        if (negConstraints.ContainsKey(term.Key))
                        {
                            foreach (var v in negConstraints[term.Key])
                            {
                                var negn = new Node(v, term.Key);
                                constraintsViewModel.NegConstraintsItems.Add(negn);
                            }
                        }

                        if (flatConstraints.ContainsKey(term.Key))
                        {
                            foreach (var v in flatConstraints[term.Key])
                            {
                                var flatn = new Node(v, term.Key);
                                constraintsViewModel.FlatConstraintsItems.Add(flatn);
                            }
                        }
                    }
                }
                
                if (executeButton != null &&
                    initButton != null)
                {
                    initButton.IsEnabled = false;
                    executeButton.IsEnabled = true;
                }
 
                if (commandOutput != null &&
                    commandInput != null)
                {
                    commandOutput.Text += " Use buttons in solver view to init, execute, and start the solver.";
                    commandOutput.Text += "\n\n";
                }

                if (commandOutput != null)
                {
                    commandOutput.Text += "Solve initialize task completed.";
                    commandOutput.Text += "\n\n";
                    commandOutput.Text += "[]>";
                }
            }
            
            if (mainWindow != null)
            {
                mainWindow.Get<Ellipse>("Iterator").IsVisible = false;
            }
        }, DispatcherPriority.Background);
    }

    private void SolverExecute()
    {
        var solveResult = formulaProgram.FormulaPublisher.GetSolverResult();
        solveResult.Execute();
            
        var rules = formulaProgram.FormulaPublisher.GetCurrentTerms();
        var posConstraints = formulaProgram.FormulaPublisher.GetPosConstraints();
        var negConstraints = formulaProgram.FormulaPublisher.GetNegConstraints();
        var dirConstraints = formulaProgram.FormulaPublisher.GetDirConstraints();
        var flatConstraints = formulaProgram.FormulaPublisher.GetFlatConstraints();
        Dispatcher.UIThread.Post(() =>
        {
            if (commandOutput != null &&
                termsViewModel != null &&
                constraintsViewModel != null)
            {
                termsViewModel.ClearAll();
                constraintsViewModel.ClearAll();

                var flag = true;
                foreach (var term in rules)
                {
                    var node = new Node(term.Value, term.Key);
                    termsViewModel.CurrentTermItems.Add(node);

                    if (flag)
                    {
                        if (dirConstraints.ContainsKey(term.Key))
                        {
                            foreach (var v in dirConstraints[term.Key])
                            {
                                var dirn = new Node(v, term.Key);
                                constraintsViewModel.DirectConstraintsItems.Add(dirn);
                            }
                        }

                        if (posConstraints.ContainsKey(term.Key))
                        {
                            foreach (var v in posConstraints[term.Key])
                            {
                                var posn = new Node(v, term.Key);
                                constraintsViewModel.PosConstraintsItems.Add(posn);
                            }
                        }

                        if (negConstraints.ContainsKey(term.Key))
                        {
                            foreach (var v in negConstraints[term.Key])
                            {
                                var negn = new Node(v, term.Key);
                                constraintsViewModel.NegConstraintsItems.Add(negn);
                            }
                        }


                        if (flatConstraints.ContainsKey(term.Key))
                        {
                            foreach (var v in flatConstraints[term.Key])
                            {
                                var flatn = new Node(v, term.Key);
                                constraintsViewModel.FlatConstraintsItems.Add(flatn);
                            }
                        }

                        flag = false;
                    }
                }
                
                if (executeButton != null &&
                    startButton != null)
                {
                    startButton.IsEnabled = true;
                    executeButton.IsEnabled = false;
                }

                if (commandOutput != null)
                {
                    commandOutput.Text += " Solve execution task completed.";
                    commandOutput.Text += "\n\n";
                    commandOutput.Text += "[]>";
                }
            }
            
            if (mainWindow != null)
            {
                mainWindow.Get<Ellipse>("Iterator").IsVisible = false;
            }
        }, DispatcherPriority.Background);
    }

    private SolveResult SolverStart()
    {
        var startTime = DateTime.Now;

        var solveResult = formulaProgram.FormulaPublisher.GetSolverResult();
        solveResult.Start();
        
        Dispatcher.UIThread.Post(() =>
        {
            if (commandOutput != null &&
                solverViewModel != null)
            {
                commandOutput.Text += " Solve start task completed.";
                commandOutput.Text += "\n";
                commandOutput.Text += "Solveable: " + solveResult.Solvable;
                commandOutput.Text += "\n";
                commandOutput.Text += (solveResult.StopTime - startTime).Milliseconds + "ms";
                commandOutput.Text += "\n\n";
                commandOutput.Text += "[]>";
            }
            
            if (startButton != null)
            {
                startButton.IsEnabled = false;
            }
            
            var solvable = (bool)solveResult.Solvable;
            if (!solvable)
            {
                if (solutionOut != null)
                {
                    solutionOut.Text = formulaProgram.FormulaPublisher.GetUnsatOutput();
                }
            }
            else
            {
                if (solutionButton != null)
                {
                    solutionButton.IsEnabled = true;
                }
            }

            if (mainWindow != null)
            {
                mainWindow.Get<Ellipse>("Iterator").IsVisible = false;
            }
        }, DispatcherPriority.Background);

        return solveResult;
    }

    private void GetSolution(object? obj, RoutedEventArgs args)
    {
        var extractTask = new Task(() => ExecuteCommand("extract 0 0 debugger"));
        extractTask.Start();
        tasks.Add(extractTask);
    
        if (solutionOut != null)
        {
            solutionOut.Text += formulaProgram.FormulaPublisher.GetExtractOutput();
        }
    }

    private void StartSolve(object? obj, RoutedEventArgs args)
    {
        var startSolveTask = new Task<SolveResult>(SolverStart, TaskCreationOptions.LongRunning);
        formulaProgram.AddStartTask(startSolveTask);
        startSolveTask.Start();
        tasks.Add(startSolveTask);
    }
    
    private void InitSolve(object? obj, RoutedEventArgs args)
    {
        var initSolveTask = new Task(SolverInit);
        initSolveTask.Start();
        tasks.Add(initSolveTask);
    }
    
    private void ExecuteSolve(object? obj, RoutedEventArgs args)
    {
        var exeTask = new Task(SolverExecute, TaskCreationOptions.LongRunning);
        exeTask.Start();
        tasks.Add(exeTask);
    }

    private void GenerateSolve(object? obj, RoutedEventArgs args)
    {
        solutionNum += 1;
        
        var extractTask = new Task(() => ExecuteCommand("extract 0 " + solutionNum + " debugger"));
        extractTask.Start();
        tasks.Add(extractTask);
    }

    public void ClearAll(bool file)
    {
        formulaProgram.FormulaPublisher.ClearAll();
        tasks.Clear();
        if (solutionOut != null &&
            fileOutput != null)
        {
            if (file)
            {
                fileOutput.Text = "";
            }
            solutionOut.Text = "";
        }

        if (inferenceRulesViewModel != null &&
            constraintsViewModel != null &&
            termsViewModel != null)
        {
            inferenceRulesViewModel.ClearAll();
            constraintsViewModel.ClearAll();
            termsViewModel.ClearAll();
        }

        if (startButton != null &&
            executeButton != null &&
            initButton != null &&
            genButton != null &&
            solutionButton != null)
        {
            startButton.IsEnabled = false;
            initButton.IsEnabled = false;
            executeButton.IsEnabled = false;
            genButton.IsEnabled = false;
            solutionButton.IsEnabled = false;
        }
    }
    
    public void SetConstraintPanelEnabled(bool x)
    {
        if (mainWindow != null)
        {
            mainWindow.Get<SolverView>("SolverCommandView")
                      .Get<ComboBox>("VariableSelection").IsEnabled = x;
            mainWindow.Get<SolverView>("SolverCommandView")
                      .Get<ComboBox>("ConstraintSelection").IsEnabled = x;
            mainWindow.Get<SolverView>("SolverCommandView")
                      .Get<TextBox>("InputExpression").IsEnabled = x;
            mainWindow.Get<SolverView>("SolverCommandView")
                      .Get<Button>("AddConstraintButton").IsEnabled = x;
        }
    }
}