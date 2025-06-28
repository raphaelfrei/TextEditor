using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using TextEditor.Models;

namespace TextEditor;

public partial class MainWindow : Window
{

    private List<FileTab> _openTabs = new();
    private FileTab? _currentTab;
    private TextDocument _document { get; set; } = new();
    private string Text { get; set; } = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        TxtEditor.Document = _document;
        SetupNativeMenu();
    }

    private void CreateNewTab(string fileName = "New File", string? filePath = null)
    {
        var tabItem = new TabItem
        {
            Header = fileName,
            Content = new TextBlock { Text = fileName } // Placeholder
        };

        var fileTab = new FileTab(tabItem)
        {
            FileName = fileName,
            FilePath = filePath
        };

        _openTabs.Add(fileTab);
        FileTabControl.Items.Add(tabItem);
        FileTabControl.SelectedItem = tabItem;

        SwitchToTab(fileTab);
    }

    private void SwitchToTab(FileTab tab)
    {
        _currentTab = tab;
        TxtEditor.Document = tab.Document;
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        if (_currentTab == null)
            return;
        var modified = _currentTab.IsModified ? "*" : "";
        Title = $"Text Editor - {_currentTab.FileName}{modified}";
    }

    private FileTab? GetTabFromTabItem(TabItem tabItem)
    {
        return _openTabs.FirstOrDefault(t => t.TabItem == tabItem);
    }

    private async void OpenFileInNewTab(IStorageFile file)
    {
        using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();

        CreateNewTab(file.Name, file.Path.LocalPath);

        if (_currentTab != null)
        {
            _currentTab.Document.Text = content;
            _currentTab.IsModified = false;
        }
    }

    private void SetupNativeMenu()
    {
        var menuBar = new NativeMenu();

        // Menu File
        var newMenuItem = new NativeMenuItem("New") { Gesture = KeyGesture.Parse("Cmd+N") };
        newMenuItem.Click += (s, e) => NewFile_Click(s, new RoutedEventArgs());

        var openMenuItem = new NativeMenuItem("Open...") { Gesture = KeyGesture.Parse("Cmd+O") };
        openMenuItem.Click += (s, e) => OpenFile_Click(s, new RoutedEventArgs());

        var saveMenuItem = new NativeMenuItem("Save") { Gesture = KeyGesture.Parse("Cmd+S") };
        saveMenuItem.Click += (s, e) => SaveFile_Click(s, new RoutedEventArgs());

        var saveAsMenuItem = new NativeMenuItem("Save As...") { Gesture = KeyGesture.Parse("Cmd+Shift+S") };
        saveAsMenuItem.Click += (s, e) => SaveFileAs_Click(s, new RoutedEventArgs());

        var quitMenuItem = new NativeMenuItem("Quit") { Gesture = KeyGesture.Parse("Cmd+Q") };
        quitMenuItem.Click += (s, e) => Close();

        var fileMenu = new NativeMenuItem("File");
        var fileSubMenu = new NativeMenu
    {
        newMenuItem,
        openMenuItem,
        new NativeMenuItemSeparator(),
        saveMenuItem,
        saveAsMenuItem,
        new NativeMenuItemSeparator(),
        quitMenuItem
    };

        fileMenu.Menu = fileSubMenu;
        menuBar.Add(fileMenu);

        // Aplicar o menu
        NativeMenu.SetMenu(this, menuBar);
    }

    #region Actions

    private void NewFile_Click(object? sender, RoutedEventArgs e)
    {
        CreateNewTab();
    }

    private void SaveFileAs_Click(object? sender, RoutedEventArgs e)
    {
        SaveFileAs();
    }

    private void CloseTab_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentTab == null || _openTabs.Count <= 1) return;

        // TODO: Verificar se precisa salvar antes de fechar

        _openTabs.Remove(_currentTab);
        FileTabControl?.Items.Remove(_currentTab.TabItem);

        if (_openTabs.Count > 0)
        {
            var nextTab = _openTabs.Last();
            FileTabControl!.SelectedItem = nextTab.TabItem;
        }
    }

    private async void OpenFile_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = true, // Permite múltiplos arquivos
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Text Files")
                {
                    Patterns = new[] { "*.txt", "*.cs", "*.json", "*.xml", "*.html", "*.*" }
                }
            }
        });

        foreach (var file in files)
        {
            OpenFileInNewTab(file);
        }
    }

    private async void SaveFile_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentTab == null)
            return;

        if (string.IsNullOrEmpty(_currentTab.FilePath))
        {
            SaveFileAs();
        }
        else
        {
            await File.WriteAllTextAsync(_currentTab.FilePath, _currentTab.Document.Text, Encoding.UTF8);
            _currentTab.IsModified = false;
            UpdateTitle();
        }
    }

    private async void SaveFileAs()
    {
        if (_currentTab == null) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Text File",
            SuggestedFileName = _currentTab.FileName,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        });

        if (file != null)
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);

            await writer.WriteAsync(_currentTab.Document.Text);

            _currentTab.FilePath = file.Path.LocalPath;
            _currentTab.FileName = file.Name;
            _currentTab.IsModified = false;
            _currentTab.TabItem.Header = file.Name;

            UpdateTitle();
        }
    }

    private void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem selectedTab)
        {
            var tab = GetTabFromTabItem(selectedTab);
            if (tab != null)
            {
                SwitchToTab(tab);
            }
        }
    }

    #endregion Actions

}
