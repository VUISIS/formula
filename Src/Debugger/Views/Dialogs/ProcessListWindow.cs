using ReactiveUI;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Controls.Selection;

using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;

namespace FormulaDebuggerGUI;

internal class ProcessListWindowViewModel : ReactiveObject
{
    readonly private string[] _processesToSearch = { "CommandLine", "dotnet" };

    private ProcessListWindow _win;

    private string? _selection = null;

    public ProcessListWindowViewModel(ProcessListWindow win)
    {
        _win = win;
                
        Process[] cmdProc = Process.GetProcessesByName(_processesToSearch[0]);
        Process[] dotnetProc = Process.GetProcessesByName(_processesToSearch[1]);
        Process[] processes = new Process[cmdProc.Length + dotnetProc.Length];

        Array.Copy(cmdProc, processes, cmdProc.Length);
        Array.Copy(dotnetProc, 0, processes, cmdProc.Length, dotnetProc.Length);
        
        foreach(Process proc in processes)
        {
            ListBoxItem lbi = new ListBoxItem();
            SolidColorBrush brush = new SolidColorBrush();
            brush.Color = new Color(225, 22, 27, 34);
            if(proc.ProcessName == _processesToSearch[0])
            {
                lbi.Content = proc.ProcessName + " \t" + proc.Id.ToString();
            }
            else if (proc.ProcessName == _processesToSearch[1])
            {
                lbi.Content = proc.ProcessName + "\t\t" + proc.Id.ToString();
            }
            lbi.Background = brush;
            lbi.DataContext = this;
            Items.Add(lbi);
        }
        
        ListBox? processList = _win.FindControl<ListBox>("ProcessList");
        if(processList != null)
        {
            processList.SelectionChanged += SelectionChanged;
        }
    }

    public void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if(e.AddedItems.Count > 0)
        {
            var item = e.AddedItems[0];
            var listItem = item as ListBoxItem;
            if(listItem != null)
            {
                _selection = listItem.Content as string;
            }
        }
    }

    public async void SubmitCmd() 
    { 
        if(_selection == null)
        {
            System.Console.WriteLine("NO SELECTION MADE");
        }
        else
        {
            System.Console.WriteLine(_selection);
        }
        await Task.Delay(2000);
        _win.Close();
    }

    public ObservableCollection<ListBoxItem>? Items { get; } = new ObservableCollection<ListBoxItem>();
}

public partial class ProcessListWindow : Window
{
    public ProcessListWindow()
    {
        this.InitializeComponent();

        this.DataContext = new ProcessListWindowViewModel(this);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}