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
        
        var tcv = this.Get<TermsConstraintsView>("TermsAndConstraintsView");
        SetViewWindow(tcv, new TermsConstraintsViewModel(this, formulaProgram));
        
        var iv = this.Get<InferenceRulesView>("SolverRulesView");
        SetViewWindow(iv, new InferenceRulesViewModel());

        var civ = this.Get<CommandConsoleView>("CommandInputView");
        SetViewWindow(civ, new CommandConsoleViewModel(this, formulaProgram));
        
        var fmv = this.Get<FileManagerView>("FileTreeView");
        SetViewWindow(fmv, new FileManagerViewModel(this, formulaProgram));
        
        var tbv = this.Get<ToolbarView>("TopToolbarView");
        SetViewWindow(tbv, new ToolbarViewModel(this, formulaProgram));
    }

    private void SetViewWindow(UserControl uc, ReactiveObject obj)
    {
        uc.DataContext = obj;
    }
}