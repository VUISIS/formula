using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using Avalonia.Controls.Shapes;
using ReactiveUI;
using Avalonia.Media;

using System;
using System.Threading.Tasks;

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
    private readonly TextBox? consoleOutput;
    private readonly TextBox? fileOutput;
    private CommandConsoleViewModel? commandConsoleViewModel;
    private InferenceRulesViewModel? inferenceRulesViewModel;
    private CurrentTermsViewModel? currentTermsViewModel;

    public ToolbarViewModel(MainWindow win, FormulaProgram program)
    {
        mainWindow = win;

        formulaProgram = program;

        fileOutput = mainWindow.Get<Domain4MLView>("DomainView")
                              .Get<TextBox>("FileOutput");
        consoleOutput = mainWindow.Get<CommandConsoleView>("CommandInputView")
                                  .Get<TextBox>("ConsoleOutput");
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
                var root = new Node(file.Name, fileManagerModel.Items.Count);
            
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
                        commandConsoleViewModel.ClearAll(true);
                        inferenceRulesViewModel.ClearAll();
                        currentTermsViewModel.ClearAll();
                    }
                    
                    Utils.LoadedFile = System.IO.Path.Join(uri.AbsolutePath, file.Name);
                    
                    if (mainWindow != null)
                    {
                        mainWindow.Get<Ellipse>("Iterator").Fill = new SolidColorBrush(Colors.Green);
                    }
                    
                    var loadTask = new Task(() => ExecuteLoadCommand(uri, file));
                    loadTask.Start();
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

            var fileP = System.IO.Path.Join(uri.AbsolutePath, file.Name);

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
                    mainWindow.Get<Ellipse>("Iterator").Fill = new SolidColorBrush(Colors.Gray);
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
                var root = new Node(folder.Name, 0);
                var items = await folder.GetItemsAsync();
                if (items.Count > 0)
                {
                    foreach (var file in items)
                    {
                        if (file.Name.EndsWith(".4ml"))
                        {
                            var child = new Node(root, file.Name, root.Children.Count);
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