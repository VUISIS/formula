using ReactiveUI;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using FormulaDebugger;

using System.Threading.Tasks;

namespace FormulaDebuggerGUI;

internal class MainWindowViewModel : ReactiveObject
{
    private MainWindow _win;

    public MainWindowViewModel(MainWindow win)
    {
        _win = win;
    }

    public async void AttachCmd() 
    {
        var dialog = new ProcessListWindow();
        await dialog.ShowDialog(_win);
    }

    public void ExitCmd() 
    {
        _win.Close();
    }
}

public partial class MainWindow : Window
{    
    public MainWindow()
    {
        this.InitializeComponent();

        this.DataContext = new MainWindowViewModel(this);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}