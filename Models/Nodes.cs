using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Hierarchanizer.Models {
	
    public partial class TitleNode : ObservableObject {
        [ObservableProperty]
        private string _name = string.Empty;
        public ObservableCollection<GroupNode> Groups { get; set; } = new();
    }
    public partial class GroupNode : ObservableObject {
        [ObservableProperty]
        private string _name = string.Empty;
        public ObservableCollection<TaskNode> Tasks { get; set; } = new();
    }
    public partial class TaskNode : ObservableObject {
        [ObservableProperty]
        private string _name = string.Empty;
        [ObservableProperty]
        private string _details = string.Empty;
        public ObservableCollection<SubTaskNode> SubTasks { get; set; } = new();
    }
    public partial class SubTaskNode : ObservableObject {
        [ObservableProperty]
        private string _name = string.Empty;
        [ObservableProperty]
        private string _details = string.Empty;
    }
}
