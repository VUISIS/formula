using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using ReactiveUI;

using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

using Debugger.ViewModels.Helpers;
using Debugger.Views;
using Debugger.Windows;
using Debugger.ViewModels.Types;

namespace Debugger.ViewModels;

internal class ToolbarViewModel : ReactiveObject
{
    private readonly FileManagerViewModel? fileManagerModel;
    private readonly MainWindow? mainWindow;
    private readonly FormulaProgram? formulaProgram;
    private readonly TextBlock? consoleOutput;
    private readonly TextBlock? fileOutput;
    private CommandConsoleViewModel? commandConsoleViewModel;
    private InferenceRulesViewModel? inferenceRulesViewModel;
    private CurrentTermsViewModel? currentTermsViewModel;

    public ToolbarViewModel(MainWindow win, FormulaProgram program)
    {
        mainWindow = win;

        formulaProgram = program;

        fileOutput = mainWindow.Get<Domain4MLView>("DomainView")
                              .Get<TextBlock>("FileOutput");
        consoleOutput = mainWindow.Get<CommandConsoleView>("CommandInputView")
                                  .Get<TextBlock>("ConsoleOutput");
        commandConsoleViewModel = mainWindow.Get<CommandConsoleView>("CommandInputView").DataContext as CommandConsoleViewModel;
        fileManagerModel = mainWindow.Get<FileManagerView>("FileTreeView").DataContext as FileManagerViewModel;
        currentTermsViewModel = mainWindow.Get<CurrentTermsView>("TermsView").DataContext as CurrentTermsViewModel;
        inferenceRulesViewModel = mainWindow.Get<InferenceRulesView>("SolverRulesView").DataContext as InferenceRulesViewModel;
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
                    if (commandConsoleViewModel != null &&
                        inferenceRulesViewModel != null &&
                        currentTermsViewModel != null)
                    {
                        commandConsoleViewModel.SetConstraintPanelEnabled(false);
                        commandConsoleViewModel.ClearAll();
                        inferenceRulesViewModel.ClearAll();
                        currentTermsViewModel.ClearAll();
                    }
                    
                    var loadTask = new Task(() => ExecuteLoadCommand(uri, file));
                    loadTask.Start();
                    var timeoutTask = new Task(() => TimeoutAfter(loadTask));
                    timeoutTask.Start();
                }
            }
        }
    }

    private async void TimeoutAfter(Task task)
    {
        if (formulaProgram != null)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var timeout = new TimeSpan(0, 0, 0, 10);
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (consoleOutput != null)
                        {
                            consoleOutput.Text += "\n";
                            consoleOutput.Text += "Solve LOAD task timed out.";
                            consoleOutput.Text += "\n";
                            consoleOutput.Text += "10s";
                            consoleOutput.Text += "\n\n";
                            consoleOutput.Text += "[]>";
                        }
                    }, DispatcherPriority.Render);
                }
            }
        }
    }

    private void ExecuteLoadCommand(Uri uri, IStorageFile file)
    {
        if (formulaProgram != null &&
            consoleOutput != null)
        {
            if (!formulaProgram.ExecuteCommand("unload *"))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (consoleOutput != null)
                    {
                        consoleOutput.Text += "ERROR: COMMAND failed to execute.";
                    }
                });
                return;
            }

            formulaProgram.ClearConsoleOutput();
            formulaProgram.FormulaPublisher.ClearAll();

            var fileP = Path.Join(uri.AbsolutePath, file.Name);

            if (!formulaProgram.ExecuteCommand("load " + fileP))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (consoleOutput != null)
                    {
                        consoleOutput.Text += "ERROR: COMMAND failed to execute.";
                    }
                }, DispatcherPriority.Render);
                return;
            }

            var fileTxt = Utils.OpenFileText(fileP);
            Dispatcher.UIThread.Post(() =>
            {
                if (consoleOutput != null)
                {
                    if (fileOutput != null)
                    {
                        fileOutput.Text = fileTxt;
                    }
                    
                    var txt = consoleOutput.Text;
                    if(txt == null || txt.Length <= 0)
                    {
                        consoleOutput.Text += "[]> ";
                    }

                    consoleOutput.Text += "load " + fileP;
                    consoleOutput.Text += "\n";
                    consoleOutput.Text += formulaProgram.GetConsoleOutput();
                }
            }, DispatcherPriority.Render);
        }
    }
    
    public async void OpenFolderCmd()
    {
        if (mainWindow != null &&
            fileManagerModel != null)
        {
            var folder = await Utils.GetFolder(mainWindow);
            if(folder != null)
            {
                var root = new Node(folder.Name);
                var items = await folder.GetItemsAsync();
                if (items.Count > 0)
                {
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
    }
    
    public void ExitCmd() 
    {
        if (mainWindow != null)
        {
            mainWindow.Close();
        }
    }
}