using Newtonsoft.Json;
using OpenPop.Pop3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccountManager.Models
{
    [Serializable]
    public class MailInfo
    {
        /// <summary>
        /// 邮箱名
        /// </summary>
        public string Mail_Name { get; set; } = string.Empty;
        /// <summary>
        /// 邮箱pwd
        /// </summary>
        public string Mail_Pwd { get; set; } = string.Empty;  
        /// <summary>
        /// 安全邮箱
        /// </summary>
        public string VerifyMail_Name { get; set; } = string.Empty;     
        /// <summary>
        /// 安全邮箱密码
        /// </summary>
        public string VerifyMail_Pwd { get; set; } = string.Empty;
        /// <summary>
        /// 是否已使用
        /// </summary>
        public bool Is_Used { get; set; } = false;
        /// <summary>
        /// 是否已使用
        /// </summary>
        public string Is_Used_Des { get { string des = this.Is_Used ? "已使用" : string.Empty; return des; } }
        /// <summary>
        /// 是否被锁定
        /// </summary>
        [JsonIgnore]
        public bool IsLocked { get; set; } = false;
        /// <summary>
        /// Pop3客户端对象
        /// </summary>
        [JsonIgnore]
        public Pop3Client Pop3Client { get; set; } = null;
    }
}
