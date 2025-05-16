using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace AccountManager.Models
{
    public class TaskInfo
    {
        public TaskInfo(string taskName, string displayName, bool isSelected = false) { this.TaskName = taskName; this.DisplayName = displayName; this.IsSelected = isSelected; }
        public bool IsSelected { get; set; } = false;
        public string TaskName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
