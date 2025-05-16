using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccountManager.Models
{
    public class ExcelColumnInfo
    {
        public ExcelColumnInfo() { }
        public ExcelColumnInfo(string headerName, string propertyName, int headerIndex = -1) { this.HeaderName = headerName;this.PropertyName = propertyName; this.HeaderIndex = HeaderIndex; }
        public string HeaderName { get; set; } = string.Empty;
        public int HeaderIndex { get; set; } = -1;
        public string PropertyName { get; set; } = string.Empty;
    }
}
