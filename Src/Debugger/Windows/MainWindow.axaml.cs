using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;

using Debugger.ViewModels;
using Debugger.Views;
using ReactiveUI;

namespace Debugger.Windows;

public partial class MainWindow : Window
{    
    private readonly FormulaProgram formulaProgram;
    public MainWindow()
    {
        formulaProgram = new FormulaProgram();
        
        InitializeComponent();
        
        DataContext = new MainWindowViewModel();

        var bar = this.Get<DockPanel>("TopBar");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            bar.Margin = new Thickness(0.0,30.0,0.0,5.0);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        var conModel = new ConstraintsViewModel();
        var cmdModel = new CommandConsoleViewModel(this, formulaProgram);
        var termModel = new CurrentTermsViewModel(this, formulaProgram);
        var solveModel = new SolverViewModel(this, formulaProgram);
        var infModel = new InferenceRulesViewModel();
        var fileModel = new FileManagerViewModel(this, formulaProgram);
        var toolModel = new ToolbarViewModel(this, formulaProgram);
        
        var cv = this.Get<ConstraintsView>("ConstrView");
        SetViewWindow(cv, conModel);
        
        var ctv = this.Get<CurrentTermsView>("TermsView");
        SetViewWindow(ctv, termModel);

        var sv = this.Get<SolverView>("SolverCommandView");
        SetViewWindow(sv, solveModel);
        
        var iv = this.Get<InferenceRulesView>("SolverRulesView");
        SetViewWindow(iv, infModel);

        var civ = this.Get<CommandConsoleView>("CommandInputView");
        SetViewWindow(civ, cmdModel);
        
        var fmv = this.Get<FileManagerView>("FileTreeView");
        SetViewWindow(fmv, fileModel);
        
        var tbv = this.Get<ToolbarView>("TopToolbarView");
        SetViewWindow(tbv, toolModel);
    }

    private void SetViewWindow(UserControl uc, ReactiveObject obj)
    {
        uc.DataContext = obj;
    }
}