using AccountManager.Common;
using AccountManager.Models;
using CsharpHttpHelper;
using Jurassic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenPop.Pop3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace AccountManager.DAL
{
    /// <summary>
    /// FacebookAPI
    /// </summary>
    public class InstagramService
    {
        private ScriptEngine scriptEngine = null;

        #region 密码加密算法
        /// <summary>
        /// 密码加密
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        private string Ins_Encpass_Method(string pwd, string publicKey, string keyId)
        {
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;

            hi = new HttpItem();
            hi.URL = $"http://127.0.0.1:30005/FB_Ins/GetInsEncPwd?pwd={pwd}&publicKey={publicKey}&keyId={keyId}";
            hi.UserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");

            hi.Timeout = 3000;

            hr = hh.GetHtml(hi);

            string encPwd = string.Empty;
            JObject joRet = null;
            try { joRet = JObject.Parse(hr.Html); } catch { }
            if (joRet != null && joRet.SelectToken("Data") != null) encPwd = joRet.SelectToken("Data").ToString().Trim();

            return encPwd;
        }
        #endregion

        public InstagramService()
        {
            this.scriptEngine = new ScriptEngine(); this.scriptEngine.Execute(Properties.Resources.FB);
            //this.DllInit_Ins_Encpass();
        }

        /// <summary>
        /// 检测CK是否有效
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject Ins_LoginByCookie(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["isNeedLoop"] = false;

            if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
            account.LoginInfo.CookieCollection = StringHelper.GetCookieCollectionByCookieJsonStr(account.Facebook_CK);

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            bool isNeedLoop = false;
            bool isSuccess = false;

            JObject jr = null;
            int tryTimes = 0;
            int tryTimesMax = 2;
            int trySpan = 500;

            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/data/manifest.json/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9;q=0.9");
            hi.Referer = hi.URL;
            hi.Allowautoredirect = false;

            hi.Timeout = 20000;
            hi.Header.Add("Sec-Fetch-Site", "same-origin");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //判断
            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr != null)
            {
                if (jr.SelectToken("gcm_sender_id") != null)
                {
                    //判断并设置语言为英文
                    int ckIndex = account.LoginInfo.CookieCollection.Cast<Cookie>().ToList().FindIndex(ck => ck.Name == "ig_lang");
                    if (ckIndex != -1) account.LoginInfo.CookieCollection[ckIndex].Value = "en";
                    else account.LoginInfo.CookieCollection.Add(new Cookie() { Name = "ig_lang", Value = "en", Domain = ".instagram.com", Expires = DateTime.Now, });

                    //访问主页，获取ID
                    hi.URL = $"https://www.instagram.com";
                    tryTimes = 0;
                    tryTimesMax = 2;
                    while ((string.IsNullOrEmpty(account.LoginInfo.LoginData_Account_Id) || string.IsNullOrEmpty(account.LoginInfo.LoginData_UserName)) && tryTimes < tryTimesMax)
                    {
                        if (tryTimes > 0) { Thread.Sleep(trySpan); Application.DoEvents(); }
                        tryTimes += 1;

                        hr = hh.GetHtml(hi);
                        //"actorID":"17841458514222190"
                        //"username":"ho_devrin.au"
                        account.LoginInfo.LoginData_Account_Id = StringHelper.GetMidStr(hr.Html, "\"actorID\":\"", "\"");
                        account.LoginInfo.LoginData_UserName = StringHelper.GetMidStr(hr.Html, "\"username\":\"", "\"");
                    }
                    if (string.IsNullOrEmpty(account.LoginInfo.LoginData_Account_Id) || string.IsNullOrEmpty(account.LoginInfo.LoginData_UserName))
                    {
                        jo_Result["ErrorMsg"] = "Cookie无效";
                    }
                    else
                    {
                        isSuccess = true;
                        jo_Result["ErrorMsg"] = "Cookie有效";
                    }
                }
                else
                {
                    //未知的状态
                    isNeedLoop = true;
                    jo_Result["ErrorMsg"] = "Cookie无效(未知的状态)";
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(hr.RedirectUrl))
                {
                    //https://www.instagram.com/accounts/login/?next=https%3A%2F%2Fwww.instagram.com%2Fdata%2Fmanifest.json%2F&is_from_rle
                    if (hr.RedirectUrl.Contains("https://www.instagram.com/accounts/login/?next=https"))
                    {
                        jo_Result["ErrorMsg"] = "Cookie无效";
                    }
                    else if (hr.RedirectUrl == "https://www.instagram.com/" && !string.IsNullOrEmpty(hr.Cookie) && hr.Cookie.Contains("sessionid=deleted;"))
                    {
                        jo_Result["ErrorMsg"] = "Cookie无效";
                    }
                    else if (hr.RedirectUrl.Contains("https://www.instagram.com/challenge/?next="))
                    {
                        jo_Result["ErrorMsg"] = $"Cookie无效(暂时有验证:{hr.RedirectUrl})";
                    }
                    else
                    {
                        //未知的状态
                        isNeedLoop = true;
                        jo_Result["ErrorMsg"] = "Cookie无效(未知的状态)";
                    }
                }
                else
                {
                    //未知的状态
                    isNeedLoop = true;
                    jo_Result["ErrorMsg"] = "Cookie无效(未知的状态)";
                }
            }

            jo_Result["Success"] = isSuccess;
            jo_Result["isNeedLoop"] = isNeedLoop;

            return jo_Result;
        }
        /// <summary>
        /// 绑定新邮箱
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject Ins_BindNewEmail(Account_FBOrIns account, MailInfo mail)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["IsMailUsed"] = false;

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            int timeSpan = 0;
            int timeCount = 0;
            int timeOut = 0;
            string confirmCode = string.Empty;
            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;
            bool isSuccess = false;
            bool isBinded = false;
            DateTime sendCodeTime = DateTime.Parse("1970-01-01");

            string encrypted_context = string.Empty;
            string errorMsg = string.Empty;

            #region 先访问联系人页面
            account.Running_Log = $"绑邮箱:进入目标页面(contact_points)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/personal_info/contact_points";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (isSuccess) { if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie); }
            #endregion

            #region 获取API所需要的参数
            //{"connectionClass":"EXCELLENT"}
            string __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            //"{"server_revision":1014268370,
            string __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            //"cavalry_get_lid":"7381512262390677322"
            string __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
            string __dyn = "7xeUmwlEnwn8K2Wmh0no6u5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O0H8-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2exa0GE6-3u360hq1Iwqo5u0i67E5y2-2K";
            string __csr = "hIrf94ERhlZWkLOqqaCOQiXBNlAsyfiiHWiRtblmzOqmy6IIKBAEySmCgDGRiAvJqG8HBnoCcJ2uBJCXhy9ExVRFlAl4KJtAhO9q-gzWzWJ2FVUTz5IAM-hGenp5WzuqcludzXx2AFe9-8V4bhHDCgp-agiCyUm-VA00jHi1Ey8-0ewg0Ha0JrJk5obWxGaWAKHGEd8pwJw0EUg5He0iSaCwhvxy8Wg0Ti5WwbKmOA4O04AhWUCzpQa42eu5aCOwBx6biWxSdxaSit4BjVHye9xN4Byu4EKiFE3ggWh03T84K4EkU";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            string fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            string lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            string server_timestamps = "true";
            string __hs = StringHelper.GetMidStr(hr.Html, "\"haste_session\":\"", "\"");
            string dpr = StringHelper.GetMidStr(hr.Html, "\"pr\":", ",");
            string __spin_t = StringHelper.GetMidStr(hr.Html, "\"__spin_t\":", ",");
            string __spin_b = StringHelper.GetMidStr(hr.Html, "\"__spin_b\":\"", "\"");
            #endregion

            #region 获取现有的联系方式列表，判断是否已经绑定过
            account.Running_Log = $"绑邮箱:获取绑定列表";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterAddContactPointQuery";
            doc_id = "7318565184899119";
            variables = "{\"contact_point_type\":\"email\",\"interface\":\"IG_WEB\",\"is_confirming_pending\":false,\"normalized_contact_point\":\"\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (isSuccess) { if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie); }

            html = hr.Html;
            if (html.Contains("\"errorSummary\":\"")) html = StringHelper.Usc2ConvertToAnsi(html);

            //获取所有的联系方式
            jr = null;
            try { jr = JObject.Parse(html); } catch { }
            if (jr == null || jr.SelectToken("data['fxcal_settings'].node['all_contact_points']") == null)
            {
                jo_Result["ErrorMsg"] = $"获取联系方式列表失败({hr.Html})";
                return jo_Result;
            }
            JToken jo_contacts = jr.SelectToken("data['fxcal_settings'].node['all_contact_points']");
            //判断邮箱是否被自己绑定过
            JToken jt_Find = jo_contacts.Where(jta => jta["normalized_contact_point"] != null && jta["normalized_contact_point"].ToString().Trim() == mail.Mail_Name).FirstOrDefault();
            if (jt_Find != null && jt_Find.SelectToken("['contact_point_info'][0]['contact_point_status']") != null)
            {
                if (jt_Find.SelectToken("['contact_point_info'][0]['contact_point_status']").ToString() == "CONFIRMED")
                {
                    jo_Result["IsMailUsed"] = true;
                    jo_Result["Success"] = true;
                    jo_Result["ErrorMsg"] = $"邮箱已绑定[{mail.Mail_Name}]";
                    return jo_Result;
                }
                else isBinded = true;
            }
            #endregion

            #region 请求绑定新的邮箱
            if (!isBinded)
            {
                account.Running_Log = $"绑邮箱:请求绑定新的邮箱[{mail.Mail_Name}]";

                hi = new HttpItem();
                hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en-US;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "FXAccountsCenterAddContactPointMutation";
                doc_id = "6970150443042883";
                variables = "{\"country\":\"US\",\"contact_point\":\"" + mail.Mail_Name + "\",\"contact_point_type\":\"email\",\"selected_accounts\":[\"" + account.LoginInfo.LoginData_Account_Id + "\"],\"family_device_id\":\"device_id_fetch_ig_did\",\"client_mutation_id\":\"mutation_id_" + StringHelper.GetUnixTime(DateTime.Now) + "\"}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = "0";
                jo_postdata["__a"] = "1";
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = "24";
                jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                jo_postdata["jazoest"] = string.Empty;
                jo_postdata["lsd"] = lsd;
                jo_postdata["__spin_r"] = __rev;
                jo_postdata["__spin_b"] = string.Empty;
                jo_postdata["__spin_t"] = string.Empty;
                jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                jo_postdata["server_timestamps"] = server_timestamps;
                jo_postdata["doc_id"] = doc_id;
                #endregion

                hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                sendCodeTime = DateTime.UtcNow.AddMinutes(-1);
                hr = hh.GetHtml(hi);

                //合并CK
                if (isSuccess) { if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie); }

                //判断是否成功
                jr = null;
                try { jr = JObject.Parse(hr.Html); } catch { }
                if (jr == null)
                {
                    jo_Result["ErrorMsg"] = $"绑邮箱:绑定邮箱失败({hr.Html})";
                    return jo_Result;
                }
                else if (jr.SelectToken("errors[0].description") != null)
                {
                    if (jr.SelectToken("errors[0].description").ToString().Contains("\"challenge_type\":\"reauth\""))
                    {
                        encrypted_context = StringHelper.GetMidStr(jr.SelectToken("errors[0].description").ToString(), "\"encrypted_context\":\"", "\"");
                        if (!string.IsNullOrEmpty(encrypted_context))
                        {
                            #region 整理提交数据
                            fb_api_caller_class = "RelayModern";
                            fb_api_req_friendly_name = "TwoStepVerificationRootQuery";
                            doc_id = "8096727293670907";
                            variables = "{\"encryptedContext\":\"" + encrypted_context + "\",\"isLoginChallenges\":false}";

                            jo_postdata = new JObject();
                            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                            jo_postdata["__user"] = "0";
                            jo_postdata["__a"] = "1";
                            jo_postdata["__req"] = string.Empty;
                            jo_postdata["__hs"] = string.Empty;
                            jo_postdata["dpr"] = string.Empty;
                            jo_postdata["__ccg"] = __ccg;
                            jo_postdata["__rev"] = __rev;
                            jo_postdata["__s"] = string.Empty;
                            jo_postdata["__hsi"] = __hsi;
                            jo_postdata["__dyn"] = __dyn;
                            jo_postdata["__csr"] = __csr;
                            jo_postdata["__comet_req"] = "24";
                            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                            jo_postdata["jazoest"] = string.Empty;
                            jo_postdata["lsd"] = lsd;
                            jo_postdata["__spin_r"] = __rev;
                            jo_postdata["__spin_b"] = string.Empty;
                            jo_postdata["__spin_t"] = string.Empty;
                            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                            jo_postdata["server_timestamps"] = server_timestamps;
                            jo_postdata["doc_id"] = doc_id;
                            #endregion

                            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                            //Cookie
                            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                            //代理
                            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                            hr = hh.GetHtml(hi);

                            jr = null;
                            try { jr = JObject.Parse(hr.Html); } catch { }
                            if (jr == null)
                            {
                                jo_Result["ErrorMsg"] = $"绑邮箱:绑定邮箱失败({hr.Html})";
                                return jo_Result;
                            }
                            else if (jr.SelectToken("data['xfb_two_factor_login_default_method'].method['method_representation']") != null)
                            {
                                jo_Result["ErrorMsg"] = $"绑邮箱:绑定邮箱失败(需要验证其它方式:{jr.SelectToken("data['xfb_two_factor_login_default_method'].method['method_representation']").ToString().Trim()})";
                                return jo_Result;
                            }
                            else
                            {
                                jo_Result["ErrorMsg"] = $"绑邮箱:绑定邮箱失败({hr.Html})";
                                return jo_Result;
                            }
                        }
                        else
                        {
                            jo_Result["ErrorMsg"] = $"绑邮箱:绑定邮箱失败({hr.Html})";
                            return jo_Result;
                        }
                    }
                    else if (jr.SelectToken("errors[0].description").ToString().Contains("\"challenge_type\":\"block\""))
                    {
                        #region 查询错误描述
                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        fb_api_req_friendly_name = "SecuredActionBlockDialogQuery";
                        doc_id = "6108889802569432";
                        variables = "{\"accountType\":\"FACEBOOK\"}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__user"] = "0";
                        jo_postdata["__a"] = "1";
                        jo_postdata["__req"] = string.Empty;
                        jo_postdata["__hs"] = string.Empty;
                        jo_postdata["dpr"] = string.Empty;
                        jo_postdata["__ccg"] = __ccg;
                        jo_postdata["__rev"] = __rev;
                        jo_postdata["__s"] = string.Empty;
                        jo_postdata["__hsi"] = __hsi;
                        jo_postdata["__dyn"] = __dyn;
                        jo_postdata["__csr"] = __csr;
                        jo_postdata["__comet_req"] = "24";
                        jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                        jo_postdata["jazoest"] = string.Empty;
                        jo_postdata["lsd"] = lsd;
                        jo_postdata["__spin_r"] = __rev;
                        jo_postdata["__spin_b"] = string.Empty;
                        jo_postdata["__spin_t"] = string.Empty;
                        jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                        jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                        jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                        jo_postdata["server_timestamps"] = server_timestamps;
                        jo_postdata["doc_id"] = doc_id;
                        #endregion

                        hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                        //Cookie
                        hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                        //代理
                        if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                        hr = hh.GetHtml(hi);

                        //合并CK
                        if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                        #endregion

                        jr = null;
                        try { jr = JObject.Parse(hr.Html); } catch { }
                        if (jr == null || jr.SelectToken("data['xfb_secured_action'].content['block_message']") == null || string.IsNullOrEmpty(jr.SelectToken("data['xfb_secured_action'].content['block_message']").ToString().Trim()))
                        {
                            jo_Result["ErrorMsg"] = $"绑定邮箱失败({hr.Html})";
                        }
                        else
                        {
                            errorText = $"{jr.SelectToken("data['xfb_secured_action'].content['block_message']").ToString().Replace("\r\n", string.Empty).Replace("\n", string.Empty).Trim()}";
                            //This is because we noticed you are using a device you don't usually use and we need to keep your account safe.We'll allow you to make this change after you've used this device for a while.
                            if (errorText.Contains("using a device you don't usually use and we need to keep your account safe")) errorText = $"需要验证设备:{errorText}";
                            jo_Result["ErrorMsg"] = $"绑定邮箱失败({errorText})";
                        }
                        return jo_Result;
                    }
                    else
                    {
                        jo_Result["ErrorMsg"] = $"绑定邮箱失败({hr.Html})";
                        return jo_Result;
                    }
                }
                else if (jr.SelectToken("data.xfb_add_contact_point.success") == null || jr.SelectToken("data.xfb_add_contact_point.error_text") == null)
                {
                    jo_Result["ErrorMsg"] = $"绑邮箱:绑定邮箱失败({hr.Html})";
                    return jo_Result;
                }
                else if (jr.SelectToken("data.xfb_add_contact_point.success").ToString().ToLower() != "true")
                {
                    errorText = jr.SelectToken("data.xfb_add_contact_point.error_text").ToString().Trim();
                    if (errorText.Contains("That email is already being used"))
                    {
                        jo_Result["Success"] = false;
                        jo_Result["ErrorMsg"] = $"绑邮箱:邮箱已被占用[{mail.Mail_Name}]";
                        jo_Result["IsMailUsed"] = true;
                        return jo_Result;
                    }
                    else
                    {
                        jo_Result["Success"] = false;
                        jo_Result["ErrorMsg"] = $"绑邮箱:{errorText}";
                        return jo_Result;
                    }
                }
            }
            #endregion

            #region 去邮箱提取验证码
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

                if (mail.Pop3Client != null && mail.Pop3Client.Connected) try { mail.Pop3Client.Disconnect(); } catch { }
                mail.Pop3Client = Pop3Helper.GetPop3Client(mail.Mail_Name, mail.Mail_Pwd);
                if (mail.Pop3Client == null) continue;

                msgList = Pop3Helper.GetMessageByIndex(mail.Pop3Client);
                pop3MailMessage = msgList.Where(m => m.DateSent >= sendCodeTime && m.Subject.ToLower().Contains("confirm email") && m.From.Contains("<no-reply@mail.instagram.com>")).FirstOrDefault();
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
            //You may be asked to enter this confirmation code: &nbsp; 512703 &nbsp;</span>
            //You may be asked to enter this confirmation code:\r\n\r\n547937\r\n\r\nThanks
            confirmCode = StringHelper.GetMidStr(pop3MailMessage.Html, "You may be asked to enter this confirmation code:</td></td>", "</td></td>").Trim();
            Match match = new Regex(@"\d{6}").Match(confirmCode);
            if (match.Success) confirmCode = match.Value;
            else
            {
                jo_Result["ErrorMsg"] = "提取邮件验证码失败";
                return jo_Result;
            }
            #endregion

            #region 输入验证码进行验证
            account.Running_Log = $"绑邮箱:输入验证码进行验证[{mail.Mail_Name}]";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterContactPointConfirmationDialogVerifyContactPointMutation";
            doc_id = "8108292719198518";
            //{"contact_point":"cribliavidie1987@rambler.ru","contact_point_type":"email","pin_code":"428176","selected_accounts":["17841458514222190"],"family_device_id":"device_id_fetch_ig_did","client_mutation_id":"mutation_id_1720317282089","contact_point_event_type":"ADD","normalized_contact_point_to_replace":""}
            variables = "{\"contact_point\":\"" + mail.Mail_Name + "\",\"contact_point_type\":\"email\",\"pin_code\":\"" + confirmCode + "\",\"selected_accounts\":[\"" + account.LoginInfo.LoginData_Account_Id + "\"],\"family_device_id\":\"device_id_fetch_ig_did\",\"client_mutation_id\":\"mutation_id_" + StringHelper.GetUnixTime(DateTime.Now) + "\",\"contact_point_event_type\":\"ADD\",\"normalized_contact_point_to_replace\":\"\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (isSuccess) { if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie); }

            //判断结果
            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr == null || jr.SelectToken("data['xfb_verify_contact_point'][0]['mutation_data'].success") == null || jr.SelectToken("data['xfb_verify_contact_point'][0]['mutation_data'].error_text") == null)
            {
                jo_Result["ErrorMsg"] = $"绑邮箱:校验验证码失败({hr.Html})";
                return jo_Result;
            }

            //You can't access certain settings for a few days: This is because we noticed a new login from a device you don't usually use. You can still access these settings from a device you've logged in with in the past
            isSuccess = jr.SelectToken("data['xfb_verify_contact_point'][0]['mutation_data'].success").ToString().ToLower() == "true";
            jo_Result["Success"] = isSuccess;
            if (!isSuccess)
            {
                errorMsg = jr.SelectToken("data['xfb_verify_contact_point'][0]['mutation_data'].error_text").ToString().Trim();
                if (errorMsg.Contains("This is because we noticed a new login from a device you don't usually use")) jo_Result["ErrorMsg"] = $"需要验证设备:{errorMsg}";
                else jo_Result["ErrorMsg"] = $"{errorMsg}";
            }
            else { jo_Result["ErrorMsg"] = $"邮箱已绑定[{mail.Mail_Name}]"; jo_Result["IsMailUsed"] = true; }
            #endregion

            return jo_Result;
        }
        /// <summary>
        /// 忘记密码
        /// </summary>
        /// <param name="account"></param>
        /// <param name="newPassword"></param>
        /// <param name="logMeOut">是否关闭其他会话</param>
        /// <returns></returns>
        public JObject Ins_ForgotPassword(Account_FBOrIns account, string newPassword, bool logMeOut)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;

            if (string.IsNullOrEmpty(account.New_Mail_Name) || string.IsNullOrEmpty(account.New_Mail_Pwd))
            {
                jo_Result["ErrorMsg"] = "忘记密码失败(未绑定新邮箱)";
                return jo_Result;
            }
            if (!string.IsNullOrEmpty(account.Facebook_Pwd))
            {
                jo_Result["ErrorMsg"] = "忘记密码失败(已经有Facebook密码)";
                return jo_Result;
            }

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jo_params = null;
            JObject jr = null;
            string html = string.Empty;
            int timeSpan = 0;
            int timeCount = 0;
            int timeOut = 0;
            //bool isSuccess = false;
            string confirmUrl = string.Empty;
            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;

            string __ccg = string.Empty;
            string __rev = string.Empty;
            string __hsi = string.Empty;
            string __dyn = string.Empty;
            string __csr = string.Empty;
            string fb_dtsg = string.Empty;
            string lsd = string.Empty;
            string server_timestamps = string.Empty;

            string password_reset_uri = string.Empty;
            string encryptPwd1 = string.Empty;
            string encryptPwd2 = string.Empty;
            DateTime sendCodeTime = DateTime.Parse("1970-01-01");

            string ck_ig_did = string.Empty;
            string ck_datr = string.Empty;
            string ck_mid = string.Empty;
            string ck_mid_new = string.Empty;
            string ck_CSRFToken = string.Empty;
            string randomUUID = string.Empty;
            string appId = string.Empty;
            CookieCollection ck_Reset_Pwd = new CookieCollection();
            string uidb36 = string.Empty;
            string token = string.Empty;
            string source = string.Empty;
            string X_Instagram_AJAX = string.Empty;
            string X_BLOKS_VERSION_ID = string.Empty;
            string X_ASBD_ID = string.Empty;
            string X_IG_WWW_Claim = string.Empty;
            string state_id = string.Empty;
            string public_key = string.Empty;
            string key_id = string.Empty;
            string __hs = string.Empty;
            string __spin_t = string.Empty;
            string route_url = string.Empty;
            string encrypted_data = string.Empty;
            string params_forState_id = string.Empty;

            #region 先访问目标页面
            account.Running_Log = $"忘记密码:进入目标页面(password/change)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/password_and_security/password/change/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            #endregion

            #region 获取API所需要的参数
            //{"connectionClass":"EXCELLENT"}
            __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            //"{"server_revision":1014268370,
            __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            //"cavalry_get_lid":"7381512262390677322"
            __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
            __dyn = "7xeUjG1mxu1syUbFp41twpUnwgU7SbzEdF8aUco2qwJxS0k24o0B-q1ew65xO0FE2awpUO0n24o4a786a3a1YwBgao6C0Mo2swaOfK0EUjwGzEaE2iwNwKwHw8Xwn8e87q7U88138bpEbUGdG1QwTU9UaQ0Lo6-3u2WE5B08-269wr86C1mwPwUQp1yUd8K6V8aUuxK3OqcyU-2K";
            __csr = "glNAnkcPW4YhaB4h7iIAaRQyiniAGGFWh4haLGiWCRrKHAKVliLK8xe-m8AWgTDyaUHKGxu4HKFKXWhVA4k54265qK8wDGi54aUsQ6Voymq68hy8G9xWuEO16gkCw0dou00xj9U4wx9U1lE9Ea8gw0KXDga185OgB190oEigak19yUkidw18u0uYx9Q0rG4qx20ebwso";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";
            #endregion

            #region 选择Ins账号
            account.Running_Log = $"忘记密码:选择Ins账号";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterChangePasswordDialogQuery";
            doc_id = "7728877477131487";
            //{"account_id":"17841458514222190","account_name":"ho_devrin.au","account_type":"INSTAGRAM","interface":"IG_WEB"}
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_name\":\"" + account.LoginInfo.LoginData_UserName + "\",\"account_type\":\"INSTAGRAM\",\"interface\":\"IG_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            //判断结果
            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr == null || jr.SelectToken("data['fxcal_settings'].node['public_key_and_id_for_encryption']['public_key']") == null)
            {
                jo_Result["ErrorMsg"] = $"获取忘记密码的链接失败({hr.Html})";
                return jo_Result;
            }

            //password_reset_uri = jr.SelectToken("data['fxcal_settings'].node['password_reset_uri']").ToString();
            //cuid = StringHelper.GetMidStr(password_reset_uri + "&", "cuid=", "&");
            #endregion

            #region 向选择的Ins账号发送改密链接
            account.Running_Log = $"忘记密码:向选择的Ins账号发送改密链接[{account.New_Mail_Name}]";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useFXSettingsSendPasswordResetLinkMutation";
            doc_id = "5486099721405023";
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\",\"client_mutation_id\":\"" + randomUUID + "\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = "t";
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            sendCodeTime = DateTime.UtcNow.AddMinutes(-1);
            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            //判断结果
            //向邮箱发送验证码失败({"data":{"submit_contact_point":null},"errors":[{"message":"A server error field_exception occured. Check server logs for details.","severity":"CRITICAL","mids":["456557e14288d1acdebc1454f5040333"],"code":1675030,"api_error_code":-1,"summary":"Query error","description":"Error performing query.","description_raw":"Error performing query.","is_silent":false,"is_transient":false,"is_not_critical":false,"requires_reauth":false,"allow_user_retry":false,"debug_info":null,"query_path":null,"fbtrace_id":"AK7HbCZypE1","www_request_id":"A5W4xXfpu3EujxRmeiZlNho","sentry_block_user_info":{"sentry_block_data":"Aeh9injaeTbUpNVcq_MXt9-y2D3SLVZiMzhCHI0pheZuauaDITIhWciutkjINEkWFefGhzD_CUHk5bSjmG3ildBMdD_tNf_e5uI8ygoxHU-kS_crrZ1CLpuJCVW_DfpRZpE_iwtMSaOiyYtc13zXLTyv_ixtKrngm1skoBG04enYokQKTDJ8bgFuDHfq608Bk3r62wyhR6NGHiiqZ309Vu5MFunWp6NTnElqohhos0qDSHIPcVwZ16aiRmoY5-j1W3DKFxoRGG90LXIMDocUVYecqNEspcrF-NPDM-DeNdybE-RjUvgUp10RQaceqk8NOqWruEuDnAhPX-i9t7vEv8dnskEirdXl-Hm5afc3RFClYA"},"path":["submit_contact_point"]}],"extensions":{"is_final":true}})
            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr == null || jr.SelectToken("data['xfb_send_password_reset_link'].success") == null || jr.SelectToken("data['xfb_send_password_reset_link'].success").ToString().ToLower() != "true")
            {
                jo_Result["ErrorMsg"] = $"忘记密码:向选择的Ins账号发送改密链接失败({hr.Html})";
                return jo_Result;
            }
            #endregion

            #region 提取忘记密码链接
            account.Running_Log = $"忘记密码:提取忘记密码链接[{account.New_Mail_Name}]";
            timeSpan = 500;
            timeCount = 0;
            timeOut = 30000;
            Pop3Client pop3Client = null;
            List<Pop3MailMessage> msgList = null;
            Pop3MailMessage pop3MailMessage = null;
            while (pop3MailMessage == null && timeCount < timeOut)
            {
                Thread.Sleep(timeSpan);
                Application.DoEvents();
                timeCount += timeSpan;

                pop3Client = Pop3Helper.GetPop3Client(account.New_Mail_Name, account.New_Mail_Pwd);
                if (pop3Client == null) continue;

                msgList = Pop3Helper.GetMessageByIndex(pop3Client);
                pop3MailMessage = msgList.Where(m => m.DateSent >= sendCodeTime && m.Subject.Contains("Reset your password") && m.From.Contains("<security@mail.instagram.com>")).FirstOrDefault();
            }

            if (pop3Client == null)
            {
                jo_Result["ErrorMsg"] = "忘记密码:Pop3连接失败";
                return jo_Result;
            }
            pop3Client.Disconnect();
            if (pop3MailMessage == null)
            {
                jo_Result["ErrorMsg"] = "忘记密码:没有找到指定的邮件信息";
                return jo_Result;
            }
            //568242 is your Facebook account recovery code
            //https://instagram.com/accounts/password/reset/confirm/?uidb36=qssnnii&token=zWYGbrHWhjHXbpYQyVYsSsNPwnEoFlUpWDs91mk8u4QIO87HBDYGnP2Qn5VSKPAC:password_reset_email&s=password_reset_email
            //<a href="https://instagram.com/accounts/password/reset/confirm/?uidb36=7bhz51a&amp;token=yzDgytKJkrEysDn0hAcvQtbC8r0dx1jL6TPbHjGwQama7pHscr6qrcdT9rBb65o8:password_reset_email&amp;s=password_reset_email" style="color:#1b74e4;text-decoration:none;display:block;width:370px;">
            confirmUrl = "https://instagram.com/accounts/password/reset/confirm/" + StringHelper.GetMidStr(pop3MailMessage.Html, "<a href=\"https://instagram.com/accounts/password/reset/confirm/", "\"").Replace("&amp;", "&").Trim();
            if (string.IsNullOrEmpty(confirmUrl))
            {
                jo_Result["ErrorMsg"] = "忘记密码:提取忘记密码链接失败";
                return jo_Result;
            }
            #endregion

            #region 打开忘记密码连接进行验证
            account.Running_Log = $"忘记密码:打开忘记密码连接进行验证";
            hi = new HttpItem();
            hi.URL = $"{confirmUrl}";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = true;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            //hi.Cookie = string.Join("; ", ck_Reset_Pwd.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}"));
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            //if (hr.Cookie != null) ck_Reset_Pwd = StringHelper.UpdateCookies(ck_Reset_Pwd, hr.Cookie);
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            ck_ig_did = StringHelper.GetMidStr(hr.Html, "\"device_id\":\"", "\"");
            if (ck_ig_did.LastIndexOf("|") > -1) ck_ig_did = ck_ig_did.Substring(ck_ig_did.LastIndexOf("|") + 1, ck_ig_did.Length - ck_ig_did.LastIndexOf("|") - 1);
            if (string.IsNullOrEmpty(ck_ig_did))
            {
                jo_Result["ErrorMsg"] = $"忘记密码时打开忘记密码链接失败(ck_ig_did获取失败)";
                return jo_Result;
            }
            ck_datr = StringHelper.GetMidStr(hr.Html, "\"_js_datr\":{\"value\":\"", "\"");
            ck_mid = StringHelper.GetMidStr(hr.Html, "\"mid\":{\"value\":\"", "\"");
            ck_CSRFToken = StringHelper.GetMidStr(hr.Html, "\"csrf_token\":\"", "\"");//
            appId = StringHelper.GetMidStr(hr.Html, "\"X-IG-App-ID\":\"", "\"");//
            X_Instagram_AJAX = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            X_BLOKS_VERSION_ID = StringHelper.GetMidStr(hr.Html, "\"versioningID\":\"", "\"");//
            X_ASBD_ID = "129477";//
            X_IG_WWW_Claim = StringHelper.GetMidStr(hr.Html, "\"claim\":\"", "\"");

            __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            __hsi = StringHelper.GetMidStr(hr.Html, "\"cavalry_get_lid\":\"", "\"");
            __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");

            uidb36 = StringHelper.GetMidStr(confirmUrl + "&", "uidb36=", "&");
            token = StringHelper.GetMidStr(confirmUrl + "&", "token=", "&");
            source = StringHelper.GetMidStr(confirmUrl + "&", "s=", "&");

            key_id = StringHelper.GetMidStr(hr.Html, "\"key_id\":\"", "\"");
            public_key = StringHelper.GetMidStr(hr.Html, "\"public_key\":\"", "\"");
            //","haste_session":"19914.HYP:instagram_web_pkg.2.1..0.1"
            __hs = StringHelper.GetMidStr(hr.Html, "\"haste_session\":\"", "\"");
            __spin_t = StringHelper.GetMidStr(hr.Html, "\"__spin_t\":", ",");
            //{"uidb36":"5gsztb9","token":"gCdj2yOdMU1dZpbtv83slSfFSAzt8fFPVnicWzy7LqCQ5Gk9lwLxw5g8H0zOdNks:password_reset_email","s":"password_reset_email","is_caa":null,"afv":null}
            //{"afv":"","is_caa":false,"is_web":true,"source":"password_reset_email","token":"gCdj2yOdMU1dZpbtv83slSfFSAzt8fFPVnicWzy7LqCQ5Gk9lwLxw5g8H0zOdNks:password_reset_email","uidb36":"5gsztb9"}
            params_forState_id = StringHelper.GetMidStr(StringHelper.GetMidStr(hr.Html, "\\/accounts\\/password\\/reset\\/confirm\\/?", "\"routePath\""), "\"params\":", "},") + "}";
            jo_postdata = null;
            try { jo_postdata = JObject.Parse(params_forState_id); } catch { }
            jo_params = new JObject();
            if (jo_postdata.ContainsKey("afv")) jo_params["afv"] = jo_postdata["afv"] == null ? string.Empty : jo_postdata["afv"].ToString();
            if (jo_postdata.ContainsKey("is_caa")) jo_params["is_caa"] = jo_postdata["is_caa"] == null || jo_postdata["is_caa"].ToString().Trim().Length == 0 ? false : jo_postdata["is_caa"].Value<bool>();
            jo_params["is_web"] = true;
            if (jo_postdata.ContainsKey("token")) jo_params["token"] = jo_postdata["token"].ToString();
            if (jo_postdata.ContainsKey("s")) jo_params["source"] = jo_postdata["s"].ToString();
            if (jo_postdata.ContainsKey("uidb36")) jo_params["uidb36"] = jo_postdata["uidb36"].ToString();
            params_forState_id = JsonConvert.SerializeObject(jo_params);
            #endregion

            #region ajax/qm/
            account.Running_Log = $"忘记密码:跳转至下一个页面[ajax/qm/]";
            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/ajax/qm/?__a=1&__user=0&__comet_req=7&jazoest=26274";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Referer = confirmUrl;
            hi.Header.Add("Origin", "https://www.instagram.com");

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            jo_postdata = new JObject();
            jo_postdata["event_id"] = __hsi;
            jo_postdata["marker_page_time"] = $"{StringHelper.GetRandomNumber(1000, 2000)}";
            jo_postdata["script_path"] = "XPolarisLoggedOutPasswordResetController";
            jo_postdata["weight"] = "0";
            jo_postdata["client_start"] = "1";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            //判断结果
            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (string.IsNullOrEmpty(StringHelper.GetMidStr(hr.Html, ",\"lid\":\"", "\"").Trim()))
            {
                jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败(跳转页面[ajax/qm/]失败)";
                return jo_Result;
            }
            #endregion

            #region 跳转至下一个页面 route-definition
            account.Running_Log = $"忘记密码:跳转至下一个页面[route-definition]";
            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/ajax/route-definition/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("X-IG-D", "www");
            hi.Header.Add("X-FB-LSD", lsd);
            hi.Header.Add("X-ASBD-ID", X_ASBD_ID);
            hi.Referer = confirmUrl;
            hi.Header.Add("Origin", "https://www.instagram.com");

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            jo_postdata = new JObject();
            route_url = $"/accounts/password/reset/confirm/?uidb36=iq25xl9&token={token}&s=password_reset_email";
            jo_postdata["route_url"] = StringHelper.UrlEncode(route_url);
            jo_postdata["routing_namespace"] = "igx_www";
            jo_postdata["__d"] = "www";
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = "1";
            jo_postdata["__hs"] = StringHelper.UrlEncode(__hs);
            jo_postdata["dpr"] = "2";
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = StringHelper.UrlEncode("ooffng:9m73hh:whr0of");
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "7";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = "26274";
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = "trunk";
            jo_postdata["__spin_t"] = __spin_t;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            //判断结果
            jr = null;
            try { jr = JObject.Parse(hr.Html.Replace("for (;;);", string.Empty)); } catch { }
            if (!hr.Html.Contains("\"payload\":{\"error\":false"))
            {
                jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败(跳转页面[route-definition]失败)";
                return jo_Result;
            }
            #endregion

            #region 跳转至下一个页面 ig_sso_users
            account.Running_Log = $"忘记密码:跳转至下一个页面[ig_sso_users]";
            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/api/v1/web/fxcal/ig_sso_users/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("X-IG-WWW-Claim", "0");
            hi.Header.Add("X-Requested-With", "XMLHttpRequest");
            hi.Header.Add("X-CSRFToken", ck_CSRFToken);
            hi.Header.Add("X-IG-App-ID", appId);
            hi.Header.Add("X-Instagram-AJAX", X_Instagram_AJAX);
            hi.Header.Add("X-ASBD-ID", X_ASBD_ID);
            hi.Referer = confirmUrl;
            hi.Header.Add("Origin", "https://www.instagram.com");

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null)
            {
                account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                var cks = account.LoginInfo.CookieCollection.Cast<Cookie>().Where(ck => ck.Name != "th_eu_pref"
                && ck.Name != "ig_direct_region_hint"
                && !ck.Name.StartsWith("fbm_"));
                account.LoginInfo.CookieCollection = new CookieCollection();
                foreach (var ck in cks) { account.LoginInfo.CookieCollection.Add(ck); }
            }

            //判断结果
            if (!hr.Html.Contains("\"status\":\"ok\""))
            {
                jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败(跳转页面[ig_sso_users]失败)";
                return jo_Result;
            }
            #endregion

            #region 获取state_id
            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/async/wbloks/fetch/?appid=com.instagram.account_security.password_reset&type=app&__bkv={X_BLOKS_VERSION_ID}";
            hi.UserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Origin", "https://www.instagram.com");
            hi.Referer = confirmUrl;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded;charset=UTF-8";

            #region 整理提交数据
            jo_postdata = new JObject();
            jo_postdata["__d"] = "www";
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = "4";
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = "2";
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = "cpgitg%3Apgyvs3%3A45ivxz";
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "7";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["params"] = StringHelper.UrlEncode(params_forState_id);
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //"id":"495502426"
            state_id = StringHelper.GetMidStr(hr.Html, "\"id\":\"", "\"");
            if (string.IsNullOrEmpty(state_id))
            {
                jo_Result["ErrorMsg"] = $"忘记密码时获取state_id失败({hr.Html})";
                return jo_Result;
            }
            #endregion

            #region 跳转至下一个页面 route-definition
            //account.Running_Log = $"忘记密码:跳转至下一个页面[route-definition]";
            //hi = new HttpItem();
            //hi.URL = $"https://www.instagram.com/ajax/route-definition/";
            //hi.UserAgent = account.UserAgent;
            //hi.Accept = $"*/*";
            //hi.Header.Add("Accept-Encoding", "gzip");
            //hi.Header.Add("Accept-Language", "en-US;q=0.9");
            //hi.Allowautoredirect = false;

            //hi.Header.Add("X-IG-D", "www");
            //hi.Header.Add("X-FB-LSD", lsd);
            //hi.Header.Add("X-ASBD-ID", X_ASBD_ID);
            //hi.Referer = confirmUrl;
            //hi.Header.Add("Origin", "https://www.instagram.com");

            //hi.Header.Add("Sec-Fetch-Site", "same-origin");
            //hi.Method = "POST";
            //hi.ContentType = $"application/x-www-form-urlencoded";

            //#region 整理提交数据
            //jo_postdata = new JObject();
            //route_url = $"/";
            ////jo_postdata["route_urls[0]"] = StringHelper.UrlEncode(route_url);
            //jo_postdata["routing_namespace"] = "igx_www";
            //jo_postdata["trace_policy"] = "polaris.LoggedOutPasswordResetPage";
            //jo_postdata["__d"] = "www";
            //jo_postdata["__user"] = "0";
            //jo_postdata["__a"] = "1";
            //jo_postdata["__req"] = "4";
            //jo_postdata["__hs"] = StringHelper.UrlEncode(__hs);
            //jo_postdata["dpr"] = "2";
            //jo_postdata["__ccg"] = __ccg;
            //jo_postdata["__rev"] = __rev;
            //jo_postdata["__s"] = StringHelper.UrlEncode("ooffng:9m73hh:whr0of");
            //jo_postdata["__hsi"] = __hsi;
            //jo_postdata["__dyn"] = __dyn;
            //jo_postdata["__csr"] = __csr;
            //jo_postdata["__comet_req"] = "7";
            //jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            //jo_postdata["jazoest"] = "26274";
            //jo_postdata["lsd"] = lsd;
            //jo_postdata["__spin_r"] = __rev;
            //jo_postdata["__spin_b"] = "trunk";
            //jo_postdata["__spin_t"] = __spin_t;
            //#endregion

            //hi.Postdata = "route_url=" + StringHelper.UrlEncode(route_url) + "&" + string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            ////Cookie
            //hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            ////代理
            //if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            //hr = hh.GetHtml(hi);

            ////合并CK
            //if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            ////判断结果
            //jr = null;
            //try { jr = JObject.Parse(hr.Html.Replace("for (;;);", string.Empty)); } catch { }
            //if (!hr.Html.Contains("\"payload\":{\"error\":false"))
            //{
            //    jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败(跳转页面[route-definition]失败)";
            //    return jo_Result;
            //}
            #endregion

            #region 获取 encrypted_data
            //account.Running_Log = $"忘记密码:获取encrypted_data";
            //hi = new HttpItem();
            //hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            //hi.UserAgent = account.UserAgent;
            //hi.Accept = $"*/*";
            //hi.Header.Add("Accept-Encoding", "gzip");
            //hi.Header.Add("Accept-Language", "en-US;q=0.9");
            //hi.Allowautoredirect = false;

            //hi.Header.Add("Sec-Fetch-Site", "same-origin");
            //hi.Method = "POST";
            //hi.ContentType = $"application/x-www-form-urlencoded";

            //#region 整理提交数据
            //randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

            //fb_api_caller_class = "RelayModern";
            //fb_api_req_friendly_name = "PolarisAPIGetFrCookieQuery";
            //doc_id = "6534825339958064";
            //variables = "{\"_request_data\":{},\"payload\":\"ASBizHgv_n66OR7_oDpKv1vr016Mulra4iLHUBRP3cX99HdphZ8sOBI3GNmx5JaiQoTXh1A-MzE2vq1cEmC3Y6pZtJCAkMYO70XCA7VYbljNd90mhSdeT_14Q2heK5QxlSCZkg\"}";

            //jo_postdata = new JObject();
            //jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            //jo_postdata["__user"] = "0";
            //jo_postdata["__a"] = "1";
            //jo_postdata["__req"] = "7";
            //jo_postdata["__hs"] = string.Empty;
            //jo_postdata["dpr"] = string.Empty;
            //jo_postdata["__ccg"] = __ccg;
            //jo_postdata["__rev"] = __rev;
            //jo_postdata["__s"] = string.Empty;
            //jo_postdata["__hsi"] = __hsi;
            //jo_postdata["__dyn"] = __dyn;
            //jo_postdata["__csr"] = __csr;
            //jo_postdata["__comet_req"] = "7";
            //jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            //jo_postdata["jazoest"] = string.Empty;
            //jo_postdata["lsd"] = lsd;
            //jo_postdata["__spin_r"] = __rev;
            //jo_postdata["__spin_b"] = string.Empty;
            //jo_postdata["__spin_t"] = string.Empty;
            //jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            //jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            //jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            //jo_postdata["server_timestamps"] = server_timestamps;
            //jo_postdata["doc_id"] = doc_id;
            //#endregion

            //hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            ////Cookie
            //hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            ////代理
            //if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            //hr = hh.GetHtml(hi);

            ////合并CK
            //if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            ////判断结果
            ////向邮箱发送验证码失败({"data":{"submit_contact_point":null},"errors":[{"message":"A server error field_exception occured. Check server logs for details.","severity":"CRITICAL","mids":["456557e14288d1acdebc1454f5040333"],"code":1675030,"api_error_code":-1,"summary":"Query error","description":"Error performing query.","description_raw":"Error performing query.","is_silent":false,"is_transient":false,"is_not_critical":false,"requires_reauth":false,"allow_user_retry":false,"debug_info":null,"query_path":null,"fbtrace_id":"AK7HbCZypE1","www_request_id":"A5W4xXfpu3EujxRmeiZlNho","sentry_block_user_info":{"sentry_block_data":"Aeh9injaeTbUpNVcq_MXt9-y2D3SLVZiMzhCHI0pheZuauaDITIhWciutkjINEkWFefGhzD_CUHk5bSjmG3ildBMdD_tNf_e5uI8ygoxHU-kS_crrZ1CLpuJCVW_DfpRZpE_iwtMSaOiyYtc13zXLTyv_ixtKrngm1skoBG04enYokQKTDJ8bgFuDHfq608Bk3r62wyhR6NGHiiqZ309Vu5MFunWp6NTnElqohhos0qDSHIPcVwZ16aiRmoY5-j1W3DKFxoRGG90LXIMDocUVYecqNEspcrF-NPDM-DeNdybE-RjUvgUp10RQaceqk8NOqWruEuDnAhPX-i9t7vEv8dnskEirdXl-Hm5afc3RFClYA"},"path":["submit_contact_point"]}],"extensions":{"is_final":true}})
            //jr = null;
            //try { jr = JObject.Parse(hr.Html); } catch { }
            //if (jr == null || jr.SelectToken("data['xdt_api__v1__web__accounts__get_encrypted_credentials'].fr") == null || jr.SelectToken("data['xdt_api__v1__web__accounts__get_encrypted_credentials'].fr").ToString().Trim().Length == 0)
            //{
            //    jo_Result["ErrorMsg"] = $"忘记密码:获取encrypted_data失败({hr.Html})";
            //    return jo_Result;
            //}
            //encrypted_data = jr.SelectToken("data['xdt_api__v1__web__accounts__get_encrypted_credentials'].fr").ToString().Trim();
            #endregion

            #region 跳转至下一个页面 sync/instagram
            //account.Running_Log = $"忘记密码:跳转至下一个页面[sync/instagram]";
            //hi = new HttpItem();
            //hi.URL = $"https://www.instagram.com/sync/instagram/";
            //hi.UserAgent = account.UserAgent;
            //hi.Accept = $"*/*";
            //hi.Header.Add("Accept-Encoding", "gzip");
            //hi.Header.Add("Accept-Language", "en-US;q=0.9");
            //hi.Allowautoredirect = false;

            //hi.Header.Add("X-FB-LSD", lsd);
            //hi.Header.Add("X-ASBD-ID", X_ASBD_ID);

            //hi.Referer = confirmUrl;
            //hi.Header.Add("Origin", "https://www.instagram.com");

            //hi.Header.Add("Sec-Fetch-Site", "same-origin");
            //hi.Method = "POST";
            //hi.ContentType = $"application/x-www-form-urlencoded";

            //#region 整理提交数据
            //jo_postdata = new JObject();
            //route_url = $"/";
            //jo_postdata["encrypted_data"] = encrypted_data;
            //jo_postdata["__d"] = "www";
            //jo_postdata["__user"] = "0";
            //jo_postdata["__a"] = "1";
            //jo_postdata["__req"] = "8";
            //jo_postdata["__hs"] = StringHelper.UrlEncode(__hs);
            //jo_postdata["dpr"] = "2";
            //jo_postdata["__ccg"] = __ccg;
            //jo_postdata["__rev"] = __rev;
            //jo_postdata["__s"] = StringHelper.UrlEncode("ooffng:9m73hh:whr0of");
            //jo_postdata["__hsi"] = __hsi;
            //jo_postdata["__dyn"] = __dyn;
            //jo_postdata["__csr"] = __csr;
            //jo_postdata["__comet_req"] = "7";
            //jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            //jo_postdata["jazoest"] = "26274";
            //jo_postdata["lsd"] = lsd;
            //jo_postdata["__spin_r"] = __rev;
            //jo_postdata["__spin_b"] = "trunk";
            //jo_postdata["__spin_t"] = __spin_t;
            //#endregion

            //hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            ////Cookie
            //hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            ////代理
            //if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            //hr = hh.GetHtml(hi);

            ////合并CK
            //if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            ////判断结果
            //jr = null;
            //try { jr = JObject.Parse(hr.Html.Replace("for (;;);", string.Empty)); } catch { }
            //if (jr == null || jr.SelectToken($"lid") == null)
            //{
            //    jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败(跳转页面[sync/instagram]失败)";
            //    return jo_Result;
            //}
            #endregion

            #region 修改密码
            account.Running_Log = $"忘记密码:修改密码[{newPassword}]";
            if (string.IsNullOrEmpty(newPassword)) newPassword = $"Ins_{account.LoginInfo.LoginData_Account_Id}";

            //暂时先固定密码
            encryptPwd1 = this.Ins_Encpass_Method(newPassword, public_key, key_id);
            encryptPwd2 = this.Ins_Encpass_Method(newPassword, public_key, key_id);

            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/api/v1/bloks/apps/com.instagram.account_security.password_reset_submit_action_handler/";
            hi.UserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("X-IG-WWW-Claim", X_IG_WWW_Claim);
            hi.Header.Add("X-Requested-With", "XMLHttpRequest");
            hi.Header.Add("X-CSRFToken", ck_CSRFToken);
            hi.Header.Add("X-IG-App-ID", appId);
            hi.Header.Add("X-Instagram-AJAX", X_Instagram_AJAX);
            hi.Header.Add("X-BLOKS-VERSION-ID", X_BLOKS_VERSION_ID);
            hi.Header.Add("X-ASBD-ID", X_ASBD_ID);

            hi.Header.Add("Origin", "https://www.instagram.com");
            hi.Referer = confirmUrl;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            jo_postdata = new JObject();
            jo_postdata["enc_new_password1"] = StringHelper.UrlEncode(encryptPwd1);
            jo_postdata["enc_new_password2"] = StringHelper.UrlEncode(encryptPwd2);
            jo_postdata["uidb36"] = uidb36;
            jo_postdata["token"] = StringHelper.UrlEncode(token);
            jo_postdata["error_state"] = StringHelper.UrlEncode("{\"state_id\":" + state_id + ",\"index\":0,\"type_name\":\"str\"}");
            jo_postdata["source"] = source;
            jo_postdata["nest_data_manifest"] = "true";
            if (jo_params.ContainsKey("afv")) jo_postdata["afv"] = jo_params["afv"].ToString().Trim();
            if (jo_params.ContainsKey("is_caa")) jo_postdata["is_caa"] = jo_params["is_caa"].ToString().Trim();
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //判断结果
            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr == null || jr.SelectToken("status") == null || jr.SelectToken("status").ToString().Trim() != "ok")
            {
                jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败(status属性不存在)";
                return jo_Result;
            }
            if (jr.SelectToken("layout['bloks_payload'].tree['bk.components.internal.Action'].handler") == null)
            {
                jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败(handler属性不存在)";
                return jo_Result;
            }
            if (!jr.SelectToken("layout['bloks_payload'].tree['bk.components.internal.Action'].handler").ToString().Trim().Contains("\\/accounts\\/login\\/?confirmReset=1"))
            {
                if (jr.SelectToken("layout['bloks_payload'].tree['bk.components.internal.Action'].handler").ToString().Trim().Contains("\\/two_factor\\/two_factor_login\\/?"))
                {
                    jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败(需要验证2FA)";
                    return jo_Result;
                }
                else
                {
                    jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败(需要跳转其它链接:{jr.SelectToken("layout['bloks_payload'].tree['bk.components.internal.Action'].handler").ToString().Trim()})";
                    return jo_Result;
                }
            }

            //合并CK
            if (hr.Cookie != null)
            {
                account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                var cks = account.LoginInfo.CookieCollection.Cast<Cookie>().Where(ck => ck.Name != "th_eu_pref"
                && ck.Name != "ig_direct_region_hint"
                && !ck.Name.StartsWith("fbm_"));
                account.LoginInfo.CookieCollection = new CookieCollection();
                foreach (var ck in cks) { account.LoginInfo.CookieCollection.Add(ck); }
            }
            #endregion

            #region 跳转至下一个页面 route-definition
            //account.Running_Log = $"忘记密码:跳转至下一个页面[route-definition]";
            //hi = new HttpItem();
            //hi.URL = $"https://www.instagram.com/ajax/route-definition/";
            //hi.UserAgent = account.UserAgent;
            //hi.Accept = $"*/*";
            //hi.Header.Add("Accept-Encoding", "gzip");
            //hi.Header.Add("Accept-Language", "en-US;q=0.9");
            //hi.Allowautoredirect = false;

            //hi.Header.Add("X-IG-D", "www");
            //hi.Header.Add("X-FB-LSD", lsd);
            //hi.Header.Add("X-ASBD-ID", X_ASBD_ID);
            //hi.Referer = confirmUrl;
            //hi.Header.Add("Origin", "https://www.instagram.com");

            //hi.Header.Add("Sec-Fetch-Site", "same-origin");
            //hi.Method = "POST";
            //hi.ContentType = $"application/x-www-form-urlencoded";

            //#region 整理提交数据
            //jo_postdata = new JObject();
            //route_url = $"/";
            //jo_postdata["route_url"] = "%2Faccounts%2Flogin%2F%3FconfirmReset%3D1";
            //jo_postdata["routing_namespace"] = "igx_www";
            //jo_postdata["trace_policy"] = "polaris.LoggedOutPasswordResetPage";
            //jo_postdata["__d"] = "www";
            //jo_postdata["__user"] = "0";
            //jo_postdata["__a"] = "1";
            //jo_postdata["__req"] = "9";
            //jo_postdata["__hs"] = StringHelper.UrlEncode(__hs);
            //jo_postdata["dpr"] = "2";
            //jo_postdata["__ccg"] = __ccg;
            //jo_postdata["__rev"] = __rev;
            //jo_postdata["__s"] = StringHelper.UrlEncode("ooffng:9m73hh:whr0of");
            //jo_postdata["__hsi"] = __hsi;
            //jo_postdata["__dyn"] = __dyn;
            //jo_postdata["__csr"] = __csr;
            //jo_postdata["__comet_req"] = "7";
            //jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            //jo_postdata["jazoest"] = "26274";
            //jo_postdata["lsd"] = lsd;
            //jo_postdata["__spin_r"] = __rev;
            //jo_postdata["__spin_b"] = "trunk";
            //jo_postdata["__spin_t"] = __spin_t;
            //#endregion

            //hi.Postdata = "client_previous_actor_id&" + string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            ////Cookie
            //hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            ////代理
            //if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            //hr = hh.GetHtml(hi);

            ////合并CK
            //if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            ////判断结果
            //jr = null;
            //try { jr = JObject.Parse(hr.Html.Replace("for (;;);", string.Empty)); } catch { }
            //if (jr == null || jr.SelectToken("dtsgToken") == null)
            //{
            //    jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败(跳转页面[route-definition]失败)";
            //    return jo_Result;
            //}
            #endregion

            #region 跳转至主页
            //account.Running_Log = $"忘记密码:跳转至主页(login/?confirmReset)";
            //hi = new HttpItem();
            //hi.URL = $"https://www.instagram.com/accounts/login/?confirmReset=1";
            //hi.UserAgent = account.UserAgent;
            //hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            //hi.Header.Add("Accept-Encoding", "gzip");
            //hi.Header.Add("Accept-Language", "en-US;q=0.9");
            //hi.Allowautoredirect = true;

            //hi.Referer = $"{confirmUrl}";
            //hi.Header.Add("Upgrade-Insecure-Requests", "1");
            //hi.Header.Add("Sec-Fetch-Site", "same-origin");

            ////Cookie
            //hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            ////代理
            //if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            //hr = hh.GetHtml(hi);
            //if (!hr.Html.Contains(account.LoginInfo.LoginData_Account_Id))
            //{
            //    jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败(跳转至主页login/?confirmReset失败)";
            //    return jo_Result;
            //}
            #endregion

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = "忘记密码:操作成功";

            //Cookie刷新
            account.Facebook_CK = StringHelper.GetCookieJsonStrByCookieCollection(account.LoginInfo.CookieCollection);

            return jo_Result;
        }
        /// <summary>
        /// 打开动态2FA
        /// </summary>
        /// <returns></returns>
        public JObject Ins_OpenTwoFA_Dynamic(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;

            if (string.IsNullOrEmpty(account.New_Mail_Name) || string.IsNullOrEmpty(account.New_Mail_Pwd) || string.IsNullOrEmpty(account.Facebook_Pwd))
            {
                jo_Result["ErrorMsg"] = "打开动态2FA失败(未绑定新邮箱或未进行忘记密码)";
                return jo_Result;
            }

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;

            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;

            string __ccg = string.Empty;
            string __rev = string.Empty;
            string __hsi = string.Empty;
            string __dyn = string.Empty;
            string __csr = string.Empty;
            string fb_dtsg = string.Empty;
            string lsd = string.Empty;
            string server_timestamps = string.Empty;

            string randomUUID = string.Empty;

            int timeSpan = 0;
            int timeCount = 0;
            int timeOut = 0;
            string confirmCode = string.Empty;
            string secretKey = string.Empty;
            string des = string.Empty;

            #region 先访问目标页面
            account.Running_Log = $"打开2FA:进入目标页面(two_factor)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/password_and_security/two_factor";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            #endregion

            #region 获取API所需要的参数
            //{"connectionClass":"EXCELLENT"}
            __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            //"{"server_revision":1014268370,
            __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            //"cavalry_get_lid":"7381512262390677322"
            __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0no6u5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O0H8-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2exa0GE6-3u360hq1Iwqo5u0i67E5y2-2K";
            __csr = "hIrf94ERhlZWkLOqqaCOQiXBNlAsyfiiHWiRtblmzOqmy6IIKBAEySmCgDGRiAvJqG8HBnoCcJ2uBJCXhy9ExVRFlAl4KJtAhO9q-gzWzWJ2FVUTz5IAM-hGenp5WzuqcludzXx2AFe9-8V4bhHDCgp-agiCyUm-VA00jHi1Ey8-0ewg0Ha0JrJk5obWxGaWAKHGEd8pwJw0EUg5He0iSaCwhvxy8Wg0Ti5WwbKmOA4O04AhWUCzpQa42eu5aCOwBx6biWxSdxaSit4BjVHye9xN4Byu4EKiFE3ggWh03T84K4EkU";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";

            if (string.IsNullOrEmpty(fb_dtsg)) { jo_Result["ErrorMsg"] = $"打开2FA:访问目标页面失败(two_factor)"; return jo_Result; }
            #endregion

            #region 选择Ins账号,查看是否已经开启
            account.Running_Log = $"打开2FA:选择Ins账号";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterTwoFactorSettingsDialogQuery";
            doc_id = "7613749188645358";
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\",\"interface\":\"IG_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr == null || jr.SelectToken("data['fxcal_settings'].node['two_factor_data']") == null)
            {
                jo_Result["ErrorMsg"] = $"打开2FA:选择Ins账号失败({hr.Html})";
                return jo_Result;
            }
            #endregion

            #region 如果已经开启，需要先关闭，再打开
            if (jr.SelectToken("data['fxcal_settings'].node.content['two_factor_is_on_subtitle']") != null && !string.IsNullOrEmpty(jr.SelectToken("data['fxcal_settings'].node.content['two_factor_is_on_subtitle']").ToString().Trim()))
            {
                #region 关闭2FA
                randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

                account.Running_Log = $"打开2FA:先关闭2FA";
                hi = new HttpItem();
                hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en-US;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "useFXSettingsTwoFactorDisableTOTPMutation";
                doc_id = "6758587527522047";
                variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\",\"family_device_id\":\"device_id_fetch_ig_did\"}}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = "0";
                jo_postdata["__a"] = "1";
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = "24";
                jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                jo_postdata["jazoest"] = string.Empty;
                jo_postdata["lsd"] = lsd;
                jo_postdata["__spin_r"] = __rev;
                jo_postdata["__spin_b"] = string.Empty;
                jo_postdata["__spin_t"] = string.Empty;
                jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                jo_postdata["server_timestamps"] = server_timestamps;
                jo_postdata["doc_id"] = doc_id;
                #endregion

                hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);

                //合并CK
                if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

                jr = null;
                try { jr = JObject.Parse(hr.Html); } catch { }
                if (jr == null || jr.SelectToken("data['xfb_two_fac_disable_totp'].success") == null || jr.SelectToken("data['xfb_two_fac_disable_totp'].success").ToString().ToLower() != "true")
                {
                    jo_Result["ErrorMsg"] = $"打开2FA:先关闭2FA失败({hr.Html})";
                    return jo_Result;
                }
                #endregion

                #region 重新打开目标页面，刷新参数
                account.Running_Log = $"打开2FA:再次进入目标页面(two_factor)";
                hi = new HttpItem();
                hi.URL = $"https://accountscenter.instagram.com/password_and_security/two_factor";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en-US;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "none");

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);

                //合并CK
                if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                #endregion

                #region 获取API所需要的参数
                //{"connectionClass":"EXCELLENT"}
                __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
                //"{"server_revision":1014268370,
                __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
                //"cavalry_get_lid":"7381512262390677322"
                __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
                __dyn = "7xeUmwlEnwn8K2Wmh0no6u5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O0H8-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2exa0GE6-3u360hq1Iwqo5u0i67E5y2-2K";
                __csr = "hIrf94ERhlZWkLOqqaCOQiXBNlAsyfiiHWiRtblmzOqmy6IIKBAEySmCgDGRiAvJqG8HBnoCcJ2uBJCXhy9ExVRFlAl4KJtAhO9q-gzWzWJ2FVUTz5IAM-hGenp5WzuqcludzXx2AFe9-8V4bhHDCgp-agiCyUm-VA00jHi1Ey8-0ewg0Ha0JrJk5obWxGaWAKHGEd8pwJw0EUg5He0iSaCwhvxy8Wg0Ti5WwbKmOA4O04AhWUCzpQa42eu5aCOwBx6biWxSdxaSit4BjVHye9xN4Byu4EKiFE3ggWh03T84K4EkU";
                //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
                fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
                //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
                //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
                lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
                server_timestamps = "true";

                if (string.IsNullOrEmpty(fb_dtsg)) { jo_Result["ErrorMsg"] = $"打开2FA:再次访问目标页面失败(two_factor)"; return jo_Result; }
                #endregion
            }
            #endregion

            #region 生成2FA的随机密钥和二维码(可能需要验证FB登录密码)
            string encryptPwd = string.Empty;

            randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

            account.Running_Log = $"打开2FA:生成随机密钥";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useFXSettingsTwoFactorGenerateTOTPKeyMutation";
            doc_id = "6282672078501565";
            variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\",\"device_id\":\"device_id_fetch_ig_did\",\"fdid\":\"device_id_fetch_ig_did\"}}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }

            //先判断是否有密码验证
            if (jr != null && jr.SelectToken("errors[0].description") != null && jr.SelectToken("errors[0].description").ToString().Contains("\"challenge_type\":\"password\""))
            {
                #region 进行密码验证
                account.Running_Log = $"打开2FA:进行密码验证";

                //暂时先固定密码
                //niubi.fb
                encryptPwd = this.Ins_Encpass_Method(account.Facebook_Pwd, "", "");

                hi = new HttpItem();
                hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en-US;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "FXPasswordReauthenticationMutation";
                doc_id = "5864546173675027";
                variables = "{\"input\":{\"account_id\":" + account.LoginInfo.LoginData_Account_Id + ",\"account_type\":\"INSTAGRAM\",\"password\":{\"sensitive_string_value\":\"" + encryptPwd + "\"},\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"client_mutation_id\":\"1\"}}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = "0";
                jo_postdata["__aaid"] = "0";//特殊参数
                jo_postdata["__a"] = "1";
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = "24";
                jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                jo_postdata["jazoest"] = string.Empty;
                jo_postdata["lsd"] = lsd;
                jo_postdata["__spin_r"] = __rev;
                jo_postdata["__spin_b"] = string.Empty;
                jo_postdata["__spin_t"] = string.Empty;
                jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                jo_postdata["server_timestamps"] = server_timestamps;
                jo_postdata["doc_id"] = doc_id;
                #endregion

                hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);

                //合并CK
                if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                #endregion

                //判断验证结果
                //{"data":{"xfb_password_reauth_fb_only":{"is_reauth_successful":true}},"extensions":{"is_final":true}}
                jr = null;
                try { jr = JObject.Parse(hr.Html); } catch { }
                if (jr == null || jr.SelectToken("data['xfb_password_reauth_fb_only']['is_reauth_successful']") == null || jr.SelectToken("data['xfb_password_reauth_fb_only']['is_reauth_successful']").ToString().ToLower() != "true")
                {
                    jo_Result["ErrorMsg"] = $"打开2FA:进行密码验证失败({hr.Html})";
                    return jo_Result;
                }

                #region 重新生成2FA的随机密钥和二维码
                randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

                account.Running_Log = $"打开2FA:重新生成随机密钥";
                hi = new HttpItem();
                hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en-US;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "useFXSettingsTwoFactorGenerateTOTPKeyMutation";
                doc_id = "6282672078501565";
                variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\",\"device_id\":\"device_id_fetch_ig_did\",\"fdid\":\"device_id_fetch_ig_did\"}}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = "0";
                jo_postdata["__a"] = "1";
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = "24";
                jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                jo_postdata["jazoest"] = string.Empty;
                jo_postdata["lsd"] = lsd;
                jo_postdata["__spin_r"] = __rev;
                jo_postdata["__spin_b"] = string.Empty;
                jo_postdata["__spin_t"] = string.Empty;
                jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                jo_postdata["server_timestamps"] = server_timestamps;
                jo_postdata["doc_id"] = doc_id;
                #endregion

                hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);

                //合并CK
                if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

                jr = null;
                try { jr = JObject.Parse(hr.Html); } catch { }
                #endregion
            }

            if (jr == null || jr.SelectToken("data['xfb_two_factor_generate_totp_key']['totp_key']['key_text']") == null || string.IsNullOrEmpty(jr.SelectToken("data['xfb_two_factor_generate_totp_key']['totp_key']['key_text']").ToString().Trim()))
            {
                jo_Result["ErrorMsg"] = $"打开2FA:打开2FA失败({hr.Html})";
                return jo_Result;
            }

            //记录数据
            secretKey = jr.SelectToken("data['xfb_two_factor_generate_totp_key']['totp_key']['key_text']").ToString().Replace(" ", string.Empty);
            #endregion

            #region 继续点击Next，弹出输入验证码的框
            account.Running_Log = $"打开2FA:点击Next弹出验证码提示";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterTwoFactorConfirmCodeDialogQuery";
            doc_id = "6792696137448786";
            variables = "{\"interface\":\"IG_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr == null || jr.SelectToken("data['fxcal_settings'].node.content['totp_code_subtitle']") == null || string.IsNullOrEmpty(jr.SelectToken("data['fxcal_settings'].node.content['totp_code_subtitle']").ToString().Trim()))
            {
                jo_Result["ErrorMsg"] = $"打开2FA:弹出验证码提示失败({hr.Html})";
                return jo_Result;
            }
            #endregion

            #region 获取2FA的6位验证码
            //https://2fa.live/tok/3K2TUFNSSJWGKLYKDFQXF5RWGCSWFVDG
            //{"token":"272627"}

            hi = new HttpItem();
            hi.URL = $"https://2fa.live/tok/{secretKey}";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-CN,zh;q=0.9");
            hi.Allowautoredirect = false;

            timeSpan = 500;
            timeCount = 0;
            timeOut = 30000;
            confirmCode = string.Empty;
            while (string.IsNullOrEmpty(confirmCode) && timeCount < timeOut)
            {
                hr = hh.GetHtml(hi);
                confirmCode = StringHelper.GetMidStr(hr.Html, "\"token\":\"", "\"");

                if (string.IsNullOrEmpty(confirmCode)) { Thread.Sleep(timeSpan); timeCount += timeSpan; Application.DoEvents(); }
            }

            if (string.IsNullOrEmpty(confirmCode))
            {
                jo_Result["ErrorMsg"] = $"打开2FA:获取验证码失败({hr.Html})";
                return jo_Result;
            }
            #endregion

            #region 进行打开2FA操作
            randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

            account.Running_Log = $"打开2FA:进行打开2FA操作";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useFXSettingsTwoFactorEnableTOTPMutation";
            doc_id = "7032881846733167";

            variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\",\"verification_code\":\"" + confirmCode + "\",\"device_id\":\"device_id_fetch_ig_did\",\"fdid\":\"device_id_fetch_ig_did\"}}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr == null)
            {
                jo_Result["ErrorMsg"] = $"打开2FA:进行打开2FA操作失败(Error JObject.Parse {(hr.Html.Length > 50 ? hr.Html.Substring(0, 50) : hr.Html)})";
                return jo_Result;
            }
            else if (hr.Html.Contains("This is because we noticed a new login from a device"))
            {
                jo_Result["ErrorMsg"] = $"打开2FA:进行打开2FA操作失败(需要设备验证:This is because we noticed a new login from a device)";
                return jo_Result;
            }
            else if (jr.SelectToken("data['xfb_two_factor_enable_totp'].success") == null || jr.SelectToken("data['xfb_two_factor_enable_totp'].success").ToString().ToLower().Trim() != "true")
            {
                if (jr.SelectToken("errors[0].description") == null || string.IsNullOrEmpty(jr.SelectToken("errors[0].description").ToString().Trim()))
                {
                    jo_Result["ErrorMsg"] = $"打开2FA:进行打开2FA操作失败(Error description {hr.Html})";
                    return jo_Result;
                }
                else
                {
                    des = jr.SelectToken("errors[0].description").ToString().Trim();
                    if (des.Contains("\"challenge_type\":\"block\""))
                    {
                        #region 查询错误描述
                        account.Running_Log = $"打开2FA:查询错误描述";
                        hi = new HttpItem();
                        hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                        hi.UserAgent = account.UserAgent;
                        hi.Accept = $"*/*";
                        hi.Header.Add("Accept-Encoding", "gzip");
                        hi.Header.Add("Accept-Language", "en-US;q=0.9");
                        hi.Allowautoredirect = false;

                        hi.Header.Add("Sec-Fetch-Site", "same-origin");
                        hi.Method = "POST";
                        hi.ContentType = $"application/x-www-form-urlencoded";

                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        fb_api_req_friendly_name = "SecuredActionBlockDialogQuery";
                        doc_id = "6108889802569432";
                        variables = "{\"accountType\":\"INSTAGRAM\"}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__user"] = "0";
                        jo_postdata["__a"] = "1";
                        jo_postdata["__req"] = string.Empty;
                        jo_postdata["__hs"] = string.Empty;
                        jo_postdata["dpr"] = string.Empty;
                        jo_postdata["__ccg"] = __ccg;
                        jo_postdata["__rev"] = __rev;
                        jo_postdata["__s"] = string.Empty;
                        jo_postdata["__hsi"] = __hsi;
                        jo_postdata["__dyn"] = __dyn;
                        jo_postdata["__csr"] = __csr;
                        jo_postdata["__comet_req"] = "24";
                        jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                        jo_postdata["jazoest"] = string.Empty;
                        jo_postdata["lsd"] = lsd;
                        jo_postdata["__spin_r"] = __rev;
                        jo_postdata["__spin_b"] = string.Empty;
                        jo_postdata["__spin_t"] = string.Empty;
                        jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                        jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                        jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                        jo_postdata["server_timestamps"] = server_timestamps;
                        jo_postdata["doc_id"] = doc_id;
                        #endregion

                        hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                        //Cookie
                        hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                        //代理
                        if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                        hr = hh.GetHtml(hi);

                        //合并CK
                        if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                        #endregion

                        jr = null;
                        try { jr = JObject.Parse(hr.Html); } catch { }
                        if (jr == null || jr.SelectToken("data['xfb_secured_action'].content['block_message']") == null || string.IsNullOrEmpty(jr.SelectToken("data['xfb_secured_action'].content['block_message']").ToString().Trim()))
                        {
                            jo_Result["ErrorMsg"] = $"打开2FA:进行打开2FA操作失败({hr.Html})";
                        }
                        else
                        {
                            errorText = jr.SelectToken("data['xfb_secured_action'].content['block_message']").ToString().Replace("\r\n", string.Empty).Replace("\n", string.Empty).Trim();
                            //打开2FA:进行打开2FA操作失败(This is because we noticed you are using a device you don't usually use and we need to keep your account safe.We'll allow you to make this change after you've used this device for a while.)
                            if (errorText.Contains("using a device you don't usually use and we need to keep your account safe")) errorText = $"需要设备验证:{errorText}";
                            jo_Result["ErrorMsg"] = $"打开2FA:进行打开2FA操作失败({errorText})";
                        }
                        return jo_Result;
                    }
                    else
                    {
                        jo_Result["ErrorMsg"] = $"打开2FA:进行打开2FA操作失败({hr.Html})";
                        return jo_Result;
                    }
                }
            }
            #endregion

            //记录数据
            account.TwoFA_Dynamic_Status = 1;
            account.TwoFA_Dynamic_SecretKey = secretKey;

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = "打开2FA:操作成功";

            return jo_Result;
        }
        /// <summary>
        /// 更新语言
        /// </summary>
        /// <returns></returns>
        public JObject Ins_UpdateLanguageLocale(Account_FBOrIns account, string languageName)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            //JObject jr = null;
            string html = string.Empty;
            string confirmCode = string.Empty;
            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;

            string __ccg = string.Empty;
            string __rev = string.Empty;
            string __hsi = string.Empty;
            string __dyn = string.Empty;
            string __csr = string.Empty;
            string fb_dtsg = string.Empty;
            string lsd = string.Empty;
            string server_timestamps = string.Empty;
            string deviceId = string.Empty;

            string redirect_uri = string.Empty;
            string nh = string.Empty;
            string jazoest = string.Empty;
            string checkpoint_data = string.Empty;


            #region 先访问目标页面
            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/language/preferences/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            #endregion

            #region 获取API所需要的参数
            //{"connectionClass":"EXCELLENT"}
            __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            //"{"server_revision":1014268370,
            __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            //"cavalry_get_lid":"7381512262390677322"
            __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
            __dyn = "7xeUjG1mxu1syUbFp41twpUnwgU7SbzEdF8aUco2qwJxS0k24o0B-q1ew65xO0FE2awpUO0n24o4a786a3a1YwBgao6C0Mo2swtUd8-U2zxe2GewGw9a362W2K0zK1swUwtEvw4JwJCwLyES1TwTU9UaQ0Lo6-3u2WE5B08-269wr86C1mwPwUQp1yUd8K6V8aUuxK3OqcyU-2K";
            __csr = "iMFs448lsAjln8GFiSR6EOu_ni4cBuBHuAh4VaBF7DGgx6h5nCzXVZoObyHDXAK8VEOazVoVypC_BqU4-GBzk2uFpEiDyQ-2kxEWF-5o95z8O6Foc9EqjzE01mnqa3a0gqheax62l00Ndc1zg3QwyAob-fIERxy2dsU0Uu0Au0lu8yH80bGo-2l0Ewxwe7DwTU188dU4y0Aoy";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";
            deviceId = StringHelper.GetMidStr(hr.Html, "\"clientID\":\"", "\"");
            #endregion

            #region 获取语言列表，并检测是否已经是要设置的语言
            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "LSPlatformGraphQLLightspeedRequestForIGDQuery";
            doc_id = "7289167067878883";
            variables = Properties.Resources.JsonStrDemo_LSPlatformGraphQLLightspeedRequestForIGDQuery.Replace("{deviceId}", deviceId);

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__aaid"] = "0";//特殊参数
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);
            //html = StringHelper.Usc2ConvertToAnsi(hr.Html);

            #endregion

            #region 更新语言
            //hi = new HttpItem();
            //hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            //hi.UserAgent = account.UserAgent;
            //hi.Accept = $"*/*";
            //hi.Header.Add("Accept-Encoding", "gzip");
            //hi.Header.Add("Accept-Language", "en-US;q=0.9");
            //hi.Allowautoredirect = false;

            //hi.Header.Add("Sec-Fetch-Site", "same-origin");
            //hi.Method = "POST";
            //hi.ContentType = $"application/x-www-form-urlencoded";

            //#region 整理提交数据
            //fb_api_caller_class = "RelayModern";
            //fb_api_req_friendly_name = "useCometLocaleSelectorLanguageChangeMutation";
            //doc_id = "6451777188273168";
            //variables = "{\"locale\":\"" + languageName + "\",\"referrer\":\"WWW_COMET_NAVBAR\",\"fallback_locale\":null}";

            //jo_postdata = new JObject();
            //jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            //jo_postdata["__user"] = "0";
            //jo_postdata["__aaid"] = "0";//特殊参数
            //jo_postdata["__a"] = "1";
            //jo_postdata["__req"] = string.Empty;
            //jo_postdata["__hs"] = string.Empty;
            //jo_postdata["dpr"] = string.Empty;
            //jo_postdata["__ccg"] = __ccg;
            //jo_postdata["__rev"] = __rev;
            //jo_postdata["__s"] = string.Empty;
            //jo_postdata["__hsi"] = __hsi;
            //jo_postdata["__dyn"] = __dyn;
            //jo_postdata["__csr"] = __csr;
            //jo_postdata["__comet_req"] = "24";
            //jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            //jo_postdata["jazoest"] = string.Empty;
            //jo_postdata["lsd"] = lsd;
            //jo_postdata["__spin_r"] = __rev;
            //jo_postdata["__spin_b"] = string.Empty;
            //jo_postdata["__spin_t"] = string.Empty;
            //jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            //jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            //jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            //jo_postdata["server_timestamps"] = server_timestamps;
            //jo_postdata["doc_id"] = doc_id;
            //#endregion

            //hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            ////Cookie
            //hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            ////代理
            //if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            //hr = hh.GetHtml(hi);

            ////合并CK
            //if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            ////{"data":{"updateLanguageLocale":{"success":true}},"extensions":{"is_final":true}}
            //if (!hr.Html.Contains("{\"updateLanguageLocale\":{\"success\":true}}"))
            //{
            //    jo_Result["ErrorMsg"] = $"设置语言失败({hr.Html})";
            //    return jo_Result;
            //}
            #endregion

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = $"设置语言成功({languageName})";

            //Cookie刷新
            account.Facebook_CK = StringHelper.GetCookieJsonStrByCookieCollection(account.LoginInfo.CookieCollection);

            return jo_Result;
        }
        /// <summary>
        /// 删除FB关联
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject Ins_RemoveFBAccount(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            string confirmCode = string.Empty;
            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;

            string __ccg = string.Empty;
            string __rev = string.Empty;
            string __hsi = string.Empty;
            string __dyn = string.Empty;
            string __csr = string.Empty;
            string fb_dtsg = string.Empty;
            string lsd = string.Empty;
            string server_timestamps = string.Empty;

            //string account_id = string.Empty;
            List<string> idList = null;
            JArray ja_account_idList = null;
            string randomUUID = string.Empty;
            string accountsStr = string.Empty;

            #region 先访问目标页面
            account.Running_Log = $"删FB关联:进入目标页面(accounts)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/accounts";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            #endregion

            #region 获取API所需要的参数
            //{"connectionClass":"EXCELLENT"}
            __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            //"{"server_revision":1014268370,
            __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            //"cavalry_get_lid":"7381512262390677322"
            __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0no6u5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O0H8-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2exa0GE6-3u360hq1Iwqo5u0i67E5y2-2K";
            __csr = "hIrf94ERhlZWkLOqqaCOQiXBNlAsyfiiHWiRtblmzOqmy6IIKBAEySmCgDGRiAvJqG8HBnoCcJ2uBJCXhy9ExVRFlAl4KJtAhO9q-gzWzWJ2FVUTz5IAM-hGenp5WzuqcludzXx2AFe9-8V4bhHDCgp-agiCyUm-VA00jHi1Ey8-0ewg0Ha0JrJk5obWxGaWAKHGEd8pwJw0EUg5He0iSaCwhvxy8Wg0Ti5WwbKmOA4O04AhWUCzpQa42eu5aCOwBx6biWxSdxaSit4BjVHye9xN4Byu4EKiFE3ggWh03T84K4EkU";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";

            if (string.IsNullOrEmpty(fb_dtsg))
            {
                jo_Result["ErrorMsg"] = $"删FB关联:进入目标页面失败(accounts)";
                return jo_Result;
            }

            //获取Ins账号
            accountsStr = StringHelper.GetMidStr(hr.Html, "\"accounts\":", ",\"accounts_partition_index\"");
            ja_account_idList = null;
            try { ja_account_idList = JArray.Parse(accountsStr); } catch { }
            if (ja_account_idList == null || ja_account_idList.Count() == 0)
            {
                jo_Result["Success"] = false;
                jo_Result["ErrorMsg"] = $"删FB关联:操作失败(accounts获取失败)";
                return jo_Result;
            }
            else if (ja_account_idList.Count() == 1)
            {
                jo_Result["Success"] = true;
                jo_Result["ErrorMsg"] = $"删FB关联:操作成功(无FB关联)";
                return jo_Result;
            }

            idList = ja_account_idList.Where(jt => jt["id"] != null && jt["id"].ToString().Trim() != account.LoginInfo.LoginData_Account_Id).Select(jt => jt["id"].ToString().Trim()).ToList();
            if (idList.Count == 0)
            {
                jo_Result["Success"] = true;
                jo_Result["ErrorMsg"] = $"删FB关联:操作成功(无FB关联)";
                return jo_Result;
            }

            ja_account_idList = new JArray();
            for (int i = 0; i < idList.Count; i++) { JObject jo = new JObject(); jo["account_id"] = idList[i].ToString(); ja_account_idList.Add(jo); }
            #endregion

            for (int i = 0; i < ja_account_idList.Count; i++)
            {
                #region 删除FB关联
                account.Running_Log = $"删FB关联:{ja_account_idList[i]["account_id"].ToString().Trim()}";
                randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

                hi = new HttpItem();
                hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en-US;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "useFXUnlinkMutation";
                doc_id = "7734742543282932";
                variables = "{\"client_mutation_id\":\"" + randomUUID + "\",\"account_id\":\"" + ja_account_idList[i]["account_id"].ToString().Trim() + "\",\"account_type\":\"FACEBOOK\",\"flow\":\"IG_WEB_SETTINGS\",\"device_id\":\"device_id_fetch_ig_did\",\"interface\":\"IG_WEB\",\"platform\":\"INSTAGRAM\",\"scale\":2,\"entrypoint\":null}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = "0";
                jo_postdata["__a"] = "1";
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = "24";
                jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                jo_postdata["jazoest"] = string.Empty;
                jo_postdata["lsd"] = lsd;
                jo_postdata["__spin_r"] = __rev;
                jo_postdata["__spin_b"] = string.Empty;
                jo_postdata["__spin_t"] = string.Empty;
                jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                jo_postdata["server_timestamps"] = server_timestamps;
                jo_postdata["doc_id"] = doc_id;
                #endregion

                hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);

                //合并CK
                if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

                jr = null;
                try { jr = JObject.Parse(hr.Html); } catch { }
                if (jr == null)
                {
                    ja_account_idList[i]["Success"] = false;
                    ja_account_idList[i]["ErrorMsg"] = $"{hr.Html}";
                }
                else if (jr.SelectToken("data['fxcal_ig_web_destroy']['mani_unlink_success_dialog_content']") != null && !string.IsNullOrEmpty(jr.SelectToken("data['fxcal_ig_web_destroy']['mani_unlink_success_dialog_content']").ToString().Trim()))
                {
                    ja_account_idList[i]["Success"] = true;
                    ja_account_idList[i]["ErrorMsg"] = $"删FB关联成功";
                }
                else
                {
                    ja_account_idList[i]["Success"] = false;
                    ja_account_idList[i]["ErrorMsg"] = $"{hr.Html}";
                }
                #endregion
            }

            var jts_Success = ja_account_idList.Where(jt => jt["Success"].Value<bool>() == true);
            var jts_Failed = ja_account_idList.Where(jt => !jt["Success"].Value<bool>());
            jo_Result["Success"] = jts_Failed.Count() == 0;
            if (jo_Result["Success"].Value<bool>()) jo_Result["ErrorMsg"] = "删FB关联:操作成功";
            else jo_Result["ErrorMsg"] = $"删FB关联:操作失败({string.Join(",", jts_Failed.Select(j => $"{j["account_id"].ToString().Trim()}==>{j["ErrorMsg"].ToString().Trim()}"))})";

            return jo_Result;
        }
        /// <summary>
        /// 删除登录会话
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject Ins_LogOutOfOtherSession(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            string confirmCode = string.Empty;
            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;

            string __ccg = string.Empty;
            string __rev = string.Empty;
            string __hsi = string.Empty;
            string __dyn = string.Empty;
            string __csr = string.Empty;
            string fb_dtsg = string.Empty;
            string lsd = string.Empty;
            string server_timestamps = string.Empty;

            string redirect_uri = string.Empty;
            string nh = string.Empty;
            string jazoest = string.Empty;
            string randomUUID = string.Empty;
            string activeId = string.Empty;
            string session_ids = string.Empty;
            JToken jtFind = null;

            #region 先访问目标页面
            account.Running_Log = $"删登录会话:进入目标页面(login_activity)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/password_and_security/login_activity";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            #endregion

            #region 获取API所需要的参数
            //{"connectionClass":"EXCELLENT"}
            __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            //"{"server_revision":1014268370,
            __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            //"cavalry_get_lid":"7381512262390677322"
            __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0no6u5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O0H8-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2exa0GE6-3u360hq1Iwqo5u0i67E5y2-2K";
            __csr = "hIrf94ERhlZWkLOqqaCOQiXBNlAsyfiiHWiRtblmzOqmy6IIKBAEySmCgDGRiAvJqG8HBnoCcJ2uBJCXhy9ExVRFlAl4KJtAhO9q-gzWzWJ2FVUTz5IAM-hGenp5WzuqcludzXx2AFe9-8V4bhHDCgp-agiCyUm-VA00jHi1Ey8-0ewg0Ha0JrJk5obWxGaWAKHGEd8pwJw0EUg5He0iSaCwhvxy8Wg0Ti5WwbKmOA4O04AhWUCzpQa42eu5aCOwBx6biWxSdxaSit4BjVHye9xN4Byu4EKiFE3ggWh03T84K4EkU";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";
            #endregion

            #region 选择INSTAGRAM账号
            account.Running_Log = $"删登录会话:选择INSTAGRAM账号";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterDeviceLoginActivitiesDialogQuery";
            doc_id = "6487574057995774";
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\",\"interface\":\"IG_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr == null || jr.SelectToken("data['fxcal_settings'].node['sessions_data']") == null)
            {
                jo_Result["ErrorMsg"] = $"删登录会话:选择INSTAGRAM账号失败({hr.Html})";
                return jo_Result;
            }

            if (jr.SelectToken("data['fxcal_settings'].node['sessions_data']").Where(jt => jt["is_active"] != null && !jt["is_active"].Value<bool>()).Count() == 0)
            {
                jo_Result["Success"] = true;
                jo_Result["ErrorMsg"] = $"删登录会话:操作成功";
                return jo_Result;
            }
            jtFind = jr.SelectToken("data['fxcal_settings'].node['sessions_data']").Where(jt => jt["is_active"] != null && jt["is_active"].Value<bool>()).FirstOrDefault();
            if (jtFind == null || jtFind["id"].ToString().Trim().Length == 0)
            {
                jo_Result["ErrorMsg"] = $"删登录会话:选择INSTAGRAM账号失败(获取当前设备ID失败)";
                return jo_Result;
            }
            activeId = jtFind["id"].ToString().Trim();
            #endregion

            #region 进行全选
            account.Running_Log = $"删登录会话:进行全选";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterDeviceLoginLogoutDevicesDialogQuery";
            doc_id = "6132705960192441";
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\",\"interface\":\"IG_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr == null || jr.SelectToken("data['fxcal_settings'].node['sessions_data']") == null)
            {
                jo_Result["ErrorMsg"] = $"删登录会话:选择INSTAGRAM账号失败({hr.Html})";
                return jo_Result;
            }
            var jts = jr.SelectToken("data['fxcal_settings'].node['sessions_data']").Where(jt => jt["id"] != null && !string.IsNullOrEmpty(jt["id"].ToString().Trim()) && jt["id"].ToString().Trim() != activeId);
            if (jts.Count() == 0)
            {
                jo_Result["ErrorMsg"] = $"删登录会话:操作成功";
                return jo_Result;
            }
            session_ids = $"[{string.Join(",", jts.Select(jt => $"\"{jt["id"].ToString().Trim()}\""))}]";
            #endregion

            #region 删除登录会话
            account.Running_Log = $"删登录会话:删除其它设备";
            randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useFXSettingsLogoutSessionMutation";
            doc_id = "24072290329086422";
            variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\"" +
                ",\"account_type\":\"INSTAGRAM\",\"mutate_params\":{\"logout_all\":false,\"session_ids\":" + session_ids + "},\"fdid\":\"device_id_fetch_datr\"}}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            //{"data":{"xfb_logout_session":{"__typename":"FXCALSettingsMutationErrorWithDialog","success":false,
            //"__isFXCALSettingsMutationReturnData":"FXCALSettingsMutationErrorWithDialog"}},"extensions":{"is_final":true}}
            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr == null || jr.SelectToken("data['xfb_logout_session'].success") == null)
            {
                jo_Result["ErrorMsg"] = $"删登录会话:操作失败({hr.Html})";
                return jo_Result;
            }
            if (jr.SelectToken("data['xfb_logout_session'].success").Value<bool>())
            {
                jo_Result["ErrorMsg"] = $"删登录会话:操作成功";
                return jo_Result;
            }
            else
            {
                #region 选择FB账号
                account.Running_Log = $"删登录会话:选择INSTAGRAM账号";
                hi = new HttpItem();
                hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en-US;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "FXAccountsCenterDeviceLoginActivitiesDialogQuery";
                doc_id = "6487574057995774";
                variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\",\"interface\":\"IG_WEB\"}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = "0";
                //jo_postdata["__aaid"] = "0";//特殊参数
                jo_postdata["__a"] = "1";
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = "24";
                jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                jo_postdata["jazoest"] = string.Empty;
                jo_postdata["lsd"] = lsd;
                jo_postdata["__spin_r"] = __rev;
                jo_postdata["__spin_b"] = string.Empty;
                jo_postdata["__spin_t"] = string.Empty;
                jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                jo_postdata["server_timestamps"] = server_timestamps;
                jo_postdata["doc_id"] = doc_id;
                #endregion

                hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);

                //合并CK
                if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

                jr = null;
                try { jr = JObject.Parse(hr.Html); } catch { }
                if (jr == null || jr.SelectToken("data['fxcal_settings'].node['sessions_data']") == null)
                {
                    jo_Result["ErrorMsg"] = $"删登录会话:操作失败";
                    return jo_Result;
                }

                if (jr.SelectToken("data['fxcal_settings'].node['sessions_data']").Where(jt => jt["is_active"] != null && !jt["is_active"].Value<bool>()).Count() == 0) jo_Result["ErrorMsg"] = $"删登录会话:操作成功";
                else jo_Result["ErrorMsg"] = $"删登录会话:操作失败(剩余数:{jr.SelectToken("data['fxcal_settings'].node['sessions_data']").Where(jt => jt["is_active"] != null && !jt["is_active"].Value<bool>()).Count()})";
                return jo_Result;
                #endregion
            }
            #endregion
        }
        /// <summary>
        /// 删除信任设备
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject Ins_TwoFactorRemoveTrustedDevice(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            string confirmCode = string.Empty;
            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;

            string __ccg = string.Empty;
            string __rev = string.Empty;
            string __hsi = string.Empty;
            string __dyn = string.Empty;
            string __csr = string.Empty;
            string fb_dtsg = string.Empty;
            string lsd = string.Empty;
            string server_timestamps = string.Empty;

            string encryptPwd = string.Empty;
            JArray ja_trusted_devices = null;
            int jaCount = 0;
            string randomUUID = string.Empty;
            string device_id = string.Empty;
            string ig_is_web_device = string.Empty;

            #region 先访问目标页面
            account.Running_Log = $"删信任设备:进入目标页面(two_factor)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/password_and_security/two_factor";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            #endregion

            #region 获取API所需要的参数
            //{"connectionClass":"EXCELLENT"}
            __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            //"{"server_revision":1014268370,
            __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            //"cavalry_get_lid":"7381512262390677322"
            __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0no6u5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O0H8-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2exa0GE6-3u360hq1Iwqo5u0i67E5y2-2K";
            __csr = "hIrf94ERhlZWkLOqqaCOQiXBNlAsyfiiHWiRtblmzOqmy6IIKBAEySmCgDGRiAvJqG8HBnoCcJ2uBJCXhy9ExVRFlAl4KJtAhO9q-gzWzWJ2FVUTz5IAM-hGenp5WzuqcludzXx2AFe9-8V4bhHDCgp-agiCyUm-VA00jHi1Ey8-0ewg0Ha0JrJk5obWxGaWAKHGEd8pwJw0EUg5He0iSaCwhvxy8Wg0Ti5WwbKmOA4O04AhWUCzpQa42eu5aCOwBx6biWxSdxaSit4BjVHye9xN4Byu4EKiFE3ggWh03T84K4EkU";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";
            #endregion

            #region 选择Facebook账号,查看设备列表
            account.Running_Log = $"删信任设备:选择Facebook账号,查看设备列表";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            //fb_api_req_friendly_name = "FXAccountsCenterTwoFactorSettingsDialogQuery";
            //doc_id = "7613749188645358";
            fb_api_req_friendly_name = "FXAccountsCenterTwoFactorTrustedDevicesListDialogQuery";
            doc_id = "9293008200739170";
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\",\"interface\":\"IG_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            jr = null;
            try { jr = JObject.Parse(hr.Html); } catch { }
            if (jr == null || jr.SelectToken("data['fxcal_settings'].node['two_factor_data']['trusted_devices']") == null)
            {
                //判断是否需要密码验证
                if (jr.SelectToken("errors[0].description") != null)
                {
                    if (jr.SelectToken("errors[0].description").ToString().Contains("\"challenge_type\":\"password\""))
                    {
                        #region 进行密码验证
                        account.Running_Log = $"删信任设备:进行密码验证";

                        //暂时先固定密码
                        //niubi.fb
                        //encryptPwd = "#PWD_BROWSER:5:1719252308:AZ5QADDtiGcrGB/Cv2WKo6h5+BznJr3xwRrAw/Wd2l08B7QqNDXbpMCfA8K/xam6uPP54i3BKiyI51peOJDL1jQZxv/MF1bZywHXUmA8Ydh/zGS5xYh/aBH1KMKdrt+Ea3AcGgXwxcHsljDm";
                        encryptPwd = this.Ins_Encpass_Method(account.Facebook_Pwd, "", "");

                        hi = new HttpItem();
                        hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                        hi.UserAgent = account.UserAgent;
                        hi.Accept = $"*/*";
                        hi.Header.Add("Accept-Encoding", "gzip");
                        hi.Header.Add("Accept-Language", "en-US;q=0.9");
                        hi.Allowautoredirect = false;

                        hi.Header.Add("Sec-Fetch-Site", "same-origin");
                        hi.Method = "POST";
                        hi.ContentType = $"application/x-www-form-urlencoded";

                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        fb_api_req_friendly_name = "FXPasswordReauthenticationMutation";
                        doc_id = "5864546173675027";
                        variables = "{\"input\":{\"account_id\":" + account.LoginInfo.LoginData_Account_Id + ",\"account_type\":\"INSTAGRAM\",\"password\":{\"sensitive_string_value\":\"" + encryptPwd + "\"},\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"client_mutation_id\":\"1\"}}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__user"] = "0";
                        jo_postdata["__a"] = "1";
                        jo_postdata["__req"] = string.Empty;
                        jo_postdata["__hs"] = string.Empty;
                        jo_postdata["dpr"] = string.Empty;
                        jo_postdata["__ccg"] = __ccg;
                        jo_postdata["__rev"] = __rev;
                        jo_postdata["__s"] = string.Empty;
                        jo_postdata["__hsi"] = __hsi;
                        jo_postdata["__dyn"] = __dyn;
                        jo_postdata["__csr"] = __csr;
                        jo_postdata["__comet_req"] = "24";
                        jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                        jo_postdata["jazoest"] = string.Empty;
                        jo_postdata["lsd"] = lsd;
                        jo_postdata["__spin_r"] = __rev;
                        jo_postdata["__spin_b"] = string.Empty;
                        jo_postdata["__spin_t"] = string.Empty;
                        jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                        jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                        jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                        jo_postdata["server_timestamps"] = server_timestamps;
                        jo_postdata["doc_id"] = doc_id;
                        #endregion

                        hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                        //Cookie
                        hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                        //代理
                        if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                        hr = hh.GetHtml(hi);

                        //合并CK
                        if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                        #endregion

                        //判断验证结果
                        //{"data":{"xfb_password_reauth_fb_only":{"is_reauth_successful":true}},"extensions":{"is_final":true}}
                        jr = null;
                        try { jr = JObject.Parse(hr.Html); } catch { }
                        if (jr == null || jr.SelectToken("data['xfb_password_reauth_fb_only']['is_reauth_successful']") == null || jr.SelectToken("data['xfb_password_reauth_fb_only']['is_reauth_successful']").ToString().ToLower() != "true")
                        {
                            jo_Result["ErrorMsg"] = $"删信任设备:进行密码验证失败({hr.Html})";
                            return jo_Result;
                        }

                        #region 再次选择Facebook账号,查看设备列表
                        account.Running_Log = $"删信任设备:再次选择Facebook账号,查看设备列表";
                        hi = new HttpItem();
                        hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                        hi.UserAgent = account.UserAgent;
                        hi.Accept = $"*/*";
                        hi.Header.Add("Accept-Encoding", "gzip");
                        hi.Header.Add("Accept-Language", "en-US;q=0.9");
                        hi.Allowautoredirect = false;

                        hi.Header.Add("Sec-Fetch-Site", "same-origin");
                        hi.Method = "POST";
                        hi.ContentType = $"application/x-www-form-urlencoded";

                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        //fb_api_req_friendly_name = "FXAccountsCenterTwoFactorSettingsDialogQuery";
                        //doc_id = "7613749188645358";
                        fb_api_req_friendly_name = "FXAccountsCenterTwoFactorTrustedDevicesListDialogQuery";
                        doc_id = "9293008200739170";
                        variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\",\"interface\":\"IG_WEB\"}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__user"] = "0";
                        jo_postdata["__a"] = "1";
                        jo_postdata["__req"] = string.Empty;
                        jo_postdata["__hs"] = string.Empty;
                        jo_postdata["dpr"] = string.Empty;
                        jo_postdata["__ccg"] = __ccg;
                        jo_postdata["__rev"] = __rev;
                        jo_postdata["__s"] = string.Empty;
                        jo_postdata["__hsi"] = __hsi;
                        jo_postdata["__dyn"] = __dyn;
                        jo_postdata["__csr"] = __csr;
                        jo_postdata["__comet_req"] = "24";
                        jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                        jo_postdata["jazoest"] = string.Empty;
                        jo_postdata["lsd"] = lsd;
                        jo_postdata["__spin_r"] = __rev;
                        jo_postdata["__spin_b"] = string.Empty;
                        jo_postdata["__spin_t"] = string.Empty;
                        jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                        jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                        jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                        jo_postdata["server_timestamps"] = server_timestamps;
                        jo_postdata["doc_id"] = doc_id;
                        #endregion

                        hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                        //Cookie
                        hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                        //代理
                        if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                        hr = hh.GetHtml(hi);

                        //合并CK
                        if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

                        jr = null;
                        try { jr = JObject.Parse(hr.Html); } catch { }
                        if (jr == null || jr.SelectToken("data['fxcal_settings'].node['two_factor_data']['trusted_devices']") == null)
                        {
                            jo_Result["ErrorMsg"] = $"删信任设备:选择Facebook账号,查看设备列表失败({hr.Html})";
                            return jo_Result;
                        }
                        #endregion
                    }
                    else
                    {
                        jo_Result["ErrorMsg"] = $"删信任设备:选择Facebook账号,查看设备列表失败({hr.Html})";
                        return jo_Result;
                    }
                }
                else
                {
                    jo_Result["ErrorMsg"] = $"删信任设备:选择Facebook账号,查看设备列表失败({hr.Html})";
                    return jo_Result;
                }
            }

            ja_trusted_devices = (JArray)jr.SelectToken("data['fxcal_settings'].node['two_factor_data']['trusted_devices']");
            if (ja_trusted_devices.Count() == 0)
            {
                jo_Result["Success"] = true;
                jo_Result["ErrorMsg"] = $"删信任设备:操作成功(无需要删除的信任设备)";
                return jo_Result;
            }
            #endregion

            #region 逐条删除
            jaCount = ja_trusted_devices.Count();
            for (int i = 0; i < jaCount; i++)
            {
                #region 删信任设备
                //{"device_id":"1616500789200744","device_name":"Firefox on Windows","ig_is_web_device":false,"last_login_location":"Unknown location","last_login_time":"10 hours ago","latitude":0,"longitude":0}
                account.Running_Log = $"删信任设备:{i + 1} / {jaCount}";
                randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

                ja_trusted_devices[i]["Success"] = false;
                ja_trusted_devices[i]["ErrorMsg"] = string.Empty;
                device_id = ja_trusted_devices[i]["device_id"] == null ? string.Empty : ja_trusted_devices[i]["device_id"].ToString().Trim();
                if (string.IsNullOrEmpty(device_id)) { ja_trusted_devices[i]["ErrorMsg"] = "device_id is null"; continue; }
                ig_is_web_device = ja_trusted_devices[i]["ig_is_web_device"] == null ? "false" : ja_trusted_devices[i]["ig_is_web_device"].ToString().ToLower().Trim();

                hi = new HttpItem();
                hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en-US;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "useFXSettingsTwoFactorRemoveTrustedDeviceMutation";
                doc_id = "6716390001764111";
                variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\"," +
                    "\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"INSTAGRAM\"," +
                    "\"device_id\":\"" + device_id + "\",\"ig_is_web_device\":" + ig_is_web_device + ",\"fdid\":\"device_id_fetch_datr\"}}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = "0";
                //jo_postdata["__aaid"] = "0";//特殊参数
                jo_postdata["__a"] = "1";
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = "24";
                jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                jo_postdata["jazoest"] = string.Empty;
                jo_postdata["lsd"] = lsd;
                jo_postdata["__spin_r"] = __rev;
                jo_postdata["__spin_b"] = string.Empty;
                jo_postdata["__spin_t"] = string.Empty;
                jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                jo_postdata["server_timestamps"] = server_timestamps;
                jo_postdata["doc_id"] = doc_id;
                #endregion

                hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);

                //合并CK
                if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

                //{"data":{"xfb_two_factor_remove_trusted_device":{"__typename":"FXCALSettingsMutationReturnDataSuccess",
                //"client_mutation_id":"dabc79b0-f719-4b0a-9476-84c8401b0ec0","success":true,
                //"__isFXCALSettingsMutationReturnData":"FXCALSettingsMutationReturnDataSuccess"}},"extensions":{"is_final":true}}
                jr = null;
                try { jr = JObject.Parse(hr.Html); } catch { }
                if (jr == null || jr.SelectToken("data['xfb_two_factor_remove_trusted_device'].success") == null)
                {
                    ja_trusted_devices[i]["ErrorMsg"] = $"{hr.Html}";
                }
                else if (jr.SelectToken("data['xfb_two_factor_remove_trusted_device'].success").Value<bool>())
                {
                    ja_trusted_devices[i]["Success"] = true;
                    ja_trusted_devices[i]["ErrorMsg"] = $"操作成功";
                }
                else ja_trusted_devices[i]["ErrorMsg"] = $"操作失败";
                #endregion

                continue;
            }
            #endregion

            //整理结果
            var successList = ja_trusted_devices.Where(jt => jt["Success"].Value<bool>());
            var failedList = ja_trusted_devices.Where(jt => !jt["Success"].Value<bool>());

            jo_Result["Success"] = failedList.Count() == 0;
            if (jo_Result["Success"].Value<bool>()) jo_Result["ErrorMsg"] = $"删信任设备:操作成功";
            else jo_Result["ErrorMsg"] = $"删信任设备:操作失败(剩余数:{failedList.Count()})";

            return jo_Result;
        }
        /// <summary>
        /// 删除联系方式/查询国家
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject Ins_DeleteOtherContacts_QueryCountry(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["GuoJia"] = "";

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            JArray ja_contact_points = null;
            string html = string.Empty;
            string confirmCode = string.Empty;
            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;

            string __ccg = string.Empty;
            string __rev = string.Empty;
            string __hsi = string.Empty;
            string __dyn = string.Empty;
            string __csr = string.Empty;
            string fb_dtsg = string.Empty;
            string lsd = string.Empty;
            string server_timestamps = string.Empty;

            string redirect_uri = string.Empty;
            string nh = string.Empty;
            string jazoest = string.Empty;
            string checkpoint_data = string.Empty;
            string encryptPwd = string.Empty;
            int deleteCount = 0;
            int loopCount = 0;
            string errorMsg = string.Empty;
            string selected_accounts = string.Empty;
            string blockMessage = string.Empty;
            JToken jtFind = null;

            #region 先访问目标页面
            account.Running_Log = $"删联系:进入目标页面(contact_points)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/personal_info/contact_points/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            #endregion

            #region 获取API所需要的参数
            //{"connectionClass":"EXCELLENT"}
            __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            //"{"server_revision":1014268370,
            __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            //"cavalry_get_lid":"7381512262390677322"
            __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0no6u5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O0H8-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2exa0GE6-3u360hq1Iwqo5u0i67E5y2-2K";
            __csr = "hIrf94ERhlZWkLOqqaCOQiXBNlAsyfiiHWiRtblmzOqmy6IIKBAEySmCgDGRiAvJqG8HBnoCcJ2uBJCXhy9ExVRFlAl4KJtAhO9q-gzWzWJ2FVUTz5IAM-hGenp5WzuqcludzXx2AFe9-8V4bhHDCgp-agiCyUm-VA00jHi1Ey8-0ewg0Ha0JrJk5obWxGaWAKHGEd8pwJw0EUg5He0iSaCwhvxy8Wg0Ti5WwbKmOA4O04AhWUCzpQa42eu5aCOwBx6biWxSdxaSit4BjVHye9xN4Byu4EKiFE3ggWh03T84K4EkU";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";
            #endregion

            #region 获取现有的联系方式列表，判断是否已经绑定过
            account.Running_Log = $"删联系:获取绑定的联系方式列表";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterAddContactPointQuery";
            doc_id = "7318565184899119";
            variables = "{\"contact_point_type\":\"email\",\"interface\":\"FB_WEB\",\"is_confirming_pending\":false,\"normalized_contact_point\":\"\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "24";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            html = hr.Html;
            if (html.Contains("\"errorSummary\":\"")) html = StringHelper.Usc2ConvertToAnsi(html);

            //获取所有的联系方式
            jr = null;
            try { jr = JObject.Parse(html); } catch { }
            if (jr == null || jr.SelectToken("data['fxcal_settings'].node['all_contact_points']") == null)
            {
                jo_Result["ErrorMsg"] = $"删除其它绑定失败(获取联系方式列表失败：{hr.Html})";
                return jo_Result;
            }
            ja_contact_points = null;
            try { ja_contact_points = (JArray)jr.SelectToken("data['fxcal_settings'].node['all_contact_points']"); } catch { }
            if (ja_contact_points == null || ja_contact_points.Count == 0)
            {
                jo_Result["ErrorMsg"] = $"删除其它绑定失败(获取联系方式列表失败：{hr.Html})";
                return jo_Result;
            }
            #endregion

            #region 根据手机号查询国家
            jtFind = ja_contact_points.Where(jt => jt["normalized_contact_point"] != null && jt["normalized_contact_point"].ToString().Trim().StartsWith("+")).FirstOrDefault();
            if (jtFind != null)
            {
                if (Program.setting.Ja_CountrysPhoneNumInfo.Where(jt => jtFind["normalized_contact_point"].ToString().Trim().StartsWith(jt["code"].ToString().Trim())).FirstOrDefault() != null)
                {
                    jtFind = Program.setting.Ja_CountrysPhoneNumInfo.Where(jt => jtFind["normalized_contact_point"].ToString().Trim().StartsWith(jt["code"].ToString().Trim())).FirstOrDefault();
                    jo_Result["GuoJia"] = jtFind["en"].ToString().Trim();
                }
                else jo_Result["GuoJia"] = jtFind["normalized_contact_point"].ToString().Trim();
            }
            #endregion

            #region 历遍删除所有联系方式
            account.Running_Log = $"删联系:历遍列表";
            deleteCount = 0;
            loopCount = ja_contact_points.Count;
            jtFind = null;
            for (int i = 0; i < loopCount; i++)
            {
                int jtIndex = i - deleteCount;
                ja_contact_points[jtIndex]["contact_point_type"] = ja_contact_points[jtIndex]["normalized_contact_point"].ToString().Contains("@") ? "email" : "phone_number";
                ja_contact_points[jtIndex]["Success"] = false;
                ja_contact_points[jtIndex]["ErrorMsg"] = string.Empty;

                JToken jt_contact = ja_contact_points[jtIndex];

                //跳过新绑定的邮箱
                if (jt_contact["normalized_contact_point"].ToString() == account.New_Mail_Name) { ja_contact_points.RemoveAt(jtIndex); deleteCount += 1; continue; }

                //遇到非密码验证，不能修改，就统一标记即可，不必真的尝试
                if (!string.IsNullOrEmpty(blockMessage))
                {
                    ja_contact_points[jtIndex]["ErrorMsg"] = $"{blockMessage}";
                    continue;
                }

                //跳过非Ins的账号
                jtFind = jt_contact["contact_point_info"].Where(jt => jt.SelectToken("['owner_profile'].id") != null && jt.SelectToken("['owner_profile'].id").ToString() == account.LoginInfo.LoginData_Account_Id).FirstOrDefault();
                if (jtFind == null) { ja_contact_points.RemoveAt(jtIndex); deleteCount += 1; continue; }
                selected_accounts = account.LoginInfo.LoginData_Account_Id;

                #region 点击Delete键
                account.Running_Log = $"删联系:点击Detete[{jt_contact["normalized_contact_point"].ToString()}]";
                hi = new HttpItem();
                hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en-US;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "FXAccountsCenterDeleteContactPointMutation";
                doc_id = "6716611361758391";
                variables = "{\"normalized_contact_point\":\"" + jt_contact["normalized_contact_point"].ToString() + "\",\"contact_point_type\":\"" + jt_contact["contact_point_type"].ToString() + "\",\"selected_accounts\":[" + selected_accounts + "],\"client_mutation_id\":\"mutation_id_" + StringHelper.GetUnixTime(DateTime.Now) + "\",\"family_device_id\":\"device_id_fetch_datr\"}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = "0";
                jo_postdata["__aaid"] = "0";//特殊参数
                jo_postdata["__a"] = "1";
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = "24";
                jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                jo_postdata["jazoest"] = string.Empty;
                jo_postdata["lsd"] = lsd;
                jo_postdata["__spin_r"] = __rev;
                jo_postdata["__spin_b"] = string.Empty;
                jo_postdata["__spin_t"] = string.Empty;
                jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                jo_postdata["server_timestamps"] = server_timestamps;
                jo_postdata["doc_id"] = doc_id;
                #endregion

                hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);

                //合并CK
                if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                #endregion

                //判断是否需要密码验证
                jr = null;
                try { jr = JObject.Parse(hr.Html); } catch { }
                if (jr == null || jr.SelectToken("data['xfb_delete_contact_point'][0]['mutation_data']['response_data'].success") == null && jr.SelectToken("errors[0].description") == null)
                {
                    ja_contact_points[jtIndex]["ErrorMsg"] = $"删除失败({hr.Html})";
                    continue;
                }

                if (jr.SelectToken("errors[0].description") != null)
                {
                    if (!jr.SelectToken("errors[0].description").ToString().Contains("\"challenge_type\":\"password\""))
                    {
                        //{"challenge_type":"block","account_id":17841467064214123}
                        if (jr.SelectToken("errors[0].description").ToString().Contains("\"challenge_type\":\"block\""))
                        {
                            #region 查询错误描述
                            account.Running_Log = $"删联系:查询错误描述";
                            hi = new HttpItem();
                            hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                            hi.UserAgent = account.UserAgent;
                            hi.Accept = $"*/*";
                            hi.Header.Add("Accept-Encoding", "gzip");
                            hi.Header.Add("Accept-Language", "en-US;q=0.9");
                            hi.Allowautoredirect = false;

                            hi.Header.Add("Sec-Fetch-Site", "same-origin");
                            hi.Method = "POST";
                            hi.ContentType = $"application/x-www-form-urlencoded";

                            #region 整理提交数据
                            fb_api_caller_class = "RelayModern";
                            fb_api_req_friendly_name = "SecuredActionBlockDialogQuery";
                            doc_id = "6108889802569432";
                            variables = "{\"accountType\":\"INSTAGRAM\"}";

                            jo_postdata = new JObject();
                            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                            jo_postdata["__user"] = "0";
                            jo_postdata["__a"] = "1";
                            jo_postdata["__req"] = string.Empty;
                            jo_postdata["__hs"] = string.Empty;
                            jo_postdata["dpr"] = string.Empty;
                            jo_postdata["__ccg"] = __ccg;
                            jo_postdata["__rev"] = __rev;
                            jo_postdata["__s"] = string.Empty;
                            jo_postdata["__hsi"] = __hsi;
                            jo_postdata["__dyn"] = __dyn;
                            jo_postdata["__csr"] = __csr;
                            jo_postdata["__comet_req"] = "24";
                            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                            jo_postdata["jazoest"] = string.Empty;
                            jo_postdata["lsd"] = lsd;
                            jo_postdata["__spin_r"] = __rev;
                            jo_postdata["__spin_b"] = string.Empty;
                            jo_postdata["__spin_t"] = string.Empty;
                            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                            jo_postdata["server_timestamps"] = server_timestamps;
                            jo_postdata["doc_id"] = doc_id;
                            #endregion

                            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                            //Cookie
                            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                            //代理
                            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                            hr = hh.GetHtml(hi);

                            //合并CK
                            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                            #endregion

                            jr = null;
                            try { jr = JObject.Parse(hr.Html); } catch { }
                            if (jr == null || jr.SelectToken("data['xfb_secured_action'].content['block_message']") == null || string.IsNullOrEmpty(jr.SelectToken("data['xfb_secured_action'].content['block_message']").ToString().Trim())) ja_contact_points[jtIndex]["ErrorMsg"] = $"非密码验证，无法删除({hr.Html})";
                            else ja_contact_points[jtIndex]["ErrorMsg"] = $"需等待时间，无法删除({jr.SelectToken("data['xfb_secured_action'].content['block_message']").ToString().Replace("\r\n", string.Empty).Replace("\n", string.Empty).Trim()})";
                        }
                        else ja_contact_points[jtIndex]["ErrorMsg"] = $"非密码验证，无法删除({hr.Html})";
                        blockMessage = ja_contact_points[jtIndex]["ErrorMsg"].ToString().Trim();
                        continue;
                    }

                    #region 进行密码验证
                    account.Running_Log = $"删联系:进行密码验证[{jt_contact["normalized_contact_point"].ToString()}]";

                    //暂时先固定密码
                    //niubi.fb
                    //encryptPwd = "#PWD_BROWSER:5:1719252308:AZ5QADDtiGcrGB/Cv2WKo6h5+BznJr3xwRrAw/Wd2l08B7QqNDXbpMCfA8K/xam6uPP54i3BKiyI51peOJDL1jQZxv/MF1bZywHXUmA8Ydh/zGS5xYh/aBH1KMKdrt+Ea3AcGgXwxcHsljDm";
                    encryptPwd = this.Ins_Encpass_Method(account.Facebook_Pwd, "", "");

                    hi = new HttpItem();
                    hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                    hi.UserAgent = account.UserAgent;
                    hi.Accept = $"*/*";
                    hi.Header.Add("Accept-Encoding", "gzip");
                    hi.Header.Add("Accept-Language", "en-US;q=0.9");
                    hi.Allowautoredirect = false;

                    hi.Header.Add("Sec-Fetch-Site", "same-origin");
                    hi.Method = "POST";
                    hi.ContentType = $"application/x-www-form-urlencoded";

                    #region 整理提交数据
                    fb_api_caller_class = "RelayModern";
                    fb_api_req_friendly_name = "FXPasswordReauthenticationMutation";
                    doc_id = "5864546173675027";
                    variables = "{\"input\":{\"account_id\":" + account.LoginInfo.LoginData_Account_Id + ",\"account_type\":\"INSTAGRAM\",\"password\":{\"sensitive_string_value\":\"" + encryptPwd + "\"},\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"client_mutation_id\":\"1\"}}";

                    jo_postdata = new JObject();
                    jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                    jo_postdata["__user"] = "0";
                    jo_postdata["__aaid"] = "0";//特殊参数
                    jo_postdata["__a"] = "1";
                    jo_postdata["__req"] = string.Empty;
                    jo_postdata["__hs"] = string.Empty;
                    jo_postdata["dpr"] = string.Empty;
                    jo_postdata["__ccg"] = __ccg;
                    jo_postdata["__rev"] = __rev;
                    jo_postdata["__s"] = string.Empty;
                    jo_postdata["__hsi"] = __hsi;
                    jo_postdata["__dyn"] = __dyn;
                    jo_postdata["__csr"] = __csr;
                    jo_postdata["__comet_req"] = "24";
                    jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                    jo_postdata["jazoest"] = string.Empty;
                    jo_postdata["lsd"] = lsd;
                    jo_postdata["__spin_r"] = __rev;
                    jo_postdata["__spin_b"] = string.Empty;
                    jo_postdata["__spin_t"] = string.Empty;
                    jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                    jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                    jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                    jo_postdata["server_timestamps"] = server_timestamps;
                    jo_postdata["doc_id"] = doc_id;
                    #endregion

                    hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                    //Cookie
                    hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                    //代理
                    if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                    hr = hh.GetHtml(hi);

                    //合并CK
                    if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                    #endregion

                    //判断验证结果
                    //{"data":{"xfb_password_reauth_fb_only":{"is_reauth_successful":true}},"extensions":{"is_final":true}}
                    jr = null;
                    try { jr = JObject.Parse(hr.Html); } catch { }
                    if (jr == null || jr.SelectToken("data['xfb_password_reauth_fb_only']['is_reauth_successful']") == null || jr.SelectToken("data['xfb_password_reauth_fb_only']['is_reauth_successful']").ToString().ToLower() != "true")
                    {
                        ja_contact_points[jtIndex]["ErrorMsg"] = $"进行密码验证时失败{hr.Html}";
                        continue;
                    }

                    #region 重新点击Delete键
                    account.Running_Log = $"删联系:重新点击Delete[{jt_contact["normalized_contact_point"].ToString()}]";
                    hi = new HttpItem();
                    hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                    hi.UserAgent = account.UserAgent;
                    hi.Accept = $"*/*";
                    hi.Header.Add("Accept-Encoding", "gzip");
                    hi.Header.Add("Accept-Language", "en-US;q=0.9");
                    hi.Allowautoredirect = false;

                    hi.Header.Add("Sec-Fetch-Site", "same-origin");
                    hi.Method = "POST";
                    hi.ContentType = $"application/x-www-form-urlencoded";

                    #region 整理提交数据
                    fb_api_caller_class = "RelayModern";
                    fb_api_req_friendly_name = "FXAccountsCenterDeleteContactPointMutation";
                    doc_id = "6716611361758391";
                    variables = "{\"normalized_contact_point\":\"" + jt_contact["normalized_contact_point"].ToString() + "\",\"contact_point_type\":\"" + jt_contact["contact_point_type"].ToString() + "\",\"selected_accounts\":[" + selected_accounts + "],\"client_mutation_id\":\"mutation_id_" + StringHelper.GetUnixTime(DateTime.Now) + "\",\"family_device_id\":\"device_id_fetch_datr\"}";

                    jo_postdata = new JObject();
                    jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                    jo_postdata["__user"] = "0";
                    jo_postdata["__aaid"] = "0";//特殊参数
                    jo_postdata["__a"] = "1";
                    jo_postdata["__req"] = string.Empty;
                    jo_postdata["__hs"] = string.Empty;
                    jo_postdata["dpr"] = string.Empty;
                    jo_postdata["__ccg"] = __ccg;
                    jo_postdata["__rev"] = __rev;
                    jo_postdata["__s"] = string.Empty;
                    jo_postdata["__hsi"] = __hsi;
                    jo_postdata["__dyn"] = __dyn;
                    jo_postdata["__csr"] = __csr;
                    jo_postdata["__comet_req"] = "24";
                    jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                    jo_postdata["jazoest"] = string.Empty;
                    jo_postdata["lsd"] = lsd;
                    jo_postdata["__spin_r"] = __rev;
                    jo_postdata["__spin_b"] = string.Empty;
                    jo_postdata["__spin_t"] = string.Empty;
                    jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                    jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                    jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                    jo_postdata["server_timestamps"] = server_timestamps;
                    jo_postdata["doc_id"] = doc_id;
                    #endregion

                    hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                    //Cookie
                    hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                    //代理
                    if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                    hr = hh.GetHtml(hi);

                    //合并CK
                    if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                    #endregion

                    jr = null;
                    try { jr = JObject.Parse(hr.Html); } catch { }
                    if (jr == null || jr.SelectToken("data['xfb_delete_contact_point'][0]['mutation_data']['response_data'].success") == null)
                    {
                        account.Running_Log = $"删联系:删除失败{jt_contact["normalized_contact_point"].ToString()}";
                        ja_contact_points[jtIndex]["ErrorMsg"] = $"删除失败({hr.Html})";
                        continue;
                    }
                }

                if (jr.SelectToken("data['xfb_delete_contact_point'][0]['mutation_data']['response_data'].success").ToString().ToLower() != "true")
                {
                    account.Running_Log = $"删联系:删除失败{jt_contact["normalized_contact_point"].ToString()}";
                    ja_contact_points[jtIndex]["ErrorMsg"] = $"删除失败({hr.Html})";
                    continue;
                }

                ja_contact_points[jtIndex]["Success"] = true;

                account.Running_Log = $"删联系:删除成功[{jt_contact["normalized_contact_point"].ToString()}]";
            }
            #endregion

            if (ja_contact_points.Count == 0)
            {
                jo_Result["Success"] = true;
                jo_Result["ErrorMsg"] = $"删除联系方式成功(没有需要删除的项)";
            }
            else
            {
                errorMsg = string.Empty;
                var successList = ja_contact_points.Where(jt => jt["Success"].ToString().ToLower() == "true");
                var failedList = ja_contact_points.Where(jt => jt["Success"].ToString().ToLower() != "true");
                jo_Result["Success"] = successList.Count() > 0;

                if (successList.Count() > 0) errorMsg = $"Success:{string.Join(",", successList.Select(jt => jt["normalized_contact_point"].ToString().Trim()))}";
                if (failedList.Count() > 0) { if (!string.IsNullOrEmpty(errorMsg)) errorMsg += ","; errorMsg += $"Failed:{string.Join(",", failedList.Select(jt => $"{jt["normalized_contact_point"].ToString().Trim()}({jt["ErrorMsg"].ToString().Trim()})"))}"; }

                jo_Result["ErrorMsg"] = $"删除联系方式{(successList.Count() > 0 ? "成功" : "失败")}[{errorMsg}]";
            }

            return jo_Result;
        }
        /// <summary>
        /// 查询国家
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject Ins_Query_Country(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["GuoJia"] = "";

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            string confirmCode = string.Empty;
            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;

            string __ccg = string.Empty;
            string __rev = string.Empty;
            string __hsi = string.Empty;
            string __dyn = string.Empty;
            string __csr = string.Empty;
            string fb_dtsg = string.Empty;
            string lsd = string.Empty;
            string server_timestamps = string.Empty;

            string country = string.Empty;
            JToken jtFind = null;
            JArray ja_contact_points = null;
            bool isSuccess_ByPhone = false;

            #region 先去联系方式通过手机号查

            while (true)
            {
                #region 先访问目标页面
                account.Running_Log = $"删联系:进入目标页面(contact_points)";
                hi = new HttpItem();
                hi.URL = $"https://accountscenter.instagram.com/personal_info/contact_points/";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en-US;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "none");

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);

                //合并CK
                if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                #endregion

                #region 获取API所需要的参数
                //{"connectionClass":"EXCELLENT"}
                __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
                //"{"server_revision":1014268370,
                __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
                //"cavalry_get_lid":"7381512262390677322"
                __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
                __dyn = "7xeUmwlEnwn8K2Wmh0no6u5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O0H8-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2exa0GE6-3u360hq1Iwqo5u0i67E5y2-2K";
                __csr = "hIrf94ERhlZWkLOqqaCOQiXBNlAsyfiiHWiRtblmzOqmy6IIKBAEySmCgDGRiAvJqG8HBnoCcJ2uBJCXhy9ExVRFlAl4KJtAhO9q-gzWzWJ2FVUTz5IAM-hGenp5WzuqcludzXx2AFe9-8V4bhHDCgp-agiCyUm-VA00jHi1Ey8-0ewg0Ha0JrJk5obWxGaWAKHGEd8pwJw0EUg5He0iSaCwhvxy8Wg0Ti5WwbKmOA4O04AhWUCzpQa42eu5aCOwBx6biWxSdxaSit4BjVHye9xN4Byu4EKiFE3ggWh03T84K4EkU";
                //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
                fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
                //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
                //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
                lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
                server_timestamps = "true";

                if (string.IsNullOrEmpty(fb_dtsg)) break;
                #endregion

                #region 获取现有的联系方式列表，判断是否已经绑定过
                account.Running_Log = $"删联系:获取绑定的联系方式列表";
                hi = new HttpItem();
                hi.URL = $"https://accountscenter.instagram.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "en-US;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "FXAccountsCenterAddContactPointQuery";
                doc_id = "7318565184899119";
                variables = "{\"contact_point_type\":\"email\",\"interface\":\"IG_WEB\",\"is_confirming_pending\":false,\"normalized_contact_point\":\"\"}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = "0";
                jo_postdata["__a"] = "1";
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = "24";
                jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
                jo_postdata["jazoest"] = string.Empty;
                jo_postdata["lsd"] = lsd;
                jo_postdata["__spin_r"] = __rev;
                jo_postdata["__spin_b"] = string.Empty;
                jo_postdata["__spin_t"] = string.Empty;
                jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
                jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
                jo_postdata["variables"] = StringHelper.UrlEncode(variables);
                jo_postdata["server_timestamps"] = server_timestamps;
                jo_postdata["doc_id"] = doc_id;
                #endregion

                hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                //Cookie
                hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                //代理
                if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

                hr = hh.GetHtml(hi);

                //合并CK
                if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

                html = hr.Html;
                if (html.Contains("\"errorSummary\":\"")) html = StringHelper.Usc2ConvertToAnsi(html);

                //获取所有的联系方式
                jr = null;
                try { jr = JObject.Parse(html); } catch { }
                if (jr == null || jr.SelectToken("data['fxcal_settings'].node['all_contact_points']") == null) break;
                ja_contact_points = null;
                try { ja_contact_points = (JArray)jr.SelectToken("data['fxcal_settings'].node['all_contact_points']"); } catch { }
                if (ja_contact_points == null || ja_contact_points.Count == 0) break;
                #endregion

                #region 根据手机号查询国家
                jtFind = ja_contact_points.Where(jt => jt["normalized_contact_point"] != null && jt["normalized_contact_point"].ToString().Trim().StartsWith("+")).FirstOrDefault();
                if (jtFind != null)
                {
                    isSuccess_ByPhone = true;
                    if (Program.setting.Ja_CountrysPhoneNumInfo.Where(jt => jtFind["normalized_contact_point"].ToString().Trim().StartsWith(jt["code"].ToString().Trim())).FirstOrDefault() != null)
                    {
                        jtFind = Program.setting.Ja_CountrysPhoneNumInfo.Where(jt => jtFind["normalized_contact_point"].ToString().Trim().StartsWith(jt["code"].ToString().Trim())).FirstOrDefault();
                        jo_Result["GuoJia"] = jtFind["en"].ToString().Trim();
                    }
                    else jo_Result["GuoJia"] = jtFind["normalized_contact_point"].ToString().Trim();
                }
                #endregion

                break;
            }

            #endregion

            jo_Result["Success"] = isSuccess_ByPhone;
            jo_Result["ErrorMsg"] = $"查国家:操作{(isSuccess_ByPhone ? "成功" : "失败")}";
            return jo_Result;
        }
        /// <summary>
        /// 查询生日
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject Ins_Query_Birthday(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["ShengRi"] = "";

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            string html = string.Empty;
            string confirmCode = string.Empty;
            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;

            string __ccg = string.Empty;
            string __rev = string.Empty;
            string __hsi = string.Empty;
            string __dyn = string.Empty;
            string __csr = string.Empty;
            string fb_dtsg = string.Empty;
            string lsd = string.Empty;
            string server_timestamps = string.Empty;

            string birStr = string.Empty;
            JToken jtFind = null;
            JArray ja_personal_details = null;
            string dateStr = string.Empty;

            #region 先访问目标页面
            account.Running_Log = $"删联系:进入目标页面(personal_info)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.instagram.com/personal_info/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            #endregion

            #region 查询操作
            birStr = StringHelper.GetMidStr(hr.Html, "\"PERSONAL_DETAILS\",\"nodes\":", "}]},\"is_eligible_for_account_level_social_interactions_control_on_web\"");
            ja_personal_details = null;
            try { ja_personal_details = JArray.Parse(birStr); } catch { }
            if (ja_personal_details == null || ja_personal_details.Count() == 0)
            {
                account.Running_Log = $"查国家:操作失败(获取PERSONAL_DETAILS节点信息失败)";
                return jo_Result;
            }

            jtFind = ja_personal_details.Where(jt => jt["node_id"] != null && jt["node_id"].ToString().Trim() == "BIRTHDAY").FirstOrDefault();
            if (jtFind == null || jtFind["navigation_row_subtitle"] == null || string.IsNullOrEmpty(jtFind["navigation_row_subtitle"].ToString().Trim()))
            {
                account.Running_Log = $"查国家:操作失败(获取BIRTHDAY节点信息失败)";
                return jo_Result;
            }

            birStr = jtFind["navigation_row_subtitle"].ToString().Trim();
            if (birStr == "Add your birthday") jo_Result["ShengRi"] = "Null";
            else
            {
                dateStr = this.ConvertDate(birStr, "MM dd, yyyy");
                jo_Result["ShengRi"] = dateStr;
            }
            #endregion

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = $"查生日:操作成功";
            return jo_Result;
        }
        /// <summary>
        /// 查注册日期
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject Ins_Query_ZhuCeRiQi(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["ZhuCeRiQi"] = "";

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            string html = string.Empty;
            string confirmCode = string.Empty;
            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;

            string __ccg = string.Empty;
            string __rev = string.Empty;
            string __hsi = string.Empty;
            string __dyn = string.Empty;
            string __csr = string.Empty;
            string fb_dtsg = string.Empty;
            string lsd = string.Empty;
            string server_timestamps = string.Empty;

            string dateStr = string.Empty;
            string frontStr = string.Empty;
            string jsonStr = string.Empty;
            JArray ja_infos = null;
            JToken jtFind = null;


            #region 先访问目标页面
            account.Running_Log = $"查帖子|好友|关注:打开目标页面";

            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/your_activity/account_history";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = true;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            #endregion

            #region 获取API所需要的参数
            //{"connectionClass":"EXCELLENT"}
            __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            //"{"server_revision":1014268370,
            __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            //"cavalry_get_lid":"7381512262390677322"
            __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0no6u5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O0H8-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2exa0GE6-3u360hq1Iwqo5u0i67E5y2-2K";
            __csr = "hIrf94ERhlZWkLOqqaCOQiXBNlAsyfiiHWiRtblmzOqmy6IIKBAEySmCgDGRiAvJqG8HBnoCcJ2uBJCXhy9ExVRFlAl4KJtAhO9q-gzWzWJ2FVUTz5IAM-hGenp5WzuqcludzXx2AFe9-8V4bhHDCgp-agiCyUm-VA00jHi1Ey8-0ewg0Ha0JrJk5obWxGaWAKHGEd8pwJw0EUg5He0iSaCwhvxy8Wg0Ti5WwbKmOA4O04AhWUCzpQa42eu5aCOwBx6biWxSdxaSit4BjVHye9xN4Byu4EKiFE3ggWh03T84K4EkU";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";

            if (string.IsNullOrEmpty(fb_dtsg))
            {
                jo_Result["ErrorMsg"] = $"查帖子|好友|关注:打开目标页面失败";
                return jo_Result;
            }
            #endregion

            #region 查注册日期
            account.Running_Log = $"查注册日期操作";
            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/async/wbloks/fetch/?appid=com.instagram.privacy.activity_center.account_history_screen&type=app&__bkv=213c82555f99bb0cecb045c627a22f08209d7a699fc238c7e73a0482e70267f9";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded;charset=UTF-8";

            #region 整理提交数据
            jo_postdata = new JObject();
            jo_postdata["__d"] = "www";
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "7";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["params"] = StringHelper.UrlEncode("{}");
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            html = hr.Html.Replace("for (;;);", string.Empty);

            //查注册日期
            frontStr = "\"children\":[{\"bk.components.TextSpan\":{\"text\":\"You created your account on";
            if (!html.Contains(frontStr))
            {
                jo_Result["ErrorMsg"] = $"查注册日期失败:查找关键词 You created your account on 失败)";
                return jo_Result;
            }
            jsonStr = "[{\"bk.components.TextSpan\":{\"text\":\"You created your account on" + StringHelper.GetMidStr(html, frontStr, "]") + "]";
            ja_infos = null;
            try { ja_infos = JArray.Parse(jsonStr); } catch { }
            if (ja_infos == null || ja_infos.Count() < 2)
            {
                jo_Result["ErrorMsg"] = $"查注册日期失败:JsonStr 提取失败)";
                return jo_Result;
            }
            jtFind = ja_infos.SelectToken("[1]['bk.components.TextSpan'].text");
            if (jtFind == null || string.IsNullOrEmpty(jtFind.ToString().Trim()))
            {
                jo_Result["ErrorMsg"] = $"查注册日期失败:没有找到注册日期节点)";
                return jo_Result;
            }
            dateStr = jtFind.ToString().Trim();
            if (string.IsNullOrEmpty(dateStr))
            {
                jo_Result["ErrorMsg"] = $"查注册日期失败:没有找到注册日期节点)";
                return jo_Result;
            }
            //July 10, 2022
            jo_Result["ZhuCeRiQi"] = this.ConvertDate(dateStr, "MM dd, yyyy");
            #endregion

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = $"查注册日期:操作成功";
            return jo_Result;
        }
        /// <summary>
        /// 查帖子|好友|关注
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject Ins_Query_Posts_Followers_Following(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["TieZiCount"] = "";
            jo_Result["HaoYouCount"] = "";
            jo_Result["GuanZhuCount"] = "";

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;
            string html = string.Empty;
            string confirmCode = string.Empty;
            string errorText = string.Empty;
            string fb_api_caller_class = string.Empty;
            string fb_api_req_friendly_name = string.Empty;
            string doc_id = string.Empty;
            string variables = string.Empty;

            string __ccg = string.Empty;
            string __rev = string.Empty;
            string __hsi = string.Empty;
            string __dyn = string.Empty;
            string __csr = string.Empty;
            string fb_dtsg = string.Empty;
            string lsd = string.Empty;
            string server_timestamps = string.Empty;

            string X_BLOKS_VERSION_ID = string.Empty;
            string X_ASBD_ID = string.Empty;
            string ck_CSRFToken = string.Empty;
            string appId = string.Empty;
            string profile_id = string.Empty;

            int tieZiCount = 0;
            string tieZiStr = string.Empty;

            #region 先访问目标页面
            account.Running_Log = $"查帖子|好友|关注:打开目标页面";

            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/{account.LoginInfo.LoginData_UserName}/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = true;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            #endregion

            #region 获取API所需要的参数
            //{"connectionClass":"EXCELLENT"}
            __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            //"{"server_revision":1014268370,
            __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            //"cavalry_get_lid":"7381512262390677322"
            __hsi = StringHelper.GetMidStr(hr.Html, "\"hsi\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0no6u5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O0H8-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2exa0GE6-3u360hq1Iwqo5u0i67E5y2-2K";
            __csr = "hIrf94ERhlZWkLOqqaCOQiXBNlAsyfiiHWiRtblmzOqmy6IIKBAEySmCgDGRiAvJqG8HBnoCcJ2uBJCXhy9ExVRFlAl4KJtAhO9q-gzWzWJ2FVUTz5IAM-hGenp5WzuqcludzXx2AFe9-8V4bhHDCgp-agiCyUm-VA00jHi1Ey8-0ewg0Ha0JrJk5obWxGaWAKHGEd8pwJw0EUg5He0iSaCwhvxy8Wg0Ti5WwbKmOA4O04AhWUCzpQa42eu5aCOwBx6biWxSdxaSit4BjVHye9xN4Byu4EKiFE3ggWh03T84K4EkU";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";

            if (string.IsNullOrEmpty(fb_dtsg))
            {
                jo_Result["ErrorMsg"] = $"查帖子|好友|关注:打开目标页面失败";
                return jo_Result;
            }

            ck_CSRFToken = StringHelper.GetMidStr(hr.Html, "\"csrf_token\":\"", "\"");//
            appId = StringHelper.GetMidStr(hr.Html, "\"X-IG-App-ID\":\"", "\"");//
            X_BLOKS_VERSION_ID = StringHelper.GetMidStr(hr.Html, "\"versioningID\":\"", "\"");//
            X_ASBD_ID = "129477";//
            profile_id = StringHelper.GetMidStr(hr.Html, "\"profile_id\":\"", "\"");
            #endregion

            #region 查帖子
            //<meta property="og:description" content="0 Followers, 271 Following, 2 Posts - See Instagram photos and videos from Luan Da Silva (&#064;sr_luuan)" />
            tieZiStr = StringHelper.GetMidStr(StringHelper.GetMidStr(hr.Html, "<meta property=\"og:description\"", "/>"), "Following,", "Posts").Trim();
            if (int.TryParse(tieZiStr, out tieZiCount)) jo_Result["TieZiCount"] = tieZiCount.ToString();
            #endregion

            #region 查好友
            account.Running_Log = $"查帖子|好友|关注:查好友、关注";
            hi = new HttpItem();
            hi.URL = $"https://www.instagram.com/graphql/query";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "en-US;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("X-IG-App-ID", appId);
            hi.Header.Add("X-BLOKS-VERSION-ID", X_BLOKS_VERSION_ID);
            hi.Header.Add("X-FB-LSD", lsd);
            hi.Header.Add("X-FB-Friendly-Name", "PolarisProfilePageContentDirectQuery");
            hi.Header.Add("X-ASBD-ID", X_ASBD_ID);
            hi.Header.Add("X-CSRFToken", ck_CSRFToken);

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "PolarisProfilePageContentDirectQuery";
            doc_id = "7663723823674585";
            variables = "{\"id\":\"" + profile_id + "\",\"render_surface\":\"PROFILE\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__d"] = "www";
            jo_postdata["__user"] = "0";
            jo_postdata["__a"] = "1";
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = "7";
            jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            jo_postdata["jazoest"] = string.Empty;
            jo_postdata["lsd"] = lsd;
            jo_postdata["__spin_r"] = __rev;
            jo_postdata["__spin_b"] = string.Empty;
            jo_postdata["__spin_t"] = string.Empty;
            jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            jo_postdata["server_timestamps"] = server_timestamps;
            jo_postdata["doc_id"] = doc_id;
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            html = hr.Html;

            //查好友和关注
            jr = null;
            try { jr = JObject.Parse(html); } catch { }
            if (jr == null || jr.SelectToken("data.user") == null)
            {
                jo_Result["ErrorMsg"] = $"查帖子|好友|关注:查好友、关注失败(缺少data.user对象)";
                return jo_Result;
            }

            if (jr.SelectToken("data.user['follower_count']") != null) jo_Result["HaoYouCount"] = jr.SelectToken("data.user['follower_count']").ToString().Trim();
            if (jr.SelectToken("data.user['following_count']") != null) jo_Result["GuanZhuCount"] = jr.SelectToken("data.user['following_count']").ToString().Trim();
            #endregion

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = $"查帖子|好友|关注:操作成功";
            return jo_Result;
        }

        #region 其它辅助方法
        private Dictionary<string, string> MonthMap = new Dictionary<string, string>
        {
            { "January", "01" }, { "February", "02" }, { "March", "03" },
            { "April", "04" }, { "May", "05" }, { "June", "06" },
            { "July", "07" }, { "August", "08" }, { "September", "09" },
            { "October", "10" }, { "November", "11" }, { "December", "12" },
            { "Jan", "01" }, { "Feb", "02" }, { "Mar", "03" },
            { "Apr", "04" }, { "Jun", "06" },
            { "Jul", "07" }, { "Aug", "08" }, { "Sep", "09" },
            { "Oct", "10" }, { "Nov", "11" }, { "Dec", "12" },
        };
        private string ReplaceMonth(string dateStr)
        {
            foreach (var kvp in this.MonthMap)
            {
                if (dateStr.Contains(kvp.Key)) dateStr = dateStr.Replace(kvp.Key, kvp.Value);
            }

            return dateStr;
        }
        private string ConvertDate(string dateString, string formatStr)
        {
            dateString = this.ReplaceMonth(dateString);
            DateTime date;
            bool isSuccess = DateTime.TryParseExact(dateString, formatStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
            if (!isSuccess) isSuccess = DateTime.TryParseExact(dateString, formatStr.Replace("dd", "d"), CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

            if (isSuccess) return date.ToString("yyyy-MM-dd");
            else return "1900-01-01";
        }
        #endregion
    }
}
