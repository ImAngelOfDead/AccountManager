using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AccountManager.Models
{
    [Serializable]
    public class Setting
    {
        /// <summary>
        /// 任务信息列表
        /// </summary>
        [JsonIgnore]
        public List<ToolTaskInfo> TaskInfos { get; set; } = null;

        /// <summary>
        /// 提取密码
        /// </summary>
        public bool IsGetPassword { get; set; } = false;

        /// <summary>
        /// 域名信息列表
        /// </summary>
        public List<DomainInfo> DomainInfos { get; set; } = null;

        /// <summary>
        /// 并发线程数
        /// </summary>
        public int ThreadCountMax { get; set; } = 100;

        /// <summary>
        /// 城市列表
        /// </summary>
        [JsonIgnore] public JArray Ja_CitysInfo = null;

        /// <summary>
        /// 国家区号列表
        /// </summary>
        [JsonIgnore]
        public JArray Ja_CountrysPhoneNumInfo { get; set; } = null;

        /// <summary>
        /// Facebook设置
        /// </summary>
        public Setting_FBOrIns Setting_FB { get; set; } = new Setting_FBOrIns();

        /// <summary>
        /// IN设置
        /// </summary>
        public Setting_FBOrIns Setting_IN { get; set; } = new Setting_FBOrIns();

        public Setting_FBOrIns Setting_IN_RE { get; set; } = new Setting_FBOrIns();

        /// <summary>
        /// Ins设置
        /// </summary>
        public Setting_FBOrIns Setting_IG { get; set; } = new Setting_FBOrIns();

        public Setting_FBOrIns Setting_EM { get; set; } = new Setting_FBOrIns();

        // public Setting_FBOrIns Setting_CK { get; set; } = new Setting_FBOrIns();
    }

    public class WebProxyInfo
    {
        /// <summary>
        /// 是否使用代理
        /// </summary>
        public bool Proxy_IsUse { get; set; } = false;

        /// <summary>
        /// 代理类型 922
        /// </summary>
        public bool Proxy_Type_922 { get; set; } = false;

        /// <summary>
        /// 代理地址
        /// </summary>
        public string Proxy_Url { get; set; } = string.Empty;

        /// <summary>
        /// 代理用户名
        /// </summary>
        public string Proxy_UserName { get; set; } = string.Empty;

        /// <summary>
        /// 代理密码
        /// </summary>
        public string Proxy_Pwd { get; set; } = string.Empty;
    }

    public class Setting_FBOrIns
    {
        /// <summary>
        /// 任务信息列表
        /// </summary>
        [JsonIgnore]
        public List<ToolTaskInfo> TaskInfos { get; set; } = null;

        /// <summary>
        /// 域名信息列表
        /// </summary>
        public List<DomainInfo> DomainInfos { get; set; } = null;

        /// <summary>
        /// 并发线程数
        /// </summary>
        public int ThreadCountMax { get; set; } = 100;

        /// <summary>
        /// 账号列表
        /// </summary>
        public List<Account_FBOrIns> Account_List { get; set; } = null;
        /// <summary>
        /// 密码列表
        /// </summary>
        public List<PassWordInfo> Password_List { get; set; } = null;

        /// <summary>
        /// 用于绑定FB的GM邮箱账号列表
        /// </summary>
        public List<MailInfo> Mail_ForBind_List { get; set; } = null;

        /// <summary>
        /// 列表读写锁
        /// </summary>
        public object Lock_Mail_ForBind_List { get; set; } = new object();

        /// <summary>
        /// 全局代理设置
        /// </summary>
        public WebProxyInfo Global_WebProxyInfo { get; set; } = new WebProxyInfo();

        /// <summary>
        /// 执行流程列表
        /// </summary>
        public List<TaskInfo> TaskInfoList { get; set; } = null;

        /// <summary>
        /// 忘记密码_前缀模式
        /// 0:随机10位,1:自定义
        /// </summary>
        public int ForgotPwdSetting_Front_Mode { get; set; } = 0;

        /// <summary>
        /// 自定义前缀内容
        /// </summary>
        public string ForgotPwdSetting_Front_Custom_Content { get; set; } = string.Empty;

        /// <summary>
        /// 忘记密码_是否加日期后缀(MMdd)
        /// </summary>
        public bool ForgotPwdSetting_After_IsAddDate { get; set; } = false;

        /// <summary>
        /// 提取密码
        /// </summary>
        public bool IsGetPassword { get; set; } = false;

        /// <summary>
        /// 协议
        /// </summary>
        public bool Protocol { get; set; } = false;

        /// <summary>
        /// Selenium
        /// </summary>
        public bool Selenium { get; set; } = false;

        /// <summary>
        /// ADSPower
        /// </summary>
        public bool ADSPower { get; set; } = false;

        /// <summary>
        /// BitBrowser
        /// </summary>
        public bool BitBrowser { get; set; } = false;

        /// <summary>
        /// RecoveryEmail
        /// </summary>
        public bool RecoveryEmail { get; set; } = false;

        /// <summary>
        /// SmsActivate
        /// </summary>
        public bool SmsActivate { get; set; } = false;

        /// <summary>
        /// FiveSim
        /// </summary>
        public bool FiveSim { get; set; } = false;

        /// <summary>
        /// Country
        /// </summary>
        public string Country { get; set; } = string.Empty;
        /// <summary>
        /// TokenSms
        /// </summary>
        public string TokenSms { get; set; } = string.Empty;

        /// <summary>
        /// TokenSis
        /// </summary>
        public string TokenSis { get; set; } = string.Empty;

        /// <summary>
        /// SmsBalance
        /// </summary>
        public string SmsBalance { get; set; } = string.Empty;

        /// <summary>
        /// SisBalance
        /// </summary>
        public string SisBalance { get; set; } = string.Empty;

        /// <summary>
        /// 是否注销当前会话
        /// </summary>
        public bool ForgotPwdSetting_LogMeOut { get; set; } = false;

        /// <summary>
        /// 原数据的读写锁
        /// </summary>
        [JsonIgnore]
        public object LockObj_TongJi_Real { get; set; } = new object();

        /// <summary>
        /// 当前使用的统计信息类型
        /// </summary>
        public int TongJi_UseType { get; set; } = 0;

        /// <summary>
        /// 当前使用的统计信息
        /// </summary>
        public TongJi_FBOrIns TongJi_Real { get; set; } = new TongJi_FBOrIns();

        /// <summary>
        /// 当前使用的统计信息
        /// </summary>
        public TongJi_FBOrIns TongJi_False { get; set; } = new TongJi_FBOrIns();
    }
    public class PassWordInfo
    {
        //public string MatchDomainName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string PassWord { get; set; } = string.Empty;
        public string URL { get; set; } = string.Empty;
    }
    /// <summary>
    /// 统计数据
    /// </summary>
    public class TongJi_FBOrIns
    {
        public int WanChengShu { get; set; } = 0;
        public int FengHaoShu { get; set; } = 0;
        public int WuXiao { get; set; } = 0;
        public int YanZhengYouXiang { get; set; } = 0;
        public int YanZhengSheBei { get; set; } = 0;
        public int QiTaCuoWu { get; set; } = 0;
    }
}