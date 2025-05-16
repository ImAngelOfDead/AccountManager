using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AccountManager.DAL;
using Amib.Threading;
using Newtonsoft.Json;

namespace AccountManager.Models
{
    /// <summary>
    /// 任务信息实体类
    /// </summary>
    public class ToolTaskInfo
    {
        /// <summary>
        /// 传进来的压缩包全路径
        /// </summary>
        public string ZipFullName { get; set; } = string.Empty;
        /// <summary>
        /// 传进来的压缩包短名称
        /// </summary>
        public string ZipShortName
        {
            get
            {
                string shortName = string.Empty;
                if (!string.IsNullOrEmpty(this.ZipFullName))
                {
                    int index = this.ZipFullName.LastIndexOf(@"\");
                    if (index > -1 && index < this.ZipFullName.Length - 1) shortName = this.ZipFullName.Substring(index + 1);
                }
                return shortName;
            }
        }
        /// <summary>
        /// 文档列表
        /// </summary>
        [JsonIgnore]
        public List<string> TxtFileList { get; set; } = new List<string>();
        /// <summary>
        /// 文档数量
        /// </summary>
        public int TxtFileCount { get { return this.TxtFileList == null ? 0 : this.TxtFileList.Count; } }
        /// <summary>
        /// 完成数量
        /// </summary>
        public int CompleteCount { get; set; } = 0;
        /// <summary>
        /// 任务进度
        /// </summary>
        public string TaskProgressDes { get { return $"{this.CompleteCount} / {this.TxtFileCount}"; } }
        /// <summary>
        /// 成功匹配列表
        /// </summary>
        public List<CookieInfo> SuccessList { get; set; } = new List<CookieInfo>();   
        /// <summary>
        /// 成功匹配列表
        /// </summary>
        public List<CookieInfo> SuccessPasswordList { get; set; } = new List<CookieInfo>();
        /// <summary>
        /// 成功匹配数量
        /// </summary>
        public int SuccessCount { get { return this.SuccessList == null ? 0 : this.SuccessList.Count; } }
        /// <summary>
        /// 有效数量
        /// </summary>
        public int MatchCount { get; set; } = 0;

        #region 线程相关属性
        /// <summary>
        /// 线程组的任务的结果
        /// </summary>
        public IWorkItemResult WorkItemResult { get; set; } = null;
        /// <summary>
        /// 线程组对象
        /// </summary>
        public IWorkItemsGroup WorkItemsGroup { get; set; } = null;
        public string WorkStatusDes
        {
            get
            {
                string des = string.Empty;
                if (this.WorkItemResult == null) des = "未运行";
                else if (!this.WorkItemResult.IsCanceled && !this.WorkItemResult.IsCompleted) des = "运行中";
                else if (this.WorkItemResult.IsCanceled) des = "已取消";
                else if (this.WorkItemResult.IsCompleted) des = "已完成";
                return des;
            }
        }
        public string WorkLog { get; set; } = string.Empty;
        public LockObject LockObject { get; set; } = new LockObject();
        #endregion
    }
    public class CookieInfo
    {
        //public string MatchDomainName { get; set; } = string.Empty;
        public DomainInfo MatchDomainInfo { get; set; } = null;
        public string CookieJsonStr { get; set; } = string.Empty;
    }
    public class DomainInfo
    {
        /// <summary>
        /// 域名名称
        /// </summary>
        public string DomainName { get; set; } = string.Empty;

        private string domainListStr = string.Empty;
        /// <summary>
        /// 域名匹配规则字符串
        /// </summary>
        public string DomainListStr
        {
            get { return this.domainListStr; }
            set
            {
                this.domainListStr = value;

                if (string.IsNullOrEmpty(this.domainListStr)) this.domainPatternList = null;
                else
                {
                    this.domainList = Regex.Split(this.domainListStr, "(\\||;|,)")
                        .Where(s => s.Trim().Length > 0 && !Regex.IsMatch(s.Trim(), "(\\||;|,)"))
                        .Select(s => s.Trim())
                        .ToList();
                    this.domainPatternList = Regex.Split(this.domainListStr, "(\\||;|,)")
                        .Where(s => s.Trim().Length > 0)
                        .Select(s => "(.?" + s.Trim() + ".?)\t(FALSE|TRUE|false|true)\t(.+)\t(FALSE|TRUE|false|true)\t([1-9][0-9]{9,12})\t(.+)\t(.+)")
                        .ToList();
                }
            }
        }

        private List<string> domainList;
        /// <summary>
        /// 域名匹配列表
        /// </summary>
        public List<string> DomainList
        {
            get { return domainList; }
        }

        private List<string> domainPatternList = null;
        /// <summary>
        /// 域名匹配正则字符串列表
        /// </summary>
        public List<string> DomainPatternList
        {
            get { return this.domainPatternList; }
        }
        /// <summary>
        /// 验证方式
        /// </summary>
        public string CheckingMethod { get; set; } = "None";
        /// <summary>
        /// 接口实现类
        /// </summary>
        [JsonIgnore]
        public ICheckingMethod ICheckingMethod { get; set; } = null;
    }
    public class LockObject
    {
        public object Lock_CompleteCount { get; set; } = new object();
        public object Lock_MatchCount { get; set; } = new object();
    }
}
