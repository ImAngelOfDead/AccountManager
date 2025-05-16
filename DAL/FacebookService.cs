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
using WinInetHelperCSharp;

namespace AccountManager.DAL
{
    /// <summary>
    /// FacebookAPI
    /// </summary>
    public class FacebookService
    {
        private ScriptEngine scriptEngine = null;

        #region 密码加密算法
        /// <summary>
        /// 密码加密
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        private string FB_Encpass_Method(string pwd)
        {
            string publicKey = "2b540d9b98d020172e0009287b74cbc152e7cc1b2be8e7bd14addef953c4af31";
            string keyId = "218";

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;

            hi = new HttpItem();
            hi.URL = $"http://127.0.0.1:30005/FB_Ins/GetFBEncPwd?pwd={pwd}&publicKey={publicKey}&keyId={keyId}";
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

        public FacebookService()
        {
            this.scriptEngine = new ScriptEngine();
            this.scriptEngine.Execute(Properties.Resources.FB);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// 检测CK是否有效
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_LoginByCookie(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["isNeedLoop"] = true;

            if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
            account.LoginInfo.CookieCollection = StringHelper.GetCookieCollectionByCookieJsonStr(account.Facebook_CK);

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            bool isNeedLoop = false;
            string errorCode = string.Empty;

            JObject jo_postdata = null;
            JObject jr = null;
            IEnumerable<JToken> jtsFind = null;
            JToken jtFind = null;
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

            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Referer = hi.URL;
            hi.Allowautoredirect = false;

            hi.Timeout = 20000;
            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            do
            {
                isNeedLoop = false;
                hr = hh.GetHtml(hi);

                if (!string.IsNullOrEmpty(hr.RedirectUrl))
                {
                    if (hr.RedirectUrl.StartsWith("https://www.facebook.com/login/?next=")) continue;
                    errorCode = StringHelper.GetMidStr(hr.RedirectUrl, "checkpoint/", "/");

                    WinInet_HttpHelper whh = new WinInet_HttpHelper();
                    WinInet_HttpItem whi = null;
                    WinInet_HttpResult whr = null;
                    HttpItem hi_Temp = null;

                    whi = new WinInet_HttpItem();
                    whi.Url = hr.RedirectUrl;
                    whi.UserAgent = account.UserAgent;
                    whi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                    whi.AcceptEncoding = $"gzip";
                    whi.AcceptLanguage = $"zh-HK,zh;q=0.9";
                    whi.AutoRedirect = false;
                    whi.TimeOut_Connect = 10000;
                    whi.TimeOut_Request = 20000;

                    whi.Referrer = hr.RedirectUrl;
                    whi.Cookie = account.LoginInfo.LoginInfo_CookieStr;
                    whi.OtherHeaders.Add(new WinInet_Header() { Name = "Sec-Fetch-Site", Value = "none" });

                    whr = whh.GetHtml(whi);

                    if (whr.HtmlString.Contains("Please enter your password to continue</h2>"))
                    {
                        hi.URL = $"https://www.facebook.com/";
                        isNeedLoop = true;
                    }
                    else if (whr.HtmlString.Contains("your account has been locked"))
                    {
                        //进一步判断，是否能解开
                        #region 获取API所需要的参数
                        //{"connectionClass":"EXCELLENT"}
                        __ccg = StringHelper.GetMidStr(whr.HtmlString, "\"connectionClass\":\"", "\"");
                        //"{"server_revision":1014268370,
                        __rev = StringHelper.GetMidStr(whr.HtmlString, "\"server_revision\":", ",");
                        //"cavalry_get_lid":"7381512262390677322"
                        __hsi = StringHelper.GetMidStr(whr.HtmlString, "\"brsid\":\"", "\"");
                        __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
                        __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
                        //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
                        fb_dtsg = StringHelper.GetMidStr(whr.HtmlString, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
                        //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
                        //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
                        lsd = StringHelper.GetMidStr(whr.HtmlString, "\"LSD\",[],{\"token\":\"", "\"");
                        server_timestamps = "true";

                        //"ACCOUNT_ID":"100090715681373"
                        string accountId = StringHelper.GetMidStr(whr.HtmlString, "\"ACCOUNT_ID\":\"", "\"");
                        string token = StringHelper.GetMidStr(whr.HtmlString, "\"button_text\":\"Get started\",\"token\":\"", "\"");
                        #endregion

                        #region 账户被锁定,点击Get started
                        account.Running_Log = $"账户被锁定,点击Get started";
                        hi_Temp = new HttpItem();
                        hi_Temp.URL = $"https://www.facebook.com/api/graphql/";
                        hi_Temp.UserAgent = account.UserAgent;
                        hi_Temp.Accept = $"*/*";
                        hi_Temp.Header.Add("Accept-Encoding", "gzip");
                        hi_Temp.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                        hi_Temp.Allowautoredirect = false;

                        hi_Temp.Header.Add("Sec-Fetch-Site", "same-origin");
                        hi_Temp.Method = "POST";
                        hi_Temp.ContentType = $"application/x-www-form-urlencoded";

                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        fb_api_req_friendly_name = "useEpsilonNavigateMutation";
                        doc_id = "7583995681722871";
                        //{"input":{"client_mutation_id":"1","actor_id":"100094907002338","step":"STEPPER_CONFIRMATION","token":{"sensitive_string_value":"AVhIJMLP1y3ME9F_OvwvVrNsTNEbXks-R1tdKi7bIrAW72NMaPBUY9s_2-S5ABu-CU7d0lTUmteauDJzgDPKaMjkVW2Rzrlsl72gk2467PRAuYP6OrwlhJIxEQEN9bHoHzMphx1S4iYKyF6P3kM62HWzMLGtjmMOXDmgup6tvosj5Y2jn1oNg52yyaBnrp0qTRM0iR-GPynvNPRewTFmZnGuJyX-vJggcznNCLbs-COUns-1_prb--9qwvF87tbYFCAGD8_seaMiUzRTnTLouV46_7_JXFGx7tMU1U2rg8LWKYzTWnUmtFdvjQxYZ5OX1gpt1k_hfQCLO55DdgEm37su23o3kkaYFCrq20Seyxbnp5rRtBgl35B-yZdfG9Q8WhNsZs6AwPidEb3sQLEIQob1iAvT5gLDYADiHX3LOA5U"}},"scale":2}
                        variables = "{\"input\":{\"client_mutation_id\":\"1\",\"actor_id\":\"" + accountId + "\",\"step\":\"STEPPER_CONFIRMATION\",\"token\":{\"sensitive_string_value\":\"" + token + "\"}},\"scale\":2}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = accountId;
                        jo_postdata["__user"] = accountId;
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
                        jo_postdata["__comet_req"] = "15";
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

                        hi_Temp.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                        //Cookie
                        hi_Temp.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                        //代理
                        if (account.WebProxy != null) hi_Temp.WebProxy = account.WebProxy;

                        hr = hh.GetHtml(hi_Temp);

                        //合并CK
                        if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                        #endregion

                        #region 判断结果，是否能被解开
                        jr = null;
                        try { jr = JObject.Parse(hr.Html); } catch { }
                        if (jr == null || jr.SelectToken("data['epsilon_navigate']['epsilon_checkpoint'].help.questions[1].steps") == null) { account.Running_Log = $"账户被锁定,点击Get started失败，即将重试"; isNeedLoop = true; continue; }
                        jtsFind = jr.SelectToken("data['epsilon_navigate']['epsilon_checkpoint'].help.questions[1].steps");

                        jtFind = jtsFind.Where(jt => jt["title"] != null && (jt["title"].ToString().Trim() == "Confirm that this is your account" || jt["title"].ToString().Trim() == "Account confirmed")).FirstOrDefault();
                        if (jtFind == null || jtFind["active"] == null)
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,操作步骤不能识别:{JsonConvert.SerializeObject(jtsFind)})";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }
                        if (jtFind["active"].Value<bool>())
                        {
                            jo_Result["ErrorMsg"] = "Cookie无效(账户被锁定,无法被解开)";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }

                        jtFind = jtsFind.Where(jt => jt["title"] != null && jt["title"].ToString().Trim() == "Secure your login details").FirstOrDefault();
                        if (jtFind == null || jtFind["active"] == null || !jtFind["active"].Value<bool>())
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,操作步骤不能识别:{JsonConvert.SerializeObject(jtsFind)})";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }
                        if (string.IsNullOrEmpty(account.New_Mail_Name.Trim()))
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,未绑定新邮箱)";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }

                        //获取下一步Token
                        jtFind = jr.SelectToken("data['epsilon_navigate']['epsilon_checkpoint'].screen.token");
                        if (jtFind == null)
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,点击Get started 后 获取token失败)";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }
                        token = jtFind.ToString().Trim();
                        if (string.IsNullOrEmpty(token))
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,点击Get started 后 获取token失败)";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }
                        #endregion

                        #region 账户被锁定,点击Next
                        account.Running_Log = $"账户被锁定,点击Next";
                        hi_Temp = new HttpItem();
                        hi_Temp.URL = $"https://www.facebook.com/api/graphql/";
                        hi_Temp.UserAgent = account.UserAgent;
                        hi_Temp.Accept = $"*/*";
                        hi_Temp.Header.Add("Accept-Encoding", "gzip");
                        hi_Temp.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                        hi_Temp.Allowautoredirect = false;

                        hi_Temp.Header.Add("Sec-Fetch-Site", "same-origin");
                        hi_Temp.Method = "POST";
                        hi_Temp.ContentType = $"application/x-www-form-urlencoded";

                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        fb_api_req_friendly_name = "useEpsilonNavigateMutation";
                        doc_id = "7914047355320799";
                        //{"input":{"client_mutation_id":"2","actor_id":"100007690299469","step":"CONTACT_POINT_REVIEW","token":{"sensitive_string_value":"AVjOjwsMv7QpcxSN2ujEblfemYNna0gq33tTsKlE3l-cih7m0PA_W4QRL1cJJidKKr5FypComZywp3hFimZImq5-fOCU4JXRjc392I78KfuAGa-gJT7Z0DoMWxm9mWFREvi1ohr968JEqA99SMHS5h2BVCcsiA_F_Wc7N9Ru9y8sqbog5id-685XkzTJUTiDL7NtTVRHW55HjCh-d-Kvw3jMl4q4XfKVWITePMHCLGy22xXpDUVlpbGYQwFGc6wPKH-fhxXeMdEQ_ZxB7WiJ2yTYdszk7ZG6FujgXQRUAzuObg7rw9KjIbImJQuXF7Md5zmBaZO8Q5CGd4zTZFmpdQ0qRWatDtLyjFQPHMiEo1NPMr3KnBgin0WzQfqRTwr2BPpQGktOygyaLcc0xm9Nof9oS_9WqDtWos_ARqT4WQSJ-TJxPk4z8YA"}},"scale":2}
                        variables = "{\"input\":{\"client_mutation_id\":\"2\",\"actor_id\":\"" + accountId + "\",\"step\":\"CONTACT_POINT_REVIEW\",\"token\":{\"sensitive_string_value\":\"" + token + "\"}},\"scale\":2}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = accountId;
                        jo_postdata["__user"] = accountId;
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
                        jo_postdata["__comet_req"] = "15";
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

                        hi_Temp.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                        //Cookie
                        hi_Temp.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                        //代理
                        if (account.WebProxy != null) hi_Temp.WebProxy = account.WebProxy;

                        hr = hh.GetHtml(hi_Temp);

                        //合并CK
                        if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                        #endregion

                        #region 判断结果
                        jr = null;
                        try { jr = JObject.Parse(hr.Html); } catch { }
                        if (jr == null || jr.SelectToken("data['epsilon_navigate']['epsilon_checkpoint'].screen['contact_points']") == null) { account.Running_Log = $"账户被锁定,点击Next失败，即将重试"; isNeedLoop = true; continue; }
                        jtsFind = jr.SelectToken("data['epsilon_navigate']['epsilon_checkpoint'].screen['contact_points']");
                        jtFind = jtsFind.Where(jt => jt["label"] != null && StringHelper.Usc2ConvertToAnsi(jt["label"].ToString().Trim()) == account.New_Mail_Name.Trim()).FirstOrDefault();
                        if (jtFind == null)
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,联系方式不包含新邮箱)";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }
                        if (jtsFind.Count() == 1)
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,联系方式只有新邮箱)";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }

                        //整理需要删除的ID
                        List<string> ids_to_remove = jtsFind.Select(jt => jt["id"] == null ? string.Empty : jt["id"].ToString().Trim()).Where(s => s.Length > 0).ToList();
                        if (ids_to_remove.Count == 0)
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,点击Next 后 获取删除列表失败:{JsonConvert.SerializeObject(jtsFind)})";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }

                        //获取下一步Token
                        jtFind = jr.SelectToken("data['epsilon_navigate']['epsilon_checkpoint'].screen.token");
                        if (jtFind == null)
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,点击Next 后 获取token失败)";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }
                        token = jtFind.ToString().Trim();
                        if (string.IsNullOrEmpty(token))
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,点击Next 后 获取token失败)";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }
                        #endregion

                        #region 账户被锁定,点击Confirm
                        account.Running_Log = $"账户被锁定,点击Confirm";
                        hi_Temp = new HttpItem();
                        hi_Temp.URL = $"https://www.facebook.com/api/graphql/";
                        hi_Temp.UserAgent = account.UserAgent;
                        hi_Temp.Accept = $"*/*";
                        hi_Temp.Header.Add("Accept-Encoding", "gzip");
                        hi_Temp.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                        hi_Temp.Allowautoredirect = false;

                        hi_Temp.Header.Add("Sec-Fetch-Site", "same-origin");
                        hi_Temp.Method = "POST";
                        hi_Temp.ContentType = $"application/x-www-form-urlencoded";

                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        fb_api_req_friendly_name = "useRemoveContactPointsMutation";
                        doc_id = "8134347373266323";
                        //{"input":{"client_mutation_id":"3","actor_id":"100007690299469","ids_to_remove":["1485077585091904"],"token":{"sensitive_string_value":"AVjdVvFzLsWgKuWtif9pmO0AIHBtplJd2B_4rJIAzWayHd2GL60DOFDEYfzMOVS0FGyDEdbsx1-tXw0MDoEB7Ncbrnoc97CcvmePmXrrk4g57JW5dXKnkJ29UAbKGI9vPGYThGAyCUo3rdJ4fntZWrUSgGfAkGj-TtT5lOxxJJvYnBIhNQoZ0aF7GRLRHO25FgXtHQBONHtTgbLNyhkJYvrkO7-g5QyMsMo15FmpTDJQzil3xxMOU1fbKqqc55YOjLnlhhR1cCK5-QPppf6bjYuhL_7ezt0t13GQcMpwyaW8T-4qTJA6X1LRYVvEARD8TgUMKXDfW_9isdyyCbGdLDOd4oCp9GCWaa_YBSc3YJUYw0GOI_bnSqD5Io67IaL5TzV0fvrmOUUdfPfHBMbMCXdWvkgzEhJlSPlLvRT8eswsF5lL5Y5OKWA"}},"scale":2}
                        variables = "{\"input\":{\"client_mutation_id\":\"3\",\"actor_id\":\"" + accountId + "\",\"ids_to_remove\":" + JsonConvert.SerializeObject(ids_to_remove) + ",\"token\":{\"sensitive_string_value\":\"" + token + "\"}},\"scale\":2}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = accountId;
                        jo_postdata["__user"] = accountId;
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
                        jo_postdata["__comet_req"] = "15";
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

                        hi_Temp.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                        //Cookie
                        hi_Temp.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                        //代理
                        if (account.WebProxy != null) hi_Temp.WebProxy = account.WebProxy;

                        hr = hh.GetHtml(hi_Temp);

                        //合并CK
                        if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                        #endregion

                        #region 判断结果，判断是否删除成功了
                        jr = null;
                        try { jr = JObject.Parse(hr.Html); } catch { }
                        if (jr == null || jr.SelectToken("data['remove_contact_points']['epsilon_checkpoint'].screen.title") == null) { account.Running_Log = $"账户被锁定,点击Confirm失败，即将重试"; isNeedLoop = true; continue; }
                        jtFind = jr.SelectToken("data['remove_contact_points']['epsilon_checkpoint'].screen.title");
                        if (jtFind.ToString().Trim() != "Your new login details")
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,点击Confirm后返回异常:{jtFind.ToString().Trim()})";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }

                        //获取下一步Token
                        jtFind = jr.SelectToken("data['remove_contact_points']['epsilon_checkpoint'].screen.token");
                        if (jtFind == null)
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,点击Confirm 后 获取token失败)";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }
                        token = jtFind.ToString().Trim();
                        if (string.IsNullOrEmpty(token))
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,点击Confirm 后 获取token失败)";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }
                        #endregion

                        #region 账户被锁定,点击Back to Facebook
                        account.Running_Log = $"账户被锁定,点击Back to Facebook";
                        hi_Temp = new HttpItem();
                        hi_Temp.URL = $"https://www.facebook.com/api/graphql/";
                        hi_Temp.UserAgent = account.UserAgent;
                        hi_Temp.Accept = $"*/*";
                        hi_Temp.Header.Add("Accept-Encoding", "gzip");
                        hi_Temp.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                        hi_Temp.Allowautoredirect = false;

                        hi_Temp.Header.Add("Sec-Fetch-Site", "same-origin");
                        hi_Temp.Method = "POST";
                        hi_Temp.ContentType = $"application/x-www-form-urlencoded";

                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        fb_api_req_friendly_name = "useEpsilonNavigateMutation";
                        doc_id = "7914047355320799";
                        //{"input":{"client_mutation_id":"4","actor_id":"100007690299469","step":"OUTRO","token":{"sensitive_string_value":"AVhk......."}},"scale":2}
                        variables = "{\"input\":{\"client_mutation_id\":\"4\",\"actor_id\":\"" + accountId + "\",\"step\":\"OUTRO\",\"token\":{\"sensitive_string_value\":\"" + token + "\"}},\"scale\":2}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = accountId;
                        jo_postdata["__user"] = accountId;
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
                        jo_postdata["__comet_req"] = "15";
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

                        hi_Temp.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                        //Cookie
                        hi_Temp.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                        //代理
                        if (account.WebProxy != null) hi_Temp.WebProxy = account.WebProxy;

                        hr = hh.GetHtml(hi_Temp);

                        //合并CK
                        if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
                        #endregion

                        #region 判断结果，判断返回是否正常
                        jr = null;
                        try { jr = JObject.Parse(hr.Html); } catch { }
                        if (jr == null || jr.SelectToken("data['epsilon_navigate']['epsilon_checkpoint'].screen.title") == null) { account.Running_Log = $"账户被锁定,点击Back to Facebook失败，即将重试"; isNeedLoop = true; continue; }
                        jtFind = jr.SelectToken("data['epsilon_navigate']['epsilon_checkpoint'].screen.title");
                        if (jtFind.ToString().Trim() != "You've unlocked your account")
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效(账户被锁定,点击Back to Facebook后返回异常:{jtFind.ToString().Trim()})";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }
                        #endregion

                        isNeedLoop = true;
                    }
                    else if (whr.HtmlString.Contains("You’ll need to set up two-factor authentication to keep using Facebook"))
                    {
                        jo_Result["ErrorMsg"] = "Cookie无效(需要开启2FA)";
                        jo_Result["isNeedLoop"] = false;
                        return jo_Result;
                    }
                    else
                    {
                        if (errorCode == "601051028565049")
                        {
                            #region 获取API所需要的参数
                            //{"connectionClass":"EXCELLENT"}
                            __ccg = StringHelper.GetMidStr(whr.HtmlString, "\"connectionClass\":\"", "\"");
                            //"{"server_revision":1014268370,
                            __rev = StringHelper.GetMidStr(whr.HtmlString, "\"server_revision\":", ",");
                            //"cavalry_get_lid":"7381512262390677322"
                            __hsi = StringHelper.GetMidStr(whr.HtmlString, "\"brsid\":\"", "\"");
                            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
                            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
                            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
                            fb_dtsg = StringHelper.GetMidStr(whr.HtmlString, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
                            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
                            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
                            lsd = StringHelper.GetMidStr(whr.HtmlString, "\"LSD\",[],{\"token\":\"", "\"");
                            server_timestamps = "true";

                            //"ACCOUNT_ID":"100090715681373"
                            string accountId = StringHelper.GetMidStr(whr.HtmlString, "\"ACCOUNT_ID\":\"", "\"");
                            #endregion

                            #region 点击Dismiss
                            account.Running_Log = $"点击Dismiss";
                            hi_Temp = new HttpItem();
                            hi_Temp.URL = $"https://www.facebook.com/api/graphql/";
                            hi_Temp.UserAgent = account.UserAgent;
                            hi_Temp.Accept = $"*/*";
                            hi_Temp.Header.Add("Accept-Encoding", "gzip");
                            hi_Temp.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                            hi_Temp.Allowautoredirect = false;

                            hi_Temp.Header.Add("Sec-Fetch-Site", "same-origin");
                            hi_Temp.Method = "POST";
                            hi_Temp.ContentType = $"application/x-www-form-urlencoded";

                            #region 整理提交数据
                            fb_api_caller_class = "RelayModern";
                            fb_api_req_friendly_name = "FBScrapingWarningMutation";
                            doc_id = "6339492849481770";
                            variables = "{}";

                            jo_postdata = new JObject();
                            jo_postdata["av"] = accountId;
                            jo_postdata["__user"] = accountId;
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
                            jo_postdata["__comet_req"] = "15";
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

                            hi_Temp.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

                            //Cookie
                            hi_Temp.Cookie = account.LoginInfo.LoginInfo_CookieStr;

                            //代理
                            if (account.WebProxy != null) hi_Temp.WebProxy = account.WebProxy;

                            hr = hh.GetHtml(hi_Temp);

                            //合并CK
                            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);


                            //判断结果
                            //{"data":{"fb_scraping_warning_clear":{"success":true}},"extensions":{"is_final":true}}
                            jr = null;
                            try { jr = JObject.Parse(hr.Html); } catch { }
                            if (jr == null || jr.SelectToken("data['fb_scraping_warning_clear'].success") == null || !jr.SelectToken("data['fb_scraping_warning_clear'].success").Value<bool>()) account.Running_Log = $"点击Dismiss失败，即将重试";
                            else account.Running_Log = $"点击Dismiss成功";

                            isNeedLoop = true;
                            #endregion
                        }
                        else
                        {
                            jo_Result["ErrorMsg"] = $"Cookie无效({hr.RedirectUrl})";
                            jo_Result["isNeedLoop"] = false;
                            return jo_Result;
                        }
                    }
                }

                if (isNeedLoop) { Thread.Sleep(500); Application.DoEvents(); }
            } while (isNeedLoop);

            //",[],{"ACCOUNT_ID":"100070101416521","USER_ID":"100070101416521","NAME":"Matias Contrera","SHORT_NAME":"Matias","IS_BUSINESS_PERSON_ACCOUNT":false
            account.LoginInfo.LoginData_Account_Id = StringHelper.GetMidStr(hr.Html, "\"ACCOUNT_ID\":\"", "\"").Trim();
            bool isSuccess = !string.IsNullOrEmpty(account.LoginInfo.LoginData_Account_Id) && account.LoginInfo.LoginData_Account_Id != "0";

            jo_Result["Success"] = isSuccess;
            jo_Result["ErrorMsg"] = isSuccess ? "Cookie有效" : "Cookie无效";

            if (isSuccess) { if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie); }

            return jo_Result;
        }
        /// <summary>
        /// 绑定新邮箱
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_BindNewEmail(Account_FBOrIns account, MailInfo mail)
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

            #region 先访问联系人页面
            account.Running_Log = $"绑邮箱:进入目标页面(contact_points)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/personal_info/contact_points";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7,charset=UTF-8";
            hi.Header.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            hi.Header.Add("Accept-Language", "en-US,en;q=0.9");
            hi.Header.Add("dpr", "1");
            hi.Header.Add("priority", "u=0, i");
            hi.Header.Add("sec-ch-prefers-color-scheme", "light");
            hi.Header.Add("sec-ch-ua", "Chromium\";v=\"134\", \"Not:A-Brand\";v=\"24\", \"Google Chrome\";v=\"134");
            hi.Header.Add("sec-ch-ua-full-version-list", "Chromium\";v=\"134.0.6998.89\", \"Not:A-Brand\";v=\"24.0.0.0\", \"Google Chrome\";v=\"134.0.6998.89");
            hi.Header.Add("sec-ch-ua-mobile", "?0");
            hi.Header.Add("sec-ch-ua-model", "");
            hi.Header.Add("sec-ch-ua-platform", "Windows");
            hi.Header.Add("sec-ch-ua-platform-version", "15.0.0");
            hi.Header.Add("sec-fetch-dest", "document");
            hi.Header.Add("sec-fetch-mode", "navigate");
            hi.Header.Add("sec-fetch-site", "none");
            hi.Header.Add("sec-fetch-user", "?1");
            hi.Header.Add("upgrade-insecure-requests", "1");
            hi.Header.Add("viewport-width", "1264");
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
            string __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            string __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            string __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            string fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            string lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            string server_timestamps = "true";
            #endregion

            #region 获取现有的联系方式列表，判断是否已经绑定过
            account.Running_Log = $"绑邮箱:获取绑定列表";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
                hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "FXAccountsCenterAddContactPointMutation";
                doc_id = "6970150443042883";
                variables = "{\"country\":\"US\",\"contact_point\":\"" + mail.Mail_Name + "\",\"contact_point_type\":\"email\",\"selected_accounts\":[\"" + account.LoginInfo.LoginData_Account_Id + "\"],\"family_device_id\":\"device_id_fetch_datr\",\"client_mutation_id\":\"mutation_id_" + StringHelper.GetUnixTime(DateTime.Now) + "\"}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__a"] = string.Empty;
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = string.Empty;
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
                            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                            jo_postdata["__a"] = string.Empty;
                            jo_postdata["__req"] = string.Empty;
                            jo_postdata["__hs"] = string.Empty;
                            jo_postdata["dpr"] = string.Empty;
                            jo_postdata["__ccg"] = __ccg;
                            jo_postdata["__rev"] = __rev;
                            jo_postdata["__s"] = string.Empty;
                            jo_postdata["__hsi"] = __hsi;
                            jo_postdata["__dyn"] = __dyn;
                            jo_postdata["__csr"] = __csr;
                            jo_postdata["__comet_req"] = string.Empty;
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
                        jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__a"] = string.Empty;
                        jo_postdata["__req"] = string.Empty;
                        jo_postdata["__hs"] = string.Empty;
                        jo_postdata["dpr"] = string.Empty;
                        jo_postdata["__ccg"] = __ccg;
                        jo_postdata["__rev"] = __rev;
                        jo_postdata["__s"] = string.Empty;
                        jo_postdata["__hsi"] = __hsi;
                        jo_postdata["__dyn"] = __dyn;
                        jo_postdata["__csr"] = __csr;
                        jo_postdata["__comet_req"] = string.Empty;
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
                pop3MailMessage = msgList.Where(m => m.DateSent >= sendCodeTime && m.Subject.Contains("Confirm email") && m.From.Contains("<security@facebookmail.com>")).FirstOrDefault();
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
            confirmCode = StringHelper.GetMidStr(pop3MailMessage.Html, "You may be asked to enter this confirmation code:", "Thanks").Trim();
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
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterContactPointConfirmationDialogVerifyContactPointMutation";
            doc_id = "8108292719198518";
            variables = "{\"contact_point\":\"" + mail.Mail_Name + "\",\"contact_point_type\":\"email\",\"pin_code\":\"" + confirmCode + "\",\"selected_accounts\":[\"" + account.LoginInfo.LoginData_Account_Id + "\"],\"family_device_id\":\"device_id_fetch_datr\",\"client_mutation_id\":\"mutation_id_" + StringHelper.GetUnixTime(DateTime.Now) + "\",\"contact_point_event_type\":\"ADD\",\"normalized_contact_point_to_replace\":\"\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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

            isSuccess = jr.SelectToken("data['xfb_verify_contact_point'][0]['mutation_data'].success").ToString().ToLower() == "true";
            jo_Result["Success"] = isSuccess;
            if (!isSuccess) jo_Result["ErrorMsg"] = jr.SelectToken("data['xfb_verify_contact_point'][0]['mutation_data'].error_text").ToString().Trim();
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
        public JObject FB_ForgotPassword(Account_FBOrIns account, string newPassword, bool logMeOut)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["isNeedLoop"] = false;

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
            JObject jr = null;
            string html = string.Empty;
            int timeSpan = 0;
            int timeCount = 0;
            int timeOut = 0;
            //bool isSuccess = false;
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

            string account_name = string.Empty;
            string password_reset_uri = string.Empty;
            string cuid = string.Empty;
            string encryptPwd = string.Empty;
            DateTime sendCodeTime = DateTime.Parse("1970-01-01");
            string public_key = string.Empty;
            string key_id = string.Empty;

            #region 先访问目标页面
            account.Running_Log = $"忘记密码:进入目标页面(password/change)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/password_and_security/password/change";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";

            //XFBFXFBAccountInfo","id":"100019181377293","__isXFBFXIdentityInfo":"XFBFXFBAccountInfo","profile_identifier":"100019181377293","display_name":"Wagner Rodeigues","platform_info":{"type":"FACEBOOK","name":"Facebook"},"profile_picture_info":
            //account_name = StringHelper.GetMidStr(hr.Html, "\"profile_identifier\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"display_name\":\"", "\",\"platform_info\":{\"type\":\"FACEBOOK\"");
            //File.WriteAllText("C:\\Users\\Administrator\\Desktop\\Html.html", hr.Html);

            //"USER_ID":"100019181377293","NAME":"Wagner Rodeigues"
            account_name = StringHelper.GetMidStr(hr.Html, "\"USER_ID\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"NAME\":\"", "\"");
            #endregion

            #region 选择Facebook账号
            account.Running_Log = $"忘记密码:选择Facebook账号";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterChangePasswordDialogQuery";
            doc_id = "7728877477131487";
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_name\":\"" + account_name + "\",\"account_type\":\"FACEBOOK\",\"interface\":\"FB_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
            if (jr == null || jr.SelectToken("data['fxcal_settings'].node['password_reset_uri']") == null || jr.SelectToken("data['fxcal_settings'].node['public_key_and_id_for_encryption']['public_key']") == null)
            {
                jo_Result["ErrorMsg"] = $"获取忘记密码的链接失败({hr.Html})";
                return jo_Result;
            }

            password_reset_uri = jr.SelectToken("data['fxcal_settings'].node['password_reset_uri']").ToString();
            cuid = StringHelper.GetMidStr(password_reset_uri + "&", "cuid=", "&");
            public_key = jr.SelectToken("data['fxcal_settings'].node['public_key_and_id_for_encryption']['public_key']").ToString();
            key_id = jr.SelectToken("data['fxcal_settings'].node['public_key_and_id_for_encryption']['key_id']").ToString();
            #endregion

            #region 请求向新邮箱发送验证码
            account.Running_Log = $"忘记密码:请求向新邮箱发送验证码[{account.New_Mail_Name}]";
            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/api/graphql/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "RecoverInitiateFormMutation";
            doc_id = "7513869558630634";
            variables = "{\"input\":{\"client_mutation_id\":\"1\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"cl\":false,\"ctx\":null,\"cuid\":\"" + cuid + "\",\"recover_method\":\"send_email\",\"reg_instance\":null,\"selected_cuid\":null,\"wsr\":false}}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
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
            jo_postdata["__comet_req"] = "15";
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
            if (jr == null || jr.SelectToken("data['submit_contact_point']['redirect_uri']") == null && !hr.Html.Contains("{\"submit_contact_point\":null}"))
            {
                jo_Result["ErrorMsg"] = $"忘记密码:向邮箱发送验证码失败({hr.Html})";
                jo_Result["isNeedLoop"] = true;
                return jo_Result;
            }
            if (jr == null || jr.SelectToken("data['submit_contact_point']['redirect_uri']") == null) sendCodeTime = DateTime.Parse("1970-01-01");
            #endregion

            #region 提取邮件验证码
            account.Running_Log = $"忘记密码:提取邮件验证码[{account.New_Mail_Name}]";
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
                pop3MailMessage = msgList.Where(m => m.DateSent >= sendCodeTime && m.Subject.Contains("is your Facebook account recovery code") && m.From.Contains("<security@facebookmail.com>")).FirstOrDefault();
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
            confirmCode = StringHelper.GetMidStr("{begin}" + pop3MailMessage.Subject, "{begin}", "is your Facebook account recovery code").Trim();
            Match match = new Regex(@"\d{6}").Match(confirmCode);
            if (match.Success) confirmCode = match.Value;
            else
            {
                jo_Result["ErrorMsg"] = "忘记密码:提取邮件验证码失败";
                return jo_Result;
            }
            #endregion

            #region 输入验证码进行验证
            account.Running_Log = $"忘记密码:输入验证码进行验证[{confirmCode}]";
            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useCodeSubmittedMutation";
            doc_id = "4549690068470661";
            variables = "{\"input\":{\"client_mutation_id\":\"2\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_recovery_source\":null,\"auto_send_flow\":\"default_recover\",\"code\":\"" + confirmCode + "\",\"cuid\":null,\"notif_medium\":null,\"redirect_from\":null,\"source\":null}}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
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
            jo_postdata["__comet_req"] = "15";
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
            if (jr == null || jr.SelectToken("data['code_submitted']['redirect_uri']") == null || jr.SelectToken("data['code_submitted']['error_message']") == null)
            {
                jo_Result["ErrorMsg"] = $"忘记密码:校验验证码失败({hr.Html})";
                jo_Result["isNeedLoop"] = true;
                return jo_Result;
            }
            if (!string.IsNullOrEmpty(jr.SelectToken("data['code_submitted']['error_message']").ToString()))
            {
                jo_Result["ErrorMsg"] = $"忘记密码:校验验证码失败({jr.SelectToken("data['code_submitted']['error_message']").ToString()})";
                jo_Result["isNeedLoop"] = true;
                return jo_Result;
            }

            password_reset_uri = jr.SelectToken("data['code_submitted']['redirect_uri']").ToString().Trim();
            #endregion

            #region 修改密码
            account.Running_Log = $"忘记密码:修改密码[{newPassword}]";
            if (string.IsNullOrEmpty(newPassword)) newPassword = $"FB_{account.LoginInfo.LoginData_Account_Id}";

            //暂时先固定密码
            //niubi.fb
            //encryptPwd = "#PWD_BROWSER:5:1724298358:AdpQAGum6ij3i/y8zDies3vvLnFkwPhUApbSfdwwkjMpxxQHvIFl78shHm8paCE/htVwi0+haQWIh+Jix2xH4murxNR/Bl4RbUr5/1j41jSQbI88dEWB6N9w62b+0e76aVF32i0t9tdz6E3N";
            encryptPwd = this.FB_Encpass_Method(newPassword);
            Console.WriteLine($"忘记密码 > {newPassword} > {encryptPwd}");

            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/api/graphql/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useRecoverPasswordMutation";
            doc_id = "25957823340532875";
            variables = "{\"input\":{\"client_mutation_id\":\"3\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_recovery_source\":null,\"auto_send_flow\":\"default_recover\",\"btn_skip\":false,\"is_one_click_login\":false,\"log_out_everywhere\":" + logMeOut.ToString().ToLower() + ",\"new_password\":{\"sensitive_string_value\":\"" + encryptPwd + "\"},\"nonce\":{\"sensitive_string_value\":\"" + confirmCode + "\"},\"ocl\":false,\"ocls\":false,\"self_identified_hacked\":false,\"skip_password_change\":false,\"uid\":" + account.LoginInfo.LoginData_Account_Id + "},\"scale\":null}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
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
            jo_postdata["__comet_req"] = "15";
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
            if (jr == null || jr.SelectToken("data['recover_password_form_submit']['redirect_uri']") == null || jr.SelectToken("data['recover_password_form_submit']['error_message']") == null)
            {
                jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败({hr.Html})";
                jo_Result["isNeedLoop"] = true;
                return jo_Result;
            }
            if (!string.IsNullOrEmpty(jr.SelectToken("data['recover_password_form_submit']['error_message']").ToString()))
            {
                jo_Result["ErrorMsg"] = $"忘记密码时修改密码失败({jr.SelectToken("data['recover_password_form_submit']['error_message']").ToString()})";
                jo_Result["isNeedLoop"] = true;
                return jo_Result;
            }
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
        public JObject FB_OpenTwoFA_Dynamic(Account_FBOrIns account)
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

            #region 先访问目标页面
            account.Running_Log = $"打开2FA:进入目标页面(two_factor)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/password_and_security/two_factor";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";

            if (string.IsNullOrEmpty(fb_dtsg)) { jo_Result["ErrorMsg"] = $"打开2FA:访问目标页面失败(two_factor)"; return jo_Result; }
            #endregion

            #region 选择Facebook账号,查看是否已经开启
            account.Running_Log = $"打开2FA:选择Facebook账号";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterTwoFactorSettingsDialogQuery";
            doc_id = "7613749188645358";
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"interface\":\"FB_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
                jo_Result["ErrorMsg"] = $"打开2FA:向邮箱发送验证码失败({hr.Html})";
                return jo_Result;
            }
            #endregion

            #region 如果已经开启，需要先关闭，再打开
            if (jr.SelectToken("data['fxcal_settings'].node['two_factor_data']['is_totp_enabled']") != null && jr.SelectToken("data['fxcal_settings'].node['two_factor_data']['is_totp_enabled']").ToString().ToLower() == "true")
            {
                #region 关闭2FA
                randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

                account.Running_Log = $"打开2FA:先关闭2FA";
                hi = new HttpItem();
                hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "useFXSettingsTwoFactorDisableTOTPMutation";
                doc_id = "6758587527522047";
                variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"family_device_id\":\"device_id_fetch_datr\"}}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__a"] = string.Empty;
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = string.Empty;
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
                hi.URL = $"https://accountscenter.facebook.com/password_and_security/two_factor";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
                __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
                __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
                __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
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
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useFXSettingsTwoFactorGenerateTOTPKeyMutation";
            doc_id = "6282672078501565";
            variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"device_id\":\"device_id_fetch_datr\",\"fdid\":\"device_id_fetch_datr\"}}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
                //encryptPwd = "#PWD_BROWSER:5:1719252308:AZ5QADDtiGcrGB/Cv2WKo6h5+BznJr3xwRrAw/Wd2l08B7QqNDXbpMCfA8K/xam6uPP54i3BKiyI51peOJDL1jQZxv/MF1bZywHXUmA8Ydh/zGS5xYh/aBH1KMKdrt+Ea3AcGgXwxcHsljDm";
                encryptPwd = this.FB_Encpass_Method(account.Facebook_Pwd);

                hi = new HttpItem();
                hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "FXPasswordReauthenticationMutation";
                doc_id = "5864546173675027";
                variables = "{\"input\":{\"account_id\":" + account.LoginInfo.LoginData_Account_Id + ",\"account_type\":\"FACEBOOK\",\"password\":{\"sensitive_string_value\":\"" + encryptPwd + "\"},\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"client_mutation_id\":\"1\"}}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__aaid"] = "0";//特殊参数
                jo_postdata["__a"] = string.Empty;
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = string.Empty;
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
                hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "useFXSettingsTwoFactorGenerateTOTPKeyMutation";
                doc_id = "6282672078501565";
                variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"device_id\":\"device_id_fetch_datr\",\"fdid\":\"device_id_fetch_datr\"}}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__a"] = string.Empty;
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = string.Empty;
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
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterTwoFactorConfirmCodeDialogQuery";
            doc_id = "6792696137448786";
            variables = "{\"interface\":\"FB_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useFXSettingsTwoFactorEnableTOTPMutation";
            doc_id = "7032881846733167";
            variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"verification_code\":\"" + confirmCode + "\",\"device_id\":\"device_id_fetch_datr\",\"fdid\":\"device_id_fetch_datr\"}}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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

            if (jr != null && jr.SelectToken("errors[0].description") != null && jr.SelectToken("errors[0].description").ToString().Contains("\"challenge_type\":\"block\""))
            {
                #region 查询错误描述
                account.Running_Log = $"打开2FA:查询错误描述";
                hi = new HttpItem();
                hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "SecuredActionBlockDialogQuery";
                doc_id = "6108889802569432";
                variables = "{\"accountType\":\"FACEBOOK\"}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__a"] = string.Empty;
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = string.Empty;
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
            else if (jr == null || jr.SelectToken("data['xfb_two_factor_enable_totp'].success") == null || jr.SelectToken("data['xfb_two_factor_enable_totp'].success").ToString().ToLower().Trim() != "true")
            {
                jo_Result["ErrorMsg"] = $"打开2FA:进行打开2FA操作失败({hr.Html})";
                return jo_Result;
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
        public JObject FB_UpdateLanguageLocale(Account_FBOrIns account, string languageName)
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
            string checkpoint_data = string.Empty;

            #region 先访问目标页面
            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Referer = hi.URL;
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";
            #endregion

            #region 获取语言列表，并检测是否已经是要设置的语言
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "IntlLocaleSelectorTypeaheadSourceQuery";
            doc_id = "6390723504351289";
            variables = "{\"query\":\"\",\"suggestedLocaleLimit\":4,\"showOnlyFallbacks\":false}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__aaid"] = "0";//特殊参数
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
            html = StringHelper.Usc2ConvertToAnsi(hr.Html);

            // 正则表达式：匹配左边是英文冒号或右边是英文逗号的双引号
            string pattern = @"(?<!:)\x22(?!,)";
            // 使用正则表达式进行替换
            html = Regex.Replace(html, pattern, string.Empty);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            jr = null;
            try { jr = JObject.Parse(html); } catch { }
            if (jr == null || jr.SelectToken("data.localesQuery.results") == null)
            {
                jo_Result["ErrorMsg"] = $"设置语言时获取语言列表失败({html})";
                return jo_Result;
            }

            JToken jt_Find = jr.SelectToken("data.localesQuery.results").Where(jt => jt["recommendationReason"] != null && jt["recommendationReason"].ToString().Trim() == "RECENT").FirstOrDefault();
            if (jt_Find == null)
            {
                jo_Result["ErrorMsg"] = $"设置语言时获取语言列表失败({html})";
                return jo_Result;
            }
            else if (jt_Find["locale"].ToString() == languageName)
            {
                jo_Result["Success"] = true;
                jo_Result["ErrorMsg"] = $"设置语言成功({jt_Find["localizedName"].ToString().Trim()})";
                return jo_Result;
            }
            #endregion

            #region 更新语言
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useCometLocaleSelectorLanguageChangeMutation";
            doc_id = "6451777188273168";
            variables = "{\"locale\":\"" + languageName + "\",\"referrer\":\"WWW_COMET_NAVBAR\",\"fallback_locale\":null}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__aaid"] = "0";//特殊参数
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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

            //{"data":{"updateLanguageLocale":{"success":true}},"extensions":{"is_final":true}}
            if (!hr.Html.Contains("{\"updateLanguageLocale\":{\"success\":true}}"))
            {
                jo_Result["ErrorMsg"] = $"设置语言失败({hr.Html})";
                return jo_Result;
            }
            #endregion

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = $"设置语言成功({languageName})";

            //Cookie刷新
            account.Facebook_CK = StringHelper.GetCookieJsonStrByCookieCollection(account.LoginInfo.CookieCollection);

            return jo_Result;
        }
        /// <summary>
        /// 删除其他绑定
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_DeleteOtherContacts(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["GuoJia"] = string.Empty;

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
            hi.URL = $"https://accountscenter.facebook.com/personal_info/contact_points/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
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
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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

                //跳过非FB的账号
                jtFind = jt_contact["contact_point_info"].Where(jt => jt.SelectToken("['owner_profile'].id") != null && jt.SelectToken("['owner_profile'].id").ToString() == account.LoginInfo.LoginData_Account_Id).FirstOrDefault();
                if (jtFind == null) { ja_contact_points.RemoveAt(jtIndex); deleteCount += 1; continue; }
                selected_accounts = account.LoginInfo.LoginData_Account_Id;

                #region 点击Delete键
                account.Running_Log = $"删联系:点击Detete[{jt_contact["normalized_contact_point"].ToString()}]";
                hi = new HttpItem();
                hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__aaid"] = "0";//特殊参数
                jo_postdata["__a"] = string.Empty;
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = string.Empty;
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
                            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                            hi.UserAgent = account.UserAgent;
                            hi.Accept = $"*/*";
                            hi.Header.Add("Accept-Encoding", "gzip");
                            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                            hi.Allowautoredirect = false;

                            hi.Header.Add("Sec-Fetch-Site", "same-origin");
                            hi.Method = "POST";
                            hi.ContentType = $"application/x-www-form-urlencoded";

                            #region 整理提交数据
                            fb_api_caller_class = "RelayModern";
                            fb_api_req_friendly_name = "SecuredActionBlockDialogQuery";
                            doc_id = "6108889802569432";
                            variables = "{\"accountType\":\"FACEBOOK\"}";

                            jo_postdata = new JObject();
                            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                            jo_postdata["__a"] = string.Empty;
                            jo_postdata["__req"] = string.Empty;
                            jo_postdata["__hs"] = string.Empty;
                            jo_postdata["dpr"] = string.Empty;
                            jo_postdata["__ccg"] = __ccg;
                            jo_postdata["__rev"] = __rev;
                            jo_postdata["__s"] = string.Empty;
                            jo_postdata["__hsi"] = __hsi;
                            jo_postdata["__dyn"] = __dyn;
                            jo_postdata["__csr"] = __csr;
                            jo_postdata["__comet_req"] = string.Empty;
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
                    encryptPwd = this.FB_Encpass_Method(account.Facebook_Pwd);

                    hi = new HttpItem();
                    hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                    hi.UserAgent = account.UserAgent;
                    hi.Accept = $"*/*";
                    hi.Header.Add("Accept-Encoding", "gzip");
                    hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                    hi.Allowautoredirect = false;

                    hi.Header.Add("Sec-Fetch-Site", "same-origin");
                    hi.Method = "POST";
                    hi.ContentType = $"application/x-www-form-urlencoded";

                    #region 整理提交数据
                    fb_api_caller_class = "RelayModern";
                    fb_api_req_friendly_name = "FXPasswordReauthenticationMutation";
                    doc_id = "5864546173675027";
                    variables = "{\"input\":{\"account_id\":" + account.LoginInfo.LoginData_Account_Id + ",\"account_type\":\"FACEBOOK\",\"password\":{\"sensitive_string_value\":\"" + encryptPwd + "\"},\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"client_mutation_id\":\"1\"}}";

                    jo_postdata = new JObject();
                    jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                    jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                    jo_postdata["__aaid"] = "0";//特殊参数
                    jo_postdata["__a"] = string.Empty;
                    jo_postdata["__req"] = string.Empty;
                    jo_postdata["__hs"] = string.Empty;
                    jo_postdata["dpr"] = string.Empty;
                    jo_postdata["__ccg"] = __ccg;
                    jo_postdata["__rev"] = __rev;
                    jo_postdata["__s"] = string.Empty;
                    jo_postdata["__hsi"] = __hsi;
                    jo_postdata["__dyn"] = __dyn;
                    jo_postdata["__csr"] = __csr;
                    jo_postdata["__comet_req"] = string.Empty;
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
                    hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                    hi.UserAgent = account.UserAgent;
                    hi.Accept = $"*/*";
                    hi.Header.Add("Accept-Encoding", "gzip");
                    hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
                    jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                    jo_postdata["__aaid"] = "0";//特殊参数
                    jo_postdata["__a"] = string.Empty;
                    jo_postdata["__req"] = string.Empty;
                    jo_postdata["__hs"] = string.Empty;
                    jo_postdata["dpr"] = string.Empty;
                    jo_postdata["__ccg"] = __ccg;
                    jo_postdata["__rev"] = __rev;
                    jo_postdata["__s"] = string.Empty;
                    jo_postdata["__hsi"] = __hsi;
                    jo_postdata["__dyn"] = __dyn;
                    jo_postdata["__csr"] = __csr;
                    jo_postdata["__comet_req"] = string.Empty;
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
        /// 删除Ins关联
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_RemoveInsAccount(Account_FBOrIns account)
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

            #region 先访问目标页面
            account.Running_Log = $"删Ins关联:进入目标页面(accounts)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/accounts";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";

            //获取Ins账号
            //{"__typename":"XFBFXIGAccountInfo","id":"17841400406418311","__isXFBFXAccountInfo":"XFBFXIGAccountInfo","obfuscated_id":"FXACINFRAOBIDPERVIEWERAVPvFlIHClPcGRE5O53oVg6Hs49M7b6ufU7hHsllBelWr0PuLonaTEQ0EAOyn6Q6a43NQjYZ-NZCxHl6tPs","display_name":"topsitv","platform_info_v2":{"name":"Instagram","type":"INSTAGRAM"}
            idList = StringHelper.GetMidStrList(hr.Html, "\"__typename\":\"XFBFXIGAccountInfo\"", "\"name\":\"Instagram\"").Select(s => StringHelper.GetMidStr(s, "\"id\":\"", "\"").Trim()).Distinct().ToList();
            if (string.IsNullOrEmpty(fb_dtsg))
            {
                jo_Result["ErrorMsg"] = $"删Ins关联:进入目标页面失败(accounts)";
                return jo_Result;
            }
            if (idList.Count == 0)
            {
                jo_Result["Success"] = true;
                jo_Result["ErrorMsg"] = $"删Ins关联:操作成功(无Ins关联)";
                return jo_Result;
            }

            ja_account_idList = new JArray();
            for (int i = 0; i < idList.Count; i++) { JObject jo = new JObject(); jo["account_id"] = idList[i].ToString(); ja_account_idList.Add(jo); }
            #endregion

            for (int i = 0; i < ja_account_idList.Count; i++)
            {
                #region 删除Ins关联
                account.Running_Log = $"删Ins关联:{ja_account_idList[i]["account_id"].ToString().Trim()}";
                randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

                hi = new HttpItem();
                hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "useFXUnlinkMutation";
                doc_id = "7818255534903009";
                variables = "{\"client_mutation_id\":\"" + randomUUID + "\",\"account_id\":\"" + ja_account_idList[i]["account_id"].ToString().Trim() + "\",\"account_type\":\"INSTAGRAM\",\"flow\":\"FB_WEB_SETTINGS\",\"device_id\":\"device_id_fetch_datr\",\"interface\":\"FB_WEB\",\"platform\":\"FACEBOOK\",\"scale\":2,\"entrypoint\":null}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__aaid"] = "0";//特殊参数
                jo_postdata["__a"] = string.Empty;
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = string.Empty;
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
                else if (jr.SelectToken("data['fxcal_ig_web_destroy']['mani_unlink_success_dialog_content']") != null)
                {
                    ja_account_idList[i]["Success"] = true;
                    ja_account_idList[i]["ErrorMsg"] = $"删Ins关联成功";
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
            if (jo_Result["Success"].Value<bool>()) jo_Result["ErrorMsg"] = "删Ins关联:操作成功";
            else jo_Result["ErrorMsg"] = $"删Ins关联:操作失败({string.Join(",", jts_Failed.Select(j => $"{j["account_id"].ToString().Trim()}==>{j["ErrorMsg"].ToString().Trim()}"))})";

            return jo_Result;
        }
        /// <summary>
        /// 在其他设备注销
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_LogOutOfOtherDevices_Old(Account_FBOrIns account)
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
            string checkpoint_data = string.Empty;
            List<string> selectedList = null;

            #region 先访问目标页面
            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/password/change/reason";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";
            #endregion

            #region 请求获取注销设备的页面，获取参数：checkpoint_data
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useManageSessionsMutation";
            doc_id = "4743150309148084";
            variables = "{\"input\":{\"client_mutation_id\":\"1\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"option\":\"KILL_SESSIONS\"}}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__aaid"] = "0";//特殊参数
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
            html = StringHelper.Usc2ConvertToAnsi(hr.Html);
            jr = null;
            try { jr = JObject.Parse(html); } catch { }
            if (jr == null || jr.SelectToken("data['manage_sessions']['redirect_uri']") == null || string.IsNullOrEmpty(jr.SelectToken("data['manage_sessions']['redirect_uri']").ToString()))
            {
                jo_Result["ErrorMsg"] = $"注销其它设备时请求获取注销设备的页面失败({hr.Html})";
                return jo_Result;
            }

            redirect_uri = jr.SelectToken("data['manage_sessions']['redirect_uri']").ToString();
            //checkpoint_data
            checkpoint_data = StringHelper.UrlEncode(StringHelper.GetMidStr(redirect_uri + "{end}", "checkpoint_data=", "{end}"));
            #endregion

            #region 打开注销设备的页面，获取下一步提交的参数
            hi = new HttpItem();
            hi.URL = redirect_uri;
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //判断结果
            if (!hr.Html.Contains("<input type=\"hidden\" name=\"fb_dtsg\" value=\""))
            {
                jo_Result["ErrorMsg"] = $"注销其它设备时打开注销设备的页面失败({hr.Html})";
                return jo_Result;
            }

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            //<input type="hidden" autocomplete="off" name="nh" value="16d63c90d49028bcf672c90936f2488f362ae1f7" />
            nh = StringHelper.GetMidStr(hr.Html, "name=\"nh\" value=\"", "\"");
            //fb_dtsg
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //<input type="hidden" name="jazoest" value="25226" autocomplete="off" />
            jazoest = StringHelper.GetMidStr(hr.Html, "name=\"jazoest\" value=\"", "\"");

            //<input type="hidden" autocomplete="off" name="checkpoint_data" value="&#123;&quot;u&quot;:100019181377293
            //,&quot;t&quot;:1718847679,&quot;step&quot;:999010,&quot;n&quot;:&quot;WnqUnmoQpWg=&quot;,&quot;inst&quot;:1384772342172132
            //,&quot;f&quot;:1456805897898609,&quot;st&quot;:&quot;p&quot;,&quot;aid&quot;:null,&quot;ca&quot;:null,&quot;la&quot;:&quot;&quot;,&quot;ta&quot;
            //:null,&quot;tfvaid&quot;:null,&quot;tfvasec&quot;:null,&quot;sat&quot;:null,&quot;idg&quot;:false,&quot;cidue&quot;:&quot;&quot;,&quot;tfuln&quot;
            //:null,&quot;tfvri&quot;:null,&quot;ct&quot;:null,&quot;dss&quot;:&#123;&quot;A&quot;:[105]&#125;,&quot;ed&quot;:&#123;&quot;challenge_chooser_class&quot;
            //:null&#125;,&quot;s&quot;:&quot;AWWCVrahw_isyXClEhI&quot;,&quot;cs&quot;:[]&#125;" />
            checkpoint_data = System.Web.HttpUtility.HtmlDecode(StringHelper.GetMidStr(hr.Html, "name=\"checkpoint_data\" value=\"", "\"")).Replace("&#123;", "{").Replace("&#125;", "}");
            #endregion

            #region 点击Get Started
            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/checkpoint/flow";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            jo_postdata = new JObject();
            jo_postdata["checkpoint_created"] = "1";
            jo_postdata["checkpoint_data"] = checkpoint_data;
            jo_postdata["fb_dtsg"] = fb_dtsg;
            jo_postdata["jazoest"] = jazoest;
            jo_postdata["nh"] = nh;
            jo_postdata["submit%5BContinue%5D"] = "Continue";
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
            if (!hr.Html.Contains("<input type=\"hidden\" name=\"fb_dtsg\" value=\""))
            {
                jo_Result["ErrorMsg"] = $"注销其它设备时点击[Get Started]失败({hr.Html})";
                return jo_Result;
            }

            //<input type="hidden" autocomplete="off" name="nh" value="16d63c90d49028bcf672c90936f2488f362ae1f7" />
            nh = StringHelper.GetMidStr(hr.Html, "name=\"nh\" value=\"", "\"");
            //fb_dtsg
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //<input type="hidden" name="jazoest" value="25226" autocomplete="off" />
            jazoest = StringHelper.GetMidStr(hr.Html, "name=\"jazoest\" value=\"", "\"");
            //checkpoint_data
            checkpoint_data = System.Web.HttpUtility.HtmlDecode(StringHelper.GetMidStr(hr.Html, "name=\"checkpoint_data\" value=\"", "\"")).Replace("&#123;", "{").Replace("&#125;", "}");
            #endregion

            #region 点击Continue，获取可注销的列表
            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/checkpoint/flow";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            jo_postdata = new JObject();
            jo_postdata["checkpoint_created"] = "1";
            jo_postdata["checkpoint_data"] = checkpoint_data;
            jo_postdata["fb_dtsg"] = fb_dtsg;
            jo_postdata["jazoest"] = jazoest;
            jo_postdata["nh"] = nh;
            jo_postdata["submit%5BContinue%5D"] = "Continue";
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
            if (!hr.Html.Contains("<input type=\"hidden\" name=\"fb_dtsg\" value=\""))
            {
                jo_Result["ErrorMsg"] = $"注销其它设备时点击[Continue]失败({hr.Html})";
                return jo_Result;
            }

            //<input type="hidden" autocomplete="off" name="nh" value="16d63c90d49028bcf672c90936f2488f362ae1f7" />
            nh = StringHelper.GetMidStr(hr.Html, "name=\"nh\" value=\"", "\"");
            //fb_dtsg
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //<input type="hidden" name="jazoest" value="25226" autocomplete="off" />
            jazoest = StringHelper.GetMidStr(hr.Html, "name=\"jazoest\" value=\"", "\"");
            //checkpoint_data
            checkpoint_data = System.Web.HttpUtility.HtmlDecode(StringHelper.GetMidStr(hr.Html, "name=\"checkpoint_data\" value=\"", "\"")).Replace("&#123;", "{").Replace("&#125;", "}");

            //<input type="checkbox" name="selected[]" value="phomopugetzard7428&#064;rambler.ru" id="u_0_0_QJ" />
            //<input type="checkbox" name="selected[]" value="wagnerpradorodrigus&#064;gmail.com" id="u_0_1_2v" />
            selectedList = StringHelper.GetMidStrList(hr.Html, "<input type=\"checkbox\" name=\"selected[]\" value=\"", "\"").Select(s => s.Replace("&#064;", "@")).ToList();
            if (selectedList.Count == 0)
            {
                jo_Result["ErrorMsg"] = $"注销其它设备时点击[Continue]失败({hr.Html})";
                return jo_Result;
            }
            #endregion

            #region 选择账号请求注销
            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/checkpoint/flow";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            jo_postdata = new JObject();
            jo_postdata["checkpoint_created"] = "1";
            jo_postdata["checkpoint_data"] = checkpoint_data;
            jo_postdata["fb_dtsg"] = fb_dtsg;
            jo_postdata["jazoest"] = jazoest;
            jo_postdata["nh"] = nh;
            jo_postdata["submit%5BContinue%5D"] = "Continue";
            #endregion

            hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}")) + string.Join("&", selectedList.Select(s => $"selected%5B%5D={s}"));

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);
            #endregion

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = hr.Html;

            return jo_Result;
        }
        /// <summary>
        /// 删除登录会话
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_LogOutOfOtherSession(Account_FBOrIns account)
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
            hi.URL = $"https://accountscenter.facebook.com/password_and_security/login_activity";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";
            #endregion

            #region 选择FB账号
            account.Running_Log = $"删登录会话:选择Facebook账号";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterDeviceLoginActivitiesDialogQuery";
            doc_id = "6487574057995774";
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"interface\":\"FB_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            //jo_postdata["__aaid"] = "0";//特殊参数
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
                jo_Result["ErrorMsg"] = $"删登录会话:选择Facebook账号失败({hr.Html})";
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
                jo_Result["ErrorMsg"] = $"删登录会话:选择Facebook账号失败(获取当前设备ID失败)";
                return jo_Result;
            }
            activeId = jtFind["id"].ToString().Trim();
            #endregion

            #region 进行全选
            account.Running_Log = $"删登录会话:进行全选";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "FXAccountsCenterDeviceLoginLogoutDevicesDialogQuery";
            doc_id = "6132705960192441";
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"interface\":\"FB_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            //jo_postdata["__aaid"] = "0";//特殊参数
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
                jo_Result["ErrorMsg"] = $"删登录会话:选择Facebook账号失败({hr.Html})";
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
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useFXSettingsLogoutSessionMutation";
            doc_id = "24072290329086422";
            variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\"" +
                ",\"account_type\":\"FACEBOOK\",\"mutate_params\":{\"logout_all\":false,\"session_ids\":" + session_ids + "},\"fdid\":\"device_id_fetch_datr\"}}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            //jo_postdata["__aaid"] = "0";//特殊参数
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
                account.Running_Log = $"删登录会话:选择Facebook账号";
                hi = new HttpItem();
                hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "FXAccountsCenterDeviceLoginActivitiesDialogQuery";
                doc_id = "6487574057995774";
                variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"interface\":\"FB_WEB\"}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                //jo_postdata["__aaid"] = "0";//特殊参数
                jo_postdata["__a"] = string.Empty;
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = string.Empty;
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
        public JObject FB_TwoFactorRemoveTrustedDevice(Account_FBOrIns account)
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
            hi.URL = $"https://accountscenter.facebook.com/password_and_security/two_factor";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
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
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"interface\":\"FB_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
                        encryptPwd = this.FB_Encpass_Method(account.Facebook_Pwd);

                        hi = new HttpItem();
                        hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                        hi.UserAgent = account.UserAgent;
                        hi.Accept = $"*/*";
                        hi.Header.Add("Accept-Encoding", "gzip");
                        hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                        hi.Allowautoredirect = false;

                        hi.Header.Add("Sec-Fetch-Site", "same-origin");
                        hi.Method = "POST";
                        hi.ContentType = $"application/x-www-form-urlencoded";

                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        fb_api_req_friendly_name = "FXPasswordReauthenticationMutation";
                        doc_id = "5864546173675027";
                        variables = "{\"input\":{\"account_id\":" + account.LoginInfo.LoginData_Account_Id + ",\"account_type\":\"FACEBOOK\",\"password\":{\"sensitive_string_value\":\"" + encryptPwd + "\"},\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"client_mutation_id\":\"1\"}}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__aaid"] = "0";//特殊参数
                        jo_postdata["__a"] = string.Empty;
                        jo_postdata["__req"] = string.Empty;
                        jo_postdata["__hs"] = string.Empty;
                        jo_postdata["dpr"] = string.Empty;
                        jo_postdata["__ccg"] = __ccg;
                        jo_postdata["__rev"] = __rev;
                        jo_postdata["__s"] = string.Empty;
                        jo_postdata["__hsi"] = __hsi;
                        jo_postdata["__dyn"] = __dyn;
                        jo_postdata["__csr"] = __csr;
                        jo_postdata["__comet_req"] = string.Empty;
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
                        hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                        hi.UserAgent = account.UserAgent;
                        hi.Accept = $"*/*";
                        hi.Header.Add("Accept-Encoding", "gzip");
                        hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
                        variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"interface\":\"FB_WEB\"}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__a"] = string.Empty;
                        jo_postdata["__req"] = string.Empty;
                        jo_postdata["__hs"] = string.Empty;
                        jo_postdata["dpr"] = string.Empty;
                        jo_postdata["__ccg"] = __ccg;
                        jo_postdata["__rev"] = __rev;
                        jo_postdata["__s"] = string.Empty;
                        jo_postdata["__hsi"] = __hsi;
                        jo_postdata["__dyn"] = __dyn;
                        jo_postdata["__csr"] = __csr;
                        jo_postdata["__comet_req"] = string.Empty;
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
                hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "useFXSettingsTwoFactorRemoveTrustedDeviceMutation";
                doc_id = "6716390001764111";
                variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\"," +
                    "\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\"," +
                    "\"device_id\":\"" + device_id + "\",\"ig_is_web_device\":" + ig_is_web_device + ",\"fdid\":\"device_id_fetch_datr\"}}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                //jo_postdata["__aaid"] = "0";//特殊参数
                jo_postdata["__a"] = string.Empty;
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = string.Empty;
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
        /// 查询国家,注册日期
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_Query_Country_RegTime(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["GuoJia"] = "";
            jo_Result["ZhuCeRiQi"] = "";

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
            bool isSuccess_Country = false;
            bool isSuccess_RegTime = false;
            IEnumerable<JToken> jtsFind = null;
            JToken jtFind = null;
            string regTime = string.Empty;

            #region 先访问目标页面
            account.Running_Log = $"查国家:进行查询操作";

            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/your_information/?entry_point=accounts_center_other";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";
            #endregion

            while (true)
            {
                #region 查询国家
                //[{"text":"Your primary location is near: Sumbawanga, Rukwa"}]
                if (!hr.Html.Contains("\"text\":\"Your primary location is near:"))
                {
                    account.Running_Log = $"查国家:操作失败";
                    break;
                }
                country = StringHelper.GetMidStr(hr.Html, "\"text\":\"Your primary location is near:", "\"").Trim();
                if (country.Contains("\\u")) country = StringHelper.Usc2ConvertToAnsi(country);

                List<string> sList = Regex.Split(country, ",").Where(s => s.Trim().Length > 0).Select(s => s.Trim()).ToList();
                if (sList.Count == 0)
                {
                    account.Running_Log = $"查国家:操作失败";
                    break;
                }
                sList = sList.GetRange(sList.Count - 1, 1);

                List<string> rList = new List<string>();
                for (int i = 0; i < sList.Count; i++)
                {
                    jtsFind = Program.setting.Ja_CitysInfo.Where(jt => jt["Value_JsonStr"].ToString().Contains("\"Name\":\"" + sList[i] + "\""));

                    rList.AddRange(jtsFind.Select(jt => jt["Name"].ToString().Trim()));
                }
                rList = rList.Distinct().ToList();

                if (rList.Count == 1) country = rList[0].Trim();
                else if (rList.Count > 1) country = $"{JsonConvert.SerializeObject(rList)}({country})";
                else country = $"Unknown({country})";
                #endregion

                account.Running_Log = $"查国家:操作成功";
                jo_Result["GuoJia"] = country;
                isSuccess_Country = true;

                break;
            }

            while (true)
            {
                #region 查注册日期
                jo_Result["ErrorMsg"] = $"查注册日期:进行查询操作";

                hi = new HttpItem();
                hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "PrivacyAccessHubYourInformationSectionQuery";
                doc_id = "25404036609240614";
                variables = "{\"scale\":2}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__a"] = string.Empty;
                jo_postdata["__req"] = string.Empty;
                jo_postdata["__hs"] = string.Empty;
                jo_postdata["dpr"] = string.Empty;
                jo_postdata["__ccg"] = __ccg;
                jo_postdata["__rev"] = __rev;
                jo_postdata["__s"] = string.Empty;
                jo_postdata["__hsi"] = __hsi;
                jo_postdata["__dyn"] = __dyn;
                jo_postdata["__csr"] = __csr;
                jo_postdata["__comet_req"] = string.Empty;
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
                jr = null;
                try { jr = JObject.Parse(html); } catch { }
                if (jr == null || jr.SelectToken("data.section.tiles[1]['profile_based_links']") == null)
                {
                    account.Running_Log = $"查注册日期:操作失败({html})";
                    break;
                }
                jtsFind = jr.SelectToken("data.section.tiles[1]['profile_based_links']");
                jtFind = jtsFind.Where(jt => jt.SelectToken("label") != null && jt.SelectToken("label").ToString().Trim() == "Profile information").FirstOrDefault();
                if (jtFind == null || jtFind.SelectToken("['non_link_content'].metadata") == null)
                {
                    account.Running_Log = $"查注册日期:操作失败({html})";
                    break;
                }

                //Mar 10, 2023
                regTime = jtFind.SelectToken("['non_link_content'].metadata").ToString().Trim();
                regTime = this.ConvertDate(regTime, "MM dd, yyyy");
                #endregion

                account.Running_Log = $"查注册日期:操作成功";
                jo_Result["ZhuCeRiQi"] = regTime;
                isSuccess_RegTime = true;

                break;
            }

            jo_Result["Success"] = isSuccess_Country && isSuccess_RegTime;
            jo_Result["ErrorMsg"] = $"查国家,注册日期:操作{(isSuccess_Country && isSuccess_RegTime ? "成功" : "失败")}";
            return jo_Result;
        }
        /// <summary>
        /// 查询帖子数量
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_Query_Posts(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["TieZiCount"] = "";

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

            IEnumerable<JToken> jtsFind = null;
            string end_cursor = string.Empty;
            int pageIndex = 0;
            int tieZiCount = 0;
            int tieZiCount_Sum = 0;
            int tieZiCount_Target = 100;
            bool isEnd = false;
            int tryTimes = 0;
            int tryTimes_Max = 2;
            int trySpan = 300;
            bool isSuccess = false;
            string tkName = string.Empty;

            #region 先访问目标页面
            account.Running_Log = $"查帖子:打开目标页面";

            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/profile.php";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";

            if (string.IsNullOrEmpty(fb_dtsg))
            {
                jo_Result["ErrorMsg"] = $"查帖子:打开目标页面失败";
                return jo_Result;
            }
            #endregion

            #region 进行逐页查询帖子
            account.Running_Log = $"查帖子:进行逐页查询";

            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            end_cursor = string.Empty;
            isEnd = false;
            pageIndex = 0;
            while (!isEnd && tieZiCount_Sum < tieZiCount_Target)
            {
                pageIndex += 1;
                account.Running_Log = $"查帖子:查询 第 {pageIndex} 页";

                tryTimes = 0;
                tryTimes_Max = 2;
                trySpan = 300;
                isSuccess = false;
                while (!isSuccess && tryTimes < tryTimes_Max)
                {
                    tryTimes += 1;

                    #region 查帖子

                    #region 整理提交数据
                    fb_api_caller_class = "RelayModern";
                    if (string.IsNullOrEmpty(end_cursor))
                    {
                        fb_api_req_friendly_name = "ProfileCometManagePostsTimelineRootQuery";
                        doc_id = "7335719726532810";
                        variables = "{\"afterTime\":null,\"beforeTime\":null,\"gridMediaWidth\":230,\"includeGroupScheduledPosts\":false,\"includeScheduledPosts\":false" +
                        ",\"omitPinnedPost\":true,\"postedBy\":null,\"privacy\":null,\"privacySelectorRenderLocation\":\"COMET_STREAM\",\"scale\":2,\"taggedInOnly\":null" +
                        ",\"renderLocation\":\"timeline\",\"userID\":\"" + account.LoginInfo.LoginData_Account_Id + "\"}";

                        tkName = "data.user['timeline_manage_feed_units'].edges";
                    }
                    else
                    {
                        fb_api_req_friendly_name = "CometManagePostsFeedRefetchQuery";
                        doc_id = "7753245201411699";
                        variables = "{\"afterTime\":null,\"beforeTime\":null,\"count\":6,\"cursor\":\"" + end_cursor + "\",\"gridMediaWidth\":230,\"includeGroupScheduledPosts\":false" +
                            ",\"includeScheduledPosts\":false,\"omitPinnedPost\":true,\"postedBy\":null,\"privacy\":null,\"privacySelectorRenderLocation\":\"COMET_STREAM\"" +
                            ",\"renderLocation\":\"timeline\",\"scale\":2,\"taggedInOnly\":null,\"id\":\"" + account.LoginInfo.LoginData_Account_Id + "\"}";

                        tkName = "data.node['timeline_manage_feed_units'].edges";
                    }

                    jo_postdata = new JObject();
                    jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                    jo_postdata["__aaid"] = "0";
                    jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
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
                    jo_postdata["__comet_req"] = "15";
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

                    html = StringHelper.GetMidStr("{begin}" + hr.Html, "{begin}", "{\"label\":\"ProfileComet");
                    jr = null;
                    try { jr = JObject.Parse(html); } catch { }

                    if (jr == null || jr.SelectToken(tkName) == null)
                    {
                        if (tryTimes < tryTimes_Max) { Thread.Sleep(trySpan); Application.DoEvents(); continue; }
                        else
                        {
                            jo_Result["ErrorMsg"] = $"查帖子:第 {pageIndex} 页 查询失败({hr.Html})";
                            return jo_Result;
                        }
                    }

                    jtsFind = jr.SelectToken(tkName);
                    tieZiCount = jtsFind.Count();
                    tieZiCount_Sum += tieZiCount;

                    end_cursor = StringHelper.GetMidStr(hr.Html, "\"end_cursor\":\"", "\"");
                    isEnd = tieZiCount_Sum >= tieZiCount_Target || string.IsNullOrEmpty(end_cursor);
                    isSuccess = true;
                    #endregion

                    if (!isSuccess && tryTimes < tryTimes_Max) { Thread.Sleep(trySpan); Application.DoEvents(); }
                }
            }
            #endregion

            jo_Result["TieZiCount"] = tieZiCount_Sum < tieZiCount_Target ? $"{tieZiCount_Sum}" : $"{tieZiCount_Target}+";

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = $"查帖子:操作成功";
            return jo_Result;
        }
        /// <summary>
        /// 查询性别、出生日期、好友数量
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_Query_Birthday_Gender_Friends(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["XingBie"] = "";
            jo_Result["ShengRi"] = "";
            jo_Result["HaoYouCount"] = "";

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

            string sectionToken_Birth = string.Empty;
            string sectionToken_Friends = string.Empty;
            string collectionToken_Birth = string.Empty;
            //string collectionToken_Friends = string.Empty;
            JToken jtFind = null;
            IEnumerable<JToken> jtsFind = null;
            string birthDate = string.Empty;
            string birthYear = string.Empty;

            bool isSuccess_Birth = false;
            bool isSuccess_Friends = false;

            #region 先访问目标页面
            account.Running_Log = $"查性别,生日,好友:打开主页";
            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/profile.php";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";

            //{"tab_key":"about_contact_and_basic_info","id":"YXBwX2NvbGxlY3Rpb246MTAwMDE0NDAwMDc0MTg4OjIzMjcxNTgyMjc6MjA0"},
            collectionToken_Birth = StringHelper.GetMidStr(hr.Html, "\"tab_key\":\"about_contact_and_basic_info\",\"id\":\"", "\"");
            if (string.IsNullOrEmpty(collectionToken_Birth))
            {
                jo_Result["ErrorMsg"] = $"查性别,生日,好友:打开主页失败";
                return jo_Result;
            }
            #endregion

            #region 获取sectionToken
            account.Running_Log = $"查性别,生日,好友:获取sectionToken";
            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/api/graphql/";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "ProfileCometHeaderQuery";
            doc_id = "6980042348762400";
            variables = "{\"scale\":2,\"selectedID\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"selectedSpaceType\":\"community\"" +
                ",\"shouldUseFXIMProfilePicEditor\":false,\"userID\":\"" + account.LoginInfo.LoginData_Account_Id + "\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__aaid"] = "0";
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
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
            jo_postdata["__comet_req"] = "15";
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

            html = StringHelper.GetMidStr("{begin}" + hr.Html, "{begin}", "{\"label\":\"ProfileComet");
            jr = null;
            try { jr = JObject.Parse(html); } catch { }
            if (jr == null || jr.SelectToken("data.user['profile_header_renderer'].user['profile_tabs']['profile_user']['timeline_nav_app_sections'].edges") == null)
            {
                jo_Result["ErrorMsg"] = $"查性别,生日,好友:获取sectionToken失败({html})";
                return jo_Result;
            }
            jtsFind = jr.SelectToken("data.user['profile_header_renderer'].user['profile_tabs']['profile_user']['timeline_nav_app_sections'].edges");

            jtFind = jtsFind.Where(jt => jt.SelectToken("node.name") != null && jt.SelectToken("node.name").ToString().Trim() == "About").FirstOrDefault();
            if (jtFind == null || jtFind.SelectToken("node.id") == null || string.IsNullOrEmpty(jtFind.SelectToken("node.id").ToString().Trim()))
            {
                jo_Result["ErrorMsg"] = $"查性别,生日,好友:获取sectionToken失败({html})";
                return jo_Result;
            }
            sectionToken_Birth = jtFind.SelectToken("node.id").ToString().Trim();

            jtFind = jtsFind.Where(jt => jt.SelectToken("node.name") != null && jt.SelectToken("node.name").ToString().Trim() == "Friends").FirstOrDefault();
            if (jtFind == null || jtFind.SelectToken("node.id") == null || string.IsNullOrEmpty(jtFind.SelectToken("node.id").ToString().Trim()))
            {
                jo_Result["ErrorMsg"] = $"查性别,生日,好友:获取sectionToken失败({html})";
                return jo_Result;
            }
            sectionToken_Friends = jtFind.SelectToken("node.id").ToString().Trim();
            #endregion

            while (true)
            {
                #region 查性别,生日:进行查询操作
                account.Running_Log = $"查性别,生日:进行查询操作";
                hi = new HttpItem();
                hi.URL = $"https://www.facebook.com/api/graphql/";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                //https://www.facebook.com/cris.tabin.9/
                //这个页面能查询到 collectionToken 的值：YXBwX2NvbGxlY3Rpb246MTAwMDE0NDAwMDc0MTg4OjIzMjcxNTgyMjc6MjA0

                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "ProfileCometAboutAppSectionQuery";
                doc_id = "7420649058064306";
                variables = "{\"appSectionFeedKey\":\"\",\"collectionToken\":\"" + collectionToken_Birth + "\",\"pageID\":\"\",\"rawSectionToken\":\"\",\"scale\":2" +
                    ",\"sectionToken\":\"" + sectionToken_Birth + "\",\"showReactions\":true,\"userID\":\"" + account.LoginInfo.LoginData_Account_Id + "\"" +
                    ",\"__relay_internal__pv__CometUFIReactionsEnableShortNamerelayprovider\":false}";


                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
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
                jo_postdata["__comet_req"] = "15";
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

                html = StringHelper.GetMidStr("{begin}" + hr.Html, "{begin}", "{\"label\":\"ProfileComet");
                jr = null;
                try { jr = JObject.Parse(html); } catch { }
                if (jr == null || jr.SelectToken("data.user['about_app_sections'].nodes[0].activeCollections.nodes[0]['style_renderer']['profile_field_sections'][2]['profile_fields'].nodes") == null)
                {
                    account.Running_Log = $"查性别,生日:操作失败({html})";
                    break;
                }

                jtFind = jr.SelectToken("data.user['about_app_sections'].nodes[0].activeCollections.nodes[0]['style_renderer']['profile_field_sections'][2]['profile_fields'].nodes").Where(jt => jt["field_type"] != null && jt["field_type"].ToString().Trim() == "gender").FirstOrDefault();
                if (jtFind == null && jtFind.SelectToken("title.text") != null && string.IsNullOrEmpty(jtFind.SelectToken("title.text").ToString().Trim()))
                {
                    account.Running_Log = $"查性别,生日:操作失败(无gender字段)";
                    break;
                }
                //记录性别
                jo_Result["XingBie"] = jtFind.SelectToken("title.text").ToString().ToLower() == "male" ? "男" : "女";

                jtsFind = jr.SelectToken("data.user['about_app_sections'].nodes[0].activeCollections.nodes[0]['style_renderer']['profile_field_sections'][2]['profile_fields'].nodes").Where(jt => jt["field_type"] != null && jt["field_type"].ToString().Trim() == "birthday");
                if (jtsFind.Count() != 2)
                {
                    account.Running_Log = $"查性别,生日:操作失败(无birthday字段)";
                    break;
                }
                //[],"text":"Birth date"}}]}]
                jtFind = jtsFind.Where(jt => jt["list_item_groups"] != null && JsonConvert.SerializeObject(jt["list_item_groups"]).Contains("\"text\":\"Birth date\"")).FirstOrDefault();
                if (jtFind == null || jtFind.SelectToken("title.text") == null || string.IsNullOrEmpty(jtFind.SelectToken("title.text").ToString().Trim()))
                {
                    account.Running_Log = $"查性别,生日:操作失败(无birthday字段)";
                    break;
                }
                birthDate = jtFind.SelectToken("title.text").ToString().Trim();
                //[],"text":"Birth year"}}]}]
                jtFind = jtsFind.Where(jt => jt["list_item_groups"] != null && JsonConvert.SerializeObject(jt["list_item_groups"]).Contains("\"text\":\"Birth year\"")).FirstOrDefault();
                if (jtFind == null || jtFind.SelectToken("title.text") == null || string.IsNullOrEmpty(jtFind.SelectToken("title.text").ToString().Trim()))
                {
                    account.Running_Log = $"查性别,生日:操作失败(无birthday字段)";
                    break;
                }
                birthYear = jtFind.SelectToken("title.text").ToString().Trim();

                birthDate = this.ConvertDate($"{birthYear} {birthDate}", "yyyy MM dd");
                if (birthDate == "1900-01-01")
                {
                    account.Running_Log = $"查性别,生日:操作失败(无birthday字段)";
                    break;
                }

                jo_Result["ShengRi"] = $"{birthDate}";
                #endregion

                isSuccess_Birth = true;
                account.Running_Log = $"查性别,生日:操作成功";

                break;
            }

            while (true)
            {
                #region 查好友:进行查询操作
                account.Running_Log = $"查好友:进行查询操作";
                hi = new HttpItem();
                hi.URL = $"https://www.facebook.com/api/graphql/";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "ProfileCometTopAppSectionQuery";
                doc_id = "7494154627378833";
                variables = "{\"collectionToken\":null,\"feedbackSource\":65,\"feedLocation\":\"COMET_MEDIA_VIEWER\",\"scale\":2,\"sectionToken\":\"" + sectionToken_Friends + "\"" +
                    ",\"userID\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"__relay_internal__pv__CometUFIReactionsEnableShortNamerelayprovider\":false}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
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
                jo_postdata["__comet_req"] = "15";
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
                jr = null;
                try { jr = JObject.Parse(html); } catch { }
                if (jr == null || jr.SelectToken("data.node['all_collections'].nodes[0]['style_renderer'].collection") == null)
                {
                    account.Running_Log = $"查好友:操作失败({html})";
                    break;
                }
                jtFind = jr.SelectToken("data.node['all_collections'].nodes[0]['style_renderer'].collection");
                if (jtFind.SelectToken("name") == null || jtFind.SelectToken("items.count") == null || jtFind.SelectToken("name").ToString().Trim() != "All friends")
                {
                    account.Running_Log = $"查好友:操作失败({html})";
                    break;
                }

                jo_Result["HaoYouCount"] = jtFind.SelectToken("items.count").ToString().Trim();
                #endregion

                isSuccess_Friends = true;

                break;
            }

            jo_Result["Success"] = isSuccess_Birth && isSuccess_Friends;
            jo_Result["ErrorMsg"] = $"查性别,生日,好友:操作{(isSuccess_Birth && isSuccess_Friends ? "成功" : "失败")}";
            return jo_Result;
        }
        /// <summary>
        /// 查辅助词
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_Query_RecoveryCodes(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["FuZhuCi"] = "";

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
            string randomUUID = string.Empty;
            IEnumerable<JToken> jtsFind = null;
            string recoveryCodes = string.Empty;


            #region 先访问目标页面
            account.Running_Log = $"查辅助词:进入目标页面(two_factor)";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/password_and_security/two_factor";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";
            #endregion

            #region 选择Facebook账号
            account.Running_Log = $"查辅助词:选择Facebook账号";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"interface\":\"FB_WEB\"}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
                //判断是否需要密码验证
                if (jr.SelectToken("errors[0].description") != null)
                {
                    if (jr.SelectToken("errors[0].description").ToString().Contains("\"challenge_type\":\"password\""))
                    {
                        #region 进行密码验证
                        account.Running_Log = $"查辅助词:进行密码验证";

                        //暂时先固定密码
                        //niubi.fb
                        //encryptPwd = "#PWD_BROWSER:5:1719252308:AZ5QADDtiGcrGB/Cv2WKo6h5+BznJr3xwRrAw/Wd2l08B7QqNDXbpMCfA8K/xam6uPP54i3BKiyI51peOJDL1jQZxv/MF1bZywHXUmA8Ydh/zGS5xYh/aBH1KMKdrt+Ea3AcGgXwxcHsljDm";
                        encryptPwd = this.FB_Encpass_Method(account.Facebook_Pwd);

                        hi = new HttpItem();
                        hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                        hi.UserAgent = account.UserAgent;
                        hi.Accept = $"*/*";
                        hi.Header.Add("Accept-Encoding", "gzip");
                        hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                        hi.Allowautoredirect = false;

                        hi.Header.Add("Sec-Fetch-Site", "same-origin");
                        hi.Method = "POST";
                        hi.ContentType = $"application/x-www-form-urlencoded";

                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        fb_api_req_friendly_name = "FXPasswordReauthenticationMutation";
                        doc_id = "5864546173675027";
                        variables = "{\"input\":{\"account_id\":" + account.LoginInfo.LoginData_Account_Id + ",\"account_type\":\"FACEBOOK\",\"password\":{\"sensitive_string_value\":\"" + encryptPwd + "\"},\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"client_mutation_id\":\"1\"}}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__aaid"] = "0";//特殊参数
                        jo_postdata["__a"] = string.Empty;
                        jo_postdata["__req"] = string.Empty;
                        jo_postdata["__hs"] = string.Empty;
                        jo_postdata["dpr"] = string.Empty;
                        jo_postdata["__ccg"] = __ccg;
                        jo_postdata["__rev"] = __rev;
                        jo_postdata["__s"] = string.Empty;
                        jo_postdata["__hsi"] = __hsi;
                        jo_postdata["__dyn"] = __dyn;
                        jo_postdata["__csr"] = __csr;
                        jo_postdata["__comet_req"] = string.Empty;
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
                            jo_Result["ErrorMsg"] = $"查辅助词:进行密码验证失败({hr.Html})";
                            return jo_Result;
                        }

                        #region 再次选择Facebook账号
                        account.Running_Log = $"查辅助词:再次选择Facebook账号";
                        hi = new HttpItem();
                        hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                        hi.UserAgent = account.UserAgent;
                        hi.Accept = $"*/*";
                        hi.Header.Add("Accept-Encoding", "gzip");
                        hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
                        variables = "{\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"interface\":\"FB_WEB\"}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__a"] = string.Empty;
                        jo_postdata["__req"] = string.Empty;
                        jo_postdata["__hs"] = string.Empty;
                        jo_postdata["dpr"] = string.Empty;
                        jo_postdata["__ccg"] = __ccg;
                        jo_postdata["__rev"] = __rev;
                        jo_postdata["__s"] = string.Empty;
                        jo_postdata["__hsi"] = __hsi;
                        jo_postdata["__dyn"] = __dyn;
                        jo_postdata["__csr"] = __csr;
                        jo_postdata["__comet_req"] = string.Empty;
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
                            jo_Result["ErrorMsg"] = $"删信任设备:选择Facebook账号,查看设备列表失败({hr.Html})";
                            return jo_Result;
                        }
                        #endregion
                    }
                    else
                    {
                        jo_Result["ErrorMsg"] = $"查辅助词:选择Facebook账号({hr.Html})";
                        return jo_Result;
                    }
                }
                else
                {
                    jo_Result["ErrorMsg"] = $"查辅助词:选择Facebook账号({hr.Html})";
                    return jo_Result;
                }
            }
            #endregion

            #region 直接获取一个新的辅助词
            randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

            account.Running_Log = $"查辅助词:进行查询操作";
            hi = new HttpItem();
            hi.URL = $"https://accountscenter.facebook.com/api/graphql";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "useFXSettingsTwoFactorRegenerateRecoveryCodesMutation";
            doc_id = "24010978991879298";
            variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"fdid\":\"device_id_fetch_datr\"}}";

            jo_postdata = new JObject();
            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__a"] = string.Empty;
            jo_postdata["__req"] = string.Empty;
            jo_postdata["__hs"] = string.Empty;
            jo_postdata["dpr"] = string.Empty;
            jo_postdata["__ccg"] = __ccg;
            jo_postdata["__rev"] = __rev;
            jo_postdata["__s"] = string.Empty;
            jo_postdata["__hsi"] = __hsi;
            jo_postdata["__dyn"] = __dyn;
            jo_postdata["__csr"] = __csr;
            jo_postdata["__comet_req"] = string.Empty;
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
            if (jr == null || jr.SelectToken("data['xfb_two_factor_regenerate_recovery_codes']['recovery_codes']") == null)
            {
                //判断是否需要密码验证
                if (jr.SelectToken("errors[0].description") != null)
                {
                    if (jr.SelectToken("errors[0].description").ToString().Contains("\"challenge_type\":\"password\""))
                    {
                        #region 进行密码验证
                        account.Running_Log = $"查辅助词:进行密码验证";

                        //暂时先固定密码
                        //niubi.fb
                        //encryptPwd = "#PWD_BROWSER:5:1719252308:AZ5QADDtiGcrGB/Cv2WKo6h5+BznJr3xwRrAw/Wd2l08B7QqNDXbpMCfA8K/xam6uPP54i3BKiyI51peOJDL1jQZxv/MF1bZywHXUmA8Ydh/zGS5xYh/aBH1KMKdrt+Ea3AcGgXwxcHsljDm";
                        encryptPwd = this.FB_Encpass_Method(account.Facebook_Pwd);

                        hi = new HttpItem();
                        hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                        hi.UserAgent = account.UserAgent;
                        hi.Accept = $"*/*";
                        hi.Header.Add("Accept-Encoding", "gzip");
                        hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                        hi.Allowautoredirect = false;

                        hi.Header.Add("Sec-Fetch-Site", "same-origin");
                        hi.Method = "POST";
                        hi.ContentType = $"application/x-www-form-urlencoded";

                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        fb_api_req_friendly_name = "FXPasswordReauthenticationMutation";
                        doc_id = "5864546173675027";
                        variables = "{\"input\":{\"account_id\":" + account.LoginInfo.LoginData_Account_Id + ",\"account_type\":\"FACEBOOK\",\"password\":{\"sensitive_string_value\":\"" + encryptPwd + "\"},\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"client_mutation_id\":\"1\"}}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__aaid"] = "0";//特殊参数
                        jo_postdata["__a"] = string.Empty;
                        jo_postdata["__req"] = string.Empty;
                        jo_postdata["__hs"] = string.Empty;
                        jo_postdata["dpr"] = string.Empty;
                        jo_postdata["__ccg"] = __ccg;
                        jo_postdata["__rev"] = __rev;
                        jo_postdata["__s"] = string.Empty;
                        jo_postdata["__hsi"] = __hsi;
                        jo_postdata["__dyn"] = __dyn;
                        jo_postdata["__csr"] = __csr;
                        jo_postdata["__comet_req"] = string.Empty;
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
                            jo_Result["ErrorMsg"] = $"查辅助词:进行密码验证失败({hr.Html})";
                            return jo_Result;
                        }

                        #region 再次获取辅助词
                        randomUUID = this.scriptEngine.CallGlobalFunction("generateUUID").ToString();

                        account.Running_Log = $"查辅助词:进行查询操作";
                        hi = new HttpItem();
                        hi.URL = $"https://accountscenter.facebook.com/api/graphql";
                        hi.UserAgent = account.UserAgent;
                        hi.Accept = $"*/*";
                        hi.Header.Add("Accept-Encoding", "gzip");
                        hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                        hi.Allowautoredirect = false;

                        hi.Header.Add("Sec-Fetch-Site", "same-origin");
                        hi.Method = "POST";
                        hi.ContentType = $"application/x-www-form-urlencoded";

                        #region 整理提交数据
                        fb_api_caller_class = "RelayModern";
                        fb_api_req_friendly_name = "useFXSettingsTwoFactorRegenerateRecoveryCodesMutation";
                        doc_id = "24010978991879298";
                        variables = "{\"input\":{\"client_mutation_id\":\"" + randomUUID + "\",\"actor_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_id\":\"" + account.LoginInfo.LoginData_Account_Id + "\",\"account_type\":\"FACEBOOK\",\"fdid\":\"device_id_fetch_datr\"}}";

                        jo_postdata = new JObject();
                        jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
                        jo_postdata["__a"] = string.Empty;
                        jo_postdata["__req"] = string.Empty;
                        jo_postdata["__hs"] = string.Empty;
                        jo_postdata["dpr"] = string.Empty;
                        jo_postdata["__ccg"] = __ccg;
                        jo_postdata["__rev"] = __rev;
                        jo_postdata["__s"] = string.Empty;
                        jo_postdata["__hsi"] = __hsi;
                        jo_postdata["__dyn"] = __dyn;
                        jo_postdata["__csr"] = __csr;
                        jo_postdata["__comet_req"] = string.Empty;
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
                        if (jr == null || jr.SelectToken("data['xfb_two_factor_regenerate_recovery_codes']['recovery_codes']") == null)
                        {
                            jo_Result["ErrorMsg"] = $"查辅助词:操作失败({hr.Html})";
                            return jo_Result;
                        }
                        #endregion
                    }
                    else
                    {
                        jo_Result["ErrorMsg"] = $"查辅助词:操作失败({hr.Html})";
                        return jo_Result;
                    }
                }
                else
                {
                    jo_Result["ErrorMsg"] = $"查辅助词:操作失败({hr.Html})";
                    return jo_Result;
                }
            }
            #endregion

            jtsFind = jr.SelectToken("data['xfb_two_factor_regenerate_recovery_codes']['recovery_codes']");
            recoveryCodes = $"[{string.Join(",", jtsFind.Select(jt => jt.ToString().Trim().Replace(" ", "-")))}]";

            jo_Result["FuZhuCi"] = recoveryCodes;
            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = $"查辅助词:操作成功";

            return jo_Result;
        }
        /// <summary>
        /// 查商城,专页
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_Query_AdAccount_Pages(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["ShangCheng"] = "";
            jo_Result["ZhuanYe"] = "";
            //jo_Result["AdQuanXian"] = "";

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

            string ad_account_id = string.Empty;
            int zhuanYeCount = 0;
            //int ad_account_status = -1;
            bool isSuccess_ZhuanYe = false;
            bool isSuccess_QuanXian = false;

            #region 先访问目标页面
            account.Running_Log = $"查商城,专页:打开目标页面";

            hi = new HttpItem();
            hi.URL = $"https://www.facebook.com/business-support-home/{account.LoginInfo.LoginData_Account_Id}/?source=link";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
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
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";

            if (string.IsNullOrEmpty(fb_dtsg))
            {
                jo_Result["ErrorMsg"] = $"查商城,专页:打开目标页面失败";
                return jo_Result;
            }
            #endregion

            #region 查商城
            ad_account_id = StringHelper.GetMidStr(hr.Html, "\"ad_account_id\":\"", "\"");
            jo_Result["ShangCheng"] = $"{(string.IsNullOrEmpty(ad_account_id) ? "无" : "有")}";
            #endregion

            while (true)
            {
                #region 查专页
                jo_Result["ErrorMsg"] = $"查专页:进行查询操作";

                hi = new HttpItem();
                hi.URL = $"https://www.facebook.com/api/graphql/";
                hi.UserAgent = account.UserAgent;
                hi.Accept = $"*/*";
                hi.Header.Add("Accept-Encoding", "gzip");
                hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
                hi.Allowautoredirect = false;

                hi.Header.Add("Sec-Fetch-Site", "same-origin");
                hi.Method = "POST";
                hi.ContentType = $"application/x-www-form-urlencoded";

                #region 整理提交数据
                fb_api_caller_class = "RelayModern";
                fb_api_req_friendly_name = "AccountQualityUserPagesWrapper_UserPageQuery";
                doc_id = "5196344227155252";
                variables = "{\"assetOwnerId\":\"" + account.LoginInfo.LoginData_Account_Id + "\"}";

                jo_postdata = new JObject();
                jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
                jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
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
                jo_postdata["__comet_req"] = "15";
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
                jr = null;
                try { jr = JObject.Parse(html); } catch { }
                if (jr == null || jr.SelectToken("data.userData") == null)
                {
                    account.Running_Log = $"查专页:操作失败({html})";
                    break;
                }

                var jts = jr.SelectToken("data.userData['pages_can_administer']");
                zhuanYeCount = jts.Count();
                jo_Result["ZhuanYe"] = zhuanYeCount.ToString();
                isSuccess_ZhuanYe = true;
                #endregion

                break;
            }

            #region 旧方法
            //while (true)
            //{
            //    #region 查权限
            //    jo_Result["ErrorMsg"] = $"查权限:进行查询操作";

            //    hi = new HttpItem();
            //    hi.URL = $"https://www.facebook.com/api/graphql/";
            //    hi.UserAgent = account.UserAgent;
            //    hi.Accept = $"*/*";
            //    hi.Header.Add("Accept-Encoding", "gzip");
            //    hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            //    hi.Allowautoredirect = false;

            //    hi.Header.Add("Sec-Fetch-Site", "same-origin");
            //    hi.Method = "POST";
            //    hi.ContentType = $"application/x-www-form-urlencoded";

            //    #region 整理提交数据
            //    fb_api_caller_class = "RelayModern";
            //    fb_api_req_friendly_name = "AccountQualityPersonalAdAccountsWrapper_PersonalAdAccountsQuery";
            //    doc_id = "6141713989255682";
            //    variables = "{\"count\":20,\"cursor\":null,\"startTime\":null}";

            //    jo_postdata = new JObject();
            //    jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            //    jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
            //    jo_postdata["__a"] = "1";
            //    jo_postdata["__req"] = string.Empty;
            //    jo_postdata["__hs"] = string.Empty;
            //    jo_postdata["dpr"] = string.Empty;
            //    jo_postdata["__ccg"] = __ccg;
            //    jo_postdata["__rev"] = __rev;
            //    jo_postdata["__s"] = string.Empty;
            //    jo_postdata["__hsi"] = __hsi;
            //    jo_postdata["__dyn"] = __dyn;
            //    jo_postdata["__csr"] = __csr;
            //    jo_postdata["__comet_req"] = "15";
            //    jo_postdata["fb_dtsg"] = StringHelper.UrlEncode(fb_dtsg);
            //    jo_postdata["jazoest"] = string.Empty;
            //    jo_postdata["lsd"] = lsd;
            //    jo_postdata["__spin_r"] = __rev;
            //    jo_postdata["__spin_b"] = string.Empty;
            //    jo_postdata["__spin_t"] = string.Empty;
            //    jo_postdata["fb_api_caller_class"] = fb_api_caller_class;
            //    jo_postdata["fb_api_req_friendly_name"] = fb_api_req_friendly_name;
            //    jo_postdata["variables"] = StringHelper.UrlEncode(variables);
            //    jo_postdata["server_timestamps"] = server_timestamps;
            //    jo_postdata["doc_id"] = doc_id;
            //    #endregion

            //    hi.Postdata = string.Join("&", jo_postdata.Root.Select(jt => $"{jt.Path}={jo_postdata[jt.Path].ToString().Trim()}"));

            //    //Cookie
            //    hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //    //代理
            //    if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            //    hr = hh.GetHtml(hi);

            //    //合并CK
            //    if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            //    html = hr.Html;
            //    jr = null;
            //    try { jr = JObject.Parse(html); } catch { }
            //    if (jr == null || jr.SelectToken("data.assetOwnerData['ad_accounts'].edges") == null)
            //    {
            //        account.Running_Log = $"查权限:操作失败({html})";
            //        break;
            //    }

            //    var jts = jr.SelectToken("data.assetOwnerData['ad_accounts'].edges");
            //    var jtFind = jts.Where(jt => jt.SelectToken("node['payment_account'].id") != null && jt.SelectToken("node['payment_account'].id").ToString().Trim() == ad_account_id).FirstOrDefault();
            //    if (jtFind == null || jtFind.SelectToken("node['advertising_restriction_info'].status") == null || string.IsNullOrEmpty(jtFind.SelectToken("node['advertising_restriction_info'].status").ToString().Trim()))
            //    {
            //        account.Running_Log = $"查权限:操作失败({html})";
            //        break;
            //    }

            //    ad_account_status = jtFind.SelectToken("node['advertising_restriction_info'].status").ToString().Trim() == "VANILLA_RESTRICTED" ? 0 : 1;
            //    if (ad_account_status == 0) jo_Result["AdQuanXian"] = "无";
            //    else if (ad_account_status == 1) jo_Result["AdQuanXian"] = "有";
            //    isSuccess_QuanXian = true;
            //    #endregion

            //    break;
            //}
            #endregion

            jo_Result["Success"] = isSuccess_ZhuanYe && isSuccess_QuanXian;
            jo_Result["ErrorMsg"] = $"查商城,专页:操作{(isSuccess_ZhuanYe && isSuccess_QuanXian ? "成功" : "失败")}";
            return jo_Result;
        }
                /// <summary>
        /// 查权限,账单,余额
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public JObject FB_Query_AdStatus_ZhangDan_YuE(Account_FBOrIns account)
        {
            JObject jo_Result = new JObject();
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = string.Empty;
            jo_Result["AdQuanXian"] = "";
            jo_Result["ZhangDan"] = "";
            jo_Result["YuE"] = "";

            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;

            JObject jo_postdata = null;
            JObject jr = null;
            IEnumerable<JToken> jtsFind = null;
            JToken jtFind = null;
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

            string assetId = string.Empty;
            string jtValue = string.Empty;
            string frontStr = string.Empty;

            #region 先访问目标页面
            account.Running_Log = $"查权限,账单,余额:进入目标页面";

            hi = new HttpItem();
            hi.URL = $"https://business.facebook.com/billing_hub/payment_settings?asset_id=";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = true;

            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = account.LoginInfo.LoginInfo_CookieStr;

            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //合并CK
            if (hr.Cookie != null) account.LoginInfo.CookieCollection = StringHelper.UpdateCookies(account.LoginInfo.CookieCollection, hr.Cookie);

            if (string.IsNullOrEmpty(hr.ResponseUri) || !hr.ResponseUri.Contains("https://business.facebook.com/billing_hub/payment_settings?asset_id="))
            {
                jo_Result["ErrorMsg"] = $"查权限,账单,余额:进入目标页面失败(https://business.facebook.com/billing_hub/payment_settings?asset_id=........)";
                return jo_Result;
            }
            #endregion

            #region 获取API所需要的参数
            //{"connectionClass":"EXCELLENT"}
            __ccg = StringHelper.GetMidStr(hr.Html, "\"connectionClass\":\"", "\"");
            //"{"server_revision":1014268370,
            __rev = StringHelper.GetMidStr(hr.Html, "\"server_revision\":", ",");
            //"cavalry_get_lid":"7381512262390677322"
            __hsi = StringHelper.GetMidStr(hr.Html, "\"brsid\":\"", "\"");
            __dyn = "7xeUmwlEnwn8K2Wmh0cm5U4e0yoW3q32360CEbo19oe8hw2nVE4W0om0MU2awpUO0n24o5-0Bo7O2l0Fwqo31w9O1lwlE-U2zxe2GewbS362W2K0zK1swa-7U1bobodEGdwtU2ewbS1LwTwNwLweq1Iwqo5u1Jwbe7E5y1rw";
            __csr = "gjhqiFOlkAOlnFtrsSGAGGXWALTKLqPtliP9mniA8iVEWDpTj-QJebGV49CyISylgSUwAHrzbAminhaluF8yaGFEyHhWm_y4m9KGxFyJppUox2LqgC4FoB3EtzpEd8CcUvCxqpbKKidKWzotBw04S3wrUGi0dZK0na4GG4qw63ws819_WG1kwg80apQ0kayQ1uCAJ2oK0cxwRwtojP3CitycUme0TUohFU99BcKh11wOxta9xjByoyqdAxmXQyK6HGEmwKx25HwFwv8y9whk3h0DzA0KQ";
            //["DTSGInitialData",[],{"token":"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862"},258]
            fb_dtsg = StringHelper.GetMidStr(hr.Html, "\"DTSGInitialData\",[],{\"token\":\"", "\"");
            //string fb_dtsg = $"NAcOBvBQbCQRgOafZCSSy5bWFGKdPqgTm_vouITmaSXznxR5rVQcGsw:45:1718184862";
            //["LSD",[],{"token":"scUVeszn2EjMMafv7cpKy_"},323]
            lsd = StringHelper.GetMidStr(hr.Html, "\"LSD\",[],{\"token\":\"", "\"");
            server_timestamps = "true";

            assetId = StringHelper.GetMidStr(hr.ResponseUri + "&", "asset_id=", "&");
            if (string.IsNullOrEmpty(fb_dtsg) || string.IsNullOrEmpty(assetId))
            {
                jo_Result["ErrorMsg"] = $"查权限,账单,余额:进入目标页面失败(获取API参数失败)";
                return jo_Result;
            }
            #endregion

            #region 查权限,账单,余额:进行查询操作
            account.Running_Log = $"查权限,账单,余额:进行查询操作";
            hi = new HttpItem();
            hi.URL = $"https://business.facebook.com/api/graphql/?_callFlowletID=1&_triggerFlowletID=2";
            hi.UserAgent = account.UserAgent;
            hi.Accept = $"*/*";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Allowautoredirect = false;

            hi.Header.Add("Sec-Fetch-Site", "same-origin");
            hi.Method = "POST";
            hi.ContentType = $"application/x-www-form-urlencoded";

            #region 整理提交数据
            fb_api_caller_class = "RelayModern";
            fb_api_req_friendly_name = "BillingHubPaymentSettingsViewQuery";
            doc_id = "8513262242018309";
            variables = "{\"assetID\":\"" + assetId + "\"}";

            jo_postdata = new JObject();
            jo_postdata["__aaid"] = assetId;
            jo_postdata["__jssesw"] = "1";

            jo_postdata["av"] = account.LoginInfo.LoginData_Account_Id;
            jo_postdata["__user"] = account.LoginInfo.LoginData_Account_Id;
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
            //jo_postdata["__comet_req"] = "15";
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

            html = StringHelper.GetMidStr("{begin}" + hr.Html + "\r\n", "{begin}", "\r\n");
            jr = null;
            try { jr = JObject.Parse(html); } catch { }
            if (jr == null || jr.SelectToken("data['billable_account_by_asset_id']['account_status']") == null || !hr.Html.Contains("{\"label\":\"useBillingHubPaymentSettingsCards_billableAccount$defer$BillingHubPaymentSettingsPaymentActivityCard_billableAccount\""))
            {
                jo_Result["ErrorMsg"] = $"查权限,账单,余额:操作失败(未知错误)";
                return jo_Result;
            }

            //权限
            jtValue = jr.SelectToken("data['billable_account_by_asset_id']['account_status']").ToString().Trim();
            if (jtValue == "ACTIVE") jo_Result["AdQuanXian"] = "有";
            else jo_Result["AdQuanXian"] = "无";

            //余额
            jtFind = jr.SelectToken("data['billable_account_by_asset_id']['prepay_balance']['formatted_amount']");
            if (jtFind == null || string.IsNullOrEmpty(jtFind.ToString().Trim())) jo_Result["YuE"] = "0";
            else jo_Result["YuE"] = jtFind.ToString().Trim();

            //账单
            frontStr = "{\"label\":\"useBillingHubPaymentSettingsCards_billableAccount$defer$BillingHubPaymentSettingsPaymentActivityCard_billableAccount\"";
            html = frontStr + StringHelper.GetMidStr(hr.Html + "\r\n", frontStr, "\r\n");
            jr = null;
            try { jr = JObject.Parse(html); } catch { }
            if (jr == null || jr.SelectToken("data['billing_payment_account']") == null)
            {
                jo_Result["ErrorMsg"] = $"查权限,账单,余额:操作失败(未知错误)";
                return jo_Result;
            }

            jtsFind = jr.SelectToken("data['billing_payment_account'].txns.edges");
            if (jtsFind != null && jtsFind.Count() > 0) jo_Result["ZhangDan"] = "有";
            else jo_Result["ZhangDan"] = "无";
            #endregion

            jo_Result["Success"] = true;
            jo_Result["ErrorMsg"] = $"查权限,账单,余额:操作成功";
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
