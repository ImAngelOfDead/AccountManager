using BookingService.Common;
using CsharpHttpHelper;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AccountManager.Common;

namespace AccountManager.DAL
{
    public class GoogleService
    {
        static GoogleService()
        {
            // 获取验证证书的回调函数
            ServicePointManager.ServerCertificateValidationCallback += (object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error) => { return true; };
        }
        public Google_LoginInfo Google_LoginByCookie(string Cookies_JsonStr, string userAgent, string proxyAddress, string proxyUserName, string proxyPwd)
        {
            Google_LoginInfo loginInfo = new Google_LoginInfo();
            loginInfo.Cookies_JsonStr = Cookies_JsonStr;
            loginInfo.CookieCollection_Browser = StringHelper.GetCookieCollectionByCookieJsonStr(Cookies_JsonStr);
            loginInfo.UserAgent = userAgent;
            loginInfo.Proxy_Url = proxyAddress;
            loginInfo.Proxy_UserName = proxyUserName;
            loginInfo.Proxy_Pwd = proxyPwd;

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            string tempStr = string.Empty;

            //int timeSpan = 0;
            //int timeCount = 0;
            //int timeOut = 0;

            hi = new HttpItem();
            hi.URL = $"https://mail.google.com/mail/u/0/";
            hi.UserAgent = loginInfo.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip, deflate, br");
            hi.Header.Add("Accept-Language", "zh-CN,zh;q=0.9");
            hi.Allowautoredirect = false;

            //hi.Cookie = loginInfo.Cookies_Browser_Str;
            hi.ResultCookieType = CsharpHttpHelper.Enum.ResultCookieType.CookieCollection;
            hi.CookieCollection = loginInfo.CookieCollection_Browser;

            //代理
            if (!string.IsNullOrEmpty(loginInfo.Proxy_Url))
            {
                hi.ProxyIp = loginInfo.Proxy_Url;
                hi.ProxyUserName = loginInfo.Proxy_UserName;
                hi.ProxyPwd = loginInfo.Proxy_Pwd;
            }

            hr = hh.GetHtml(hi);

            loginInfo.Login_Success = hr.StatusCode == HttpStatusCode.OK;
            if (!loginInfo.Login_Success)
            {
                loginInfo.Login_ErrorMsg = $"登录失败(第1链接跳转失败)";
                return loginInfo;
            }

            //合并CK
            if (hr.CookieCollection != null) loginInfo.CookieCollection_Browser = StringHelper.UpdateCookies(loginInfo.CookieCollection_Browser, hr.CookieCollection);

            loginInfo.Login_ErrorMsg = $"登录成功";
            loginInfo.Postdata_GM_ID_KEY = StringHelper.GetMidStr(hr.Html, "var GM_ID_KEY = '", "'");
            loginInfo.Postdata_GM_BUILD_LABEL = StringHelper.GetMidStr(hr.Html, "var GM_BUILD_LABEL = '", "'");
            //var GLOBALS=[null,null,620257413,
            long num = 0;
            long.TryParse(StringHelper.GetMidStr(hr.Html, "var GLOBALS=[null,null,", ","), out num);
            loginInfo.Postdata_GLOBALS_Number = num;
            loginInfo.Postdata_LanguageStr = StringHelper.GetMidStr(hr.Html, "<html lang=\"", "\">");
            loginInfo.Postdata_Jsver = StringHelper.GetMidStr(hr.Html, "var GLOBALS=[null,null," + loginInfo.Postdata_GLOBALS_Number.ToString() + ",\"" + loginInfo.Postdata_GM_BUILD_LABEL + "\",\"", "\"");

            tempStr = StringHelper.GetMidStr(hr.Html, "\\\"sync_reasons\\\":[", "]");
            num = 0;
            long.TryParse(StringHelper.GetMidStr(tempStr, "\\\"h\\\":", "}"), out num);
            loginInfo.Postdata_GLOBALS_ID = num;

            tempStr= StringHelper.GetMidStr(hr.Html, "var GLOBALS=", "gmail.com");
            loginInfo.EMailName= StringHelper.GetMidStr(tempStr, "\"44a3f648c5\",\"", "@");
            if (!string.IsNullOrEmpty(loginInfo.EMailName)) loginInfo.EMailName += "@gmail.com";

            return loginInfo;
        }
        public bool Google_LogOut(Google_LoginInfo loginInfo)
        {
            bool result = false;
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;

            #region 注销登录
            //https://accounts.google.com/Logout?hl=pt-PT&continue=https://mail.google.com/mail/&service=mail&timeStmp=1711060848&secTok=.AG5fkS8SRZTMyEScrN2mMrtW4VZAUG3OUg&ec=GAdAFw
            hi = new HttpItem();
            hi.URL = $"https://accounts.google.com/Logout?continue=https://mail.google.com/mail/&service=mail";
            hi.UserAgent = loginInfo.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip, deflate, br");
            hi.Header.Add("Accept-Language", "zh-CN,zh;q=0.9");

            hi.Allowautoredirect = true;
            hi.IsUpdateCookie = true;
            hi.AutoRedirectCookie = true;

            hi.ResultCookieType = CsharpHttpHelper.Enum.ResultCookieType.CookieCollection;
            hi.CookieCollection = loginInfo.CookieCollection_Browser;

            //代理
            if (!string.IsNullOrEmpty(loginInfo.Proxy_Url))
            {
                hi.ProxyIp = loginInfo.Proxy_Url;
                hi.ProxyUserName = loginInfo.Proxy_UserName;
                hi.ProxyPwd = loginInfo.Proxy_Pwd;
            }

            hr = hh.GetHtml(hi);

            //<p>请稍候...</p>
            //location.replace('https:\/\/mail.google.com\/mail\/');
            loginInfo.Login_Success = hr.StatusCode == HttpStatusCode.OK && hr.Html.Contains("location.replace('https:\\/\\/mail.google.com\\/mail\\/');");
            if (!loginInfo.Login_Success)
            {
                loginInfo.Login_ErrorMsg = $"注销登录跳转失败";
                return result;
            }

            //更新Cookie
            if (hr.CookieCollection != null) loginInfo.CookieCollection_Browser = StringHelper.UpdateCookies(loginInfo.CookieCollection_Browser, hr.CookieCollection);
            #endregion

            loginInfo.Login_ErrorMsg = $"注销成功";

            return result;
        }
        public List<Google_MailInfo> Google_GetMailList(Google_LoginInfo loginInfo)
        {
            List<Google_MailInfo> mails = new List<Google_MailInfo>();
            JArray ja_Result = null;

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            string X_Gmail_BTAI = string.Empty;
            JArray ja_X_Gmail_BTAI = null;

            hi = new HttpItem();
            hi.URL = $"https://mail.google.com/sync/u/0/i/bv?hl={loginInfo.Postdata_LanguageStr}&rt=r&pt=ji";
            hi.UserAgent = loginInfo.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip, deflate, br");
            hi.Header.Add("Accept-Language", "zh-CN,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.ResultCookieType = CsharpHttpHelper.Enum.ResultCookieType.CookieCollection;
            hi.CookieCollection = loginInfo.CookieCollection_Browser;

            //特殊的协议头
            ja_X_Gmail_BTAI = JArray.Parse(Properties.Resources.JsonStr_X_Gmail_BTAI);

            ja_X_Gmail_BTAI[2][36] = loginInfo.Postdata_LanguageStr;
            ja_X_Gmail_BTAI[2][37] = loginInfo.UserAgent;

            ja_X_Gmail_BTAI[4] = loginInfo.Postdata_GM_ID_KEY;
            ja_X_Gmail_BTAI[7] = loginInfo.Postdata_GM_BUILD_LABEL;
            ja_X_Gmail_BTAI[15] = loginInfo.Postdata_GLOBALS_Number;
            ja_X_Gmail_BTAI[18] = long.Parse(StringHelper.GetUnixTime(DateTime.Now));
            ja_X_Gmail_BTAI[20] = loginInfo.Postdata_GLOBALS_ID;
            hi.Header.Add("X-Gmail-BTAI", JsonConvert.SerializeObject(ja_X_Gmail_BTAI));

            hi.Method = "POST";
            hi.ContentType = "application/json";
            hi.Postdata = "[[49,50,null,\"((in:^i ((in:^smartlabel_personal) OR (in:^t))) OR (in:^i -in:^smartlabel_promo -in:^smartlabel_social))\",[null,null,null,null,0],\"itemlist-ViewType(49)-3\",3,2000,null,0,null,null,null,1,null,null,null,10,0],0,[null,1,null,null,0,1,1,null,1],[null,0,null,0]]";

            hr = hh.GetHtml(hi);

            try { ja_Result = JArray.Parse(hr.Html); } catch { }

            //["er",null,null,null,null,401,null,null,null,16]
            //[0,null,[[["","",1710337629288,"thread-f:1793418990364943209",[["
            if (hr.Html.StartsWith("[\"er\"") || ja_Result.SelectToken("[2]") == null) { Console.WriteLine($"获取邮件列表失败 >>> {hr.Html}"); }
            else
            {
                mails = ja_Result.SelectToken("[2]").Select(jt =>
                {
                    Google_MailInfo mail = new Google_MailInfo();
                    try
                    {
                        mail.Title = jt[0][1].ToString().Trim();
                        mail.ShortMsg = jt[0][2].ToString().Trim();
                        mail.Thread_f = jt[0][3].ToString().Trim();
                        mail.Msg_f_List = jt[0][4].Select(jta => jta[0].ToString().Trim()).ToList();
                    }
                    catch { mail = null; }
                    if (string.IsNullOrEmpty(mail.Thread_f) || mail.Msg_f_List.Count == 0) mail = null;
                    return mail;
                }).Where(m => m != null).ToList();
            }

            return mails;
        }
        public bool Google_GetMailDetail(Google_LoginInfo loginInfo, Google_MailInfo mail)
        {
            bool isSuccess = false;
            JArray ja_Result = null;

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            string X_Gmail_BTAI = string.Empty;
            JArray ja_X_Gmail_BTAI = null;
            JArray ja_postdata = null;

            hi = new HttpItem();
            hi.URL = $"https://mail.google.com/sync/u/0/i/fd?hl={loginInfo.Postdata_LanguageStr}&rt=r&pt=ji";
            hi.UserAgent = loginInfo.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip, deflate, br");
            hi.Header.Add("Accept-Language", "zh-CN,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.ResultCookieType = CsharpHttpHelper.Enum.ResultCookieType.CookieCollection;
            hi.CookieCollection = loginInfo.CookieCollection_Browser;

            //特殊的协议头
            ja_X_Gmail_BTAI = JArray.Parse(Properties.Resources.JsonStr_X_Gmail_BTAI);

            ja_X_Gmail_BTAI[2][36] = loginInfo.Postdata_LanguageStr;
            ja_X_Gmail_BTAI[2][37] = loginInfo.UserAgent;

            ja_X_Gmail_BTAI[4] = loginInfo.Postdata_GM_ID_KEY;
            ja_X_Gmail_BTAI[7] = loginInfo.Postdata_GM_BUILD_LABEL;
            ja_X_Gmail_BTAI[15] = loginInfo.Postdata_GLOBALS_Number;
            ja_X_Gmail_BTAI[18] = long.Parse(StringHelper.GetUnixTime(DateTime.Now));
            ja_X_Gmail_BTAI[20] = loginInfo.Postdata_GLOBALS_ID;
            hi.Header.Add("X-Gmail-BTAI", JsonConvert.SerializeObject(ja_X_Gmail_BTAI));

            //postdata
            ja_postdata = JArray.Parse("[[[\"\",null,[]]],2]");
            ja_postdata[0][0][0] = mail.Thread_f;
            for (int i = 0; i < mail.Msg_f_List.Count; i++) { ((JArray)(ja_postdata[0][0][2])).Add(mail.Msg_f_List[i]); }

            hi.Method = "POST";
            hi.ContentType = "application/json";
            hi.Postdata = $"{JsonConvert.SerializeObject(ja_postdata)}";

            hr = hh.GetHtml(hi);

            try { ja_Result = JArray.Parse(hr.Html); } catch { }

            //["er",null,null,null,null,401,null,null,null,16]
            //[0,null,[[["","",1710337629288,"thread-f:1793418990364943209",[["
            isSuccess = !hr.Html.StartsWith("[\"er\"") && hr.Html.Contains(mail.Thread_f) && ja_Result != null && ja_Result.SelectToken("[1][0][2]").Count() > 0;

            //[1][0][2][1][1][5][1][0][2][1]
            //[1][0][2][0][1][5][1][0][2][1]
            if (isSuccess) mail.MailHtmls = ja_Result.SelectToken("[1][0][2]").Select(jt => { return jt.SelectToken("[1][5][1][0][2][1]") == null ? string.Empty : jt.SelectToken("[1][5][1][0][2][1]").ToString().Trim(); }).Where(s => s.Trim().Length > 0).ToList();

            return isSuccess;
        }
        public bool Google_Delete(Google_LoginInfo loginInfo, Google_MailInfo mailInfo)
        {
            bool result = false;

            JArray ja_Result = null;

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            string X_Gmail_BTAI = string.Empty;
            JArray ja_X_Gmail_BTAI = null;
            JArray ja_postdata = null;
            long now = 0;

            hi = new HttpItem();
            //hi.URL = $"https://mail.google.com/sync/u/0/i/bv?hl={loginInfo.Postdata_LanguageStr}&rt=r&pt=ji";
            hi.URL = $"https://mail.google.com/sync/u/0/i/bv?rt=r&pt=ji";
            hi.UserAgent = loginInfo.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip, deflate, br");
            hi.Header.Add("Accept-Language", "zh-CN,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.ResultCookieType = CsharpHttpHelper.Enum.ResultCookieType.CookieCollection;
            hi.CookieCollection = loginInfo.CookieCollection_Browser;

            now = long.Parse(StringHelper.GetUnixTime(DateTime.Now));
            //特殊的协议头
            ja_X_Gmail_BTAI = JArray.Parse(Properties.Resources.JsonStr_X_Gmail_BTAI);

            ja_X_Gmail_BTAI[2][36] = loginInfo.Postdata_LanguageStr;
            ja_X_Gmail_BTAI[2][37] = loginInfo.UserAgent;

            ja_X_Gmail_BTAI[4] = loginInfo.Postdata_GM_ID_KEY;
            ja_X_Gmail_BTAI[7] = loginInfo.Postdata_GM_BUILD_LABEL;
            ja_X_Gmail_BTAI[15] = loginInfo.Postdata_GLOBALS_Number;
            ja_X_Gmail_BTAI[18] = now;
            ja_X_Gmail_BTAI[20] = loginInfo.Postdata_GLOBALS_ID;
            hi.Header.Add("X-Gmail-BTAI", JsonConvert.SerializeObject(ja_X_Gmail_BTAI));
            hi.Header.Add("Origin", "https://mail.google.com");
            hi.Referer = "https://mail.google.com/mail/u/0/";

            //ja_postdata
            ja_postdata = JArray.Parse(Properties.Resources.JsonStr_Postdata_Delete_Gmail);
            ja_postdata[1][0][0][1][0] = mailInfo.Thread_f;
            ja_postdata[1][0][0][1][1][7][1][0] = mailInfo.Msg_f_List[0];
            ja_postdata[1][0][1][1][0] = mailInfo.Thread_f;
            ja_postdata[1][0][1][1][1][6][2][0] = mailInfo.Msg_f_List[0];
            ja_postdata[1][0][1][1][1][6][3] = now;
            ja_postdata[1][0][1][1][1][6][8][0] = now - 1;
            ja_postdata[2][1] = loginInfo.Postdata_GLOBALS_ID;
            ja_postdata[3][0] = now - (new Random().Next(10000, 20000));
            ja_postdata[3][2] = now + (new Random().Next(50, 150));

            hi.Method = "POST";
            hi.ContentType = "application/json";
            hi.Postdata = JsonConvert.SerializeObject(ja_postdata);
            //hi.PostDataType = CsharpHttpHelper.Enum.PostDataType.Byte;
            //hi.PostdataByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ja_postdata));

            hr = hh.GetHtml(hi);

            try { ja_Result = JArray.Parse(hr.Html); } catch { }

            //["er",null,null,null,null,401,null,null,null,16]
            //[0,null,[[["","",1710337629288,"thread-f:1793418990364943209",[["
            if (hr.Html.StartsWith("[\"er\"")) { Console.WriteLine($"删除邮件失败 >>> {hr.Html}"); }
            else { Console.WriteLine($"删除邮件成功 >>> {hr.Html}"); result = true; }

            return result;
        }
    }
    /// <summary>
    /// 用户登录数据类
    /// </summary>
    public class Google_LoginInfo
    {
        /// <summary>
        /// UserAgent
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;
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
        /// <summary>
        /// 用户传入的CK
        /// </summary>
        public string Cookies_JsonStr { get; set; } = string.Empty;
        /// <summary>
        /// 网页使用的CK列表
        /// </summary>
        public CookieCollection CookieCollection_Browser { get; set; } = null;
        /// <summary>
        /// 网页使用的CK字符串
        /// </summary>
        public string Cookies_Browser_Str
        {
            get
            {
                if (this.CookieCollection_Browser == null || this.CookieCollection_Browser.Count == 0) return string.Empty;
                else return string.Join("; ", this.CookieCollection_Browser.Cast<Cookie>().Select(ck => $"{ck.Name}={ck.Value}"));
            }
        }
        public bool Login_Success { get; set; } = false;
        public string Login_ErrorMsg { get; set; } = string.Empty;

        #region 后续请求所需要的参数
        public string EMailName { get; set; } = string.Empty;
        public string Postdata_GM_ID_KEY { get; set; } = string.Empty;
        public string Postdata_GM_BUILD_LABEL { get; set; } = string.Empty;
        public long Postdata_GLOBALS_Number { get; set; } = 0;
        public string Postdata_LanguageStr { get; set; } = string.Empty;
        public long Postdata_GLOBALS_ID { get; set; } = 0;
        public string Postdata_Jsver { get; internal set; }
        #endregion
    }
    /// <summary>
    /// 邮件信息实体类
    /// </summary>
    public class Google_MailInfo
    {
        public string Title { get; set; } = string.Empty;
        public string ShortMsg { get; set; } = string.Empty;
        public string Thread_f { get; set; } = string.Empty;
        public List<string> Msg_f_List { get; set; } = new List<string>();
        public List<string> MailHtmls { get; set; } = new List<string>();
    }
}
