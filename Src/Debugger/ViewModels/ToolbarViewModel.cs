using ReactiveUI;
using Avalonia.Controls;

namespace Debugger.ViewModels;

internal class ToolbarViewModel : ReactiveObject
{
    private Window? mainWindow;

    public ToolbarViewModel(Window win)
    {
        mainWindow = win;
    }
    
    public void ExitCmd() 
    {
        if (mainWindow != null)
        {
            mainWindow.Close();
        }
    }
}