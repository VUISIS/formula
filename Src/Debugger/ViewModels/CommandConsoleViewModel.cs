using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ReactiveUI;

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Debugger.ViewModels.Helpers;
using Debugger.Views;
using Debugger.Windows;
using Microsoft.Formula.API;
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
    private int solutionNum = 0;

    private enum TaskType
    {
        INIT = 0,
        EXECUTE = 1,
        START = 2,
        COMMAND = 3,
        SOLUTION = 4,
        GENERATE = 5,
        SOLVE = 6,
        LOAD = 7
    }
    
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
                    var cmdTask = new Task(() => LoadCommand(commandInput.Text));
                    cmdTask.Start();
                    var timeOutTask = new Task(() => TimeoutAfter(cmdTask, TaskType.LOAD));
                    timeOutTask.Start();
                    tasks.Add(timeOutTask);
                }
                else if (commandInput.Text.StartsWith("solve"))
                {
                    ClearAll();

                    if (initButton != null)
                    {
                        initButton.IsEnabled = true;
                    }
                    
                    if (!formulaProgram.ExecuteCommand("tunload *"))
                    {
                        if (commandOutput != null)
                        {
                            commandOutput.Text += "ERROR: " + TaskType.COMMAND + " failed to execute.";
                        }
                    }
                        
                    formulaProgram.ClearConsoleOutput();

                    var commandExecuteTask = new Task(() => ExecuteCommand(commandInput.Text));
                    commandExecuteTask.Start();
                    var timeoutExecuteTask = new Task(() => TimeoutAfter(commandExecuteTask, TaskType.SOLVE));
                    timeoutExecuteTask.Start();
                    tasks.Add(timeoutExecuteTask);
                }
                else
                {
                    if (commandInput.Text.StartsWith("reload") ||
                        commandInput.Text.StartsWith("rl"))
                    {
                        ClearAll();
                        SetConstraintPanelEnabled(false);
                    }

                    var commandExecuteTask = new Task(() => ExecuteCommand(commandInput.Text));
                    commandExecuteTask.Start();
                    var timeoutTask = new Task(() => TimeoutAfter(commandExecuteTask, TaskType.COMMAND));
                    timeoutTask.Start();
                    tasks.Add(timeoutTask);
                }
            }
        }
    }
    
    private async void TimeoutAfter(Task task, TaskType type) 
    {
        using(var timeoutCancellationTokenSource = new CancellationTokenSource())
        {
            formulaProgram.ClearConsoleOutput();

            Dispatcher.UIThread.Post(() =>
            {
                if (commandOutput != null)
                {
                    if (commandOutput.Text == null ||
                        commandOutput.Text.Length <= 0)
                    {
                        commandOutput.Text += "[]> ";
                    }
                }
            }, DispatcherPriority.Background);

            var timeout = new TimeSpan(0, 0, 0, 10);
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task)
            {
                if (type == TaskType.INIT)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
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
                    }, DispatcherPriority.Background);
                }
                else if (type == TaskType.EXECUTE)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (executeButton != null &&
                            startButton != null)
                        {
                            startButton.IsEnabled = true;
                            executeButton.IsEnabled = false;
                        }
                    }, DispatcherPriority.Background);
                }
                else if (type == TaskType.START)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (startButton != null &&
                            solutionButton != null)
                        {
                            startButton.IsEnabled = false;
                            solutionButton.IsEnabled = true;
                        }
                    }, DispatcherPriority.Background);
                }
                else if (type == TaskType.SOLUTION)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (solutionButton != null &&
                            genButton != null)
                        {
                            solutionButton.IsEnabled = false;
                            genButton.IsEnabled = true;
                        }
                        
                        if (solutionOut != null)
                        {
                            solutionOut.Text += formulaProgram.FormulaPublisher.GetExtractOutput();
                        }
                    }, DispatcherPriority.Background);
                }
                else if (type == TaskType.LOAD)
                {
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
                            ClearAll();
                        }
                    }, DispatcherPriority.Background);
                }

                Dispatcher.UIThread.Post(() =>
                {
                    if (commandOutput != null)
                    {
                        commandOutput.Text += formulaProgram.GetConsoleOutput();
                    }
                }, DispatcherPriority.Background);
                timeoutCancellationTokenSource.Cancel();
            } 
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (commandOutput != null)
                    {
                        commandOutput.Text += "\n";
                        commandOutput.Text += "Solve " + type + " task timed out.";
                        commandOutput.Text += "\n";
                        commandOutput.Text += "10s";
                        commandOutput.Text += "\n\n";
                        commandOutput.Text += "[]>";
                    }
                }, DispatcherPriority.Background);
            }
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
                    commandOutput.Text += "ERROR: " + TaskType.COMMAND + " failed to execute.";
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

        var fileOut = Utils.OpenFileText(outP);
        Dispatcher.UIThread.Post(() =>
        {
            if (fileOutput != null)
            {
                fileOutput.Text = fileOut;
            }
        }, DispatcherPriority.Background);

        if (!formulaProgram.ExecuteCommand(input))
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (commandOutput != null)
                {
                    commandOutput.Text += "ERROR: " + TaskType.COMMAND + " failed to execute.";
                }
            }, DispatcherPriority.Background);
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (commandOutput != null)
            {
                commandOutput.Text += input;
                commandOutput.Text += "\n";
                commandOutput.Text += formulaProgram.GetConsoleOutput();
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
                    commandOutput.Text += "ERROR: " + TaskType.COMMAND + " failed to execute.";
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

                commandOutput.Text += formulaProgram.GetConsoleOutput();
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

                if (commandOutput != null)
                {
                    commandOutput.Text += "Solve " + TaskType.INIT + " task completed.";
                    commandOutput.Text += "\n\n";
                    commandOutput.Text += "[]>";
                }
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

                if (commandOutput != null)
                {
                    commandOutput.Text += "[]> Solve " + TaskType.EXECUTE + " task completed.";
                    commandOutput.Text += "\n\n";
                    commandOutput.Text += "[]>";
                }
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
                commandOutput.Text += " Solve " + TaskType.START + " task completed.";
                commandOutput.Text += "\n";
                commandOutput.Text += "Solveable: " + solveResult.Solvable;
                commandOutput.Text += "\n";
                commandOutput.Text += (solveResult.StopTime - startTime).Milliseconds + "ms";
                commandOutput.Text += "\n\n";
                commandOutput.Text += "[]>";
            }
        }, DispatcherPriority.Background);

        return solveResult;
    }

    private void GetSolution(object? obj, RoutedEventArgs args)
    {
        var extractTask = new Task(() => ExecuteCommand("extract 0 0 debugger"));
        extractTask.Start();
        var timeoutTask = new Task(() => TimeoutAfter(extractTask, TaskType.SOLUTION));
        timeoutTask.Start();
        tasks.Add(timeoutTask);
    }

    private void StartSolve(object? obj, RoutedEventArgs args)
    {
        var startSolveTask = new Task<SolveResult>(SolverStart, TaskCreationOptions.LongRunning);
        formulaProgram.AddStartTask(startSolveTask);
        startSolveTask.Start();
        var timeoutStartTask = new Task(() => TimeoutAfter(startSolveTask, TaskType.START));
        timeoutStartTask.Start();
        tasks.Add(timeoutStartTask);
    }
    
    private void InitSolve(object? obj, RoutedEventArgs args)
    {
        var initSolveTask = new Task(SolverInit);
        initSolveTask.Start();
        var timeoutTask = new Task(() => TimeoutAfter(initSolveTask, TaskType.INIT));
        timeoutTask.Start();
        tasks.Add(timeoutTask);
    }
    
    private void ExecuteSolve(object? obj, RoutedEventArgs args)
    {
        var exeTask = new Task(SolverExecute, TaskCreationOptions.LongRunning);
        exeTask.Start();
        var timeExeTask = new Task(() => TimeoutAfter(exeTask, TaskType.EXECUTE));
        timeExeTask.Start();
        tasks.Add(timeExeTask);
    }

    private void GenerateSolve(object? obj, RoutedEventArgs args)
    {
        solutionNum += 1;
        
        var extractTask = new Task(() => ExecuteCommand("extract 0 1 debugger"));
        extractTask.Start();
        var timeoutTask = new Task(() => TimeoutAfter(extractTask, TaskType.SOLUTION));
        timeoutTask.Start();
        tasks.Add(timeoutTask);
    }

    public void ClearAll()
    {
        formulaProgram.FormulaPublisher.ClearAll();
        tasks.Clear();
                        
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