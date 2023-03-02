using ReactiveUI;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;

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
    private List<Task> tasks = new List<Task>();
    
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
                var loadTask = new Task(() => ExecuteLoadCommand(uri, SelectedItems[0].Header));
                loadTask.Start();
                tasks.Add(loadTask);
                var timeoutTask = new Task(() => TimeoutAfter(loadTask));
                timeoutTask.Start();
                tasks.Add(timeoutTask);
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

    private void ExecuteLoadCommand(Uri uri, string file)
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
                }, DispatcherPriority.Render);
                return;
            }

            formulaProgram.ClearConsoleOutput();
            formulaProgram.FormulaPublisher.ClearAll();

            var fileP = Path.Join(uri.AbsolutePath, file);

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
}