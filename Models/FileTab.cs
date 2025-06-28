using Avalonia.Controls;
using AvaloniaEdit.Document;

namespace TextEditor.Models;

public class FileTab
{
    public string FileName { get; set; } = "New File";
    public string? FilePath { get; set; }
    public TextDocument Document { get; set; } = new();
    public bool IsModified { get; set; } = false;
    public TabItem TabItem { get; set; }

    public FileTab(TabItem tabItem)
    {
        TabItem = tabItem;
    }
}