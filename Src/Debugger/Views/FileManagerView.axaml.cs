using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Debugger.ViewModels;

namespace Debugger.Views;

public partial class FileManagerView : UserControl
{
    public FileManagerView()
    {
        InitializeComponent();

        DoubleTapped += FileDoubleTap;
    }
    
    private void FileDoubleTap(object? sender, TappedEventArgs e)
    {
        var model = DataContext as FileManagerViewModel;
        if (model != null)
        {
            model.LoadFormulaFileCmd();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}