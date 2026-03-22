using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hierarchanizer.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hierarchanizer.ViewModels {
    public partial class MainWindowViewModel : ViewModelBase {
        // 1. The main collection bound to the TreeView
        [ObservableProperty]
        private ObservableCollection<TitleNode> _titles = new();

        // 2. The currently selected item in the TreeView
        [ObservableProperty]
        private object? _selectedNode;

        // 3. Properties bound to the Editor TextBoxes
        [ObservableProperty]
        private string _selectedNodeName = string.Empty;

        [ObservableProperty]
        private string _selectedNodeDetails = string.Empty;

        [ObservableProperty]
        private bool _isNodeSelected;

        [ObservableProperty]
        private string currentFilePath = string.Empty;

        public MainWindowViewModel() {
            // You can add some dummy data here later to test the UI before JSON is ready
        }

        // --- AUTOMATIC PROPERTY HOOKS ---
        // The toolkit automatically looks for methods named "On[PropertyName]Changed" 
        // and runs them whenever that property is updated.

        // Executes whenever the TreeView selection changes
        partial void OnSelectedNodeChanged(object? value) {
            IsNodeSelected = value != null;
            
            // Pattern matching to extract the correct data based on the node level
            if (value is TitleNode title) {
                SelectedNodeName = title.Name;
                SelectedNodeDetails = "Titles do not have details."; 
            }
            else if (value is GroupNode group) {
                SelectedNodeName = group.Name;
                SelectedNodeDetails = "Groups do not have details.";
            }
            else if (value is TaskNode task) {
                SelectedNodeName = task.Name;
                SelectedNodeDetails = task.Details;
            }
            else if (value is SubTaskNode subTask) {
                SelectedNodeName = subTask.Name;
                SelectedNodeDetails = subTask.Details;
            }
            else {
                SelectedNodeName = string.Empty;
                SelectedNodeDetails = string.Empty;
            }
        }

        // Executes when the user types in the Name text box (TwoWay Binding)
        partial void OnSelectedNodeNameChanged(string value) {
            // Push the new string back down to the underlying custom class
            if (SelectedNode is TitleNode title) title.Name = value;
            else if (SelectedNode is GroupNode group) group.Name = value;
            else if (SelectedNode is TaskNode task) task.Name = value;
            else if (SelectedNode is SubTaskNode subTask) subTask.Name = value;
        }

        // Executes when the user types in the Details text box (TwoWay Binding)
        partial void OnSelectedNodeDetailsChanged(string value) {
            if (SelectedNode is TaskNode task) task.Details = value;
            else if (SelectedNode is SubTaskNode subTask) subTask.Details = value;
        }

        // --- UI COMMANDS ---
        // The [RelayCommand] attribute automatically generates an ICommand that 
        // our AXAML Buttons can bind to.

        private IStorageProvider? GetStorageProvider() {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                return desktop.MainWindow?.StorageProvider;
            }
            return null;
        }

        [RelayCommand]
        private void DeselectNode() {
            SelectedNode = null;
        }

        [RelayCommand]
        private async Task LoadJson() {
            var storageProvider = GetStorageProvider();
            if (storageProvider == null) {
                return;
            }

            // Configure and open the file picker dialog
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
                Title = "Open JSON Data File",
                AllowMultiple = false,
                FileTypeFilter = new[] { 
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count >= 1) {
                try {
                    // OpenReadAsync returns a stream that JsonSerializer can read directly
                    await using var stream = await files[0].OpenReadAsync();
                    var loadedTitles = await JsonSerializer.DeserializeAsync<ObservableCollection<TitleNode>>(stream);
            
                    if (loadedTitles != null) {
                        Titles = loadedTitles;
                        SelectedNode = null; // Reset selection to prevent UI bugs from old data
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error loading JSON: {ex.Message}");
                    return;
                }
            }
            CurrentFilePath = files[0].TryGetLocalPath();

        }

        [RelayCommand]
        private async Task SaveAsJson() {
            var storageProvider = GetStorageProvider();
            if (storageProvider == null) {
                return;
            }

            // Configure and open the save file dialog
            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
                Title = "Save JSON Data",
                DefaultExtension = "json",
                SuggestedFileName = "organizer_data",
                FileTypeChoices = new[] { 
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } } 
                }
            });

            if (file != null) {
                try {
                    var options = new JsonSerializerOptions { WriteIndented = true };
            
                    // OpenWriteAsync creates or overwrites the file, returning a stream
                    await using var stream = await file.OpenWriteAsync();
                    await JsonSerializer.SerializeAsync(stream, Titles, options);
                } catch (Exception ex) {
                    Console.WriteLine($"Error saving JSON: {ex.Message}");
                    return;
                }
            }
            CurrentFilePath = file.TryGetLocalPath();
        }
        [RelayCommand]
        private async Task SaveJson() {
            if(CurrentFilePath != "") {
                try {
                    // WriteIndented makes the JSON file human-readable and hierarchically formatted
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(Titles, options);
        
                    File.WriteAllText(CurrentFilePath, jsonString);
                } catch (Exception ex) {
                    Console.WriteLine($"Error saving JSON: {ex.Message}");
                }
            } else {
               await SaveAsJson();
            }

        }

        [RelayCommand]
        private void AddNode() {
            if (SelectedNode == null) {
                // If nothing is selected, add a new root Title
                Titles.Add(new TitleNode { Name = "New Title" });
            } else if (SelectedNode is TitleNode title) {
                // If a Title is selected, add a new Group to it
                title.Groups.Add(new GroupNode { Name = "New Group" });
            } else if (SelectedNode is GroupNode group) {
                // If a Group is selected, add a new Task to it
                group.Tasks.Add(new TaskNode { Name = "New Task" });
            } else if (SelectedNode is TaskNode task) {
                // If a Task is selected, add a new SubTask to it
                task.SubTasks.Add(new SubTaskNode { Name = "New Subtask" });
            } else if (SelectedNode is SubTaskNode) {
                // SubTasks don't have children, so we do nothing (or you could show a message)
                Console.WriteLine("Cannot add children to a SubTask.");
            }
        }

        [RelayCommand]
        private void DeleteNode() {
            if (SelectedNode == null) {
                return; // Nothing to delete
            }

            // 1. If it's a root TitleNode, we can remove it directly from the main collection
            if (SelectedNode is TitleNode titleToRemove) {
                Titles.Remove(titleToRemove);
                SelectedNode = null;
                return;
            }

            // 2. Otherwise, we traverse the tree to find the parent of the selected node
            foreach (var title in Titles) {
        
                // Check if the selected node is a Group inside this Title
                if (SelectedNode is GroupNode groupToRemove && title.Groups.Contains(groupToRemove)) {
                    title.Groups.Remove(groupToRemove);
                    SelectedNode = null;
                    return;
                }

                foreach (var group in title.Groups) {
            
                    // Check if the selected node is a Task inside this Group
                    if (SelectedNode is TaskNode taskToRemove && group.Tasks.Contains(taskToRemove)) {
                        group.Tasks.Remove(taskToRemove);
                        SelectedNode = null;
                        return;
                    }

                    foreach (var task in group.Tasks) {
                
                        // Check if the selected node is a SubTask inside this Task
                        if (SelectedNode is SubTaskNode subTaskToRemove && task.SubTasks.Contains(subTaskToRemove)) {
                            task.SubTasks.Remove(subTaskToRemove);
                            SelectedNode = null;
                            return;
                        }
                    }
                }
            }
        }

        [RelayCommand]
        private void MoveNodeUp() {
            if (SelectedNode == null) {
                return; // Nothing to move
            }

            if (SelectedNode is TitleNode titleToMove) {
                int index = Titles.IndexOf(titleToMove);
                if(index > 0) {
                    
                    //only move up if not in zero position
                    Titles.Move(index, index - 1);
                }
                return;
            }

            // 2. Otherwise, we traverse the tree to find the parent of the selected node
            foreach (var title in Titles) {
        
                // Check if the selected node is a Group inside this Title
                if (SelectedNode is GroupNode groupToMove && title.Groups.Contains(groupToMove)) {
                    int index = title.Groups.IndexOf(groupToMove);
                    if(index > 0) {
                        title.Groups.Move(index, index - 1);
                    }
                    return;
                }

                foreach (var group in title.Groups) {
            
                    // Check if the selected node is a Task inside this Group
                    if (SelectedNode is TaskNode taskToMove && group.Tasks.Contains(taskToMove)) {
                        //group.Tasks.Remove(taskToRemove);
                        int index = group.Tasks.IndexOf(taskToMove);
                        if(index > 0) {
                            group.Tasks.Move(index, index - 1);
                        }
                        return;
                    }

                    foreach (var task in group.Tasks) {
                
                        // Check if the selected node is a SubTask inside this Task
                        if (SelectedNode is SubTaskNode subTaskToMove && task.SubTasks.Contains(subTaskToMove)) {
                            //task.SubTasks.Remove(subTaskToRemove);
                            int index = task.SubTasks.IndexOf(subTaskToMove);
                            if(index > 0) {
                                task.SubTasks.Move(index, index - 1);
                            }
                            return;
                        }
                    }
                }
            }

        }

        [RelayCommand]
        private void MoveNodeDown() {
            if (SelectedNode == null) {
                return; // Nothing to move
            }

            if (SelectedNode is TitleNode titleToMove) {

                int index = Titles.IndexOf(titleToMove);
                if(index < Titles.Count - 1) {
                    
                    //only move down if not in array final position
                    Titles.Move(index, index + 1);
                }
                //SelectedNode = null;
                return;
            }

            // 2. Otherwise, we traverse the tree to find the parent of the selected node
            foreach (var title in Titles) {
        
                // Check if the selected node is a Group inside this Title
                if (SelectedNode is GroupNode groupToMove && title.Groups.Contains(groupToMove)) {
                    int index = title.Groups.IndexOf(groupToMove);
                    if(index < title.Groups.Count - 1) {
                        title.Groups.Move(index, index + 1);
                    }
                    return;
                }

                foreach (var group in title.Groups) {
            
                    // Check if the selected node is a Task inside this Group
                    if (SelectedNode is TaskNode taskToMove && group.Tasks.Contains(taskToMove)) {
                        //group.Tasks.Remove(taskToRemove);
                        int index = group.Tasks.IndexOf(taskToMove);
                        if(index < group.Tasks.Count - 1) {
                            group.Tasks.Move(index, index + 1);
                        }
                        return;
                    }

                    foreach (var task in group.Tasks) {
                
                        // Check if the selected node is a SubTask inside this Task
                        if (SelectedNode is SubTaskNode subTaskToMove && task.SubTasks.Contains(subTaskToMove)) {
                            //task.SubTasks.Remove(subTaskToRemove);
                            int index = task.SubTasks.IndexOf(subTaskToMove);
                            if(index < task.SubTasks.Count - 1) {
                                task.SubTasks.Move(index, index + 1);
                            }
                            return;
                        }
                    }
                }
            }

        }
    }
}
