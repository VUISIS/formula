using ReactiveUI;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Debugger.Windows;
using Debugger.ViewModels.Types;
using Debugger.ViewModels.Helpers;
using Debugger.Views;

namespace Debugger.ViewModels;


internal class FileManagerViewModel : ReactiveObject
{
    private readonly MainWindow? mainWindow;
    private readonly FormulaProgram? formulaProgram;
    private readonly TextBox? consoleOutput;
    private readonly TextBox? fileOutput;
    private List<Task> tasks = new List<Task>();
    private readonly CommandConsoleViewModel? commandConsoleViewModel;
    private readonly InferenceRulesViewModel? inferenceRulesViewModel;
    private readonly CurrentTermsViewModel? currentTermsViewModel;
    
    public FileManagerViewModel(MainWindow win, FormulaProgram program)
    {
        mainWindow = win;
        formulaProgram = program;

        Items = new ObservableCollection<Node>();
        ItemsSource = new ObservableCollection<Node>();
        SelectedItems = new ObservableCollection<Node>();
        
        fileOutput = mainWindow.Get<Domain4MLView>("DomainView")
                              .Get<TextBox>("FileOutput");

        consoleOutput = mainWindow.Get<CommandConsoleView>("CommandInputView")
                                  .Get<TextBox>("ConsoleOutput");
        commandConsoleViewModel = mainWindow.Get<CommandConsoleView>("CommandInputView").DataContext as CommandConsoleViewModel;
        currentTermsViewModel = mainWindow.Get<CurrentTermsView>("TermsView").DataContext as CurrentTermsViewModel;
        inferenceRulesViewModel = mainWindow.Get<InferenceRulesView>("SolverRulesView").DataContext as InferenceRulesViewModel;
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
                if (commandConsoleViewModel != null &&
                    inferenceRulesViewModel != null &&
                    currentTermsViewModel != null)
                {
                    commandConsoleViewModel.SetConstraintPanelEnabled(false);
                    commandConsoleViewModel.ClearAll(true);
                    inferenceRulesViewModel.ClearAll();
                    currentTermsViewModel.ClearAll();
                }
                
                Utils.LoadedFile = System.IO.Path.Join(uri.AbsolutePath, SelectedItems[0].Header);
                
                if (mainWindow != null)
                {
                    mainWindow.Get<Ellipse>("Iterator").IsVisible = true;
                }

                var loadTask = new Task(() => ExecuteLoadCommand(uri, SelectedItems[0].Header));
                loadTask.Start();
                tasks.Add(loadTask);
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

            var fileP = System.IO.Path.Join(uri.AbsolutePath, file);

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
                if (mainWindow != null)
                {
                    mainWindow.Get<Ellipse>("Iterator").IsVisible = false;
                }
            }, DispatcherPriority.Render);
        }
    }
}