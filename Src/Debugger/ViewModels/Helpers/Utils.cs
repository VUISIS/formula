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
    public static string LoadedFile = "";
    
    public static readonly HashSet<string> InputCommands = new HashSet<string>()
    {
        "unload",
        "load",
        "help",
        "solve",
        "extract"
    };
    
    public static readonly HashSet<string> TUnloadCommands = new HashSet<string>()
    {
        "solve",
        "apply",
        "query"
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
        using (StreamReader sr = new StreamReader(filepath, Encoding.UTF8))
        {
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                sb.AppendLine(line);
            }
        }
        return sb.ToString().Trim();
    }
}