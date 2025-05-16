using AccountManager.Models;
using BookingService.Common;
using CsharpHttpHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using AccountManager.Common;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WinInetHelperCSharp;

namespace AccountManager.DAL
{
    /// <summary>
    /// FacebookAPI
    /// </summary>
    public class LinkedinService
    {
        private EmailService emailService = new EmailService();
        private CodingPlatformService codingPlatformService = new CodingPlatformService();

        /// <summary>
        /// 检测CK是否有效
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject IN_EmailByCookie(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["isNeedLoop"] = true;
            var emailAccount = string.Empty;
            if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
            account.LoginInfo.EmailCookieCollection =
                StringHelper.GetCookieCollectionByCookieJsonStr(account.Old_Mail_CK);

            //验证邮箱是否有效
            if (account.Old_Mail_CK.Contains(".google.com"))
            {
                emailAccount = emailService.Email_GetGoolge(GetCookie(account.Old_Mail_CK));
            }
            else if (account.Old_Mail_CK.Contains(".live.com") ||
                     account.Old_Mail_CK.StartsWith("DefaultAnchorMailbox"))
            {
                var outlookMailLoginInfo =
                    OutlookMailHelper.GetLoginInfo(account.Old_Mail_CK, account.UserAgent);
                account.LoginInfo.CookieCollection =
                    StringHelper.GetCookieCollectionByCookieJsonStr(outlookMailLoginInfo.Cookie_Json_Old);
                emailAccount = outlookMailLoginInfo.EmailAccount;
            }
            else if (account.Old_Mail_CK.Contains(".yahoo.com"))
            {
                emailAccount =
                    emailService.Email_GetYahoo(account.LoginInfo.LoginInfo_EmailCookieStr, account.UserAgent);
            }

            bool isSuccess = true;
            if (string.IsNullOrEmpty(emailAccount))
            {
                jo_Result["isNeedLoop"] = false;
                isSuccess = false;
            }
            else
            {
                account.Old_Mail_Name = emailAccount;
            }

            jo_Result["Success"] = isSuccess;
            jo_Result["ErrorMsg"] = isSuccess ? "Cookie有效" : "Cookie无效";

            return jo_Result;
        }

        public class Cookie
        {
            public string name { get; set; }
            public string value { get; set; }
            public string domain { get; set; }
            public string path { get; set; }
            public bool secure { get; set; }
            public bool httpOnly { get; set; }
            public string sameSite { get; set; }
            public long expiry { get; set; }
        }

        public static string GetCookie(string cookie)
        {
            try
            {
                var objs = JsonConvert.DeserializeObject<Cookie[]>(cookie);
                string tmp = "", _tmp = "";
                foreach (var obj in objs)
                {
                    var name = obj.name;
                    var value = obj.value;
                    if (obj.domain.Contains("mail.google.com"))
                    {
                        _tmp += $"{name}={value}; ";
                    }
                    else if (obj.domain == ".google.com")
                    {
                        tmp += $"{name}={value}; ";
                    }
                    // else if (obj.domain.EndsWith(".live") && (obj.path == "/" || obj.path == "/owa/0/"))
                    else if (obj.domain.Contains(".live"))
                    {
                        if (!tmp.Contains(value))
                        {
                            tmp += $"{name}={value}; ";
                        }
                    }
                    else if (obj.domain.Contains("yahoo.com"))
                    {
                        tmp += $"{name}={value}; ";
                    }
                    // else
                    // {
                    //     tmp += $"{name}={value}; ";
                    // }
                }

                var ck = tmp + _tmp;
                return ck;
            }
            catch (Exception ex)
            {
                return cookie;
            }
        }

        private static DllInvoke dllInvoke_Fingerprinting = null;


        private delegate IntPtr GetGather_JsonStr_Delegate(IntPtr pCsrf_oken);

        private static GetGather_JsonStr_Delegate getGather_JsonStr_Delegate = null;

        private static GetRandomStr_Delegate getRandomStr_Delegate = null;

        private delegate IntPtr GetRandomStr_Delegate();

        private static GetFingerprintData_JsonStr_Delegate getFingerprintData_JsonStr_Delegate = null;

        private delegate IntPtr GetFingerprintData_JsonStr_Delegate();

        public LinkedinService()
        {
            string dllName = $@"{Application.StartupPath}\Fingerprinting.dll";

            dllInvoke_Fingerprinting = new DllInvoke(dllName);

            if (File.Exists(dllName))
            {
                getFingerprintData_JsonStr_Delegate =
                    (GetFingerprintData_JsonStr_Delegate)dllInvoke_Fingerprinting.Invoke("GetFingerprintData_JsonStr",
                        typeof(GetFingerprintData_JsonStr_Delegate));
                getGather_JsonStr_Delegate =
                    (GetGather_JsonStr_Delegate)dllInvoke_Fingerprinting.Invoke("GetGather_JsonStr",
                        typeof(GetGather_JsonStr_Delegate));
                getEntrypt_ApfcDf_Delegate =
                    (GetEntrypt_ApfcDf_Delegate)dllInvoke_Fingerprinting.Invoke("GetEntrypt_ApfcDf",
                        typeof(GetEntrypt_ApfcDf_Delegate));
                getRandomStr_Delegate =
                    (GetRandomStr_Delegate)dllInvoke_Fingerprinting.Invoke("GetRandomStr",
                        typeof(GetRandomStr_Delegate));
            }
        }

        public JObject IN_Re()
        {
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            int timeSpan = 0;
            int timeCount = 0;
            int timeOut = 0;
            Account_FBOrIns account = new Account_FBOrIns();
            account.LoginInfo = new LoginInfo_FBOrIns();
            var randomUserAgent = StringHelper.CreateRandomUserAgent();

            #region 先访问目标页面

            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/signup";
            hi.UserAgent = randomUserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Allowautoredirect = true;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            Debug.Write("121221");

            #endregion

            #region 注册提交

            var csrfToken = string.Empty;
            if (account.LoginInfo.LoginInfo_CookieStr.Contains("JSESSIONID=\""))
            {
                csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=\"", "\";");
            }
            else
            {
                csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=", ";");
            }

            string login_Fc = String.Empty;
            if (string.IsNullOrEmpty(login_Fc))
            {
                login_Fc = this.login_Fc(null, randomUserAgent, null);
            }

            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/signup/api/cors/createAccount?csrfToken=" + csrfToken;
            hi.UserAgent = randomUserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Allowautoredirect = true;

            hi.Header.Add("Sec-Fetch-Site", "none");
            jo_postdata = new JObject();
            jo_postdata["firstName"] = "qwwqe";
            jo_postdata["lastName"] = "qweweq";
            jo_postdata["emailAddress"] = "kbeanpayii@rambler.ru";
            jo_postdata["password"] = "3729034Qf6V7K";
            jo_postdata["source"] = string.Empty;
            jo_postdata["redirectInfo"] = string.Empty;
            jo_postdata["invitationInfo"] = string.Empty;
            jo_postdata["sendConfirmationEmail"] = true;
            jo_postdata["apfc"] = login_Fc;

            hi.Postdata = jo_postdata.ToString();
            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            Debug.Write("121221");

            #endregion

            return null;
        }

        public JObject IN_ForgotPassword_choose(Account_FBOrIns account, string newPassword, ChromeDriver driverSet)
        {
            if (Program.setting.Setting_IN.Protocol)
            {
                return IN_ForgotPasswordProtocol(account, newPassword);
            }
            else
            {
                return IN_ForgotPasswordSelenium(account, newPassword, driverSet);
            }
        }

        /// <summary>
        /// 忘记密码
        /// </summary>
        /// <param name="account"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public JObject IN_ForgotPasswordSelenium(Account_FBOrIns account, string newPassword, ChromeDriver driverSet)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;

            AdsPowerService adsPowerService = null;
            openADS:
            if (Program.setting.Setting_IN.ADSPower)
            {
                account.Running_Log = "初始化ADSPower";
                adsPowerService = new AdsPowerService();
                if (!string.IsNullOrEmpty(account.user_id))
                {
                    try
                    {
                        account.Running_Log = "验证环境是否打开";
                        Thread.Sleep(3000);
                        var adsActiveBrowser = adsPowerService.ADS_ActiveBrowser(account.user_id);
                        if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                        {
                            var adsUserCreate =
                                adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                            account.user_id = adsUserCreate["data"]["id"].ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Write(e.Message);
                    }
                }
                else
                {
                    account.Running_Log = "获取user_id";
                    var adsUserCreate = adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                    account.user_id = adsUserCreate["data"]["id"].ToString();
                    account.Running_Log = "获取user_id=[" + account.user_id + "]";
                }

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
                if (adsStartBrowser["code"].ToString().Equals("-1"))
                {
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
                account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }


            try
            {
                var strSnowId = UUID.StrSnowId;

                ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
                if (driverSet == null)
                {
                    driverSet = chromeDriverSetting.GetDriverSetting("IN", strSnowId, account.selenium,
                        account.webdriver);
                }

                driverSet.Navigate().GoToUrl(
                    "https://www.linkedin.com/checkpoint/rp/request-password-reset?trk=guest_homepage-basic_nav-header-signin");
                Thread.Sleep(3000);
                account.Running_Log = "忘记密码:输入邮箱[" + account.Old_Mail_Name + "]";
                //输入邮箱
                if (CheckIsExists(driverSet,
                        By.Id("username")))
                {
                    driverSet.FindElement(By.Id("username"))
                        .SendKeys(account.Old_Mail_Name);
                }

                Thread.Sleep(3000);
                //点击下一步提交邮箱
                if (CheckIsExists(driverSet,
                        By.Id("reset-password-submit-button")))
                {
                    driverSet.FindElement(By.Id("reset-password-submit-button"))
                        .Click();
                }

                account.Running_Log = "忘记密码:点击下一步，获取邮箱验证码，提交邮箱";
                Thread.Sleep(3000);

                //打码
                bool expression = true;
                int ii = 0;
                do
                {
                    if (ii > 10)
                    {
                        break;
                    }

                    Thread.Sleep(3000);
                    if (!driverSet.PageSource.Contains("Let’s do a quick security check"))
                    {
                        expression = false;
                    }

                    if (driverSet.PageSource.Contains("Your noCAPTCHA user response code is missing or invalid."))
                    {
                        jo_Result["ErrorMsg"] = "忘记密码:打码失败";
                        return jo_Result;
                    }

                    ii++;
                } while (expression);


                //点击下一步提交邮箱验证码
                if (CheckIsExists(driverSet,
                        By.Id("input__email_verification_pin")))
                {
                    #region 获取邮箱验证码

                    Thread.Sleep(10000);
                    account.Running_Log = $"忘记密码:获取邮箱验证码";
                    var emailCode = string.Empty;
                    List<Mail> emailGetList = null;
                    if (account.Old_Mail_CK.Contains(".google.com"))
                    {
                        emailGetList =
                            emailService.Email_GetGoolgeList(GetCookie(account.Old_Mail_CK), account.UserAgent);
                    }
                    else if (account.Old_Mail_CK.Contains(".live.com") ||
                             account.Old_Mail_CK.StartsWith("DefaultAnchorMailbox"))
                    {
                        emailGetList =
                            OutlookMailHelper.Email_GetOutlookList(account.Old_Mail_CK,
                                account.UserAgent);
                    }
                    else if (account.Old_Mail_CK.Contains(".yahoo.com"))
                    {
                        emailGetList =
                            emailService.Email_GetYahooList(account.LoginInfo.LoginInfo_EmailCookieStr,
                                account.UserAgent);
                    }

                    if (emailGetList.Count > 0)
                    {
                        foreach (var item in emailGetList)
                        {
                            if (item.sender == "LinkedIn" || item.sender == "领英")
                            {
                                var htmlEmail = item.title;
                                string regexPattern = @"\b\d{6}\b";

                                Match match = Regex.Match(htmlEmail, regexPattern);

                                if (match.Success)
                                {
                                    emailCode = match.Value;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        jo_Result["ErrorMsg"] = "忘记密码:获取验证码失败";
                        return jo_Result;
                    }

                    if (string.IsNullOrEmpty(emailCode))
                    {
                        jo_Result["ErrorMsg"] = "忘记密码:验证码为空";
                        return jo_Result;
                    }

                    #endregion

                    driverSet.FindElement(By.Id("input__email_verification_pin"))
                        .SendKeys(emailCode);
                }
                else
                {
                    jo_Result["ErrorMsg"] = "忘记密码:打码失败";
                    return jo_Result;
                }

                account.Running_Log = "忘记密码:输入邮箱验证码";
                Thread.Sleep(3000);
                //点击下一步提交邮箱验证码
                if (CheckIsExists(driverSet,
                        By.Id("pin-submit-button")))
                {
                    driverSet.FindElement(By.Id("pin-submit-button"))
                        .Click();
                }

                account.Running_Log = "忘记密码:提交邮箱验证码";
                Thread.Sleep(3000);

                if (driverSet.PageSource.Contains(
                        "The verification code you entered isn’t valid. Please check the code and try again."))
                {
                    jo_Result["ErrorMsg"] = "忘记密码:验证码无效";
                    return jo_Result;
                }

                if (driverSet.PageSource.Contains("We need to make sure it’s really you."))
                {
                    jo_Result["ErrorMsg"] = "忘记密码:需要验证原邮箱";
                    return jo_Result;
                }

                if (driverSet.PageSource.Contains("Enter the code we’ve sent to phone number ending"))
                {
                    jo_Result["ErrorMsg"] = "忘记密码:账户已经被修改";
                    return jo_Result;
                }

                if (driverSet.PageSource.Contains("Check your LinkedIn app"))
                {
                    jo_Result["ErrorMsg"] = "忘记密码:验证Linkedin APP";
                    return jo_Result;
                }

                //newPassword
                if (CheckIsExists(driverSet,
                        By.Id("input__phone_verification_pin")))
                {
                    jo_Result["ErrorMsg"] = "忘记密码:二次验证手机";
                    return jo_Result;
                }

                //输入新密码
                if (CheckIsExists(driverSet,
                        By.Id("newPassword")))
                {
                    driverSet.FindElement(By.Id("newPassword"))
                        .SendKeys(newPassword);
                }
                else
                {
                    jo_Result["ErrorMsg"] = "忘记密码:跳转修改密码页面";
                    return jo_Result;
                }

                if (CheckIsExists(driverSet,
                        By.Id("confirmPassword")))
                {
                    driverSet.FindElement(By.Id("confirmPassword"))
                        .SendKeys(newPassword);
                }

                //
                //点击下一步提交密码
                if (CheckIsExists(driverSet,
                        By.Id("reset-password-submit-button")))
                {
                    driverSet.FindElement(By.Id("reset-password-submit-button"))
                        .Click();
                }

                Thread.Sleep(3000);
                if (!driverSet.PageSource.Contains("Your password has been changed"))
                {
                    jo_Result["ErrorMsg"] = "忘记密码:修改密码失败";
                    return jo_Result;
                }

                //进入主页
                driverSet.Navigate().GoToUrl(
                    "https://www.linkedin.com/nhome");
                Thread.Sleep(5000);
                if (!driverSet.Url.Contains("https://www.linkedin.com/feed/"))
                {
                    jo_Result["ErrorMsg"] = "忘记密码:跳转主页失败";
                    return jo_Result;
                }
                else
                {
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                bool isClose = false;
                TaskInfo task = null;
                string taskName = "BindNewEmail";
                task = Program.setting.Setting_IN.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
                if (task != null && task.IsSelected)
                {
                    if (!string.IsNullOrEmpty(jo_Result["ErrorMsg"].ToString()))
                    {
                        isClose = true;
                    }
                }
                else
                {
                    isClose = true;
                }

                if (isClose)
                {
                    adsPowerService.ADS_UserDelete(account.user_id);
                    try
                    {
                        driverSet?.Close();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }

                    try
                    {
                        driverSet?.Quit();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }

                    try
                    {
                        driverSet?.Dispose();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                }
            }

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = "忘记密码:操作成功";
            return jo_Result;
        }

        #region 判定元素是否存在

        public static bool CheckIsExists(ChromeDriver chromeDriver, By by)
        {
            try
            {
                int i = 0;
                while (i < 3)
                {
                    if (chromeDriver.FindElements(by).Count > 0)
                    {
                        break;
                    }
                    else
                    {
                        Thread.Sleep(2000);
                        i = i + 1;
                    }
                }

                return chromeDriver.FindElements(by).Count > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        #endregion

        /// <summary>
        /// 忘记密码
        /// </summary>
        /// <param name="account"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public JObject IN_ForgotPasswordProtocol(Account_FBOrIns account, string newPassword)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;

            if (!string.IsNullOrEmpty(account.Facebook_Pwd))
            {
                jo_Result["ErrorMsg"] = "忘记密码失败(已经有密码)";
                return jo_Result;
            }

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            int timeSpan = 0;
            int timeCount = 0;
            int timeOut = 0;


            #region 先访问目标页面

            account.Running_Log = $"忘记密码:进入目标页面(password/change)";
            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/checkpoint/rp/request-password-reset";
            hi.UserAgent = account.UserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Allowautoredirect = true;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            #endregion

            #region 获取API所需要的参数

            var csrfToken = string.Empty;
            if (account.LoginInfo.LoginInfo_CookieStr.Contains("JSESSIONID=\""))
            {
                csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=\"", "\";");
            }
            else
            {
                csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=", ";");
            }

            var pageInstance = StringHelper.GetMidStr(hr.Html, "<meta name=\"pageInstance\" content=\"", "\">");

            #endregion

            #region 提交忘记密码请求

            account.Running_Log = $"忘记密码:提交忘记密码请求";
            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/checkpoint/rp/request-password-reset-submit";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("origin", "https://www.linkedin.com");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded; charset=UTF-8";
            // string login_Fc = this.login_Fc(null, account.UserAgent, pageInstance);
            // account.login_Fc = login_Fc;

            jo_postdata = new JObject();
            jo_postdata["csrfToken"] = csrfToken;
            jo_postdata["pageInstance"] = pageInstance;
            jo_postdata["userName"] = account.Old_Mail_Name;
            jo_postdata["encryptedEmail"] = false;
            jo_postdata["fp_data"] = "default";
            // jo_postdata["apfc"] = login_Fc;

            hi.Postdata = string.Join("&",
                jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            if (string.IsNullOrEmpty(hr.RedirectUrl))
            {
                jo_Result["ErrorMsg"] = $"点击提交邮箱跳转失败";
                return jo_Result;
            }

            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            #endregion

            #region 跳转到打码

            account.Running_Log = $"忘记密码:跳转重定向到打码平台";
            hi = new HttpItem();
            hi.URL = hr.RedirectUrl;
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("origin", "https://www.linkedin.com");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "GET";
            hi.ContentType = $"application/x-www-form-urlencoded; charset=UTF-8";
            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            #endregion

            #region 平台打码

            var challengeData =
                StringHelper.GetMidStr(hr.Html, "id=\"securedDataExchange\" style=\"display: none\"><!--\"",
                    "\"--></code>");
            var taskByCapsolver = codingPlatformService.CreateTaskByCapsolver(challengeData, account);
            if (string.IsNullOrEmpty(taskByCapsolver))
            {
                jo_Result["ErrorMsg"] = "打码失败";
                return jo_Result;
            }

            int i = 0;
            bool capsolver = true;
            var taskResultByCapsolver = string.Empty;
            do
            {
                if (i > 10)
                {
                    break;
                }

                i++;
                taskResultByCapsolver = codingPlatformService.GetTaskResultByCapsolver(taskByCapsolver, account);
                if (string.IsNullOrEmpty(taskResultByCapsolver))
                {
                    Thread.Sleep(5000);
                }
                else
                {
                    capsolver = false;
                }
            } while (capsolver);

            if (string.IsNullOrEmpty(taskResultByCapsolver))
            {
                jo_Result["ErrorMsg"] = "打码平台返回失败";
                return jo_Result;
            }

            #endregion

            #region 提交打码

            account.Running_Log = $"忘记密码:提交打码";
            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/checkpoint/challenge/verify";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("origin", "https://www.linkedin.com");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded; charset=UTF-8";
            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            #region 准备参数

            jo_postdata = GetVerifyData(hr.Html);
            jo_postdata["captchaUserResponseToken"] = taskResultByCapsolver;
            hi.Postdata = string.Join("&",
                jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            #endregion

            hr = hh.GetHtml(hi);
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            if (string.IsNullOrEmpty(hr.RedirectUrl))
            {
                jo_Result["ErrorMsg"] = "提交打码失败";
                return jo_Result;
            }

            //跳转1次
            hi = new HttpItem();
            hi.URL = hr.RedirectUrl;
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("origin", "https://www.linkedin.com");
            hi.Allowautoredirect = true;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "GET";
            hi.ContentType = $"application/x-www-form-urlencoded; charset=UTF-8";
            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;
            hr = hh.GetHtml(hi);
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            html = hr.Html;
            if (string.IsNullOrEmpty(html))
            {
                jo_Result["ErrorMsg"] = "提交打码失败";
                return jo_Result;
            }

            if (html.Contains("Something unexpected happened. Please try again."))
            {
                jo_Result["ErrorMsg"] = "操作频繁";
                return jo_Result;
            }

            if (!html.Contains("Enter the 6-digit code"))
            {
                jo_Result["ErrorMsg"] = "跳转页面失败";
                return jo_Result;
            }

            #endregion

            #region 获取邮箱验证码

            Thread.Sleep(10000);
            account.Running_Log = $"忘记密码:获取邮箱验证码";
            var emailCode = string.Empty;
            List<Mail> emailGetList = null;
            if (account.Old_Mail_CK.Contains(".google.com"))
            {
                emailGetList = emailService.Email_GetGoolgeList(GetCookie(account.Old_Mail_CK), account.UserAgent);
            }
            else if (account.Old_Mail_CK.Contains(".live.com") ||
                     account.Old_Mail_CK.StartsWith("DefaultAnchorMailbox"))
            {
                emailGetList =
                    OutlookMailHelper.Email_GetOutlookList(account.Old_Mail_CK,
                        account.UserAgent);
            }
            else if (account.Old_Mail_CK.Contains(".yahoo.com"))
            {
                emailGetList =
                    emailService.Email_GetYahooList(account.LoginInfo.LoginInfo_EmailCookieStr, account.UserAgent);
            }

            if (emailGetList.Count > 0)
            {
                foreach (var item in emailGetList)
                {
                    if (item.sender == "LinkedIn" || item.sender == "领英")
                    {
                        var htmlEmail = item.title;
                        string regexPattern = @"\b\d{6}\b";

                        Match match = Regex.Match(htmlEmail, regexPattern);

                        if (match.Success)
                        {
                            emailCode = match.Value;
                            break;
                        }
                    }
                }
            }
            else
            {
                jo_Result["ErrorMsg"] = "获取验证码失败";
                return jo_Result;
            }

            if (string.IsNullOrEmpty(emailCode))
            {
                jo_Result["ErrorMsg"] = "验证码为空";
                return jo_Result;
            }

            #endregion

            #region 提交邮箱验证码

            Uri uri = new Uri(hr.ResponseUri);
            var origin = $"{uri.Scheme}://{uri.Host}";

            hi = new HttpItem();
            hi.URL = GetFormUrl(html, origin);
            hi.UserAgent = account.UserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("origin", "https://www.linkedin.com");
            hi.Allowautoredirect = false;
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded; charset=UTF-8";
            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            jo_postdata = GetVerifyData(hr.Html);
            jo_postdata["pin"] = emailCode;
            hi.Postdata = string.Join("&",
                jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));
            hr = hh.GetHtml(hi);
            html = hr.Html;
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            if (html.Contains("Please check the code and try again"))
            {
                jo_Result["ErrorMsg"] = "验证码无效";
                return jo_Result;
            }

            if (string.IsNullOrEmpty(hr.RedirectUrl))
            {
                jo_Result["ErrorMsg"] = "提交验证码失败";
                return jo_Result;
            }

            //提交验证码后跳转
            hi = new HttpItem();
            hi.URL = hr.RedirectUrl;
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("origin", "https://www.linkedin.com");
            hi.Allowautoredirect = true;

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            html = hr.Html;
            if (string.IsNullOrEmpty(hr.Html))
            {
                jo_Result["ErrorMsg"] = "提交验证码跳转失败";
                return jo_Result;
            }

            if (string.IsNullOrEmpty(html) ||
                html.Contains("Enter the code you see on your authenticator app"))
            {
                jo_Result["ErrorMsg"] = "账号开启短2FA验证|封号";
                return jo_Result;
            }

            if (string.IsNullOrEmpty(html) ||
                html.Contains("Access to your account has been temporarily restricted"))
            {
                jo_Result["ErrorMsg"] = "账号开启短2FA验证|封号";
                return jo_Result;
            }

            if (string.IsNullOrEmpty(html) ||
                html.Contains("Check your LinkedIn app"))
            {
                jo_Result["ErrorMsg"] = "账号开启短2FA验证|封号";
                return jo_Result;
            }

            if (string.IsNullOrEmpty(html) ||
                html.Contains("Enter the code we’ve sent to phone number ending with"))
            {
                jo_Result["ErrorMsg"] = "账号开启短2FA验证|封号";
                return jo_Result;
            }

            if (string.IsNullOrEmpty(html) ||
                html.Contains("Enter the 6-digit code"))
            {
                jo_Result["ErrorMsg"] = "账号开启短2FA验证|封号";
                return jo_Result;
            }

            #endregion

            #region 提交新密码

            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/checkpoint/rp/password-reset-submit";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("origin", "https://www.linkedin.com");
            hi.Allowautoredirect = true;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded; charset=UTF-8";
            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;


            jo_postdata = GetFormPostDataRespassword(html);
            jo_postdata["newPassword"] = newPassword;
            jo_postdata["confirmPassword"] = newPassword;
            hi.Postdata = string.Join("&",
                jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));
            hr = hh.GetHtml(hi);
            html = hr.Html;
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            if (!html.Contains("ve successfully reset your password. | LinkedIn"))
            {
                jo_Result["ErrorMsg"] = "忘记密码:操作失败";
                return jo_Result;
            }

            #endregion

            //Cookie刷新
            account.Facebook_CK = StringHelper.GetCookieJsonStrByCookieCollection(account.LoginInfo.CookieCollection);
            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = "忘记密码:操作成功";
            return jo_Result;
        }

        public static string GetFormUrl(string html, string origin = "")
        {
            string actionRegexPattern = @"<form.*?action=""(.*?)""";
            Regex actionRegex = new Regex(actionRegexPattern, RegexOptions.IgnoreCase);

            Match match = actionRegex.Match(html);
            string url = match.Success ? match.Groups[1].Value : string.Empty;

            if (!string.IsNullOrEmpty(origin) && url.StartsWith("/"))
            {
                url = origin + url;
            }

            return url;
        }

        /// <summary>
        /// 绑定新邮箱
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject IN_ChangeEmail_choose(Account_FBOrIns account, ChromeDriver driverSet)
        {
            if (Program.setting.Setting_IN.Protocol)
            {
                return IN_ChangeEmailProtocol(account);
            }
            else
            {
                return IN_ChangeEmailSelenium(account, driverSet);
            }
        }

        /// <summary>
        /// 切换主邮箱
        /// </summary>
        /// <param name="account"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public JObject IN_ChangeEmailSelenium(Account_FBOrIns account, ChromeDriver driverSet)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;

            AdsPowerService adsPowerService = null;
            openADS:
            if (Program.setting.Setting_IN.ADSPower)
            {
                account.Running_Log = "初始化ADSPower";
                adsPowerService = new AdsPowerService();
                if (!string.IsNullOrEmpty(account.user_id))
                {
                    try
                    {
                        account.Running_Log = "验证环境是否打开";
                        var adsActiveBrowser = adsPowerService.ADS_ActiveBrowser(account.user_id);
                        if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                        {
                            var adsUserCreate =
                                adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                            account.user_id = adsUserCreate["data"]["id"].ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Write(e.Message);
                    }
                }
                else
                {
                    account.Running_Log = "获取user_id";
                    var adsUserCreate = adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                    account.user_id = adsUserCreate["data"]["id"].ToString();
                    account.Running_Log = "获取user_id=[" + account.user_id + "]";
                }

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
                if (adsStartBrowser["code"].ToString().Equals("-1"))
                {
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
                account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }

            try
            {
                var strSnowId = UUID.StrSnowId;

                ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
                if (driverSet == null)
                {
                    driverSet = chromeDriverSetting.GetDriverSetting("IN", strSnowId, account.selenium,
                        account.webdriver);
                }


                driverSet.Navigate().GoToUrl(
                    "https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                Thread.Sleep(3000);
                if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
                {
                    loginLinkedin:

                    if (CheckIsExists(driverSet,
                            By.Id("password")))
                    {
                        driverSet.FindElement(By.Id("password"))
                            .SendKeys(account.Facebook_Pwd);
                    }

                    Thread.Sleep(3000);
                    //提交登录
                    if (CheckIsExists(driverSet,
                            By.CssSelector("[class='btn__primary--large from__button--floating']")))
                    {
                        driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                            .Click();
                    }

                    Thread.Sleep(3000);
                    driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                    Thread.Sleep(3000);
                    if (CheckIsExists(driverSet,
                            By.Id("password")))
                    {
                        goto loginLinkedin;
                    }

                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }
                }

                Thread.Sleep(3000);

                driverSet.SwitchTo().DefaultContent();
                Thread.Sleep(3000);

                IList<IWebElement> elements2 = driverSet.FindElements(By.TagName("iframe"));
                if (elements2.Count > 0)
                {
                    driverSet.SwitchTo().Frame(elements2[0]);
                }

                Thread.Sleep(3000);

                //输入邮箱
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='isPrimary tertiary-btn']")))
                {
                    var findElements = driverSet.FindElements(By.CssSelector("[class='isPrimary tertiary-btn']"));
                    foreach (var findElement in findElements)
                    {
                        var webElement = findElement.FindElement(By.CssSelector("[class='screen-reader-text']"));
                        if (webElement.Text.Equals(account.New_Mail_Name))
                        {
                            findElement.Click();
                            break;
                        }
                    }
                }

                Thread.Sleep(3000);


                //切换邮箱输入密码
                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password"))
                        .SendKeys(account.Facebook_Pwd);
                }

                Thread.Sleep(3000);

                //切换邮箱确认密码
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='submit-button btn']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='submit-button btn']"))
                        .Click();
                }

                Thread.Sleep(3000);

                // #region 获取邮箱验证码
                //
                // //点击下一步提交邮箱验证码
                // if (CheckIsExists(driverSet,
                //         By.Id("input__email_verification_pin")))
                // {
                //     Thread.Sleep(10000);
                //     account.Running_Log = $"忘记密码:获取邮箱验证码";
                //     var emailCode = string.Empty;
                //     List<Mail> emailGetList = null;
                //     if (account.Old_Mail_CK.Contains(".google.com"))
                //     {
                //         emailGetList =
                //             emailService.Email_GetGoolgeList(GetCookie(account.Old_Mail_CK), account.UserAgent);
                //     }
                //     else if (account.Old_Mail_CK.Contains(".live.com") ||
                //              account.Old_Mail_CK.StartsWith("DefaultAnchorMailbox"))
                //     {
                //         emailGetList =
                //             OutlookMailHelper.Email_GetOutlookList(account.Old_Mail_CK,
                //                 account.UserAgent);
                //     }
                //     else if (account.Old_Mail_CK.Contains(".yahoo.com"))
                //     {
                //         emailGetList =
                //             emailService.Email_GetYahooList(account.LoginInfo.LoginInfo_EmailCookieStr,
                //                 account.UserAgent);
                //     }
                //
                //     if (emailGetList.Count > 0)
                //     {
                //         foreach (var item in emailGetList)
                //         {
                //             if (item.sender == "LinkedIn" || item.sender == "领英")
                //             {
                //                 var htmlEmail = item.title;
                //                 string regexPattern = @"\b\d{6}\b";
                //
                //                 Match match = Regex.Match(htmlEmail, regexPattern);
                //
                //                 if (match.Success)
                //                 {
                //                     emailCode = match.Value;
                //                     break;
                //                 }
                //             }
                //         }
                //     }
                //     else
                //     {
                //         jo_Result["ErrorMsg"] = "获取验证码失败";
                //         return jo_Result;
                //     }
                //
                //     if (string.IsNullOrEmpty(emailCode))
                //     {
                //         jo_Result["ErrorMsg"] = "验证码为空";
                //         return jo_Result;
                //     }
                //
                //     driverSet.FindElement(By.Id("input__email_verification_pin"))
                //         .SendKeys(emailCode);
                // }
                //
                // account.Running_Log = "忘记密码:输入邮箱验证码";
                // Thread.Sleep(3000);
                // //点击下一步提交邮箱验证码
                // if (CheckIsExists(driverSet,
                //         By.Id("pin-submit-button")))
                // {
                //     driverSet.FindElement(By.Id("pin-submit-button"))
                //         .Click();
                // }
                //
                // account.Running_Log = "忘记密码:提交邮箱验证码";
                // Thread.Sleep(3000);
                //
                // #endregion
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                bool isClose = false;
                TaskInfo task = null;
                string taskName = "GetInfo";
                task = Program.setting.Setting_IN.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
                if (task != null && task.IsSelected)
                {
                    if (!string.IsNullOrEmpty(jo_Result["ErrorMsg"].ToString()))
                    {
                        isClose = true;
                    }
                }
                else
                {
                    isClose = true;
                }

                if (isClose)
                {
                    adsPowerService.ADS_UserDelete(account.user_id);
                    try
                    {
                        driverSet?.Close();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }

                    try
                    {
                        driverSet?.Quit();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }

                    try
                    {
                        driverSet?.Dispose();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                }
            }

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = "忘记密码:操作成功";
            return jo_Result;
        }

        /// <summary>
        /// 切换主邮箱
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject IN_ChangeEmailProtocol(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["IsMailUsed"] = false;
            bool isSuccess = false;
            bool isBinded = false;

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            int timeSpan = 0;
            int timeCount = 0;
            int timeOut = 0;

            #region 进入切换邮箱界面

            account.Running_Log = $"绑邮箱:进入切换邮箱界面";
            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/psettings/email?li_theme=light&openInMobileMode=true";
            hi.UserAgent = account.UserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Allowautoredirect = false;

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);
            html = hr.Html;
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            var csrfToken = string.Empty;
            if (account.LoginInfo.LoginInfo_CookieStr.Contains("JSESSIONID=\""))
            {
                csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=\"", "\";");
            }
            else
            {
                csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=", ";");
            }

            var pageInstance = StringHelper.GetMidStr(hr.Html, "\"pageInstance\":\"", "\"");

            var xx = StringHelper.GetMidStr(html, "<code id=\"emailData\" style=\"display: none;\"><!--", "--></code>");
            var info = JsonConvert.DeserializeObject<Info>(xx);
            string mailId, mailId_;
            mailId = info.data.Find((item) => item.email == account.New_Mail_Name).accountId;
            mailId_ = info.data.Find((item) => item.email == account.Old_Mail_Name).accountId;
            IN_VerifyEmailCode(account, csrfToken, pageInstance);

            #endregion

            #region 开始置换主邮箱

            account.Running_Log = $"置换主邮箱:开始置换主邮箱";
            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/psettings/email/setPrimary";
            hi.UserAgent = account.UserAgent;
            hi.Header.Add("authority", "www.linkedin.com");
            hi.Header.Add("accept-language", "en,en-US;q=0.9");
            hi.Header.Add("cache-control", "no-cache");
            hi.Header.Add("cookie", account.LoginInfo.LoginInfo_CookieStr);
            hi.Header.Add("origin", "https://www.linkedin.com");
            hi.Header.Add("pragma", "no-cache");
            hi.Header.Add("sec-ch-ua",
                "\"Chromium\";v=\"116\", \"Not)A;Brand\";v=\"24\", \"Google Chrome\";v=\"116\"");
            hi.Header.Add("sec-ch-ua-mobile", "?0");
            hi.Header.Add("sec-ch-ua-platform", "\"Windows\"");
            hi.Header.Add("sec-fetch-dest", "empty");
            hi.Header.Add("sec-fetch-mode", "cors");
            hi.Header.Add("sec-fetch-site", "same-origin");
            hi.Header.Add("x-li-page-instance", pageInstance);
            hi.Header.Add("x-requested-with", "XMLHttpRequest");
            hi.Allowautoredirect = true;
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded; charset=UTF-8";
            hi.Referer = "https://www.linkedin.com/";
            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            jo_postdata = new JObject();
            jo_postdata["csrfToken"] = csrfToken;
            jo_postdata["emailId"] = mailId;
            jo_postdata["handleUrn"] = "urn:li:emailAddress:" + mailId;
            jo_postdata["scope"] = "linkedin:member";
            jo_postdata["password"] = account.Facebook_Pwd;
            hi.Postdata = string.Join("&",
                jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            hr = hh.GetHtml(hi);
            html = hr.Html;
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            if (!html.Contains("200"))
            {
                jo_Result["ErrorMsg"] = "设置主邮箱失败";
                return jo_Result;
            }

            #endregion

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = "置换主邮箱:设置主邮箱成功";
            return jo_Result;
        }

        /// <summary>
        /// 获取信息
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject IN_GetInfo_choose(Account_FBOrIns account, ChromeDriver driverSet)
        {
            if (Program.setting.Setting_IN.Protocol)
            {
                return IN_GetInfoProtocol(account);
            }
            else
            {
                return IN_GetInfoSelenium(account, driverSet);
            }
        }

        public JObject VerifyPassword(Account_FBOrIns account, string password)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["IsMailUsed"] = false;
            bool isSuccess = false;
            bool isBinded = false;

            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            int timeSpan = 0;
            int timeCount = 0;
            int timeOut = 0;

            // #region 获取详细信息
            account.Running_Log = $"开始验证密码";
            string errorMsg = string.Empty;
            string csrfToken = string.Empty;
            if (account.LoginInfo.LoginInfo_CookieStr.Contains("JSESSIONID=\""))
            {
                csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=\"", "\";");
            }
            else
            {
                csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=", ";");
            }

            var strings = account.Facebook_Pwd.Split('|');
            foreach (var se in strings)
            {
                var oldpassword = se.Trim();
                WinInet_HttpHelper hh = new WinInet_HttpHelper();
                WinInet_HttpItem hi = new WinInet_HttpItem();
                WinInet_HttpResult hr = null;

                hi.Url = $"https://www.linkedin.com/mysettings-api/settingsApiPassword?action=updatePassword";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"application/vnd.linkedin.normalized+json+2.1";
                hi.AcceptEncoding = "gzip, deflate, br";
                hi.AcceptLanguage = "zh-CN,zh;q=0.9";

                hi.OtherHeaders.Add(new WinInet_Header() { Name = "X-RestLi-Protocol-Version", Value = "2.0.0" });
                hi.OtherHeaders.Add(new WinInet_Header() { Name = "csrf-token", Value = csrfToken });
                password = string.IsNullOrEmpty(password) ? oldpassword : password;
                hi.HttpMethod = WinInet_HttpMethod.POST;
                hi.ContentType = "application/json; charset=UTF-8";
                hi.PostData = "{\"oldPassword\":\"" + oldpassword + "\",\"newPassword\":\"" +
                              password + "\",\"shouldSignOutOfAllSessions\":true}";

                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                hr = hh.GetHtml(hi);

                JObject joRet = null;
                try
                {
                    joRet = JObject.Parse(hr.HtmlString);
                }
                catch
                {
                }

                if (joRet == null || joRet.SelectToken("data.$type") == null) errorMsg = hr.HtmlString;
                else if (joRet.SelectToken("data.value.error") != null)
                {
                    errorMsg = string.Join(",", joRet.SelectToken("data.value.error").Select(jt => jt.Value<string>()));
                    if (errorMsg == "OLD_PASSWORD_IS_INCORRECT")
                    {
                        errorMsg = "原密码错误";
                    }
                    else if (errorMsg == "NEW_PASSWORD_MATCHES_NORMALIZED_OLD_PASSWORD")
                    {
                        account.Facebook_Pwd = oldpassword;
                        errorMsg = "原密码正确，新旧密码相同";
                    }

                    jo_Result["Success"] = true;
                    jo_Result["ErrorMsg"] = errorMsg;
                    return jo_Result;
                }
                else
                {
                    account.Facebook_Pwd = password;
                    jo_Result["Success"] = true;
                    jo_Result["ErrorMsg"] = "原密码正确，修改密码成功";
                }
            }


            return jo_Result;
        }

        //生成一个新的密码
        private string GetNewPassword_IN()
        {
            string newPwd = string.Empty;
            if (Program.setting.Setting_IN.ForgotPwdSetting_Front_Mode == 0)
                newPwd = StringHelper.GetRandomString(true, true, true, true, 10, 10);
            else newPwd = Program.setting.Setting_IN.ForgotPwdSetting_Front_Custom_Content.Trim();

            if (Program.setting.Setting_IN.ForgotPwdSetting_After_IsAddDate)
                newPwd += $"{DateTime.Now.ToString("MMdd")}";

            return newPwd;
        }

        /// <summary>
        /// 获取信息
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject IN_GetInfoSelenium(Account_FBOrIns account, ChromeDriver driverSet)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;

            AdsPowerService adsPowerService = null;
            openADS:
            if (Program.setting.Setting_IN.ADSPower)
            {
                account.Running_Log = "初始化ADSPower";
                adsPowerService = new AdsPowerService();
                if (!string.IsNullOrEmpty(account.user_id))
                {
                    try
                    {
                        account.Running_Log = "验证环境是否打开";
                        Thread.Sleep(3000);
                        var adsActiveBrowser = adsPowerService.ADS_ActiveBrowser(account.user_id);
                        if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                        {
                            var adsUserCreate =
                                adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                            account.user_id = adsUserCreate["data"]["id"].ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Write(e.Message);
                    }
                }
                else
                {
                    account.Running_Log = "获取user_id";
                    var adsUserCreate = adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                    account.user_id = adsUserCreate["data"]["id"].ToString();
                    account.Running_Log = "获取user_id=[" + account.user_id + "]";
                }

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
                if (adsStartBrowser["code"].ToString().Equals("-1"))
                {
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
                account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }

            try
            {
                var strSnowId = UUID.StrSnowId;
                account.Running_Log = "初始化ChromeDriver";
                ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
                if (driverSet == null)
                {
                    driverSet = chromeDriverSetting.GetDriverSetting("IN", strSnowId, account.selenium,
                        account.webdriver);
                }

                account.Running_Log = "开始执行查询操作";
                driverSet.Navigate().GoToUrl(
                    "https://www.linkedin.com/mynetwork/invite-connect/connections/");
                Thread.Sleep(3000);
                if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
                {
                    jo_Result["ErrorMsg"] = "Cookie失效";
                    return jo_Result;
                }

                account.Running_Log = $"获取信息:获取好友信息";
                account.HaoYouCount =
                    StringHelper.GetMidStr(driverSet.PageSource, "totalResultCount\":", ",\"secondaryFilterCluster");
                if (driverSet.PageSource.Contains("urn:li:fsd_profile:"))
                {
                    account.Running_Log = $"获取信息:FsdProfile";
                    account.FsdProfile =
                        StringHelper.GetMidStr(driverSet.PageSource, $"urn:li:fsd_profile:", $"\"");
                    var accountName = StringHelper.GetMidStr(driverSet.PageSource, "https://www.linkedin.com/in/",
                        "/recent-activity");
                    account.Running_Log = $"获取信息:AccountName";
                    account.AccountName = "https://www.linkedin.com/in/" + accountName + "/";
                }

                driverSet.Navigate().GoToUrl(account.AccountName);
                Thread.Sleep(3000);

                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='text-body-small inline t-black--light break-words']")))
                {
                    account.GuoJia = driverSet
                        .FindElement(By.CssSelector("[class='text-body-small inline t-black--light break-words']"))
                        .Text;
                }

                driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/verifications");
                Thread.Sleep(3000);
                if (driverSet.PageSource.Contains("These are your verifications. You can delete them at any time"))
                {
                    account.Certification = "已认证";
                }

                driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-data-and-activity");
                Thread.Sleep(3000);
                driverSet.SwitchTo().DefaultContent();
                IList<IWebElement> elements2 = driverSet.FindElements(By.TagName("iframe"));
                if (elements2.Count > 0)
                {
                    driverSet.SwitchTo().Frame(elements2[0]);
                }

                Thread.Sleep(3000);

                //pagination-link
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='pagination-link']")))
                {
                    var readOnlyCollection = driverSet
                        .FindElements(By.CssSelector("[class='pagination-link']"));
                    for (var i = readOnlyCollection.Count - 1; i >= 0; i--)
                    {
                        readOnlyCollection[i].Click();
                        Thread.Sleep(3000);
                        break;
                    }
                }

                Thread.Sleep(3000);

                string pattern = @"<span class=""date"">(.*?)</span>";

                MatchCollection matches = Regex.Matches(driverSet.PageSource, pattern);
                string year = "";
                foreach (Match match in matches)
                {
                    if (match.Groups[1].Success)
                    {
                        year = match.Groups[1].Value;
                    }
                }

                account.ZhuCeRiQi = year;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                adsPowerService.ADS_UserDelete(account.user_id);
                try
                {
                    driverSet?.Close();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }

                try
                {
                    driverSet?.Quit();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }

                try
                {
                    driverSet?.Dispose();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = "获取信息:操作成功";
            return jo_Result;
        }

        static int CountWordOccurrences(string text, string word)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(word, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += word.Length;
            }

            return count;
        }

        /// <summary>
        /// 获取信息
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject IN_GetInfoProtocol(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["IsMailUsed"] = false;
            bool isSuccess = false;
            bool isBinded = false;

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            int timeSpan = 0;
            int timeCount = 0;
            int timeOut = 0;

            // #region 获取详细信息
            account.Running_Log = $"获取信息:获取详细信息";
            //获取邮箱
            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/psettings/email?li_theme=light&openInMobileMode=true";
            // hi.URL = $"https://www.linkedin.com/mypreferences/d/manage-email-addresses";
            hi.UserAgent = account.UserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Allowautoredirect = true;
            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
            // hi.Cookie = cookie;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);
            html = hr.Html;
            if (html.Contains("Primary email"))
            {
                account.Old_Mail_Name = StringHelper.GetMidStr(html,
                    $"Primary email</h3><p class=\"email config-setting__label-text\">", "</p></li><li class=\"");
            }
            else
            {
                account.Old_Mail_Name = String.Empty;
                jo_Result["ErrorMsg"] = "账号失效";
                return jo_Result;
            }

            if (!string.IsNullOrEmpty(account.Old_Mail_Name))
            {
                //合并CK
                if (!string.IsNullOrEmpty(hr.Cookie))
                {
                    string csrfToken = string.Empty;

                    if (hr.Cookie.Contains("JSESSIONID=\"")) csrfToken = StringHelper.GetMidStr(hr.Cookie, "JSESSIONID=\"", "\";");
                    else csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=", ";");

                    account.csrfToken = csrfToken;
                }
                    

                hi = new HttpItem();
                hi.URL = $"https://www.linkedin.com/mynetwork/invite-connect/connections";
                hi.UserAgent = account.UserAgent;
                hi.Accept =
                    $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
                hi.Allowautoredirect = true;

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);
                html = hr.Html;

                account.Running_Log = $"获取信息:获取好友信息";
                if (html.Contains("totalConnectionsCount"))
                {
                    account.HaoYouCount = StringHelper.GetMidStr(html,
                        $"totalConnectionsCount\\\",\\\"key\\\":{{\\\"$type\\\":\\\"proto.sdui.Key\\\",\\\"value\\\":{{\\\"$case\\\":\\\"id\\\",\\\"id\\\":\\\"totalConnectionsCount\\\"}}}},\\\"namespace\\\":\\\"\\\"}},\\\"value\\\":{{\\\"$case\\\":\\\"intValue\\\",\\\"intValue\\\":",
                        "}");
                }

                if (account.HaoYouCount.Contains("/") || string.IsNullOrEmpty(account.HaoYouCount))
                {
                    account.HaoYouCount = StringHelper.GetMidStr(html, "totalResultCount&quot;:", ",&quot;");
                }

                if (string.IsNullOrEmpty(account.HaoYouCount))
                {
                    account.HaoYouCount = StringHelper.GetMidStr(html, "\"toasts-title\">", "notifications</h2>");
                }


                //j检查是否打过连接
                hi = new HttpItem();
                hi.URL = $"https://www.linkedin.com/mynetwork/invitation-manager/sent/";
                // hi.URL = $"https://www.linkedin.com/mypreferences/d/manage-email-addresses";
                hi.UserAgent = account.UserAgent;
                hi.Accept =
                    $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
                hi.Allowautoredirect = true;
                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
                // hi.Cookie = cookie;


                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);
                html = hr.Html;
                int count = 0;
                if (count == 0)
                {
                    count = CountWordOccurrences(html, "发送时间: 今天");
                }

                if (count == 0)
                {
                    count = CountWordOccurrences(html, "昨天");
                }

                if (count == 0)
                {
                    count = CountWordOccurrences(html, "yesterday");
                }

                if (count == 0)
                {
                    count = CountWordOccurrences(html, "今天送出");
                }

                if (count == 0)
                {
                    count = CountWordOccurrences(html, "Sent today");
                }

                if (count > 5)
                {
                    account.Account_Type_Des = "串号";
                }
                else if (!string.IsNullOrEmpty(account.Old_Mail_Name))
                {
                    account.Account_Type_Des = "正常";
                }
            }

            // account.HaoYouCount = StringHelper.GetMidStr(html, "&quot;totalResultCount&quot;:", ",&quot;");
            // if (html.Contains("urn:li:fsd_profile:"))
            // {
            //     account.Running_Log = $"获取信息:FsdProfile";
            //     account.FsdProfile = StringHelper.GetMidStr(html, $"urn:li:fsd_profile:", $"&quot;");
            //     var accountName = StringHelper.GetMidStr(html, "https://www.linkedin.com/in/", "/recent-activity");
            //     account.Running_Log = $"获取信息:AccountName";
            //     account.AccountName = "https://www.linkedin.com/in/" + accountName + "/";
            // }
            // else if (html.Contains("<meta name=\"voyager-web/config/environment\""))
            // {
            //     var csrfToken = string.Empty;
            //     if (account.LoginInfo.LoginInfo_CookieStr.Contains("JSESSIONID=\""))
            //     {
            //         csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=\"", "\";");
            //     }
            //     else
            //     {
            //         csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=", ";");
            //     }
            //
            // account.Running_Log = $"获取信息:获取主页";
            // hi = new HttpItem();
            // hi.URL = $"https://www.linkedin.com/feed/";
            // hi.UserAgent = account.UserAgent;
            // hi.Accept =
            //     $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            // hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            // hi.Allowautoredirect = false;
            //
            // //Cookie
            // hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
            //
            // //代理
            // if (account.WebProxy != null) hi.WebProxy = account.WebProxy;
            //
            // hr = hh.GetHtml(hi);
            // html = hr.Html;
            //
            // account.FsdProfile = StringHelper.GetMidStr(html, $"urn:li:fsd_profile:", $"\"");
            // var AccountName = StringHelper.GetMidStr(html, $"\"publicIdentifier\":\"", $"\"");
            // account.AccountName = "https://www.linkedin.com/in/" + AccountName + "/";
            //
            // account.Running_Log = $"获取信息:获取主页";
            // hi = new HttpItem();
            // hi.URL = $"https://www.linkedin.com/feed/";
            // hi.UserAgent = account.UserAgent;
            // hi.Accept =
            //     $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            // hi.Header.Add("Accept-Encoding", "gzip");
            // hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            // hi.Allowautoredirect = false;
            //
            // //Cookie
            // hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
            //
            // //代理
            // if (account.WebProxy != null) hi.WebProxy = account.WebProxy;
            //
            // hr = hh.GetHtml(hi);
            // html = hr.Html;
            //
            //     account.HaoYouCount = StringHelper.GetMidStr(html, $"totalConnectionsCount\\\",\\\"key\\\":{{\\\"$type\\\":\\\"proto.sdui.Key\\\",\\\"value\\\":{{\\\"$case\\\":\\\"id\\\",\\\"id\\\":\\\"totalConnectionsCount\\\"}}}},\\\"namespace\\\":\\\"\\\"}},\\\"value\\\":{{\\\"$case\\\":\\\"intValue\\\",\\\"intValue\\\":", "}");
            //
            //     hi = new HttpItem();
            //     hi.URL = $"https://www.linkedin.com/voyager/api/relationships/connectionsSummary";
            //     hi.UserAgent = account.UserAgent;
            //     hi.Accept =
            //         $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            //     hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            //     hi.Header.Add("csrf-token", csrfToken);
            //     hi.Allowautoredirect = false;
            //
            //     //Cookie
            //     hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
            //
            //     //代理
            //     if (account.WebProxy != null) hi.WebProxy = account.WebProxy;
            //
            //     hr = hh.GetHtml(hi);
            //     html = hr.Html;
            //
            //     account.HaoYouCount = StringHelper.GetMidStr(html, $"numConnections\":", "}");
            // }
            //
            // #endregion
            //
            // #region 查询国家
            //
            // account.Running_Log = $"获取信息:查询国家";
            // hi = new HttpItem();
            // hi.URL = account.AccountName;
            // hi.UserAgent = account.UserAgent;
            // hi.Accept =
            //     $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            // hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            // hi.Allowautoredirect = false;
            //
            // //Cookie
            // hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
            //
            // //代理
            // if (account.WebProxy != null) hi.WebProxy = account.WebProxy;
            //
            // hr = hh.GetHtml(hi);
            // html = hr.Html;
            // //合并CK
            // if (hr.Cookie != null)
            //     account.LoginInfo.CookieCollection =
            //         StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            //
            //
            // account.GuoJia = StringHelper.GetMidStr(html, "defaultLocalizedNameWithoutCountryName&quot;:&quot;",
            //     "&quot;");
            //
            // #endregion
            //
            // #region 是否认证
            //
            // account.Running_Log = $"获取信息:是否认证";
            // hi = new HttpItem();
            // hi.URL = $"https://www.linkedin.com/mypreferences/d/verifications";
            // hi.UserAgent = account.UserAgent;
            // hi.Accept =
            //     $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            // hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            // hi.Allowautoredirect = false;
            //
            // //Cookie
            // hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
            //
            // //代理
            // if (account.WebProxy != null) hi.WebProxy = account.WebProxy;
            //
            // hr = hh.GetHtml(hi);
            // html = hr.Html;
            // //合并CK
            // if (hr.Cookie != null)
            //     account.LoginInfo.CookieCollection =
            //         StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            //
            // if (html.Contains("These are your verifications. You can delete them at any time"))
            // {
            //     account.Certification = "已认证";
            // }
            //
            // #endregion

            // #region 注册日期
            //
            // account.Running_Log = $"获取信息:注册日期";
            // hi = new HttpItem();
            // hi.URL = $"https://www.linkedin.com/psettings/data-log?page=1&li_theme=light&openInMobileMode=true";
            // hi.UserAgent = account.UserAgent;
            // hi.Accept =
            //     $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            // hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            // hi.Allowautoredirect = false;
            //
            // //Cookie
            // hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
            //
            // //代理
            // if (account.WebProxy != null) hi.WebProxy = account.WebProxy;
            //
            // hr = hh.GetHtml(hi);
            // html = hr.Html;
            // //合并CK
            // if (hr.Cookie != null)
            //     account.LoginInfo.CookieCollection =
            //         StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            // string pattern = @"\?page=\d+";
            //
            // MatchCollection matches = Regex.Matches(html, pattern);
            // if (matches.Count > 0)
            // {
            //     var page = 1;
            //     foreach (Match match in matches)
            //     {
            //         page = Math.Max(Convert.ToInt32(match.Value.Replace("?page=", "")), page);
            //     }
            //
            //     hi = new HttpItem();
            //     hi.URL = "https://www.linkedin.com/psettings/data-log?page=" + page +
            //              "&li_theme=light&openInMobileMode=true";
            //     hi.UserAgent = account.UserAgent;
            //     hi.Accept =
            //         $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            //     hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            //     hi.Allowautoredirect = false;
            //
            //     //Cookie
            //     hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
            //
            //     //代理
            //     if (account.WebProxy != null) hi.WebProxy = account.WebProxy;
            //
            //     hr = hh.GetHtml(hi);
            //     html = hr.Html;
            // }
            //
            // pattern = @"<span class=""date"">(.*?)</span>";
            //
            // matches = Regex.Matches(html, pattern);
            // string year = "";
            // foreach (Match match in matches)
            // {
            //     if (match.Groups[1].Success)
            //     {
            //         year = match.Groups[1].Value;
            //     }
            // }
            //
            // account.ZhuCeRiQi = year;
            //
            // #endregion

            return jo_Result;
        }

        public JObject IN_VerifyEmailCode(Account_FBOrIns account, string csrfToken, string pageInstance)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["IsMailUsed"] = false;
            bool isSuccess = false;
            bool isBinded = false;

            DateTime sendCodeTime = DateTime.Parse("1970-01-01");
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            int timeSpan = 0;
            int timeCount = 0;
            int timeOut = 0;

            #region 判断是否需要邮箱验证

            account.Running_Log = $"绑邮箱:判断是否需要邮箱验证";
            hi = new HttpItem();
            hi.URL = "https://www.linkedin.com/psettings/challenge-recently-verified?{}";
            hi.UserAgent = account.UserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("csrf-token", csrfToken);
            hi.Header.Add("x-li-page-instance", pageInstance);
            hi.Allowautoredirect = false;
            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);
            html = hr.Html;
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            if (string.IsNullOrEmpty(html))
            {
                //验证接口跳转失败
                jo_Result["ErrorMsg"] = "验证接口跳转失败|封号";
                return jo_Result;
            }

            if (html.Contains("SESSION_UPDATE_NOT_REQUIRED"))
            {
                jo_Result["Success"] = true;
                jo_Result["ErrorMsg"] = "邮箱验证:操作成功";
                return jo_Result;
            }

            var challengeId = StringHelper.GetMidStr(html, "\"challengeId\":\"", "\"");

            account.Running_Log = $"绑邮箱:请求验证码";
            hi = new HttpItem();
            hi.URL = "https://www.linkedin.com/checkpoint/challengesV0/" + challengeId + "?{}";
            hi.UserAgent = account.UserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("csrf-token", csrfToken);
            hi.Header.Add("x-li-page-instance", pageInstance);
            hi.Allowautoredirect = false;
            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);
            html = hr.Html;
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            var challenge = JsonConvert.DeserializeObject<Challenge>(html);
            Thread.Sleep(5000);

            #region 查询邮箱验证码

            account.Running_Log = $"绑邮箱:查询邮箱验证码";
            var emailCode = string.Empty;
            List<Mail> emailGetList = null;
            if (account.Old_Mail_CK.Contains(".google.com"))
            {
                emailGetList = emailService.Email_GetGoolgeList(GetCookie(account.Old_Mail_CK), account.UserAgent);
            }
            else if (account.Old_Mail_CK.Contains(".live.com") ||
                     account.Old_Mail_CK.StartsWith("DefaultAnchorMailbox"))
            {
                emailGetList =
                    OutlookMailHelper.Email_GetOutlookList(account.Old_Mail_CK,
                        account.UserAgent);
            }
            else if (account.Old_Mail_CK.Contains(".yahoo.com"))
            {
                emailGetList =
                    emailService.Email_GetYahooList(account.LoginInfo.LoginInfo_EmailCookieStr, account.UserAgent);
            }

            Match match;
            if (emailGetList != null && emailGetList.Count > 0)
            {
                foreach (var item in emailGetList)
                {
                    if (item.sender == "LinkedIn" || item.sender == "领英")
                    {
                        var htmlEmail = item.title;
                        string regexPattern = @"\b\d{6}\b";

                        match = Regex.Match(htmlEmail, regexPattern);

                        if (match.Success)
                        {
                            emailCode = match.Value;
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(emailCode) && (!string.IsNullOrEmpty(account.Old_Mail_Name) &&
                                                    !string.IsNullOrEmpty(account.Old_Mail_Pwd)))
            {
                //pop 3 获取验证码

                #region 去邮箱提取验证码

                MailInfo mail = new MailInfo();
                mail.Mail_Name = account.Old_Mail_Name;
                mail.Mail_Pwd = account.Old_Mail_Pwd;
                account.Running_Log = $"绑邮箱:提取邮箱验证码[{mail.Mail_Name}]";
                timeSpan = 500;
                timeCount = 0;
                timeOut = 25000;
                List<Pop3MailMessage> msgList = null;
                Pop3MailMessage pop3MailMessage = null;
                while (pop3MailMessage == null && timeCount < timeOut)
                {
                    Thread.Sleep(timeSpan);
                    Application.DoEvents();
                    timeCount += timeSpan;

                    if (mail.Pop3Client != null && mail.Pop3Client.Connected)
                        try
                        {
                            mail.Pop3Client.Disconnect();
                        }
                        catch
                        {
                        }

                    mail.Pop3Client = Pop3Helper.GetPop3Client(mail.Mail_Name, mail.Mail_Pwd);
                    if (mail.Pop3Client == null) continue;

                    msgList = Pop3Helper.GetMessageByIndex(mail.Pop3Client);
                    pop3MailMessage = msgList.Where(m =>
                        m.DateSent >= sendCodeTime && m.Subject.Contains("here's your PIN") &&
                        m.From.Contains("<security-noreply@linkedin.com>")).FirstOrDefault();
                }

                if (mail.Pop3Client == null)
                {
                    jo_Result["ErrorMsg"] = "绑邮箱:Pop3连接失败";
                    jo_Result["IsMailUsed"] = true;
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                if (pop3MailMessage == null)
                {
                    jo_Result["ErrorMsg"] = "绑邮箱:没有找到指定的邮件信息";
                    jo_Result["IsMailUsed"] = true;
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                var htmlEmail = pop3MailMessage.Subject;
                string regexPattern = @"\b\d{6}\b";

                match = Regex.Match(htmlEmail, regexPattern);

                if (match.Success)
                {
                    emailCode = match.Value;
                }

                #endregion
            }

            if (string.IsNullOrEmpty(emailCode))
            {
                jo_Result["ErrorMsg"] = "验证码为空";
                return jo_Result;
            }

            #endregion

            account.Running_Log = $"绑邮箱:提交验证码";
            hi = new HttpItem();
            hi.URL = "https://www.linkedin.com/checkpoint/challengesV0/verify";
            hi.UserAgent = account.UserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("X-Requested-With", "XMLHttpRequest");
            hi.Header.Add("csrf-token", csrfToken);
            hi.Header.Add("x-li-page-instance", pageInstance);
            hi.Header.Add("accept-language", "en,en-US;q=0.9");
            hi.Header.Add("cache-control", "no-cache");
            hi.Header.Add("pragma", "no-cache");
            hi.Header.Add("Origin", "https://www.linkedin.com");
            hi.Referer = $"https://www.linkedin.com/";
            hi.Header.Add("sec-ch-ua", "\"Chromium\";v=\"122\", \"Not)A;Brand\";v=\"24\", \"Google Chrome\";v=\"122\"");
            hi.Header.Add("sec-ch-ua-mobile", "?0");
            hi.Header.Add("sec-ch-ua-platform", "\"Windows\"");
            hi.Header.Add("sec-fetch-dest", "empty");
            hi.Header.Add("sec-fetch-mode", "cors");
            hi.Header.Add("sec-fetch-site", "same-origin");
            hi.Header.Add("x-isajaxform", "1");
            hi.Header.Add("x-requested-with", "XMLHttpRequest");
            hi.ContentType = $"application/json";
            hi.Allowautoredirect = false;
            hi.Method = "POST";

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            jo_postdata = new JObject();
            jo_postdata["challengeId"] = challenge.challengeId;
            jo_postdata["displayTime"] = challenge.displayTime;
            jo_postdata["challengeType"] = challenge.challengeType;
            jo_postdata["challengeSource"] = challenge.challengeSource;
            jo_postdata["challengeData"] = challenge.encryptedChallengeViewData;
            jo_postdata["challengeDetails"] = challenge.challengeDetails;
            jo_postdata["pin"] = emailCode;
            jo_postdata["requestSubmissionId"] = challenge.requestSubmissionId;
            hi.Postdata = jo_postdata.ToString();

            hr = hh.GetHtml(hi);
            html = hr.Html;
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            if (html.Contains("200"))
            {
                jo_Result["Success"] = true;
                return jo_Result;
            }
            else
            {
                jo_Result["ErrorMsg"] = "邮箱验证:操作失败";
                return jo_Result;
            }

            #endregion
        }

        /// <summary>
        /// 绑定新邮箱
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject IN_BindNewEmail_choose(Account_FBOrIns account, MailInfo mail, ChromeDriver driverSet)
        {
            if (Program.setting.Setting_IN.Protocol)
            {
                return IN_BindNewEmailProtocol(account, mail);
            }
            else
            {
                return IN_BindNewEmailSelenium(account, mail, driverSet);
            }
        }

        /// <summary>
        /// 绑定新邮箱
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject IN_BindNewEmailSelenium(Account_FBOrIns account, MailInfo mail, ChromeDriver driverSet)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            DateTime sendCodeTime = DateTime.Parse("1970-01-01");
            var timeSpan = 500;
            var timeCount = 0;
            var timeOut = 25000;
            List<Pop3MailMessage> msgList;
            Pop3MailMessage pop3MailMessage;
            AdsPowerService adsPowerService = null;
            openADS:
            if (Program.setting.Setting_IN.ADSPower)
            {
                account.Running_Log = "初始化ADSPower";
                adsPowerService = new AdsPowerService();
                if (!string.IsNullOrEmpty(account.user_id))
                {
                    try
                    {
                        account.Running_Log = "验证环境是否打开";
                        Thread.Sleep(3000);
                        var adsActiveBrowser = adsPowerService.ADS_ActiveBrowser(account.user_id);
                        if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                        {
                            var adsUserCreate =
                                adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                            account.user_id = adsUserCreate["data"]["id"].ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Write(e.Message);
                    }
                }
                else
                {
                    account.Running_Log = "获取user_id";
                    var adsUserCreate = adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                    account.user_id = adsUserCreate["data"]["id"].ToString();
                    account.Running_Log = "获取user_id=[" + account.user_id + "]";
                }

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
                if (adsStartBrowser["code"].ToString().Equals("-1"))
                {
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
                account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }


            try
            {
                var strSnowId = UUID.StrSnowId;

                ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
                if (driverSet == null)
                {
                    driverSet = chromeDriverSetting.GetDriverSetting("IN", strSnowId, account.selenium,
                        account.webdriver);
                }


                driverSet.Navigate().GoToUrl(
                    "https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                Thread.Sleep(3000);

                driverSet.SwitchTo().DefaultContent();
                Thread.Sleep(3000);
                IList<IWebElement> elements2 = driverSet.FindElements(By.TagName("iframe"));
                if (elements2.Count > 0)
                {
                    driverSet.SwitchTo().Frame(elements2[0]);
                }

                Thread.Sleep(3000);
                bool isBind = false;
                if (!string.IsNullOrEmpty(account.New_Mail_Name))
                {
                    if (CheckIsExists(driverSet,
                            By.CssSelector("[class='email config-setting__label-text']")))
                    {
                        var readOnlyCollection =
                            driverSet.FindElements(By.CssSelector("[class='email config-setting__label-text']"));
                        foreach (var webElement in readOnlyCollection)
                        {
                            if (webElement.Text.Equals(account.New_Mail_Name))
                            {
                                isBind = true;
                                break;
                            }
                        }
                    }
                }

                if (!isBind)
                {
                    //点击添加邮箱
                    if (CheckIsExists(driverSet,
                            By.Id("add-email-btn")))
                    {
                        driverSet.FindElement(By.Id("add-email-btn"))
                            .Click();
                    }

                    Thread.Sleep(3000);
                    //输入验证码
                    if (CheckIsExists(driverSet,
                            By.Id("epc-pin-input")))
                    {
                        #region 查询邮箱验证码

                        account.Running_Log = $"绑邮箱:查询邮箱验证码";

                        Thread.Sleep(8000);
                        var emailCode = string.Empty;
                        List<Mail> emailGetList = null;
                        if (account.Old_Mail_CK.Contains(".google.com"))
                        {
                            emailGetList =
                                emailService.Email_GetGoolgeList(GetCookie(account.Old_Mail_CK), account.UserAgent);
                        }
                        else if (account.Old_Mail_CK.Contains(".live.com") ||
                                 account.Old_Mail_CK.StartsWith("DefaultAnchorMailbox"))
                        {
                            emailGetList =
                                OutlookMailHelper.Email_GetOutlookList(account.Old_Mail_CK,
                                    account.UserAgent);
                        }
                        else if (account.Old_Mail_CK.Contains(".yahoo.com"))
                        {
                            emailGetList =
                                emailService.Email_GetYahooList(account.LoginInfo.LoginInfo_EmailCookieStr,
                                    account.UserAgent);
                        }

                        Match match;
                        if (emailGetList != null && emailGetList.Count > 0)
                        {
                            foreach (var item in emailGetList)
                            {
                                if (item.sender == "LinkedIn" || item.sender == "领英")
                                {
                                    var htmlEmail = item.title;
                                    string regexPattern = @"\b\d{6}\b";

                                    match = Regex.Match(htmlEmail, regexPattern);

                                    if (match.Success)
                                    {
                                        emailCode = match.Value;
                                        break;
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(emailCode) && (!string.IsNullOrEmpty(account.Old_Mail_Name) &&
                                                                !string.IsNullOrEmpty(account.Old_Mail_Pwd)))
                        {
                            //pop 3 获取验证码

                            #region 去邮箱提取验证码

                            MailInfo mailnew = new MailInfo();
                            mailnew.Mail_Name = account.Old_Mail_Name;
                            mailnew.Mail_Pwd = account.Old_Mail_Pwd;
                            account.Running_Log = $"绑邮箱:提取邮箱验证码[{mailnew.Mail_Name}]";

                            pop3MailMessage = null;
                            while (pop3MailMessage == null && timeCount < timeOut)
                            {
                                Thread.Sleep(timeSpan);
                                Application.DoEvents();
                                timeCount += timeSpan;

                                if (mailnew.Pop3Client != null && mailnew.Pop3Client.Connected)
                                    try
                                    {
                                        mailnew.Pop3Client.Disconnect();
                                    }
                                    catch
                                    {
                                    }

                                mailnew.Pop3Client = Pop3Helper.GetPop3Client(mailnew.Mail_Name, mailnew.Mail_Pwd);
                                if (mailnew.Pop3Client == null) continue;

                                msgList = Pop3Helper.GetMessageByIndex(mailnew.Pop3Client);
                                pop3MailMessage = msgList.Where(m =>
                                    m.DateSent >= sendCodeTime && m.Subject.Contains("here's your PIN") &&
                                    m.From.Contains("<security-noreply@linkedin.com>")).FirstOrDefault();
                            }

                            if (mailnew.Pop3Client == null)
                            {
                                jo_Result["ErrorMsg"] = "绑邮箱:Pop3连接失败";
                                jo_Result["IsMailUsed"] = true;
                                jo_Result["Success"] = false;
                                return jo_Result;
                            }

                            if (pop3MailMessage == null)
                            {
                                jo_Result["ErrorMsg"] = "绑邮箱:没有找到指定的邮件信息";
                                jo_Result["IsMailUsed"] = true;
                                jo_Result["Success"] = false;
                                return jo_Result;
                            }

                            var htmlEmail = pop3MailMessage.Subject;
                            string regexPattern = @"\b\d{6}\b";

                            match = Regex.Match(htmlEmail, regexPattern);

                            if (match.Success)
                            {
                                emailCode = match.Value;
                            }

                            #endregion
                        }

                        if (string.IsNullOrEmpty(emailCode))
                        {
                            jo_Result["ErrorMsg"] = "验证码为空";
                            return jo_Result;
                        }

                        #endregion

                        driverSet.FindElement(By.Id("epc-pin-input"))
                            .SendKeys(emailCode);
                    }

                    Thread.Sleep(3000);
                    //点击提交验证码
                    if (CheckIsExists(driverSet,
                            By.Id("pin-submit-button")))
                    {
                        driverSet.FindElement(By.Id("pin-submit-button"))
                            .Click();
                    }

                    Thread.Sleep(3000);
                    //输入新邮箱
                    if (CheckIsExists(driverSet,
                            By.Id("add-email")))
                    {
                        driverSet.FindElement(By.Id("add-email"))
                            .SendKeys(mail.Mail_Name);
                    }

                    Thread.Sleep(3000);
                    //输入linkedin 密码
                    if (CheckIsExists(driverSet,
                            By.Id("enter-password")))
                    {
                        driverSet.FindElement(By.Id("enter-password"))
                            .SendKeys(account.Facebook_Pwd);
                    }

                    Thread.Sleep(3000);
                    //提交新邮箱
                    if (CheckIsExists(driverSet,
                            By.CssSelector("[class='submit-btn btn']")))
                    {
                        driverSet.FindElement(By.CssSelector("[class='submit-btn btn']"))
                            .Click();
                    }

                    Thread.Sleep(3000);
                    //获取新邮箱链接
                }
                else
                {
                    //点击重新发送请求
                    if (CheckIsExists(driverSet,
                            By.CssSelector("[class='send-verification tertiary-btn']")))
                    {
                        var readOnlyCollection =
                            driverSet.FindElements(By.CssSelector("[class='send-verification tertiary-btn']"));
                        foreach (var webElement in readOnlyCollection)
                        {
                            var findElement = webElement.FindElement(By.CssSelector("[class='screen-reader-text']"));
                            if (findElement.Text.Equals(account.New_Mail_Name))
                            {
                                webElement.Click();
                                break;
                            }
                        }
                    }
                }

                Thread.Sleep(3000);

                #region 去邮箱提取验证码

                account.Running_Log = $"绑邮箱:提取邮箱验证码[{mail.Mail_Name}]";
                Thread.Sleep(5000);
                timeSpan = 500;
                timeCount = 0;
                timeOut = 25000;
                msgList = null;
                pop3MailMessage = null;
                while (pop3MailMessage == null && timeCount < timeOut)
                {
                    Thread.Sleep(timeSpan);
                    Application.DoEvents();
                    timeCount += timeSpan;

                    if (mail.Pop3Client != null && mail.Pop3Client.Connected)
                        try
                        {
                            mail.Pop3Client.Disconnect();
                        }
                        catch
                        {
                        }

                    mail.Pop3Client = Pop3Helper.GetPop3Client(mail.Mail_Name, mail.Mail_Pwd);
                    if (mail.Pop3Client == null) continue;

                    msgList = Pop3Helper.GetMessageByIndex(mail.Pop3Client);
                    pop3MailMessage = msgList.Where(m =>
                        m.DateSent >= sendCodeTime &&
                        m.From.Contains("<security-noreply@linkedin.com>")).FirstOrDefault();
                }

                if (mail.Pop3Client == null)
                {
                    jo_Result["ErrorMsg"] = "绑邮箱:Pop3连接失败";
                    jo_Result["IsMailUsed"] = true;
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                if (pop3MailMessage == null)
                {
                    jo_Result["ErrorMsg"] = "绑邮箱:没有找到指定的邮件信息";
                    jo_Result["IsMailUsed"] = true;
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                var confirmCode = StringHelper.GetMidStr(pop3MailMessage.Html,
                    "https://www.linkedin.com/comm/psettings/email/confirm", "\n").Trim();
                if (string.IsNullOrEmpty(confirmCode))
                {
                    jo_Result["ErrorMsg"] = "提取邮件验证码失败";
                    return jo_Result;
                }

                confirmCode = "https://www.linkedin.com/comm/psettings/email/confirm" +
                              confirmCode;

                #endregion

                driverSet.Navigate().GoToUrl(confirmCode);
                Thread.Sleep(3000);
                if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
                {
                    loginLinkedin:
                    if (CheckIsExists(driverSet,
                            By.Id("password")))
                    {
                        driverSet.FindElement(By.Id("password"))
                            .SendKeys(account.Facebook_Pwd);
                    }

                    Thread.Sleep(3000);
                    //提交登录
                    if (CheckIsExists(driverSet,
                            By.CssSelector("[class='btn__primary--large from__button--floating']")))
                    {
                        driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                            .Click();
                    }

                    Thread.Sleep(10000);
                    driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                    Thread.Sleep(3000);
                    if (CheckIsExists(driverSet,
                            By.Id("password")))
                    {
                        goto loginLinkedin;
                    }

                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                bool isClose = false;
                TaskInfo task = null;
                string taskName = "ChangeEmail";
                task = Program.setting.Setting_IN.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
                if (task != null && task.IsSelected)
                {
                    if (!string.IsNullOrEmpty(jo_Result["ErrorMsg"].ToString()))
                    {
                        isClose = true;
                    }
                }
                else
                {
                    isClose = true;
                }

                if (isClose)
                {
                    adsPowerService.ADS_UserDelete(account.user_id);
                    try
                    {
                        driverSet?.Close();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }

                    try
                    {
                        driverSet?.Quit();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }

                    try
                    {
                        driverSet?.Dispose();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                }
            }

            jo_Result["IsMailUsed"] = true;
            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = $"邮箱已绑定[{mail.Mail_Name}]";
            return jo_Result;
        }

        /// <summary>
        /// 绑定新邮箱
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject IN_BindNewEmailProtocol(Account_FBOrIns account, MailInfo mail)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["IsMailUsed"] = false;
            bool isSuccess = false;
            bool isBinded = false;

            DateTime sendCodeTime = DateTime.Parse("1970-01-01");
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            int timeSpan = 0;
            int timeCount = 0;
            int timeOut = 0;

            #region 进入绑定邮箱界面

            account.Running_Log = $"绑邮箱:进入目标页面(psettings/email)";
            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/psettings/email?li_theme=light&openInMobileMode=true";
            hi.UserAgent = account.UserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Allowautoredirect = false;

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);
            html = hr.Html;
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            var csrfToken = string.Empty;
            if (account.LoginInfo.LoginInfo_CookieStr.Contains("JSESSIONID=\""))
            {
                csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=\"", "\";");
            }
            else
            {
                csrfToken = StringHelper.GetMidStr(account.LoginInfo.LoginInfo_CookieStr, "JSESSIONID=", ";");
            }

            var pageInstance = StringHelper.GetMidStr(html, "\"pageInstance\":\"", "\"");

            #endregion

            #region 开始验证

            var inVerifyEmailCode = IN_VerifyEmailCode(account, csrfToken, pageInstance);
            isSuccess = Convert.ToBoolean(inVerifyEmailCode["Success"].ToString());

            if (!isSuccess)
            {
                return inVerifyEmailCode;
            }

            #endregion

            #region 重新获取pageInstance

            account.Running_Log = $"绑邮箱:重新获取pageInstance";
            Thread.Sleep(2000);
            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/psettings/email/add?li_theme=light&openInMobileMode=true ";
            hi.UserAgent = account.UserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Allowautoredirect = false;
            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;
            hr = hh.GetHtml(hi);
            html = hr.Html;
            pageInstance = StringHelper.GetMidStr(html, "\"pageInstance\":\"", "\"");

            #endregion

            #region 开始绑定邮箱

            account.Running_Log = $"绑邮箱:进入目标页面(psettings/email)";
            Thread.Sleep(2000);
            hi = new HttpItem();
            hi.URL = $"https://www.linkedin.com/psettings/email/create";
            hi.UserAgent = account.UserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("x-li-page-instance", pageInstance);
            hi.Header.Add("X-Requested-With", "XMLHttpRequest");
            hi.ContentType = $"application/x-www-form-urlencoded; charset=UTF-8";
            hi.Method = "POST";
            hi.Allowautoredirect = false;

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            jo_postdata = new JObject();
            jo_postdata["email"] = mail.Mail_Name;
            jo_postdata["csrfToken"] = csrfToken;
            jo_postdata["fakeUsername"] = string.Empty;
            jo_postdata["password"] = account.Facebook_Pwd;
            hi.Postdata = string.Join("&",
                jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            hr = hh.GetHtml(hi);
            html = hr.Html;
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            //{"result":{"responseCode":201},"content":{"emailId":"9200917720"}}
            //{"result":{"message":"","responseCode":401},"content":"PASSWORD_VERIFICATION_UNAUTHORIZED"}
            if (html.Contains("{\"result\":{\"message\":\"\",\"responseCode\":500}}"))
            {
                jo_Result["ErrorMsg"] = "绑定邮箱:服务器请求错误，需要沉淀";
                return jo_Result;
            }
            else if (html.Contains("{\"result\":{\"message\":\"\",\"responseCode\":401}"))

            {
                jo_Result["ErrorMsg"] = "绑定邮箱:密码错误";
                return jo_Result;
            }

            #endregion

            #region 去邮箱提取验证码

            account.Running_Log = $"绑邮箱:提取邮箱验证码[{mail.Mail_Name}]";
            Thread.Sleep(5000);
            timeSpan = 500;
            timeCount = 0;
            timeOut = 25000;
            List<Pop3MailMessage> msgList = null;
            Pop3MailMessage pop3MailMessage = null;
            while (pop3MailMessage == null && timeCount < timeOut)
            {
                Thread.Sleep(timeSpan);
                Application.DoEvents();
                timeCount += timeSpan;

                if (mail.Pop3Client != null && mail.Pop3Client.Connected)
                    try
                    {
                        mail.Pop3Client.Disconnect();
                    }
                    catch
                    {
                    }

                mail.Pop3Client = Pop3Helper.GetPop3Client(mail.Mail_Name, mail.Mail_Pwd);
                if (mail.Pop3Client == null) continue;

                msgList = Pop3Helper.GetMessageByIndex(mail.Pop3Client);
                pop3MailMessage = msgList.Where(m =>
                    m.DateSent >= sendCodeTime &&
                    m.From.Contains("<security-noreply@linkedin.com>")).FirstOrDefault();
            }

            if (mail.Pop3Client == null)
            {
                jo_Result["ErrorMsg"] = "绑邮箱:Pop3连接失败";
                jo_Result["IsMailUsed"] = true;
                jo_Result["Success"] = false;
                return jo_Result;
            }

            if (pop3MailMessage == null)
            {
                jo_Result["ErrorMsg"] = "绑邮箱:没有找到指定的邮件信息";
                jo_Result["IsMailUsed"] = true;
                jo_Result["Success"] = false;
                return jo_Result;
            }

            var confirmCode = StringHelper.GetMidStr(pop3MailMessage.Html,
                "https://www.linkedin.com/comm/psettings/email/confirm", "\n").Trim();
            if (string.IsNullOrEmpty(confirmCode))
            {
                jo_Result["ErrorMsg"] = "提取邮件验证码失败";
                return jo_Result;
            }

            confirmCode = "https://www.linkedin.com/comm/psettings/email/confirm" +
                          confirmCode;

            #endregion

            #region 验证邮箱地址

            account.Running_Log = $"绑邮箱:验证邮箱地址";
            Thread.Sleep(2000);
            hi = new HttpItem();
            hi.URL = confirmCode;
            hi.UserAgent = account.UserAgent;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("x-li-page-instance", pageInstance);
            hi.Allowautoredirect = true;

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);
            html = hr.Html;
            //合并CK
            if (hr.Cookie != null)
                account.LoginInfo.CookieCollection =
                    StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            if (hr.ResponseUri.Contains("https://www.linkedin.com/uas/login?session_redirect="))
            {
                //验证登录  
                //https://www.linkedin.com/checkpoint/lg/login-profile-submit HTTP/1.1
                account.Running_Log = $"绑邮箱:验证登录";
                hi = new HttpItem();
                hi.URL = "https://www.linkedin.com/checkpoint/lg/login-profile-submit";
                hi.UserAgent = account.UserAgent;
                hi.Accept =
                    $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded; charset=UTF-8";
                hi.Allowautoredirect = true;

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;
                jo_postdata = GetFormPostData(html);
                jo_postdata["session_password"] = account.Facebook_Pwd;
                jo_postdata["loginFlow"] = "REMEMBER_ME_OPTIN";
                string login_Fc = String.Empty;
                if (string.IsNullOrEmpty(login_Fc))
                {
                    login_Fc = this.login_Fc(null, account.UserAgent, pageInstance);
                }

                // "%7B%22df%22%3A%7B%22a%22%3A%22Dd5IH4bNnNt8Hsr0g2pWeQ%3D%3D%22%2C%22b%22%3A%22jPMWjG2sLCFJg4vRK6VpsAfQWC1mRAHGeacONWKZUEwzcdLvUNnQmchix0cOZXeti%2BK1zA6KpkhBl%2BTe1kTMwRD7vvd82bIrtume7X6Z1son89jLvOLi%2B64bCRei5fTq0tGL5Yacj0cQUV51cXwnuHygpxxLF4TW%2BHciTOl6oB%2FwVmJrJqpnHx8vDxZSRF248N0N1jpOfJfEbvPGc2dv86rNAz2jYNprEnPJDBK8wQn9r4tIHuIWt433x7eRou0eOXSsxa6zRu6zUCzq1bx78ldmGZhS%2FzpHSm6Z2U14Mf0qFrswQdUq2XdDybLQWY%2BfDVataqTAvH9svyqXUPGWVg%3D%3D%22%2C%22c%22%3A%22rm51QzJ69ayb45ZclzYFWO3cCW5VvmMECl3vjdhy2j82PS8pVESBvzFxpTCuhvI5uFlgjyMuvR0ULExLSzdw2hFYu4jMrFnHjmf60RsFXmqz1Tqahtj6Ck54Fr6Hqspcwe4CBoTCX%2FMtKX0WS9OspCHss6pcGyTPuwQNmwxWl9%2B%2FU0%2Bhd%2BkfAwwf19nD5jLQtq9gEgZIgcYWTEv7DcLzJbLnjJU2OhHOSr9wT29Lg%2BI%2BoWvTXp4Y7Kw5A%2BFrzfb73q%2F%2BFfR%2BY%2FuET8lm9zPjpJ82XmqNtcRP5rzc3dlZWkcUknOMfH1cYnRNi7tGxCs1zJ4Y3knp2xJNdYYXW93IRsmVxHB4h1ehW5p3QJ%2FCsMclvNWLagvbvGltaWTPFs8JQeeNDaBh%2BZAmUxF3iVYivxQf17HSJV4V5pvnNSMe1RVHX7IAQdTNPRV%2Fpq8Y2tDv9LHkVpvFeqjIZyGPyTwv2HBRPB1tN3zPBUy8UmwVf4mpoOT1YSBBDUd%2ByQJwmueAykGsOrpaW5uxdcLObQfCxRf5Jy8GrLxX8yscMj9yIQyQde6oS7%2BfcfE5b3sX5TDwbY6m2ysZy3a32DiW2TgTXBWj0QmQb9G6HJu6LG8edlCmbLPRNFV2WeakLtxqMKOpBCEmdCZ2bL7uToPfxeVNn3j6o1%2BOSsPLsWgrFPo%2FeE8VZdak%2FgpR45PhlXwb2VmXwfHu1uWcJmQeezF7xqSIhjqJy5Cix2Biu3yNjpIHluDwjjJwbxOomtgisOsOiZ4llv7Q4aHqlJHjAXa460pHOTvbF0zZyD4T4hJ2U1Xcx3w7inYzg3MjgDwDvkwM%2FlcHndtEnsKp8m7FSrxmtSI9RbQSv6g3%2FC%2FMTK1MJ3fbakQZtJb1JHFsQT7s3fzYEdBTdiG7Lwya%2B1B3InaXQyLvbtllXrIESLWdGYE%2Fp4xZMQWURH%2B2%2BG%2FKLubvi%2B7Q%2BDBG6bwoT47sZUNNHSlgOI7abMALkbiOC%2Fvnv80nOccYkRw%2FPeNjaAalTno%2BS%2FzmvQVyvO9sw1jdSRL8rZOImCTX%2FA8XSBjZJ0mydvInpbQ8Q4lfMpXyyVKynQjTZ6wxHuwVVp15xFX16uyJwhATNzws%2Fn%2B1NWZi2AdvkrSTAgplOOazhEh7APXwxypsKoNNF6Fo1Awxi%2BG%2BftwwPps91tTseT%2BAoJvb6%2FyJi6elZcXyYCntbORfriaSritlcc%2BRnfOGHXiyQKG6iLQuSWPHK0LosFS7PXw6V0npxOm%2FsJ3uIXbrH%2BAWadxNEo3ipzrhP0HpBfh1sOtB9rTDLVe7M89Bjo9nSvYz8NKDMllq0Td%2Bv7uOd1X83LdortZPmQmgkUD3BwF2AExUdXg0xNnHl1CNvFQZlnlBMt7H1c%2BjK9tqeYa6QEKeBBJwRgRMEDNaQ2B3P0lNzGOuCB3NUnOfbCFJJcEXksa28hKCAVzvowOTJCymeXGPtOI5c0qGPNo2XlSvW1dm314KvJN6AQiDXtzX93Y%2BZwWCc3jp2LtTW%2B4DKhRTLl8OMUyM9fcFFDTKNcTx7Z9GOZXOpsap3F7wCy0Fu2LETeLHpmZ2oaP30oXsfokgFc2Ig4agbbimP2OJmFHRZgaoWk08X14OZ6UHkepCSX31QoaH9ztzKy0Q%2Fn2qGWbSrmcrv4c6i%2Fty%2BY7IzEqRi7cQAH44A2nighHgjWFObcnB4PW7V9NvCpk8%2BFBESvvYP5w5Z9bI5981wblXwuAJoGV9vyat%2FdDdJuNdyNRdqk%2BWRBafCmeXua02tQxA6w3gy%2FsgQLS%2BmqJaZDVD8OLiSAk5R6SaTYnwW807CGd8anLuBK4aSPsVDJqfN3pg%2BC0ifu%2B3PZAGBzvNxZNUTx1SLMZmKWoQJWk70EUPsbpgtH3%2BCGp5zgGZXN4ZxVuiKwXwFNQqdJZ4PBilkv33TxSQTPjbtmJT7Ud4D2isi1f5RgSn30%2FrvZxBZ%2Fds0Yev6Pcn5U3n9G9isO0wll%2BXPR5wTjzYZyN7qcbTXASHkeVjYSXAK5perqqNZtZeT%2FfwQdCs6y3zjnADGMQ9AXgXzRkp%2BKnfmNs%2FCAJhA95Y4vUEGzTMSgxCpihEtcAO7wnOAlXfQ1XgCBwfrY6%2B%2B8ku6gxehEDuCCjxJq1jHI6Nqoz270aLNsH9gRiseXfOSUFxW%2BR%2FyoQHcEkfLszeUNGjUmeMsx72fXKNnEeMHsuNcfu%2FcKiKeUSaGcXySvz4Q4DIjipDn1dXQDqGLwHOsFW%2FgdUO2Mk1l3c6V3hJOf4Imxfb%2BeFlzv0IuAqGfD7kUt81t7wxy8cTHafLTMOGSgIO%2FPQ2txaSqRWctbvKYPs9RF3pwt%2BpDYARRMuwRk%2F4FQHhaFA2PJS6hGF6yJvqubimhxUJ2kJPSYR0jir6U9JaR0m2MKATMH8swhs9j4mzpqSDyNH056NtMMMVyeko5%2FNNiNfpjp%2Br%2Bkwg4R2i4TJxsotmlvIIDoyt2Rpeps%2Fe%2B078Bbv%2Btmez6mCgbgSCqGGXAio4Kd6mbAXcbmtaMjExKpCtos3qdeosjGzISJIsWPRp3rqUBptXBMNmHD0auRPSOh5v1mEZzcEzqXbAeVNsTcCxLrMHLc%2FHpVVxePj%2F7PY0r%2FQWK%2ByCyG5po09%2BbeK1wcIMHSdxHvJ%2F8dL0Uq6v1%2FTvVx7hVH6GmvfCy%2FoxDWrlD6LmkKU61Kjzl4md9%2FyOLfPMnIa7wY2qgjiApgLsYVhRvCLW8PvdugsL2RfKJxqOWrNb49nv7w2Ix5LxrSL00mGHQcVDGjIrEsTRE1M43s1lU3j3VkwuHg5Ia4XA9PdZBu0Mdpq4Wjj2ojwulNa6cO7UuRSJBrv%2B8O7dpHJDQrAJOCmJuV15dqAHhnT3vKZPIBeDnH0IBo1XEMcsg3VKn5ukkuZl1%2F%2FDrTAHgrSHu93KezybuqnErW65csCpnWgv3ffP9zc22Kwl2Kuc1lLYSUWs395tZfK5zf7G62EczyRUmowxDogmtbNJsXzPAW8sX9VhiAMpDPr6f%2FnWLugZBk0qBtOaR7NRkqhqO%2Fs1t5Q3CYuVUuZYfRyt7xO03fSphnA4bflzFN2f9RD2ytiVj2nKGQlNULTUt0j12gxx%2FBfSD5Yd%2B5PY3pzTYArpAuRsiGtKTh3eUgtH4%2BE1%2FqiaseJBnrhOQKXq58dovSPvpqDkRF2BKHWdP%2BZwP6kO9ytsFODMPOipE45wICd8tS34VADbBDPpOTgqZ5Xs0kjWX7wUry2eI%2BBk5RS%2F67PLHJNiESt8y%2FtRAp4CMokwF8617cnrhqIQYVboiLFvXq1AoXNZm4MQTk5KWgYMQc3cIEmUq2h%2BoRaMOJqHGFHueHltV7kxHuUW%2Bcf9aN%2FpkxkkNpRCScHmdLuDhsodCK%2BPxJa5dCt6HMnw6hP1CDH2UXcKgHSwE4%2FBBV8J9yf4PQINtVIvTS3Dt3vMl8PVwk2j1IdBVV6I7GoeU96Ea4myNW8xxwHHjcnpdEAdH6pSbrSKNDsgsJFReswbEWT8MXNasQ9tz6dddE2GNdWoV%2FFzUFUmMRCtKWN4mE0c%2BkQm4%2BraHgDm6kwJcnQ%2Fq7qR5Ar2whRxj0DLHpx0uy1rDnpw9Tc9VB%2BokvyEXb6uD1uZ5vJp56O8rJ4gGd7GzdoWt2QaGzRx5BqKPgOdyYYMhn4l6GGsBJUiFTgsTVabCl26MJzAqRhDJe1VVj9ShlmLI2U7TN1BocJk9GD8kWjKgXcwOKcHnaQZ%2FsVBqZ1BdaTOSgMXSP6m4EcfPZ0rV0MmyWTiCxhFwMPOcD8Xag4np6P02pQ8n1oq25JbMVijd5XpR3swaMWuvwTicrgmPfHAzVAbvatcK7SXcnHaxHslIJKXl3NObl4v8%2FZ%2F2rjETAiEUxsT78HRfEarNt3K7dXXxPtQb2q4e9XsfO%2F0tGnkPYYpYfIR51WaulhcroidUSfOUw8GDwzSx3f81HZHztELZmXh1WCgxvHBnh373ihTsHBVlOcbx2a%2FjbgLAexRtcxxYPg8UWFece6Hn6P8XzyI%2F7GQ5VBgP98Hv7uyl9KPfITiu06PmkXZubrw5cdWinX6p5emPt7d1UdSVbrYRnyG5gU5yn9shVKLJi3KZLoN%2BFDsgdQT4pNtXuuXIHquh5v6hZHnjT0xt%2FFBVXrZlhweVj1D43O5qqnJ8jhcdCijayBCo3b3LbdtW8I8q1f4AQJmqiPcT8%2BEO8dsrHmiXm3jzdRTYbwtpaA3ICQ1pGGiO36eImOS0virFMMNri%2BSZG56c%2Bm7SedhL8iaaOWMpvDyPzPkxSwy9NXnLtq1uzjZZcdfMp3TyuLQc03jzxf9As9DFAjK1qsIfIe9lOR2%2BUaYn0XJ8lvLLbRojycTQnXx81sEgp6LLeQuaKffGIj%2B13ZVy4UmOIdRMu9YZ4Wn%2FX6f4fmq%2FKrs9uIW91oVHCR0TWSUShr8H3e1PiRqwdnkNhgXkr8Q4GICFakjXO8hZiDY0VWKhCsDNLtZKrUeK8vJUaCVyvUAEJDJH4J9oDa%2Fzyb69OqzksfwCzKCvh60fSdYuClhh2N%2BcICxuYFICUJNzIrbctszoKOTJYkK4p%2FhOc4PnBhVZ0gSnMeZ5kfqMt1UKhnjyC%2BrawBtT8CRxulJPG9X64DJBbeU151CmsHs%2BOs2Q7bxlyufoh3h6FzVbF7jDP7Dk5GZpDJv4wFc%2BAYhESEKUpSkkjdpud2s39cv1nSRy%2Frw6AfC7%2FWq9GwoTBvZfa%2FIR1G%2FITlyWi5ELWTE7w44LXxzFrKNSXNWX%2FmjUS3lNX9zMLO%2Fg8MXTxTL5j34sLyJ0Kx7Ku1meVWoDlzBUea1RM8mRPAvhITmlcoj%2FUFrWMWoCDsFUFzqkMOmws0eWudqpW8MHeeu1u66GQc8xn%2FBEkiByV1CTO8wQfuJZEIf4mg7gJ2iSsIUubrIcH%2B8ThMX%2F284Fy%2B%2FncHpNz%2B3KhiJI%2BQ2FEu0vQokk4X9vpvolemXanal0lxj37dBbl7woyUDpnm137whfa7iQaJB8EphzIB63SULAEYWlQIzkPjWq%2BNUONvTk6BPYqwpZ9q90I2jFpXIGAdg8ttC9Uo9%2BS3mX2xYEG6iHfq6FFYfH2SiBuglXrGb%2Fjd7B8dudxFzlRnepm%2FkB1eTyHC886P0Bt4naEwgfspWw6HNJCZ9ZzhQ%2BmwniQeSHHfwroGl7iOprIMPII5Bl0WbXeJmMrnhJRJpcCgsn3VbSpWL212zFyON42OUEYlQrw1WH9cbewcFrJvonA%2BUoGd13Opcmdxy8voRe%2F%2BIBqxbIs%2FzZcoZd%2FNgWYmvbG4BJ2VdFzRfYcy2DLIZfjvTWgafxJZn9La8tamP6jNJkfBVr4uopUWlrWzje3xc6eEBEfOC09WqXpn99pGTAm9sOJvideFiTRRDl3SygD9PZE0XPIYCBVpG37Yra5xB99J%2FzHzEiXj2EFb%2FHfA8cNsfZyHkNNlXO1aWH8kxlC96%2Fxd7Tu1BDMqCw77b1ynENovbG8ljHsWAo7SMd%2FZxEwm%2FujgyfNTdxDCkE%2FIvAMMnDOZSHP8l%2BtzwaUHB0Kjl6Vhji83iitFZkCQqTSXTDFRJuA99KRfnTc%2BUXqz1B5%2FUNvyvYBb983I56EmP4k2r6V%2FPAKBbvSQl50IEUiEzkzaWMwwAZ8aTGuNzvOShbzCUC%2Brh2%2FMiQd92RyVQ3GHWhRqdWZx1XLlZ6wk6Oy7df2s45wQuPSd3kPW4cdQWH4vRgIpzz7kR%2B4E9JjJ4aYVpXcqVl44q4MtwP2ECpWsjHYHuu0bw14hEZPBloC1aB5dkmUO84FULkjZDf8i9BTfhvu%2FU3xVWk8aW7m2M5XUL4qALsCXBzfmxMo0P0jNKxgpR42ckaArSLEbTuaG0hdtpg7rvLVTYBXADfVi9A8oOm9nHtpbHXEkuhiHt55785QZj6nBkEbNAmYDz%2BnNtJgFaqxOzZmzAX2WFbM%2FuZt%2FqsNiz1T5ugeOHeiIKv%2B8fnzWZGL9Vt8QajpM8%2BnpsKBz5Z9zoBFMlp10kDeiNbexmmtGvBYnwXy7ZEXn1%2BnfkFxmuHHJAFr80QM%2FL6Ck2l5SWydUyLXy0FtFPkKO%2BEhpXpVsH1EulMm9C0StKaFtd19Af5bx%2BJ%2FXUm1aCpcl8%2BzzRwyZ8GzWAVU4bywN5EjCS2YQFcnndj6tIwuHE4HbEJscMzAQTEpLJLn4Wd8NMk%2B4Yng73170atqC5nwmI41jBCyauf6DcofI85c1s6SvFo9e3ZKq19c8TyLxCPz9gWwkyYJzV0mbk22glUlXl2xbLyS%2Bjhjk4yKBFvbq%2FVob4fAKx59Wimzntey8xL7hTI3YCU0iT6gZT2rwHYL%2FCeEWJy8wYngUs917G3jQ24wqNMOmel6aDL3m0mE614XF9jxOeJwrOcs%2FgcIV3nOQ5vPMGQ5XnHP%2FCbOdtr3NhIttsaZnpczgmstK46IhX3275T9bwya6ysAAskSSbv3ECtuAf0GrZ61TzATzw0UFlVE1iIZgS3SYClC5lPHXGf0t280rNWHcrX9KyXVHWjlVkPMAdJ9ke%2FI3%2BI8A0VLpPaOhKuehx8ioxEnFYOsioL8cMGyhpNDff8cwfs0x%2BBjYcLuhgippciDlE5x82nVTz7oSZnHk93ufq57kEXMYrsEgQUlmYqJ%2FxanjI1kABh0KcRVRs8fXwtb%2BYUuAPzUiV82zaQw0rfMevVNvm8W6hNU76VTXAi5Q7TeMMFAYmuLKDwp2YYwm9%2B4p3CNC3xBkKLmOPYmO5ZKvPxzvBZV71f1TX1Gdf%2Ft8Ox3GJlFmMw4%2FHIGeYi8QZYS1e%2BHJbfdCWq4%2FOkbRqZUIXdhiBZjDBzg%2Fe4ZMoaXTLJL76YuGkTYVm3i6PBpAVd2ARMdvpNitse6Aw89O6AA4BbijVi37CFibIOjioZRpovhSQcY%2BS58wXiB5li1U4kac2Lm3iiMV1jUUdI7U8hNl04Iec0zAojeVnSexfO515wMGtCKtitlQxachB689bIql2AOSqdzdIukpnugBQRU4REFidmVyURQ724Aisr57JHuoq7f5ZL9CXeie6jyL5GC3WKlPPMX9xSaHK8phITXaNQW7p8k3RgFd%2BfNH18rpM58oN0igDdNuHIeVHxabl5U%2BrMITSnmti9xVSD3cuUhoACtjWA%2FqOUphOJWx4n5mLIVoOKs5lop1bn8zaKhoTahQc11SI6rLKgtc0i3YURWe9ouBsM0%2BjtSgDk1B%2BWeGoTpewqVlCbIdQKtGZcFoSUsGt%2BmQONyQ5aIWLKouTDYZXNyRdouyifGMunY70SWFK3FK91P%2F%2FWwJNFYR172r15I1BsvECFbdMIvZO7ONmtK9uswjlrpOmHIGJJvuK7HR6Z8cJxyBGjCXYvARKiePSPgWll%2FaoVJWHMlFW%2FMJhWWGN%2BrQlMKO02CzcfXkx5D%2FmZFyua4x%2F3xV8VMyANV3mRSSFtWRHzVGjsmxLjlDh7pVu5E%2BK2n22zh7w2CGHt18ewpxZIHZTlVHondDMAJj5mr8O1mberTLd%2Fzz9Mx83PuSSJ9McE1D5jPE%2FqdSRaF%2BrD6JyVa7xoqSKw0sPIM1Mh%2BYl8V065pfc%2B2CvBiwTgDZXeST7pb0T%2FjdKaqW10TB%2FQRMBKDWG9A3v4ywf6JDXA8pG4lYGS70QfMEG%2BZY98hgqsdXW9SUSumNZrGARU%2B4xnE3H3riR92%2FkW%2Bihj1gkKKT4GqLHuw9FOdAeUKUaYHrhhkiCbAx%2B90rFHiAOVA2OTbP0qoSq7XDbKwqFCAM42ZcKp4U%2BRuanRenlIPcBxYuIa%2FuBYc6VvRowhlNw%2FTteF%2FSnuKRiSA%2Fu939hhqLNAjlg1X3W9KUDNOE3v0Qfs%2Fsc0ShCUVRJu7woOxP6jGgj2daVTdzvX4Mp169R31vmCVUHpyMi26CKxfUgAbQhgrnCWl8qQ%2BHVevqJs72EdN%2BMvHbjhNjqX661lteZj36pcChVv1pMC2MGnkFOcBeqyxAv7HMIw26zy1wA%2Bjlvik%2Fsztw2cegx%2BL1qDTC7VVTjqi6UNRRk62BEsp1xGcbQFcFAeUTqcYam5wKRxD%2F1WE1o1T5h9iLc9HSo2wbZg81LGog3CjWL9o5liyAWnmFrklhs%2FQPKxMQWKOugFUFZm90K4w7iAYml%2F3LX5RMfm1dj%2BnXW60MhU8oa9no9fUO8JJjRZY7m2DTjt2lIzhM4mHzxp4sjM0R13oI9h%2BRJfzkYDd82YlJKfbSWhOWrgJ1bo7fgfelwiTnFMrTBupNICPw2JdJu4%2BSRXzJbebuNCmK3fvM9VNZqthuMqGUrYaNsVfmmb5ynvS1IrlDVaMRT2G9SOHSULM%2F6ywlHShx1mEEuv%2B%2Bx%2FYrvKt0t7C4Wx3MjMZJF%2FQszd0Doq0a%2BGTOjh9VloUlD6gVPXO6I6eBk%2BFaaBpfDZ58KV2gc3ol2fj0yVPuA132zixFwdoqn50%2B5IYCF3%2BbQLWjz3gAfAPj81FZ2TrEPz5BC%2B2B3AvfzFQrP7OUha4qOf63YZ6UUZDLl0hJt26dTxLorSv5GLR4AgUTEQpPtWMgF7TFkS2xCILEe8RgOEetDjhVdysuxStU7iKF6CETRkR16mdvVxSvtbxORgx6%2BC47iSkcYzOxIL3LyazQfx%2BmB2FKvwlwnnJUPYF%2BfvLKGUB8jX%2FTV2YPMZfiMU%2BksLzSnm%2FiJprhllc%2FihJcy87FLQaam%2Ffqb1uydlHs574xzvSuUPxrkBomDvrbq%2BOdSppJjTHTvUL%2FV0yKqpXiu5aQURO7CHIg9tQ4TFS2KWA5gBdlycp1ohSef8teUYF0Xz7x2pttooLf0B5eLbVPL2BBudX%2BtlFknTIZeAgxca%2BjDbJgzZZNqTSs4i14BryIZE4394GI05HxstQp%2BQV8hdmHfNLS8ti8XalObuYJvAfIef0FLHycoBj7IiG1r642Lewd0YJcLYcErdQBVqT8G94gBsXLqyyrFTGuzJlWODr1inOktMJMaiC1jzINXf2xgcJLxeXtTUmoDBsyI019ROrmOWy%2BVFe8h%2BhVYF4HAW0E30dkpJ2c4KwkhlgN3Sfjn4NYWuo5HJrdozoAJCCRmwPVpZNdmHqOs8fQT7BfI5PAjuGu9hfV1KXdUlV4luJRx3q5oZhIsuVJM2WV%2F1wLdmMyW0OJUlNypL3R6%2BkHnbSX2%2FJezKTq4jfbXStM%2FGkjKS6gq3t37XW%2Bu9AkUI1Ii39%2BCQoO6JtPD0SCYg%2F%2F7%2FGOoRylbmqL3pR%2FE7OiVEyK%2FMyAESEhMkeiiehFD0WmWGfCi7Fb3Bv0l4FmK0otJGg%2BJcyQPoGx7pOirgYucKFJcfefPRabsjYL60j%2BqFGDirX%2FSt3FaezJeIKYIofg6AllB5LuVAkifgLaF7hKTwEBu9yV6eES6uAm%2F97YNqH7SmRqyAbwetlqe%2BDTJYpxY8lxxkRWuR1WrIR6BifVo%2Fc39moURo7GMN8H5GCWenr8pINYEOg6TtZ1WjmDclFxEr7wtl1Rgha6etbuGo6o0VWCmvc5UlZA2hWeUvZHEXi3MEBDGZ%2BFnGxNIZu8txesbF6zY1itzW1blE01n39LtufC0gZlvlHL2yYw1Lzw%2F5C3dd%2FrRtOZ3xBTCudTIG6olye0CJ4Tw5MBosrAY54O6jU84HwPhY9xPTH%2BRhG1oaocHO2ljd6A9PFXxzZJoR7Zz4V7tdE8pQJJN61IDOuzAnTBKwM1YLWCNycNnvYmTsmdLe8znB4ddALeHB3VwA8aw8OaN4QOeV3dMaGkqwT6TEkDPcj8a6dSyoWzIR45NFPO%2Fsvs45q6zERSCe1nixI43FBDhgRB%2B5n5aR1OMvh9knfNCXWSBPoSqCis6cGNCSgm7OVkeetPkxhLjmWky4da4M7dC0Wc8LSqNdn6TcJ7pbSWwFy%2F5087VsMhbGYcftIXE4w2vj3FLy3Le9qQA%2F9wRA2an%2BMFpjGXBU0MgRupIBbk20dl%2BrQ%2F5GuDQup1aDJOkk5HZ3Naq2MObk6cJBQTkV8QHmRtOHwVhTXZ8kn19rzbmIR5C%2BFxnLGEt2y%2BcDayKex%2B%2BIsM0dmf2CDabDrNSSXORuond%2Bnif6mM0MY%2F6OUulcZ%2FCNHl2tWVr8DvqNkOd4%2Fmly2rU23qtocx6au17AhUgV97seArFD1quQfpLKn5hBdt%2Bbt7XUsUYB2Lb0OSKJKfPpKozt9hQEicOVmmELsSjoIkt8UrKvIUuPxd3hrS6Dta%2F95fhOw2EKPErjpgJignSC7i4qjAKcNRTWcEvJwB59iQNnfJ79VAowYqfkEVYEHRwRkuhMlX8aAEng0TDYmO6TirKWa%2FkFhDX8d5avHXCFI%2BnneAAJ1oayqQ6Vi4CK9lbG2OPf73dMYnnZcZjd7iQyWkNjBN4Y4x5hl7Biv8YNNjo3snAVVRxoiPBuov9ilnYkcWZ0dJC9A62Rxd1YCzQAKTjp6WEHIQ1uEUsnMQmSKGpOfgbxeF1jDaHAt56iZcrNwyDaBqfG31MH7LYB5PHuFJInhWDOqkVcVsbcY66VUTy5AmmjsrSDRmq3zdtHCN6W9NqfJJcvQWy3XMETdeoJuTpJTrQrvoPp8f0Qx1NVxrddarjFxOe2aAL%2Bhr3HWlOKbzFIa1QlTh82OGr29%2Fi5F8cMVOfq0LfY3VP9Z%2B17KR7rGBWa%2BtlRO9EKwfUZGk1a%2BKtU0MVte9bqnrdGmxXPiRqFTzyShJjvMTg%2FFCgwkx7CacbuFW4%2BbF1cV7L0DLHudGT9cNqHsMzAfY%2F0BY58xEhrRxoOdEO9UEqAgP4qakjvD8lg1PLtV16jpp1N03Ek%2BHpeNhuawf2KFbgigX4xeL0eJZ%2BJSzuNygtBaj7W%2B%2BtVx9TSPchSnThrb3DeVuwQPjkeyV7gEJDJ56fYo69o%2Bwb70NoS%2B4NyE1eP6uX75%2FhfuIS2xKkjsnDI6byeDanRVaqLenKejpY1E8vxz%2FuQLOm0%2FFrJLJRUL26hhhpGBe7iSWxEetkx5K0bRaHE1iy3PZrPBcKfXlC3XQoGeJIQGM7A3CFfFjO9RUYGeR6D%2FcO%2FaQ4mmBuV9krLS4oekex4TLIiLwoMxxEEDEGvGwSVcsffUR1gDu0UZjQ%2FxhtJGGwZwQcO%2BNAWiTbdSM%3D%22%2C%22d%22%3A1%2C%22e%22%3A2%7D%7D";
                jo_postdata["apfc"] = login_Fc;
                hi.Postdata = string.Join("&",
                    jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));
                hr = hh.GetHtml(hi);
                html = hr.Html;
                //合并CK
                if (hr.Cookie != null)
                    account.LoginInfo.CookieCollection =
                        StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                account.Facebook_CK = account.LoginInfo.LoginInfo_CookieStr;
            }

            if (html.Contains("{\"result\":{\"message\":\"\",\"responseCode\":500}}"))
            {
                jo_Result["ErrorMsg"] = "绑定邮箱:服务器请求错误，需要沉淀";
                return jo_Result;
            }

            #endregion

            jo_Result["IsMailUsed"] = true;
            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = $"邮箱已绑定[{mail.Mail_Name}]";
            return jo_Result;
        }

        private static GetEntrypt_ApfcDf_Delegate getEntrypt_ApfcDf_Delegate = null;

        private delegate IntPtr GetEntrypt_ApfcDf_Delegate(IntPtr data_Cavans, IntPtr entryptKey);

        public string login_Fc(string login_Fc, string ua, string pageInstance)
        {
            IntPtr p_JsCode;
            if (string.IsNullOrEmpty(login_Fc))
            {
                login_Fc = LoginInfo_LoginByUserName_GetRandomFc(ua, pageInstance);
            }


            string JS_GetEntrypt_ApfcDf_AES_IV =
                "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqyVTa3Pi5twlDxHc34nl3MlTHOweIenIid6hDqVlh5/wcHzIxvB9nZjObW3HWfwqejGM+n2ZGbo9x8R7ByS3/V4qRgAs1z4aB6F5+HcXsx8uVrQfwigK0+u7d3g1s7H8qUaguMPHxNnyj5EisTJBh2jf9ODp8TpWnhAQHCCSZcDM4JIoIlsVdGmv+dGlzZzmf1if26U4KJqFdrqS83r3nGWcEpXWiQB+mx/EX4brbrhOFCvfPovvsLEjMTm0UC68Bvki3UsB/vkkMPW9cxNiiJJdnDkOEEdQPuFmPug+sqhACl3IIHLVBFM7vO0ca14rcCNSbSDaaKOY6BQoW1A30wIDAQAB";
            p_JsCode = getEntrypt_ApfcDf_Delegate(Marshal.StringToHGlobalAnsi(login_Fc),
                Marshal.StringToHGlobalAnsi(JS_GetEntrypt_ApfcDf_AES_IV));
            string apfc = Marshal.PtrToStringAnsi(p_JsCode);

            if (!string.IsNullOrEmpty(apfc))
            {
                JObject joTemp = JObject.Parse(apfc);
                JObject jo_apfc = new JObject();
                jo_apfc.Add("df", joTemp);
                apfc = JsonConvert.SerializeObject(jo_apfc);
            }

            return apfc;
        }

        public static string LoginInfo_LoginByUserName_GetRandomFc(string ua, string pageInstance)
        {
            string JS_GetEntrypt_ApfcDf_Canvas =
                "{\"latency\":{\"acq_time\":{\"appName\":0,\"tsSeed\":0,\"appVersion\":0,\"appCodeName\":0,\"location\":0,\"javascripts\":0,\"platform\":0,\"product\":0,\"productSub\":0,\"cpuClass\":0,\"oscpu\":0,\"numOfCores\":0,\"deviceMemory\":0,\"vendor\":0,\"vendorSub\":0,\"language\":0,\"timezoneOffset\":0,\"timezone\":1,\"userAgent\":0,\"webdriver\":0,\"colorDepth\":0,\"pixelDepth\":0,\"screenResolution\":0,\"screenOrientation\":0,\"availableScreenResolution\":0,\"sessionStorage\":0,\"localStorage\":0,\"indexedDb\":0,\"addBehavior\":0,\"openDatabase\":1,\"canvas\":32,\"webgl\":38,\"signals\":2,\"touchSupport\":1,\"networkInfo\":0,\"automation\":1,\"plugins\":0,\"mimetyps\":0,\"fonts\":23,\"allFeatures\":105}},\"errors\":{},\"appName\":\"Netscape\",\"tsSeed\":1710303421806,\"appVersion\":\"5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36\",\"appCodeName\":\"Mozilla\",\"location\":{\"hash\":\"n/a\",\"host\":\"www.linkedin.com\",\"hostname\":\"www.linkedin.com\",\"href\":\"https://www.linkedin.com/\",\"origin\":\"https://www.linkedin.com\",\"pathname\":\"/\",\"port\":\"n/a\",\"protocol\":\"https:\"},\"javascripts\":[],\"platform\":\"Win32\",\"product\":\"Gecko\",\"productSub\":\"20030107\",\"cpuClass\":\"n/a\",\"oscpu\":\"n/a\",\"numOfCores\":8,\"deviceMemory\":\"8 GB\",\"vendor\":\"Google Inc.\",\"vendorSub\":\"n/a\",\"language\":\"zh-CN\",\"timezoneOffset\":-8,\"timezone\":\"Asia/Hong_Kong\",\"userAgent\":\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36\",\"webdriver\":false,\"colorDepth\":24,\"pixelDepth\":24,\"screenResolution\":{\"w\":1536,\"h\":864},\"screenOrientation\":{\"Enabled\":true,\"Orientation\":\"landscape-primary\"},\"availableScreenResolution\":{\"w\":1536,\"h\":824},\"sessionStorage\":true,\"localStorage\":true,\"indexedDb\":true,\"addBehavior\":false,\"openDatabase\":false,\"canvas\":{\"canvasWinding\":\"yes\",\"canvasHash\":\"22ce8da63eb5e598fdf10e0578fc82d4\"},\"webgl\":{\"webglHash\":\"55b55550f99c16915b3a3234fedf7afb\",\"vendorAndRenderer\":\"Google Inc. (Intel)~ANGLE (Intel, Intel(R) UHD Graphics 620 (0x00003EA0) Direct3D11 vs_5_0 ps_5_0, D3D11)\",\"extensions\":[\"ANGLE_instanced_arrays\",\"EXT_blend_minmax\",\"EXT_clip_control\",\"EXT_color_buffer_half_float\",\"EXT_depth_clamp\",\"EXT_disjoint_timer_query\",\"EXT_float_blend\",\"EXT_frag_depth\",\"EXT_polygon_offset_clamp\",\"EXT_shader_texture_lod\",\"EXT_texture_compression_bptc\",\"EXT_texture_compression_rgtc\",\"EXT_texture_filter_anisotropic\",\"EXT_sRGB\",\"KHR_parallel_shader_compile\",\"OES_element_index_uint\",\"OES_fbo_render_mipmap\",\"OES_standard_derivatives\",\"OES_texture_float\",\"OES_texture_float_linear\",\"OES_texture_half_float\",\"OES_texture_half_float_linear\",\"OES_vertex_array_object\",\"WEBGL_blend_func_extended\",\"WEBGL_color_buffer_float\",\"WEBGL_compressed_texture_s3tc\",\"WEBGL_compressed_texture_s3tc_srgb\",\"WEBGL_debug_renderer_info\",\"WEBGL_debug_shaders\",\"WEBGL_depth_texture\",\"WEBGL_draw_buffers\",\"WEBGL_lose_context\",\"WEBGL_multi_draw\",\"WEBGL_polygon_mode\"],\"webgl aliased line width range\":\"[1, 1]\",\"webgl aliased point size range\":\"[1, 1024]\",\"webgl alpha bits\":8,\"webgl antialiasing\":\"yes\",\"webgl blue bits\":8,\"webgl depth bits\":24,\"webgl green bits\":8,\"webgl max anisotropy\":16,\"webgl max combined texture image units\":32,\"webgl max cube map texture size\":16384,\"webgl max fragment uniform vectors\":1024,\"webgl max render buffer size\":16384,\"webgl max texture image units\":16,\"webgl max texture size\":16384,\"webgl max varying vectors\":30,\"webgl max vertex attribs\":16,\"webgl max vertex texture image units\":16,\"webgl max vertex uniform vectors\":4096,\"webgl max viewport dims\":\"[32767, 32767]\",\"webgl red bits\":8,\"webgl renderer\":\"WebKit WebGL\",\"webgl shading language version\":\"WebGL GLSL ES 1.0 (OpenGL ES GLSL ES 1.0 Chromium)\",\"webgl stencil bits\":0,\"webgl vendor\":\"WebKit\",\"webgl version\":\"WebGL 1.0 (OpenGL ES 2.0 Chromium)\",\"webgl unmasked vendor\":\"Google Inc. (Intel)\",\"webgl unmasked renderer\":\"ANGLE (Intel, Intel(R) UHD Graphics 620 (0x00003EA0) Direct3D11 vs_5_0 ps_5_0, D3D11)\",\"webgl vertex shader high float precision\":23,\"webgl vertex shader high float precision rangeMin\":127,\"webgl vertex shader high float precision rangeMax\":127,\"webgl vertex shader medium float precision\":23,\"webgl vertex shader medium float precision rangeMin\":127,\"webgl vertex shader medium float precision rangeMax\":127,\"webgl vertex shader low float precision\":23,\"webgl vertex shader low float precision rangeMin\":127,\"webgl vertex shader low float precision rangeMax\":127,\"webgl fragment shader high float precision\":23,\"webgl fragment shader high float precision rangeMin\":127,\"webgl fragment shader high float precision rangeMax\":127,\"webgl fragment shader medium float precision\":23,\"webgl fragment shader medium float precision rangeMin\":127,\"webgl fragment shader medium float precision rangeMax\":127,\"webgl fragment shader low float precision\":23,\"webgl fragment shader low float precision rangeMin\":127,\"webgl fragment shader low float precision rangeMax\":127,\"webgl vertex shader high int precision\":0,\"webgl vertex shader high int precision rangeMin\":31,\"webgl vertex shader high int precision rangeMax\":30,\"webgl vertex shader medium int precision\":0,\"webgl vertex shader medium int precision rangeMin\":31,\"webgl vertex shader medium int precision rangeMax\":30,\"webgl vertex shader low int precision\":0,\"webgl vertex shader low int precision rangeMin\":31,\"webgl vertex shader low int precision rangeMax\":30,\"webgl fragment shader high int precision\":0,\"webgl fragment shader high int precision rangeMin\":31,\"webgl fragment shader high int precision rangeMax\":30,\"webgl fragment shader medium int precision\":0,\"webgl fragment shader medium int precision rangeMin\":31,\"webgl fragment shader medium int precision rangeMax\":30,\"webgl fragment shader low int precision\":0,\"webgl fragment shader low int precision rangeMin\":31,\"webgl fragment shader low int precision rangeMax\":30},\"signals\":{\"adBlockInstalled\":false,\"liedLanguages\":false,\"liedResolution\":false,\"liedOS\":false,\"liedBrowser\":false},\"touchSupport\":{\"maxTouchPoints\":0,\"touchEvent\":false,\"touchStart\":false},\"networkInfo\":{\"downlink\":3.1,\"effectiveType\":\"4g\",\"rtt\":150,\"saveData\":false},\"automation\":\"n/a\",\"plugins\":[[\"PDF Viewer\",\"Portable Document Format\",[[\"application/pdf\",\"pdf\"],[\"text/pdf\",\"pdf\"]]],[\"Chrome PDF Viewer\",\"Portable Document Format\",[[\"application/pdf\",\"pdf\"],[\"text/pdf\",\"pdf\"]]],[\"Chromium PDF Viewer\",\"Portable Document Format\",[[\"application/pdf\",\"pdf\"],[\"text/pdf\",\"pdf\"]]],[\"Microsoft Edge PDF Viewer\",\"Portable Document Format\",[[\"application/pdf\",\"pdf\"],[\"text/pdf\",\"pdf\"]]],[\"WebKit built-in PDF\",\"Portable Document Format\",[[\"application/pdf\",\"pdf\"],[\"text/pdf\",\"pdf\"]]]],\"mimetyps\":[{\"type\":\"application/pdf\",\"suffixes\":\"pdf\",\"description\":\"Portable Document Format\"},{\"type\":\"text/pdf\",\"suffixes\":\"pdf\",\"description\":\"Portable Document Format\"}],\"fonts\":{\"fontsHash\":\"fe019a7907e68381aec51538cc04f32e\",\"lists\":[\"Arial\",\"Arial Black\",\"Arial Narrow\",\"Book Antiqua\",\"Bookman Old Style\",\"Calibri\",\"Cambria\",\"Cambria Math\",\"Century\",\"Century Gothic\",\"Comic Sans MS\",\"Consolas\",\"Courier\",\"Courier New\",\"Georgia\",\"Helvetica\",\"Impact\",\"Lucida Console\",\"Lucida Sans Unicode\",\"Microsoft Sans Serif\",\"Monotype Corsiva\",\"MS Gothic\",\"MS PGothic\",\"MS Reference Sans Serif\",\"MS Sans Serif\",\"MS Serif\",\"Palatino Linotype\",\"Segoe Print\",\"Segoe Script\",\"Segoe UI\",\"Segoe UI Light\",\"Segoe UI Semibold\",\"Segoe UI Symbol\",\"Tahoma\",\"Times\",\"Times New Roman\",\"Trebuchet MS\",\"Verdana\",\"Wingdings\",\"Wingdings 2\",\"Wingdings 3\"]},\"reqid\":\"a2797886-f4a7-45ae-a5fe-2c5f47a8b494\",\"pageInstance\":\"urn:li:page:d_homepage-guest-home_jsbeacon;qsb/2TwKRESy6FeNyMipEw==\",\"fullFeatureCollection\":false}";
            JObject jo_Canvas = JObject.Parse(JS_GetEntrypt_ApfcDf_Canvas);

            jo_Canvas["tsSeed"] = long.Parse(GetUnixTime(DateTime.Now));

            jo_Canvas["canvas"]["canvasHash"] = GenerateRandomString(32).ToLower();
            jo_Canvas["webgl"]["webglHash"] = GenerateRandomString(32).ToLower();
            jo_Canvas["fonts"]["fontsHash"] = GenerateRandomString(32).ToLower();
            jo_Canvas["userAgent"] = ua;
            jo_Canvas["language"] = "en-US";
            jo_Canvas["appVersion"] = ua.Substring(8);
            int a = new Random().Next(2, 32);
            jo_Canvas["numOfCores"] = a;
            jo_Canvas["deviceMemory"] = a + " GB";
            if (!string.IsNullOrEmpty(pageInstance))
            {
                jo_Canvas["pageInstance"] = pageInstance;
            }

            // /a2797886-f4a7-45ae-a5fe-2c5f47a8b494
            jo_Canvas["reqid"] =
                $"{GenerateRandomString(8).ToLower()}-{GenerateRandomString(4).ToLower()}-{GenerateRandomString(4).ToLower()}-{GenerateRandomString(4).ToLower()}-{GenerateRandomString(12).ToLower()}";

            return JsonConvert.SerializeObject(jo_Canvas);
        }

        /// <summary>
        /// 取十三位时间戳
        /// </summary>
        /// <param name="nowTime">给的时间值
        /// <returns>返回13位时间戳</returns>
        public static string GetUnixTime(DateTime nowTime)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));

            long unixTime = (long)Math.Round((nowTime - startTime).TotalMilliseconds, MidpointRounding.AwayFromZero);

            return unixTime.ToString();
        }

        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder sb = new StringBuilder(length);
            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                char randomChar = chars[random.Next(chars.Length)];
                sb.Append(randomChar);
            }

            return sb.ToString();
        }

        public JObject GetFormPostDataRespassword(string html)
        {
            JObject jo_postdata = new JObject();
            string inputRegexPattern = @"<input.*?name=""(.*?)"".*?value=""(.*?)""|<input.*?name=""(.*?)""";

            Regex inputRegex = new Regex(inputRegexPattern, RegexOptions.IgnoreCase);

            MatchCollection matches = inputRegex.Matches(html);
            foreach (Match match in matches)
            {
                string name = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[3].Value;
                string value = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;

                jo_postdata[name] = value;
            }

            return jo_postdata;
        }

        public JObject GetFormPostData(string html)
        {
            JObject jo_postdata = new JObject();


            string pattern = @"<input[^>]*\bname\s*=\s*[""'](.*?)[""'](?:\s*value\s*=\s*[""'](.*?)[""'])?[^>]*>";

            MatchCollection matches = Regex.Matches(html, pattern);

            foreach (Match match in matches)
            {
                string name = match.Groups[1].Value;
                if (name.Equals("resendUrl") || name.Equals("amp;ct") || name.Equals("amp;ct") ||
                    name.Equals("amp;sig") || name.Equals("amp;rup") || name.Equals("amp;rupm") ||
                    name.Equals("authUUID") ||
                    name.Equals("authUUID"))
                {
                    continue;
                }

                string value = match.Groups[2].Success ? match.Groups[2].Value : "";

                if (name.Equals("pageInstance"))
                {
                    var strings = value.Split(';');
                    value = "urn:li:page:checkpoint_lg_login_profile_submit_default;" + strings[1];
                }

                if (name.Equals("controlId"))
                {
                    value = "d_checkpoint_lg_consumerLoginWithProfile-login_submit_button";
                }

                if (name.Equals("pkSupported"))
                {
                    value = "true";
                }

                jo_postdata[name] = value;
            }

            return jo_postdata;
        }

        public class Challenge
        {
            public class ChallengeViewModel
            {
                [JsonProperty("com.linkedin.checkpoint.challenge.viewmodels.EmailPinChallengeViewModel")]
                public ComLinkedinCheckpointChallengeViewmodelsEmailPinChallengeViewModel
                    comlinkedincheckpointchallengeviewmodelsEmailPinChallengeViewModel { get; set; }
            }

            public class ComLinkedinCheckpointChallengeViewmodelsEmailPinChallengeViewModel
            {
                public string resendUrl { get; set; }
            }

            public string displayTime { get; set; }
            public string encryptedChallengeViewData { get; set; }
            public ChallengeViewModel challengeViewModel { get; set; }
            public string pageKey { get; set; }
            public string challengeType { get; set; }

            [JsonProperty("lix_checkpoint.idv.page.new.language")]
            public string lix_checkpointidvpagenewlanguage { get; set; }

            [JsonProperty("lix_checkpoint.rehab.scraping.self.recovery")]
            public string lix_checkpointrehabscrapingselfrecovery { get; set; }

            public string challengeId { get; set; }
            public bool isDesktop { get; set; }
            public string requestSubmissionId { get; set; }
            public string challengeSource { get; set; }
            public string challengeDetails { get; set; }
            public string challengeVerificationUrl { get; set; }

            [JsonProperty("lix_checkpoint.list.washing.prevention.pwd.reset")]
            public string lix_checkpointlistwashingpreventionpwdreset { get; set; }

            [JsonProperty("lix_checkpoint.reset.password.navbar.ctas.hide")]
            public string lix_checkpointresetpasswordnavbarctashide { get; set; }

            [JsonProperty("lix_kryptonite.idv.get.fingerprint")]
            public string lix_kryptoniteidvgetfingerprint { get; set; }
        }

        public class Info
        {
            public class Datum
            {
                public string scope { get; set; }
                public string handleUrn { get; set; }
                public bool isPrimary { get; set; }
                public string email { get; set; }
                public string accountId { get; set; }
                public bool needSendVerification { get; set; }
                public bool isBounced { get; set; }
                public bool isGenericScope { get; set; }
                public bool correctBounced { get; set; }
                public bool isAppleEmail { get; set; }
                public string id { get; set; }
                public string state { get; set; }
            }

            public class LixTests
            {
                public bool lixMemberCookies { get; set; }
                public bool lixCommunicationPreferencesInviteFilterEnabled { get; set; }
                public bool lixDataLogCommuteStartLocation { get; set; }
                public bool lixMergeAccountShouldCheck2FA { get; set; }
                public bool lixUnfollowUFOEnabled { get; set; }
                public bool lixConnectionsDropdownToToggle { get; set; }
                public bool lixSettingsCleanUp10222021 { get; set; }
                public bool lixDataLogMergeAccount { get; set; }
                public bool lixIncareerEnabled { get; set; }
                public bool lixLandingTabAsPrivacy { get; set; }
                public bool lixHideSettingsForInJobs { get; set; }
                public bool lixProfilePhotoVisibilityMobile { get; set; }
                public bool lixShowHelpCenterLink { get; set; }
                public bool lixAdvertisingAllowGroupsJoined { get; set; }
                public bool lixVideoAutoplay { get; set; }
                public bool lixMessageToRecruiterEnabled { get; set; }
                public bool lixCookieConsentEnforcementShared { get; set; }
                public bool lixDataExportMobileEnabled { get; set; }
                public bool lixSettingsCleanUp10292021 { get; set; }
                public bool lixShowManageCalendarSyncingLinkEnabled { get; set; }
                public bool lixShowMobileHeader { get; set; }
                public bool lixSettingsCleanUp11262021 { get; set; }
                public bool lixShowJobPosterBadge { get; set; }
                public bool lixIsSignInPromptViewEnabled { get; set; }
                public bool lixHibernateAccount { get; set; }
                public bool lixAdvertisingInterestCategoryVTChanged { get; set; }
                public bool lixSesameCredit { get; set; }
                public bool lixShowUpdatedSupplementalContent { get; set; }
                public bool lixMentionsEnabled { get; set; }
                public bool lixSalarySettingEnabled { get; set; }
                public bool lixScanningMessagesEnabled { get; set; }
                public bool lixUseDustNodeRenderer { get; set; }
                public bool lixPresenceSettingsEnabled { get; set; }
                public bool lixPremiumSubscriptionBillingsEnabled { get; set; }
                public bool lixDataLogEnabled { get; set; }
                public bool lixAdvertisingAllowConnections { get; set; }
                public bool lixMessagingSuggestionCopyEnabled { get; set; }
                public bool lixConfigSettingV1Enabled { get; set; }
                public bool lixConfigSettingV2Enabled { get; set; }
                public bool lixOptInFollowAsPrimaryButtonMobile { get; set; }
                public bool lixUpgradeToPremiumUpsell { get; set; }
                public bool lixShowGenericScopeEmail { get; set; }
                public bool lixAllowFollowAudienceBuilderCopy { get; set; }
                public bool lixMenuDiscoveryDesktopEnabled { get; set; }
                public bool lixShowAllFailedPreconditionsAccountCloseMobile { get; set; }
                public bool lixShowMicrosoftAccounts { get; set; }
                public bool lixReactivateUpsell { get; set; }
                public bool lixSelectDataExportFileJobPostings { get; set; }
                public bool lixTwoStepVerificationAuthenticatorEnhancement { get; set; }
                public bool lixAdvertisingDemographics { get; set; }
                public bool lixShowPrivacyEmailComplexUI { get; set; }
                public bool lixShowUpdatedActiveStatusDisabledCopy { get; set; }
                public bool lixPrivacyActivityBroadcastTextChange { get; set; }
                public bool lixJobApplicationSettings { get; set; }
                public bool lixEnableLanguageSettingMobile { get; set; }
                public bool lixDemographicsAffectedMembers { get; set; }
                public bool lixIsForceOptOutViewEnabled { get; set; }
                public bool lixA11yFY20Q4Desktop { get; set; }
                public bool lixGroupsNotificationsEnabled { get; set; }
                public bool lixMicrosoftServicesMobile { get; set; }
                public bool lixDataExportUXImprovements { get; set; }
                public bool lixAdvertisingDrawbridgeLegalChangesEnabled { get; set; }
                public bool lixDataLogEnterpriseProfileBindingBackFilledFlagEnabled { get; set; }
                public bool lixFeedPreferences { get; set; }
                public bool lixDataLogEnterpriseProfileBindingRemoveWithNameEnabled { get; set; }
                public bool lixMultiScopePhoneEnabled { get; set; }
                public bool lixSettingsEnableEpcEnabled { get; set; }
                public bool lixShowMicrosoftLink { get; set; }
                public bool lixGlobalDarkMode { get; set; }
                public bool lixCompanyAccountEnabled { get; set; }
                public bool lixMessageNudging { get; set; }
                public bool lixAdvertisingAllowLinkedInAudienceNetwork { get; set; }
                public bool lixSettingsCleanUp11052021 { get; set; }
                public bool isDesktopMigrationLixEnabled { get; set; }
                public bool lixShowManageContactsSyncingLinkEnabled { get; set; }
                public bool lixUnfollowShowRefollow { get; set; }
                public bool lixAdvertisingEducation { get; set; }
                public bool lixRemoveRichMediaDataExportOption { get; set; }
                public bool lixSelfIdentificationEnabled { get; set; }
                public bool lixPhonePrivacy { get; set; }
                public bool lixOffsitePrivacyManagement { get; set; }
                public bool lixAllowEmailDataExportByConnections { get; set; }
                public bool lixShowV2HeaderForEmailVisibilityMobile { get; set; }
                public bool lixFollowerInsightVisibilityEnabled { get; set; }
                public bool lixCertificationPropCopy { get; set; }
                public bool lixLeveeCommunicationsMysettingsEnabled { get; set; }
                public bool lixDataLogEnterpriseProfileBindingEnabled { get; set; }
                public bool lixShowEntityInvitationsPreferenceSettings { get; set; }
                public bool lixSettingsCleanUp11122021 { get; set; }
                public bool lixShowPermittedServicesCreationDate { get; set; }
                public bool lixViewCreditCardsEnabled { get; set; }
                public bool lixShowMicrosoftHandle { get; set; }
                public bool lixDemographicsEquityV2 { get; set; }
                public bool lixManageSettingsToPsettingsRedirect { get; set; }
                public bool lixUpdatedEventsPreconditionsForAccountClosure { get; set; }
                public bool lixAdvertisingAllowCompaniesFollowed { get; set; }
                public bool lixTwoStepStandalone { get; set; }
                public bool lixPermittedServicesToastNotification { get; set; }
                public bool lixGDPRVectorImageEnabled { get; set; }
                public bool lixShowUpdatedAdSubCopy { get; set; }
                public bool lixShowMobileHeaderWhite { get; set; }
                public bool lixMessageRequestNotificationsEnabled { get; set; }
                public bool lixEnhancedPhoneSettingMessage { get; set; }
                public bool lixMarketingPrefOptoutTargetingAds { get; set; }
                public bool lixToastNotificationsDesktop { get; set; }
                public bool lixI18nStandardLanguageSelector { get; set; }
                public bool lixChangePasswordStrongPasswords { get; set; }
                public bool lixManageMaxPasswordLength { get; set; }
                public bool lixShowConsentHideExpiredMobile { get; set; }
                public bool lixSelfIdentificationQueryParamMigration { get; set; }
                public bool lixCloseAccountPreconditionsApplicationAdmin { get; set; }
                public bool lixPremiumSubscriptionEnabled { get; set; }
                public bool lixAdvertisingAllowThirdPartyData { get; set; }
                public bool lixSettingsCleanUp10012021 { get; set; }
                public bool lixSmsInmailNotificationEnabled { get; set; }
                public bool lixShowConsentHideExpired { get; set; }
                public bool lixShowNewsletterInvitationsPreferenceSetting { get; set; }
                public bool lixAdvertisingAllowZipcode { get; set; }
                public bool lixMergeConnectionsMobileEnabled { get; set; }
                public bool lixAnonymizedDataResearch { get; set; }
                public bool lixMobilePremiumUpsellEnabled { get; set; }
                public bool lixAccessibilityPrivest4076 { get; set; }
                public bool lixAdvertisingJobInformation { get; set; }
                public bool lixVectorLogoEnabled { get; set; }
                public bool lixEducationPropPrivacyEnabled { get; set; }
                public bool lixDirectJobApplyMessageEnabled { get; set; }
                public bool lixOptoutTextRemoveHiring { get; set; }
                public bool lixShowPrivacyPhoneShowUpdatedUI { get; set; }
                public bool lixToastNotificationsMobile { get; set; }
                public bool lixMyPremiumEnabled { get; set; }
                public bool lixShowV2HeaderForPhoneVisibilityMobile { get; set; }
                public bool lixOptInFollowAsPrimaryButton { get; set; }
                public bool lixShowPermittedServicesCreationDateMobile { get; set; }
                public bool lixSettingsCleanUp09242021 { get; set; }
                public bool lixTwitterDisabled { get; set; }
                public bool lixPsettingsTwitterAccountEnabled { get; set; }
                public bool lixSettingsCleanUp10082021 { get; set; }
                public bool lixAccessibilityPrivest4481 { get; set; }
                public bool lixRichMediaDocuments { get; set; }
            }

            public class SecondaryEmailAddressList
            {
                public string scope { get; set; }
                public string handleUrn { get; set; }
                public bool isPrimary { get; set; }
                public string email { get; set; }
                public string accountId { get; set; }
                public bool needSendVerification { get; set; }
                public bool isBounced { get; set; }
                public bool isGenericScope { get; set; }
                public bool correctBounced { get; set; }
                public bool isAppleEmail { get; set; }
                public string id { get; set; }
                public string state { get; set; }
            }

            public bool showCreatePassword { get; set; }
            public string pageTitle { get; set; }
            public bool isAppleSignInNoPasswordAccount { get; set; }
            public List<SecondaryEmailAddressList> secondaryEmailAddressList { get; set; }
            public List<Datum> data { get; set; }
            public LixTests lixTests { get; set; }
            public string createPasswordUrl { get; set; }
            public string backUrl { get; set; }
            public bool isDesktopMigrationLixEnabled { get; set; }
            public List<string> emailTypes { get; set; }
            public string helpCenterPath { get; set; }
            public bool shouldHideMobileHeader { get; set; }
            public bool isVerificationsSettingLixEnabled { get; set; }
            public string device { get; set; }
            public string handle { get; set; }
        }

        JObject GetVerifyData(string html)
        {
            JObject jo_postdata = new JObject();
            foreach (Match match in Regex.Matches(html,
                         "<input\\sname=\"(.*?)\"\\svalue=\"(.*?)\"\\s?type=\"hidden\">", RegexOptions.IgnoreCase))
            {
                var name = match.Groups[1].Value;
                var val = match.Groups[2].Value;
                jo_postdata[name] = val;
            }

            return jo_postdata;
        }

        public static LinkedinLoginInfo LoginByJsonCookie(string Cookies_JsonStr, string UserAgent)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;

            LinkedinLoginInfo loginInfo = new LinkedinLoginInfo();
            loginInfo.Cookies_JsonStr = Cookies_JsonStr;
            JArray jaCookie = null;
            try
            {
                jaCookie = JArray.Parse(loginInfo.Cookies_JsonStr);
            }
            catch
            {
            }

            if (jaCookie == null || jaCookie.Count == 0)
            {
                loginInfo.Login_ErrorMsg = "Cookie为空";
                return loginInfo;
            }

            loginInfo.Cookies_Browser = (from jt in jaCookie
                select new System.Net.Cookie
                {
                    Name = jt["name"].ToString().Trim(),
                    Value = jt["value"].ToString().Trim(),
                    Domain = jt["domain"].ToString().Trim()
                }).ToList<System.Net.Cookie>();
            WinInet_HttpResult hr = new WinInet_HttpHelper
            {
                ProxyInfo_Global = loginInfo.ProxyInfo
            }.GetHtml(new WinInet_HttpItem
            {
                Url = "https://www.linkedin.com/",
                UserAgent = UserAgent,
                Accept =
                    "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9",
                Cookie = loginInfo.Cookies_Browser_Str,
                AutoRedirect = true
            });
            loginInfo.Login_Success = (hr.HtmlString.Contains("publicIdentifier&quot;:&quot;") &&
                                       !string.IsNullOrEmpty(hr.CookieString));
            loginInfo.Login_ErrorMsg = "登录" + (loginInfo.Login_Success ? "成功" : "失败");
            if (!loginInfo.Login_Success)
            {
                return loginInfo;
            }

            loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
            System.Net.Cookie cookie = (from ck in loginInfo.Cookies_Browser
                where ck.Name == "JSESSIONID"
                select ck).FirstOrDefault<System.Net.Cookie>();
            if (cookie != null)
            {
                loginInfo.Csrf_Token = cookie.Value.Trim().Replace("\"", string.Empty);
            }

            loginInfo.Fsd_Profile = WinInet_HttpHelper.GetMidStr(hr.HtmlString, "urn:li:fsd_profile:", "&quot;");
            loginInfo.Login_Success = !string.IsNullOrEmpty(loginInfo.Fsd_Profile);
            loginInfo.Login_ErrorMsg = "获取用户信息" + (loginInfo.Login_Success ? "成功" : "失败");
            return loginInfo;
        }

        public static OperateResult AddFriend11(LinkedinLoginInfo loginInfo, Account_FBOrIns account)
        {
            OperateResult operateResult = new OperateResult();

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;

            string html = string.Empty;
            JObject jo_postdata = null;
            //提交验证码后跳转
            hi = new HttpItem();
            hi.URL =
                "https://www.linkedin.com/voyager/api/voyagerRelationshipsDashMemberRelationships?action=verifyQuotaAndCreateV2&decorationId=com.linkedin.voyager.dash.deco.relationships.InvitationCreationResultWithInvitee-2";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Language", "en,en-US;q=0.9");
            hi.Header.Add("origin", "https://www.linkedin.com");
            hi.Header.Add("csrf-token", loginInfo.Csrf_Token);
            hi.Allowautoredirect = true;

            //Cookie
            hi.Cookie = loginInfo.Cookies_Browser_Str;

            string body = "{\"invitee\":{\"inviteeUnion\":{\"memberProfile\":\"urn:li:fsd_profile:" +
                          account.FsdProfile +
                          "\"}}}";
            hi.Postdata = body;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            html = hr.Html;

            JObject jobject = null;
            try
            {
                jobject = JObject.Parse(html);
            }
            catch
            {
            }

            if (jobject == null || (!html.Contains("urn:li:fsd_invitation:") &&
                                    !html.Contains("\"code\":\"CANT_RESEND_YET\"")))
            {
                operateResult.IsSuccess = false;
                operateResult.ErrorMsg = html;
                return operateResult;
            }

            operateResult.IsSuccess = true;
            operateResult.ErrorMsg = ((jobject.SelectToken("data.value.invitationUrn") == null)
                ? "CANT_RESEND_YET"
                : jobject.SelectToken("data.value.invitationUrn").ToString().Trim());
            return operateResult;
        }

        #region 实体类封装

        public class LinkedinLoginInfo
        {
            /// <summary>
            /// 用户传入的CK
            /// </summary>
            public string Cookies_JsonStr { get; set; } = string.Empty;

            /// <summary>
            /// 使用的代理
            /// </summary>
            public WinInet_ProxyInfo ProxyInfo { get; set; } = null;

            /// <summary>
            /// 网页使用的CK列表
            /// </summary>
            public List<System.Net.Cookie> Cookies_Browser { get; set; } = null;

            /// <summary>
            /// 网页使用的CK列表
            /// </summary>
            public CookieCollection CookieCollection_Browser
            {
                get
                {
                    CookieCollection cookies = new CookieCollection();
                    this.Cookies_Browser.ForEach(c => cookies.Add(c));
                    return cookies;
                }
            }

            /// <summary>
            /// 网页使用的CK字符串
            /// </summary>
            public string Cookies_Browser_Str
            {
                get
                {
                    if (this.Cookies_Browser == null || this.Cookies_Browser.Count == 0) return string.Empty;
                    else return string.Join("; ", this.Cookies_Browser.Select(ck => $"{ck.Name}={ck.Value}"));
                }
            }

            public bool Login_Success { get; set; } = false;
            public string Login_ErrorMsg { get; set; } = string.Empty;

            #region 用户信息

            public string PublicIdentifier { get; set; } = string.Empty;
            public string Fsd_Profile { get; set; } = string.Empty;
            public string Csrf_Token { get; set; } = string.Empty;
            public string Login_Fc { get; set; } = string.Empty;

            #endregion
        }

        public class OperateResult
        {
            public bool IsSuccess { get; set; } = false;
            public string ErrorMsg { get; set; } = string.Empty;
        }

        #endregion
    }
}