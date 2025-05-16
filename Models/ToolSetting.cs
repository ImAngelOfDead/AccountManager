using System.Collections.Generic;
using Newtonsoft.Json;

namespace AccountManager.Models
{
    public class ToolSetting
    {
        /// <summary>
        /// 工作线程
        /// </summary>
        public int ThreadCount { get; set; } = 100;
        /// <summary>
        /// 域名信息列表
        /// </summary>
        public List<DomainInfo> DomainInfos { get; set; } = null;
        /// <summary>
        /// 任务信息列表
        /// </summary>
        [JsonIgnore]
        public List<ToolTaskInfo> TaskInfos { get; set; } = null;
    }
}
