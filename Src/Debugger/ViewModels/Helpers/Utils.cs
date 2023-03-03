using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Platform.Storage;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Text;

using Debugger.Windows;

namespace Debugger.ViewModels.Helpers;

public static class Utils
{
    public static readonly HashSet<string> InputCommands = new HashSet<string>()
    {
        "unload",
        "load",
        "help",
        "solve",
        "extract"
    };
    public static async Task<IStorageFolder?> GetFolder(MainWindow win)
    {
        var topLevel = win.GetVisualRoot() as TopLevel;
        if (topLevel == null)
        {
            throw new NullReferenceException("Invalid Owner");
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Folder",
            SuggestedStartLocation = LastDirectory,
            AllowMultiple = false
        });

        LastDirectory = folders.FirstOrDefault();
        return LastDirectory;
    }
    
    public static IStorageFolder? LastDirectory { get; set; }

    public static async Task<IStorageFile?> GetFile(MainWindow win)
    {
        var topLevel = win.GetVisualRoot() as TopLevel;
        if (topLevel == null)
        {
            throw new NullReferenceException("Invalid Owner");
        }
        
        var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "File",
            AllowMultiple = false,
            SuggestedStartLocation = LastDirectory,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("formula")
                {
                    Patterns = new List<string>() { "*.4ml" }
                }
            }
        });
        return file.FirstOrDefault();
    }

    public static string OpenFileText(string filepath)
    {
        StringBuilder sb = new StringBuilder();
        using (FileStream fs = File.Open(filepath, FileMode.Open))
        {
            byte[] b = new byte[1024];
            UTF8Encoding temp = new UTF8Encoding(true);

            while (fs.Read(b,0,b.Length) > 0)
            {
                sb.Append(temp.GetString(b));
            }
        }

        return sb.ToString();
    }
}