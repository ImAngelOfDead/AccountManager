using Amib.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.Net;

namespace AccountManager.Models
{
    public class Account_FBOrIns
    {
        #region 账号分类字段
        /// <summary>
        /// 账号ID
        /// </summary>
        public string Account_Id { get; set; } = string.Empty;
        public string csrfToken { get; set; } = string.Empty;
        /// <summary>
        /// 账号分类字段
        /// </summary>
        public string Account_Type_Des { get; set; } = string.Empty;

        #endregion

        #region 原邮箱信息

        /// <summary>
        /// 旧邮箱CK
        /// </summary>
        public string Old_Mail_CK { get; set; } = string.Empty;

        /// <summary>
        /// 旧邮箱名称
        /// </summary>
        public string Old_Mail_Name { get; set; } = string.Empty;

        /// <summary>
        /// 旧邮箱密码
        /// </summary>
        public string Old_Mail_Pwd { get; set; } = string.Empty;

        #endregion

        #region 绑定邮箱数据

        /// <summary>
        /// 辅助邮箱
        /// </summary>
        public string Recovery_Email { get; set; } = string.Empty;   
        /// <summary>
        /// 辅助邮箱密码
        /// </summary>
        public string Recovery_Email_Password { get; set; } = string.Empty;      
        /// <summary>
        /// 新绑定的邮箱名
        /// </summary>
        public string New_Mail_Name { get; set; } = string.Empty;

        /// <summary>
        /// 新绑定的邮箱pwd
        /// </summary>
        public string New_Mail_Pwd { get; set; } = string.Empty;

        /// <summary>
        /// Facebook_CK
        /// </summary>
        public string Facebook_CK { get; set; } = string.Empty;

        /// <summary>
        /// Facebook_Pwd
        /// </summary>
        public string Facebook_Pwd { get; set; } = string.Empty;

        #endregion
        #region RU邮箱

        /// <summary>
        /// 新绑定的邮箱名
        /// </summary>
        public string RU_Mail_Name { get; set; } = string.Empty;

        /// <summary>
        /// 新绑定的邮箱pwd
        /// </summary>
        public string RU_Mail_Pwd { get; set; } = string.Empty;

        #endregion

        #region UA和代理信息

        [JsonIgnore] public WebProxy WebProxy { get; set; } = null;

        /// <summary>
        /// UserAgent
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;

        #endregion

        #region 登录数据

        /// <summary>
        /// 登录数据
        /// </summary
        public LoginInfo_FBOrIns LoginInfo { get; set; } = null;

        #endregion

        #region Selenium相关

        /// <summary>
        /// ads 操作id
        /// </summary
        public string user_id { get; set; } = null;      
        /// <summary>
        /// 注册方式
        /// </summary
        public bool IsAuthorization { get; set; } = false;

        /// <summary>
        /// selenium 
        /// </summary
        public string selenium { get; set; } = null;

        /// <summary>
        /// webdriver 
        /// </summary
        public string webdriver { get; set; } = null;

        #endregion

        #region 2FA等各个动作的状态

        /// <summary>
        /// C_User
        /// </summary>
        public string C_User
        {
            get
            {
                string des = string.Empty;
                if (this.LoginInfo != null) des = this.LoginInfo.LoginData_Account_Id.Trim();
                return des;
            }
        }

        /// <summary>
        /// 2FA开启状态(-1:待查,0:关,1:开)
        /// </summary>
        public int TwoFA_Dynamic_Status { get; set; } = -1;

        /// <summary>
        /// 2FA开启状态
        /// </summary>
        public string TwoFA_Dynamic_StatusDes
        {
            get
            {
                string des = "待查";
                if (this.TwoFA_Dynamic_Status == 0) des = "关";
                else if (this.TwoFA_Dynamic_Status == 1) des = "开";
                return des;
            }
        }

        /// <summary>
        /// 2FA密钥
        /// </summary>
        public string TwoFA_Dynamic_SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// 电话号码
        /// </summary>
        public string Phone_Num { get; set; } = string.Empty;

        /// <summary>
        /// 接码地址
        /// </summary>
        public string Phone_Num_Url { get; set; } = string.Empty;

        #endregion

        #region 操作信息

        /// <summary>
        /// 工作线程对象
        /// </summary>
        [JsonIgnore]
        public IWorkItemResult WorkItemResult { get; set; } = null;

        /// <summary>
        /// 工作线程对象
        /// </summary>
        [JsonIgnore]
        public IWorkItemsGroup WorkItemsGroup { get; set; } = null;

        /// <summary>
        /// 操作日志信息
        /// </summary>
        public string Running_Log { get; set; } = string.Empty;

        /// <summary>
        /// 是否在工作中
        /// </summary>
        [JsonIgnore]
        public bool Running_IsWorking
        {
            get
            {
                bool isWorking = this.WorkItemResult != null && !this.WorkItemResult.IsCanceled &&
                                 !this.WorkItemResult.IsCompleted;
                return isWorking;
            }
        }

        /// <summary>
        /// 运行状态
        /// </summary>
        [JsonIgnore]
        public string Running_Status_Des
        {
            get
            {
                string des;
                if (this.WorkItemResult == null) des = "未运行";
                else if (this.WorkItemResult.IsCanceled) des = "已取消";
                else if (this.WorkItemResult.IsCompleted) des = "已完成";
                else des = "运行中";
                return des;
            }
        }

        /// <summary>
        /// 操作结果
        /// </summary>
        [JsonIgnore]
        public JObject Jo_TaskResult { get; set; } = null;

        #endregion

        #region 任务操作记录

        /// <summary>
        /// 记录_删除Ins关联
        /// </summary>
        public string Log_RemoveInsAccount { get; set; } = string.Empty;

        /// <summary>
        /// 记录_删除其它登录会话
        /// </summary>
        public string Log_LogOutOfOtherSession { get; set; } = string.Empty;

        /// <summary>
        /// 记录_删除信任设备
        /// </summary>
        public string Log_TwoFactorRemoveTrustedDevice { get; set; } = string.Empty;

        /// <summary>
        /// 记录_删除联系方式
        /// </summary>
        public string Log_DeleteOtherContacts { get; set; } = string.Empty;

        #endregion

        #region 查询信息

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName
        {
            get
            {
                string des = string.Empty;
                if (this.LoginInfo != null) des = this.LoginInfo.LoginData_UserName.Trim();
                return des;
            }
        }

        /// <summary>
        /// 关注
        /// </summary>
        public string GuanZhuCount { get; set; } = string.Empty;

        /// <summary>
        /// 广告权限
        /// </summary>
        public string AdQuanXian { get; set; } = "/";

        /// <summary>
        /// 账单
        /// </summary>
        public string ZhangDan { get; set; } = "/";

        /// <summary>
        /// 余额
        /// </summary>
        public string YuE { get; set; } = "/";

        /// <summary>
        /// 专页数量
        /// </summary>
        public string ZhuanYe { get; set; } = "/";

        /// <summary>
        /// 商城
        /// </summary>
        public string ShangCheng { get; set; } = "/";

        /// <summary>
        /// 辅助词
        /// </summary>
        public string FuZhuCi { get; set; } = "/";

        /// <summary>
        /// 国家
        /// </summary>
        public string GuoJia { get; set; } = "/";

        /// <summary>
        /// 好友数量
        /// </summary>
        public string HaoYouCount { get; set; } = "/";

        /// <summary>
        /// 帖子数量
        /// </summary>
        public string TieZiCount { get; set; } = "/";

        /// <summary>
        /// 性别
        /// </summary>
        public string XingBie { get; set; } = "/";

        /// <summary>
        /// 生日
        /// </summary>
        public string ShengRi { get; set; } = "/";

        /// <summary>
        /// 注册日期
        /// </summary>
        public string ZhuCeRiQi { get; set; } = "/";

        /// <summary>
        /// 加粉字段
        /// </summary>
        public string FsdProfile { get; set; } = "/";

        /// <summary>
        /// 账户名称
        /// </summary>
        public string AccountName { get; set; } = "/";

        /// <summary>
        /// 是否认证
        /// </summary>
        public string Certification { get; set; } = "/";

        /// <summary>
        /// 指纹信息
        /// </summary>
        public string login_Fc { get; set; } = string.Empty;

        #endregion
    }

    /// <summary>
    /// 用户登录数据类
    /// </summary>
    public class LoginInfo_FBOrIns
    {
        /// <summary>
        /// 登录Cookie
        /// </summary>
        [JsonIgnore]
        public CookieCollection CookieCollection { get; set; } = new CookieCollection();

        /// <summary>
        /// 登录EmailCookie
        /// </summary>
        [JsonIgnore]
        public CookieCollection EmailCookieCollection { get; set; } = new CookieCollection();

        /// <summary>
        /// 登录Cookie
        /// </summary>
        [JsonIgnore]
        public string LoginInfo_CookieStr
        {
            get
            {
                string ckStr = string.Empty;
                //if (this.CookieCollection != null) ckStr = string.Join("; ", this.CookieCollection.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}; domain={c.Domain}"));

                if (this.CookieCollection != null)
                    ckStr = string.Join("; ", this.CookieCollection.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}"));
                return ckStr;
            }
        }

        /// <summary>
        /// 登录Cookie
        /// </summary>
        [JsonIgnore]
        public string LoginInfo_EmailCookieStr
        {
            get
            {
                string ckStr = string.Empty;
                //if (this.CookieCollection != null) ckStr = string.Join("; ", this.CookieCollection.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}; domain={c.Domain}"));

                if (this.EmailCookieCollection != null)
                    ckStr = string.Join("; ",
                        this.EmailCookieCollection.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}"));
                return ckStr;
            }
        }

        public string LoginData_Account_Id { get; set; } = string.Empty;
        public string LoginData_UserName { get; set; } = string.Empty;
    }
}