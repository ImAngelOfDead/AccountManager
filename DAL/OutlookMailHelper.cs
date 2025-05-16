#region DLL引入

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WinInetHelperCSharp;

#endregion

namespace AccountManager.DAL
{
    /// <summary>
    /// Outlook邮箱帮助类
    /// </summary>
    public static class OutlookMailHelper
    {
        /// <summary>
        /// 访问协议头
        /// </summary>
        public static WinInet_ProxyInfo GetProxyInfo(string proxy)
        {
            if (string.IsNullOrEmpty(proxy))
            {
                return null;
            }

            string[] strArray = proxy.Split(':');
            WinInet_ProxyInfo proxyInfo = new WinInet_ProxyInfo();
            proxyInfo.ProxyType = WinInet_ProxyType.Http_https;
            proxyInfo.Proxy_IP = strArray[0] + ":" + strArray[1];
            proxyInfo.Proxy_User = strArray[2];
            proxyInfo.Proxy_Pwd = strArray[3];
            return proxyInfo;
        }

        // /// <summary>
        // /// 根据Json格式的Cookie，提取登录信息（初始化，访问邮件列表之前，访问1次即可）
        // /// </summary>
        // /// <param name="Cookie_Json">Json格式的Cookie</param>
        // /// <returns></returns>
        // public static OutlookMailLoginInfo GetLoginInfo(string Cookie_Json, BusinessLinkedin linkedin)
        // {
        //     //微软邮箱，必须加这一句
        //     ServicePointManager.Expect100Continue = false;
        //
        //     string errorMsg = string.Empty;
        //
        //     OutlookMailLoginInfo loginInfo = new OutlookMailLoginInfo();
        //
        //     #region Cookie转换处理
        //
        //     JArray jaCookie = null;
        //     try
        //     {
        //         jaCookie = JArray.Parse(Cookie_Json);
        //     }
        //     catch
        //     {
        //     }
        //
        //     ;
        //     if (jaCookie == null || jaCookie.Count == 0)
        //     {
        //         loginInfo.Login_ErrorMsg = $"Cookie为空";
        //         return loginInfo;
        //     }
        //
        //     loginInfo.Cookie_Json_Old = Cookie_Json;
        //     loginInfo.Cookies_Browser = jaCookie.Select(jt =>
        //     {
        //         Cookie ck = new Cookie();
        //         ck.Name = jt["name"].ToString().Trim();
        //         ck.Value = jt["value"].ToString().Trim();
        //         ck.Domain = jt["domain"].ToString().Trim();
        //         return ck;
        //     }).ToList();
        //
        //     #endregion
        //
        //     WinInet_HttpHelper hh = new WinInet_HttpHelper();
        //     if (LinkedinProxy.Setting.EmailProxy)
        //     {
        //         hh.ProxyInfo_Global = GetProxyInfo(linkedin.Proxy);
        //     }
        //
        //     WinInet_HttpItem hi = null;
        //     WinInet_HttpResult hr = null;
        //     hh.UserAgent_Global = linkedin.Ua; //设置当前WinInet_HttpHelper对象下使用的局部UserAgent
        //
        //     string x_owa_canary = string.Empty;
        //     JObject userInfo = null;
        //     string jsonPath = string.Empty;
        //     JToken jt_Find = null;
        //     string postdata = string.Empty;
        //     Cookie ckFind = null;
        //
        //     #region 第1个跳转(这个链接需要自动重定向，后面的链接就不用自动重定向)
        //
        //     hi = new WinInet_HttpItem();
        //     hi.Accept =
        //         $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
        //     hi.AcceptEncoding = $"gzip, deflate, br";
        //     hi.AcceptLanguage = $"zh-CN,zh;q=0.9";
        //
        //     hi.Url = $"https://outlook.live.com/mail/0/?authRedirect=true&state=0";
        //     hi.HttpMethod = WinInet_HttpMethod.GET;
        //     hi.Cookie = loginInfo.Cookies_Browser_Str;
        //     hi.AutoRedirect = true; //这个链接需要自动重定向，后面的链接就不用自动重定向
        //     hi.Referrer = $"https://outlook.live.com/";
        //
        //     //hi.OtherHeaders.Add(new WinInet_Header() { Name = "Expect", Value = "" });
        //     hr = hh.GetHtml(hi);
        //
        //     //Cookie合并更新
        //     loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //     Console.WriteLine($"第1个跳转 model.Html >>> {hr.HtmlString}");
        //
        //     #endregion
        //
        //     if (string.IsNullOrEmpty(hr.HtmlString) || !hr.HtmlString.Contains("<form name=\"fmHF\""))
        //     {
        //         loginInfo.Login_ErrorMsg = $"第1个跳转失败";
        //         return loginInfo;
        //     }
        //
        //     #region 第2个跳转
        //
        //     //<form name="fmHF" id="fmHF" action="https://outlook.live.com/owa/0/?state=1&redirectTo=aHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8&RpsCsrfState=21972af5-7ff9-dfab-e0df-de3fa9d23ee3&wa=wsignin1.0" method="post" target="_self">
        //     //<input type="hidden" name="NAPExp" id="NAPExp" value="Thu, 06-Jun-2024 21:09:47 GMT">
        //     //<input type="hidden" name="wbids" id="wbids" value="0">
        //     //<input type="hidden" name="pprid" id="pprid" value="c823c24db5dd0c0b">
        //     //<input type="hidden" name="wbid" id="wbid" value="MSFT">
        //     //<input type="hidden" name="NAP" id="NAP" value="V%3D1.9%26E%3D1d1c%26C%3DCWu5hSqthdb4MmGunhbNWxUjt2aNKOB9QKzUq839bfqip1ROIhfSVA%26W%3D1">
        //     //<input type="hidden" name="ANON" id="ANON" value="A%3D4811D3FF5DE17D589F546EAFFFFFFFFF%26E%3D1d76%26W%3D1">
        //     //<input type="hidden" name="ANONExp" id="ANONExp" value="Sat, 14-Sep-2024 21:09:47 GMT">
        //     //<input type="hidden" name="t" id="t" value="GABWAgMAAAAMgAAAFQAgcOKK+Av7YbzAXRD8ww1og9KPkb8Y2PTfeNjPu/JPb8kAAfTmq8rF+2usf40zWGm1JlYz2UphSQs9QJ9xjO9HKGcJBP5b8QEPKEvYxZrbTXCUrEJpx28aWPfvkRvcVx8QER1RlnnBJrYC8IlPYEmsZ0XCOKlnLF33zeNVqGwxdRhTFndD57A6uyA2Lu2gu/sMMmGMrz0Jnr663TwTPYZIVBSANYWWYPzDAl5CGs044zur8bPHzB/NBCLU/FatvtkZMjyxGVyGYvJfFhrz+WZwysmN7eJFE29ypenvBeORPoVAu9/NoQEYBx/CnXbEVjtkXeHYG6ydNrrtC1oP+pr+uG9zpspndSqA/WkXvYodKYSgc5iWecVtUg0krRtpaKcphqAjAX4AIwEAAAMAfujxRirt3WUNKshl6XcEAAoTIAAYGgBlZHVhcmRvMDA3MjI4MjdAZ21haWwuY29tAGIAACZlZHVhcmRvMDA3MjI4MjclZ21haWwuY29tQHBhc3Nwb3J0LmNvbQAAALRCUgAAAAAAAAQWAgAAj3NVQAAGQQAGdmFzY2tpAAh6YXZhc2NraQAAAAAAAAAAAAAAAAAAAAAAALXdDAvII8JNAAAq7d1lDtE+ZgAAAAAAAAAAAAAAAA8AMTEzLjczLjE1My4xMzMABAEAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAMhUqL9tDNe6AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwAAAA==">
        //     //</form>
        //
        //     //提取参数
        //     hi = new WinInet_HttpItem();
        //     hi.Url = $"{GetMidStr(hr.HtmlString, "action=\"", "\"")}";
        //     hi.Accept =
        //         $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
        //     hi.HttpMethod = WinInet_HttpMethod.POST;
        //     hi.PostDataType = WinInet_PostDataType.Byte;
        //
        //     //post数据
        //     string tempStr = $"{GetMidStr(hr.HtmlString, "<form name=\"fmHF\"", "</form>")}";
        //     List<string> sList = new List<string>()
        //         { "NAPExp", "wbids", "pprid", "wbid", "NAP", "ANON", "ANONExp", "t" };
        //     postdata = string.Join("&",
        //         sList.Select(s =>
        //             $"{s}={HttpUtility.UrlEncode(GetMidStr(tempStr, $"id=\"{s}\" value=\"", "\""), Encoding.UTF8)}"));
        //
        //     hi.PostBytes = Encoding.UTF8.GetBytes(postdata);
        //     hi.Cookie = loginInfo.Cookies_Browser_Str;
        //     hi.Referrer = $"https://login.live.com";
        //     hi.OtherHeaders.Add(new WinInet_Header() { Name = $"Origin", Value = $"https://login.live.com" });
        //     hi.OtherHeaders.Add(new WinInet_Header()
        //         { Name = $"Content-Type", Value = $"application/x-www-form-urlencoded" });
        //
        //     hr = hh.GetHtml(hi);
        //
        //     //Cookie合并更新
        //     loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //     Console.WriteLine($"第2个跳转 model.Location >>> {hr.LocationUrl}");
        //
        //     #endregion
        //
        //     if (string.IsNullOrEmpty(hr.LocationUrl))
        //     {
        //         loginInfo.Login_ErrorMsg = $"第2个跳转失败";
        //         return loginInfo;
        //     }
        //
        //     #region 第3个跳转
        //
        //     hi = new WinInet_HttpItem();
        //     hi.Url = hr.LocationUrl;
        //     hi.Referrer = $"https://login.live.com/";
        //     hi.Cookie = loginInfo.Cookies_Browser_Str;
        //
        //     hr = hh.GetHtml(hi);
        //
        //     //Cookie合并更新
        //     loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //     Console.WriteLine($"第3个跳转 model.Location >>> {hr.LocationUrl}");
        //
        //     #endregion
        //
        //     if (string.IsNullOrEmpty(hr.LocationUrl))
        //     {
        //         loginInfo.Login_ErrorMsg = $"第3个跳转失败";
        //         return loginInfo;
        //     }
        //
        //     #region 获取登录权限
        //
        //     hi = new WinInet_HttpItem();
        //     hi.Url = $"https://outlook.live.com/owa/0/startupdata.ashx?app=Mail&n=0";
        //     hi.HttpMethod = WinInet_HttpMethod.POST;
        //     hi.Cookie = loginInfo.Cookies_Browser_Str;
        //     hi.Referrer = "https://outlook.live.com";
        //
        //     hi.OtherHeaders.Add(new WinInet_Header() { Name = $"Origin", Value = $"https://outlook.live.com" });
        //     hi.OtherHeaders.Add(new WinInet_Header()
        //         { Name = $"Content-Type", Value = $"application/x-www-form-urlencoded" });
        //
        //     hi.OtherHeaders.Add(new WinInet_Header() { Name = $"action", Value = $"StartupData" });
        //
        //     ckFind = loginInfo.Cookies_Browser.Where(ck => ck.Name.ToString().ToLower() == "x-owa-canary")
        //         .FirstOrDefault();
        //     x_owa_canary = ckFind == null ? string.Empty : ckFind.Value.ToString().Trim();
        //     if (string.IsNullOrEmpty(x_owa_canary)) x_owa_canary = "X-OWA-CANARY_cookie_is_null_or_empty";
        //
        //     hi.OtherHeaders.Add(new WinInet_Header() { Name = $"x-owa-canary", Value = x_owa_canary });
        //
        //     hr = hh.GetHtml(hi);
        //
        //     //Cookie合并更新
        //     loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //     Console.WriteLine($"获取登录权限 model.Html >>> {hr.HtmlString}");
        //
        //     #endregion
        //
        //     try
        //     {
        //         userInfo = JObject.Parse(hr.HtmlString);
        //         var startupdata = JsonConvert.DeserializeObject<OutlookStartupdata>(hr.HtmlString);
        //         loginInfo.EmailAccount = startupdata.owaUserConfig.SessionSettings.LogonEmailAddress;
        //     }
        //     catch
        //     {
        //     }
        //
        //     if (userInfo == null) return loginInfo;
        //     if (string.IsNullOrEmpty(hr.HtmlString))
        //     {
        //         loginInfo.Login_ErrorMsg = $"获取访问API需要的用户登录关键数据失败";
        //         return loginInfo;
        //     }
        //
        //     //保存登录数据
        //     loginInfo.Login_Success = true;
        //     loginInfo.Login_ErrorMsg = "登录成功";
        //     loginInfo.UserInfo_JObject = userInfo;
        //     loginInfo.UserInfo_JsonStr = hr.HtmlString;
        //
        //     //x-owa-canary(这里需要重新获取，保存)
        //     x_owa_canary = string.Empty;
        //     ckFind = loginInfo.Cookies_Browser.Where(ck => ck.Name.ToString().ToLower() == "x-owa-canary")
        //         .FirstOrDefault();
        //     x_owa_canary = ckFind == null ? string.Empty : ckFind.Value.ToString().Trim();
        //     loginInfo.Postdata_X_OWA_CANARY = x_owa_canary;
        //
        //     //获取邮件列表的参数
        //     jsonPath = "findFolders.Header.ServerVersionInfo.Version";
        //     if (userInfo.SelectToken(jsonPath) != null)
        //         loginInfo.Postdata_FindConversation_Header_RequestServerVersion =
        //             userInfo.SelectToken(jsonPath).ToString().Trim();
        //     jsonPath = "findFolders.Body.ResponseMessages.Items[0].RootFolder.Folders[0].FolderId.Id";
        //     if (userInfo.SelectToken(jsonPath) != null)
        //         loginInfo.Postdata_FindConversation_Body_ParentFolderId_BaseFolderId_Id =
        //             userInfo.SelectToken(jsonPath).ToString().Trim();
        //     jsonPath = "findConversation.Body.SearchFolderId.Id";
        //     if (userInfo.SelectToken(jsonPath) != null)
        //         loginInfo.Postdata_FindConversation_Body_SearchFolderId_Id =
        //             userInfo.SelectToken(jsonPath).ToString().Trim();
        //
        //     //查询邮件内容的参数
        //     jsonPath = "findFolders.Body.ResponseMessages.Items[0].RootFolder.Folders";
        //     if (userInfo.SelectToken(jsonPath) != null)
        //     {
        //         jt_Find = userInfo.SelectToken(jsonPath);
        //         //FolderClass : "IPF.Journal"
        //         jt_Find = jt_Find
        //             .Where(jt => jt["FolderClass"] != null && jt["FolderClass"].ToString().Trim() == "IPF.Journal")
        //             .FirstOrDefault();
        //         if (jt_Find != null)
        //         {
        //             jsonPath = "FolderId.Id";
        //             if (jt_Find.SelectToken(jsonPath) != null)
        //                 loginInfo.Postdata_GetConversationItems_FolderId_Id =
        //                     jt_Find.SelectToken(jsonPath).ToString().Trim();
        //
        //             jsonPath = "ParentFolderId.Id";
        //             if (jt_Find.SelectToken(jsonPath) != null)
        //                 loginInfo.Postdata_GetConversationItems_ParentFolderId_Id =
        //                     jt_Find.SelectToken(jsonPath).ToString().Trim();
        //         }
        //     }
        //
        //     return loginInfo;
        // }
        // /// <summary>
        // /// 根据Json格式的Cookie，提取登录信息（初始化，访问邮件列表之前，访问1次即可）
        // /// </summary>
        // /// <param name="Cookie_Json">Json格式的Cookie</param>
        // /// <returns></returns>
        // public static OutlookMailLoginInfo GetLoginInfo(string Cookie_Json,string ua)
        // {
        //     //微软邮箱，必须加这一句
        //     ServicePointManager.Expect100Continue = false;
        //
        //     string errorMsg = string.Empty;
        //
        //     OutlookMailLoginInfo loginInfo = new OutlookMailLoginInfo();
        //
        //     #region Cookie转换处理
        //     JArray jaCookie = null;
        //     try { jaCookie = JArray.Parse(Cookie_Json); } catch { };
        //     if (jaCookie == null || jaCookie.Count == 0) { loginInfo.Login_ErrorMsg = $"Cookie为空"; return loginInfo; }
        //
        //     loginInfo.Cookie_Json_Old = Cookie_Json;
        //     loginInfo.Cookies_Browser = jaCookie.Select(jt =>
        //     {
        //         Cookie ck = new Cookie();
        //         ck.Name = jt["name"].ToString().Trim();
        //         ck.Value = jt["value"].ToString().Trim();
        //         ck.Domain = jt["domain"].ToString().Trim();
        //         return ck;
        //     }).ToList();
        //     #endregion
        //
        //     WinInet_HttpHelper hh = new WinInet_HttpHelper();
        //     WinInet_HttpItem hi = null;
        //     WinInet_HttpResult hr = null;
        //     hh.UserAgent_Global = ua;//设置当前WinInet_HttpHelper对象下使用的局部UserAgent
        //
        //     string x_owa_canary = string.Empty;
        //     JObject userInfo = null;
        //     string jsonPath = string.Empty;
        //     JToken jt_Find = null;
        //     string postdata = string.Empty;
        //     Cookie ckFind = null;
        //     string redirectTo = string.Empty;
        //     string cobrandid = string.Empty;
        //     string nlp = string.Empty;
        //     string locationUrl = string.Empty;
        //
        //     #region 第1个跳转
        //     //第1次连接
        //     hi = new WinInet_HttpItem();
        //     hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
        //     hi.AcceptEncoding = $"gzip, deflate, br";
        //     hi.AcceptLanguage = $"zh-CN,zh;q=0.9";
        //
        //     hi.Url = $"https://outlook.live.com/mail/0/?authRedirect=true&state=0";
        //     hi.HttpMethod = WinInet_HttpMethod.GET;
        //     hi.Cookie = loginInfo.Cookies_Browser_Str;
        //     hi.Referrer = $"https://outlook.live.com/";
        //
        //     hr = hh.GetHtml(hi);
        //
        //     //Cookie合并更新
        //     if (hr.Cookies != null && hr.Cookies.Count > 0) loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //     //https://outlook.live.com/owa/0/?state=1&redirectTo=aHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8
        //     Console.WriteLine($"第1个跳转 model.Html >>> {hr.LocationUrl}");
        //     if (string.IsNullOrEmpty(hr.LocationUrl)) { loginInfo.Login_ErrorMsg = $"第1个跳转失败(在第1次连接)"; return loginInfo; }
        //
        //     redirectTo = GetMidStr(hr.LocationUrl + "&", "redirectTo=", "&");
        //
        //     //第2次连接
        //     hi = new WinInet_HttpItem();
        //     hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
        //     hi.AcceptEncoding = $"gzip, deflate, br";
        //     hi.AcceptLanguage = $"zh-CN,zh;q=0.9";
        //
        //     hi.Url = hr.LocationUrl;
        //     hi.HttpMethod = WinInet_HttpMethod.GET;
        //     hi.Cookie = loginInfo.Cookies_Browser_Str;
        //     hi.Referrer = $"https://outlook.live.com/";
        //
        //     hr = hh.GetHtml(hi);
        //
        //     //Cookie合并更新
        //     if (hr.Cookies != null && hr.Cookies.Count > 0) loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //     //https://login.live.com/login.srf?wa=wsignin1.0&rpsnv=22&ct=1711780827&rver=7.0.6738.0&wp=MBI_SSL&wreply=https%3a%2f%2foutlook.live.com%2fowa%2f0%2f%3fstate%3d1%26redirectTo%3daHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8%26RpsCsrfState%3dd115a16b-0f7b-f0eb-23e2-799a9c72adff&id=292841&aadredir=1&CBCXT=out&lw=1&fl=dob%2cflname%2cwld&cobrandid=90015
        //     //https://www.microsoft.com/zh-cn/microsoft-365/outlook/email-and-calendar-software-microsoft-outlook?deeplink=%2fowa%2f0%2f%3fstate%3d1%26redirectTo%3daHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8&sdf=0
        //     Console.WriteLine($"第1个跳转 model.Html >>> {hr.LocationUrl}");
        //     if (string.IsNullOrEmpty(hr.LocationUrl)) { loginInfo.Login_ErrorMsg = $"第1个跳转失败(在第2次连接)"; return loginInfo; }
        //     if (!hr.LocationUrl.StartsWith("https://login.live.com/login.srf?") && !hr.LocationUrl.StartsWith("https://www.microsoft.com/zh-cn/microsoft-365/outlook/email-and-calendar-software-microsoft-outlook?")) { loginInfo.Login_ErrorMsg = $"第1个跳转失败(在第2次连接)"; return loginInfo; }
        //
        //     if (hr.LocationUrl.StartsWith("https://www.microsoft.com/zh-cn/microsoft-365/outlook/email-and-calendar-software-microsoft-outlook?"))
        //     {
        //         //先跳转连接
        //         hi = new WinInet_HttpItem();
        //         hi.Accept = $"*/*";
        //         hi.AcceptEncoding = $"gzip, deflate, br";
        //         hi.AcceptLanguage = $"zh-CN,zh;q=0.9";
        //
        //         hi.Url = $"{hr.LocationUrl}";
        //         hi.HttpMethod = WinInet_HttpMethod.GET;
        //         hi.Cookie = loginInfo.Cookies_Browser_Str;
        //         hi.Referrer = $"https://outlook.live.com/";
        //
        //         hr = hh.GetHtml(hi);
        //
        //         //Cookie合并更新
        //         if (hr.Cookies != null && hr.Cookies.Count > 0) loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //         locationUrl = GetMidStr(hr.HtmlString, "<script id=\"onecloud-body-script\" type=\"text/javascript\" src=\"", "\"");
        //         if (string.IsNullOrEmpty(locationUrl)) { loginInfo.Login_ErrorMsg = $"第1个跳转失败(在第2次连接)"; return loginInfo; }
        //
        //         //<script id="onecloud-body-script" type="text/javascript" src="https://query.prod.cms.rt.microsoft.com/cms/api/am/binary/RE4OCI2" async></script>
        //         hi = new WinInet_HttpItem();
        //         hi.Accept = $"*/*";
        //         hi.AcceptEncoding = $"gzip, deflate, br";
        //         hi.AcceptLanguage = $"zh-CN,zh;q=0.9";
        //
        //         hi.Url = locationUrl;
        //         hi.HttpMethod = WinInet_HttpMethod.GET;
        //         hi.Cookie = loginInfo.Cookies_Browser_Str;
        //         hi.Referrer = $"https://outlook.live.com/";
        //
        //         hr = hh.GetHtml(hi);
        //
        //         //Cookie合并更新
        //         if (hr.Cookies != null && hr.Cookies.Count > 0) loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //         //取cobrandid
        //         //{cobrandid:"ab0455a0-8d03-46b9-b18b-df2f57b9e44c",nlp:"1"}
        //         cobrandid = GetMidStr(hr.HtmlString, "cobrandid:\"", "\"");
        //         nlp = GetMidStr(hr.HtmlString, "nlp:\"", "\"");
        //
        //         if (string.IsNullOrEmpty(cobrandid) || string.IsNullOrEmpty(nlp)) { loginInfo.Login_ErrorMsg = $"第1个跳转失败(在 取cobrandid)"; return loginInfo; }
        //
        //         //跳转连接
        //         //https://outlook.live.com/owa/?cobrandid=ab0455a0-8d03-46b9-b18b-df2f57b9e44c&nlp=1&deeplink=owa/0/?state=1&redirectTo=aHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8
        //         hi = new WinInet_HttpItem();
        //         hi.Accept = $"*/*";
        //         hi.AcceptEncoding = $"gzip, deflate, br";
        //         hi.AcceptLanguage = $"zh-CN,zh;q=0.9";
        //
        //         hi.Url = $"https://outlook.live.com/owa/?cobrandid={cobrandid}&nlp={nlp}&deeplink=owa/0/?state=1&redirectTo={redirectTo}";
        //         hi.HttpMethod = WinInet_HttpMethod.GET;
        //         hi.Cookie = loginInfo.Cookies_Browser_Str;
        //         hi.Referrer = $"https://outlook.live.com/";
        //
        //         hr = hh.GetHtml(hi);
        //
        //         //Cookie合并更新
        //         if (hr.Cookies != null && hr.Cookies.Count > 0) loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //         //https://login.live.com/login.srf?wa=wsignin1.0&rpsnv=22&ct=1711771048&rver=7.0.6738.0&wp=MBI_SSL&wreply=https%3a%2f%2foutlook.live.com%2fowa%2f%3fcobrandid%3dab0455a0-8d03-46b9-b18b-df2f57b9e44c%26nlp%3d1%26deeplink%3dowa%252f0%252f%253fstate%253d1%26redirectTo%3daHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8%26RpsCsrfState%3dac41bb73-fb2d-f91e-8683-3638a276c21a&id=292841&aadredir=1&CBCXT=out&lw=1&fl=dob%2cflname%2cwld&cobrandid=ab0455a0-8d03-46b9-b18b-df2f57b9e44c
        //         Console.WriteLine($"第1个跳转 model.Html >>> {hr.LocationUrl}");
        //         if (!hr.LocationUrl.StartsWith("https://login.live.com/login.srf?")) { loginInfo.Login_ErrorMsg = $"第1个跳转失败(在 https://outlook.live.com/owa/?cobrandid=)"; return loginInfo; }
        //     }
        //
        //     //第3次连接
        //     hi = new WinInet_HttpItem();
        //     hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
        //     hi.AcceptEncoding = $"gzip, deflate, br";
        //     hi.AcceptLanguage = $"zh-CN,zh;q=0.9";
        //
        //     hi.Url = hr.LocationUrl;
        //     hi.HttpMethod = WinInet_HttpMethod.GET;
        //     hi.Cookie = loginInfo.Cookies_Browser_Str;
        //     hi.Referrer = $"https://outlook.live.com/";
        //
        //     hr = hh.GetHtml(hi);
        //
        //     //Cookie合并更新
        //     if (hr.Cookies != null && hr.Cookies.Count > 0) loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //     Console.WriteLine($"第1个跳转 model.Html >>> {hr.LocationUrl}");
        //     if (string.IsNullOrEmpty(hr.HtmlString) || !hr.HtmlString.Contains("<form name=\"fmHF\"")) { loginInfo.Login_ErrorMsg = $"第1个跳转失败"; return loginInfo; }
        //     #endregion
        //
        //     #region 第2个跳转
        //     //<form name="fmHF" id="fmHF" action="https://outlook.live.com/owa/0/?state=1&redirectTo=aHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8&RpsCsrfState=21972af5-7ff9-dfab-e0df-de3fa9d23ee3&wa=wsignin1.0" method="post" target="_self">
        //     //<input type="hidden" name="NAPExp" id="NAPExp" value="Thu, 06-Jun-2024 21:09:47 GMT">
        //     //<input type="hidden" name="wbids" id="wbids" value="0">
        //     //<input type="hidden" name="pprid" id="pprid" value="c823c24db5dd0c0b">
        //     //<input type="hidden" name="wbid" id="wbid" value="MSFT">
        //     //<input type="hidden" name="NAP" id="NAP" value="V%3D1.9%26E%3D1d1c%26C%3DCWu5hSqthdb4MmGunhbNWxUjt2aNKOB9QKzUq839bfqip1ROIhfSVA%26W%3D1">
        //     //<input type="hidden" name="ANON" id="ANON" value="A%3D4811D3FF5DE17D589F546EAFFFFFFFFF%26E%3D1d76%26W%3D1">
        //     //<input type="hidden" name="ANONExp" id="ANONExp" value="Sat, 14-Sep-2024 21:09:47 GMT">
        //     //<input type="hidden" name="t" id="t" value="GABWAgMAAAAMgAAAFQAgcOKK+Av7YbzAXRD8ww1og9KPkb8Y2PTfeNjPu/JPb8kAAfTmq8rF+2usf40zWGm1JlYz2UphSQs9QJ9xjO9HKGcJBP5b8QEPKEvYxZrbTXCUrEJpx28aWPfvkRvcVx8QER1RlnnBJrYC8IlPYEmsZ0XCOKlnLF33zeNVqGwxdRhTFndD57A6uyA2Lu2gu/sMMmGMrz0Jnr663TwTPYZIVBSANYWWYPzDAl5CGs044zur8bPHzB/NBCLU/FatvtkZMjyxGVyGYvJfFhrz+WZwysmN7eJFE29ypenvBeORPoVAu9/NoQEYBx/CnXbEVjtkXeHYG6ydNrrtC1oP+pr+uG9zpspndSqA/WkXvYodKYSgc5iWecVtUg0krRtpaKcphqAjAX4AIwEAAAMAfujxRirt3WUNKshl6XcEAAoTIAAYGgBlZHVhcmRvMDA3MjI4MjdAZ21haWwuY29tAGIAACZlZHVhcmRvMDA3MjI4MjclZ21haWwuY29tQHBhc3Nwb3J0LmNvbQAAALRCUgAAAAAAAAQWAgAAj3NVQAAGQQAGdmFzY2tpAAh6YXZhc2NraQAAAAAAAAAAAAAAAAAAAAAAALXdDAvII8JNAAAq7d1lDtE+ZgAAAAAAAAAAAAAAAA8AMTEzLjczLjE1My4xMzMABAEAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAMhUqL9tDNe6AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwAAAA==">
        //     //</form>
        //
        //     //提取参数
        //     hi = new WinInet_HttpItem();
        //     hi.Url = $"{GetMidStr(hr.HtmlString, "action=\"", "\"")}";
        //     hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
        //     hi.HttpMethod = WinInet_HttpMethod.POST;
        //     hi.PostDataType = WinInet_PostDataType.Byte;
        //
        //     //post数据
        //     string tempStr = $"{GetMidStr(hr.HtmlString, "<form name=\"fmHF\"", "</form>")}";
        //     List<string> sList = GetMidStrList(tempStr, "<input type=\"hidden\"", ">");
        //     postdata = string.Join("&", sList.Select(s => $"{GetMidStr(s, "name=\"", "\"")}={HttpUtility.UrlEncode(GetMidStr(s, $"value=\"", "\""), Encoding.UTF8)}"));
        //     //List<string> sList = new List<string>() { "NAPExp", "wbids", "pprid", "wbid", "NAP", "ANON", "ANONExp", "t" };
        //     //postdata = string.Join("&", sList.Select(s => $"{s}={HttpUtility.UrlEncode(GetMidStr(tempStr, $"id=\"{s}\" value=\"", "\""), Encoding.UTF8)}"));
        //
        //     hi.PostBytes = Encoding.UTF8.GetBytes(postdata);
        //     hi.Cookie = loginInfo.Cookies_Browser_Str;
        //     hi.Referrer = $"https://login.live.com";
        //     hi.OtherHeaders.Add(new WinInet_Header() { Name = $"Origin", Value = $"https://login.live.com" });
        //     hi.OtherHeaders.Add(new WinInet_Header() { Name = $"Content-Type", Value = $"application/x-www-form-urlencoded" });
        //
        //     hr = hh.GetHtml(hi);
        //
        //     //Cookie合并更新
        //     loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //     Console.WriteLine($"第2个跳转 model.Location >>> {hr.LocationUrl}");
        //     if (string.IsNullOrEmpty(hr.LocationUrl)) { loginInfo.Login_ErrorMsg = $"第2个跳转失败"; return loginInfo; }
        //     #endregion
        //
        //     #region 第3个跳转
        //     if (hr.LocationUrl!= "https://outlook.live.com/mail/0/")
        //     {
        //         hi = new WinInet_HttpItem();
        //         hi.Url = hr.LocationUrl;
        //         hi.Referrer = $"https://login.live.com/";
        //         hi.Cookie = loginInfo.Cookies_Browser_Str;
        //
        //         hr = hh.GetHtml(hi);
        //
        //         //Cookie合并更新
        //         loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //         Console.WriteLine($"第3个跳转 model.Location >>> {hr.LocationUrl}");
        //         if (!hr.HeadersString.Contains("HTTP/1.1 200 OK") || hr.Cookies == null || hr.Cookies.Count == 0) { loginInfo.Login_ErrorMsg = $"第3个跳转失败"; return loginInfo; }
        //     }
        //     #endregion
        //
        //     #region 获取登录权限
        //     hi = new WinInet_HttpItem();
        //     hi.Url = $"https://outlook.live.com/owa/0/startupdata.ashx?app=Mail&n=0";
        //     hi.HttpMethod = WinInet_HttpMethod.POST;
        //     hi.Cookie = loginInfo.Cookies_Browser_Str;
        //     hi.Referrer = "https://outlook.live.com";
        //
        //     hi.OtherHeaders.Add(new WinInet_Header() { Name = $"Origin", Value = $"https://outlook.live.com" });
        //     hi.OtherHeaders.Add(new WinInet_Header() { Name = $"Content-Type", Value = $"application/x-www-form-urlencoded" });
        //
        //     hi.OtherHeaders.Add(new WinInet_Header() { Name = $"action", Value = $"StartupData" });
        //
        //     ckFind = loginInfo.Cookies_Browser.Where(ck => ck.Name.ToString().ToLower() == "x-owa-canary").FirstOrDefault();
        //     x_owa_canary = ckFind == null ? string.Empty : ckFind.Value.ToString().Trim();
        //     if (string.IsNullOrEmpty(x_owa_canary)) x_owa_canary = "X-OWA-CANARY_cookie_is_null_or_empty";
        //
        //     hi.OtherHeaders.Add(new WinInet_Header() { Name = $"x-owa-canary", Value = x_owa_canary });
        //
        //     hr = hh.GetHtml(hi);
        //
        //     //Cookie合并更新
        //     loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);
        //
        //     Console.WriteLine($"获取登录权限 model.Html >>> {hr.HtmlString}");
        //     #endregion
        //     try
        //     {
        //         userInfo = JObject.Parse(hr.HtmlString);
        //         var startupdata = JsonConvert.DeserializeObject<OutlookStartupdata>(hr.HtmlString);
        //         loginInfo.EmailAccount = startupdata.owaUserConfig.SessionSettings.LogonEmailAddress;
        //     }
        //     catch
        //     {
        //     }
        //     try { userInfo = JObject.Parse(hr.HtmlString); } catch { }
        //     if (userInfo == null) return loginInfo;
        //     if (string.IsNullOrEmpty(hr.HtmlString)) { loginInfo.Login_ErrorMsg = $"获取访问API需要的用户登录关键数据失败"; return loginInfo; }
        //
        //     //保存登录数据
        //     loginInfo.Login_Success = true;
        //     loginInfo.Login_ErrorMsg = "登录成功";
        //     loginInfo.UserInfo_JObject = userInfo;
        //     loginInfo.UserInfo_JsonStr = hr.HtmlString;
        //
        //     //x-owa-canary(这里需要重新获取，保存)
        //     x_owa_canary = string.Empty;
        //     ckFind = loginInfo.Cookies_Browser.Where(ck => ck.Name.ToString().ToLower() == "x-owa-canary").FirstOrDefault();
        //     x_owa_canary = ckFind == null ? string.Empty : ckFind.Value.ToString().Trim();
        //     loginInfo.Postdata_X_OWA_CANARY = x_owa_canary;
        //
        //     //获取邮件列表的参数
        //     jsonPath = "findFolders.Header.ServerVersionInfo.Version";
        //     if (userInfo.SelectToken(jsonPath) != null) loginInfo.Postdata_FindConversation_Header_RequestServerVersion = userInfo.SelectToken(jsonPath).ToString().Trim();
        //     jsonPath = "findFolders.Body.ResponseMessages.Items[0].RootFolder.Folders[0].FolderId.Id";
        //     if (userInfo.SelectToken(jsonPath) != null) loginInfo.Postdata_FindConversation_Body_ParentFolderId_BaseFolderId_Id = userInfo.SelectToken(jsonPath).ToString().Trim();
        //     jsonPath = "findConversation.Body.SearchFolderId.Id";
        //     if (userInfo.SelectToken(jsonPath) != null) loginInfo.Postdata_FindConversation_Body_SearchFolderId_Id = userInfo.SelectToken(jsonPath).ToString().Trim();
        //
        //     //查询邮件内容的参数
        //     jsonPath = "findFolders.Body.ResponseMessages.Items[0].RootFolder.Folders";
        //     if (userInfo.SelectToken(jsonPath) != null)
        //     {
        //         jt_Find = userInfo.SelectToken(jsonPath);
        //         //FolderClass : "IPF.Journal"
        //         jt_Find = jt_Find.Where(jt => jt["FolderClass"] != null && jt["FolderClass"].ToString().Trim() == "IPF.Journal").FirstOrDefault();
        //         if (jt_Find != null)
        //         {
        //             jsonPath = "FolderId.Id";
        //             if (jt_Find.SelectToken(jsonPath) != null) loginInfo.Postdata_GetConversationItems_FolderId_Id = jt_Find.SelectToken(jsonPath).ToString().Trim();
        //
        //             jsonPath = "ParentFolderId.Id";
        //             if (jt_Find.SelectToken(jsonPath) != null) loginInfo.Postdata_GetConversationItems_ParentFolderId_Id = jt_Find.SelectToken(jsonPath).ToString().Trim();
        //         }
        //     }
        //
        //     return loginInfo;
        // }

        /// <summary>
        /// 根据Json格式的Cookie，提取登录信息（初始化，访问邮件列表之前，访问1次即可）
        /// </summary>
        /// <param name="Cookie_Json">Json格式的Cookie</param>
        /// <returns></returns>
        public static OutlookMail_LoginInfo GetLoginInfo(string Cookie_Json, string ua)
        {
            //微软邮箱，必须加这一句
            ServicePointManager.Expect100Continue = false;

            string errorMsg = string.Empty;

            OutlookMail_LoginInfo loginInfo = new OutlookMail_LoginInfo();

            #region Cookie转换处理

            JArray jaCookie = null;
            try
            {
                jaCookie = JArray.Parse(Cookie_Json);
            }
            catch
            {
            }

            ;
            if (jaCookie == null || jaCookie.Count == 0)
            {
                loginInfo.Login_ErrorMsg = $"Cookie为空";
                return loginInfo;
            }

            loginInfo.Cookie_Json_Old = Cookie_Json;
            loginInfo.Cookies_Browser = jaCookie.Select(jt =>
            {
                Cookie ck = new Cookie();
                ck.Name = jt["name"].ToString().Trim();
                ck.Value = jt["value"].ToString().Trim();
                ck.Domain = jt["domain"].ToString().Trim();
                return ck;
            }).ToList();

            #endregion

            WinInet_HttpHelper hh = new WinInet_HttpHelper();
            WinInet_HttpItem hi = null;
            WinInet_HttpResult hr = null;
            hh.UserAgent_Global = ua; //设置当前WinInet_HttpHelper对象下使用的局部UserAgent

            string x_owa_canary = string.Empty;
            JObject userInfo = null;
            string jsonPath = string.Empty;
            JToken jt_Find = null;
            string postdata = string.Empty;
            Cookie ckFind = null;
            string redirectTo = string.Empty;
            string cobrandid = string.Empty;
            string nlp = string.Empty;
            string locationUrl = string.Empty;

            #region 第1个跳转

            //第1次连接
            hi = new WinInet_HttpItem();
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.AcceptEncoding = $"gzip, deflate, br";
            hi.AcceptLanguage = $"zh-CN,zh;q=0.9";

            hi.Url = $"https://outlook.live.com/mail/0/?authRedirect=true&state=0";
            hi.HttpMethod = WinInet_HttpMethod.GET;
            hi.Cookie = loginInfo.Cookies_Browser_Str;
            hi.Referrer = $"https://outlook.live.com/";

            hr = hh.GetHtml(hi);

            //Cookie合并更新
            if (hr.Cookies != null && hr.Cookies.Count > 0)
                loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);

            //https://outlook.live.com/owa/0/?state=1&redirectTo=aHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8
            Console.WriteLine($"第1个跳转 model.Html >>> {hr.LocationUrl}");
            if (string.IsNullOrEmpty(hr.LocationUrl))
            {
                loginInfo.Login_ErrorMsg = $"第1个跳转失败(在第1次连接)";
                return loginInfo;
            }

            redirectTo = GetMidStr(hr.LocationUrl + "&", "redirectTo=", "&");

            //第2次连接
            hi = new WinInet_HttpItem();
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.AcceptEncoding = $"gzip, deflate, br";
            hi.AcceptLanguage = $"zh-CN,zh;q=0.9";

            hi.Url = hr.LocationUrl;
            hi.HttpMethod = WinInet_HttpMethod.GET;
            hi.Cookie = loginInfo.Cookies_Browser_Str;
            hi.Referrer = $"https://outlook.live.com/";

            hr = hh.GetHtml(hi);

            //Cookie合并更新
            if (hr.Cookies != null && hr.Cookies.Count > 0)
                loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);

            //https://login.live.com/login.srf?wa=wsignin1.0&rpsnv=22&ct=1711780827&rver=7.0.6738.0&wp=MBI_SSL&wreply=https%3a%2f%2foutlook.live.com%2fowa%2f0%2f%3fstate%3d1%26redirectTo%3daHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8%26RpsCsrfState%3dd115a16b-0f7b-f0eb-23e2-799a9c72adff&id=292841&aadredir=1&CBCXT=out&lw=1&fl=dob%2cflname%2cwld&cobrandid=90015
            //https://www.microsoft.com/zh-cn/microsoft-365/outlook/email-and-calendar-software-microsoft-outlook?deeplink=%2fowa%2f0%2f%3fstate%3d1%26redirectTo%3daHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8&sdf=0
            Console.WriteLine($"第1个跳转 model.Html >>> {hr.LocationUrl}");
            if (string.IsNullOrEmpty(hr.LocationUrl))
            {
                loginInfo.Login_ErrorMsg = $"第1个跳转失败(在第2次连接)";
                return loginInfo;
            }

            if (!hr.LocationUrl.StartsWith("https://login.live.com/login.srf?") && !hr.LocationUrl.StartsWith(
                    "https://www.microsoft.com/zh-cn/microsoft-365/outlook/email-and-calendar-software-microsoft-outlook?"))
            {
                loginInfo.Login_ErrorMsg = $"第1个跳转失败(在第2次连接)";
                return loginInfo;
            }

            if (hr.LocationUrl.StartsWith(
                    "https://www.microsoft.com/zh-cn/microsoft-365/outlook/email-and-calendar-software-microsoft-outlook?"))
            {
                //先跳转连接
                hi = new WinInet_HttpItem();
                hi.Accept = $"*/*";
                hi.AcceptEncoding = $"gzip, deflate, br";
                hi.AcceptLanguage = $"zh-CN,zh;q=0.9";

                hi.Url = $"{hr.LocationUrl}";
                hi.HttpMethod = WinInet_HttpMethod.GET;
                hi.Cookie = loginInfo.Cookies_Browser_Str;
                hi.Referrer = $"https://outlook.live.com/";

                hr = hh.GetHtml(hi);

                //Cookie合并更新
                if (hr.Cookies != null && hr.Cookies.Count > 0)
                    loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);

                locationUrl = GetMidStr(hr.HtmlString,
                    "<script id=\"onecloud-body-script\" type=\"text/javascript\" src=\"", "\"");
                if (string.IsNullOrEmpty(locationUrl))
                {
                    loginInfo.Login_ErrorMsg = $"第1个跳转失败(在第2次连接)";
                    return loginInfo;
                }

                //<script id="onecloud-body-script" type="text/javascript" src="https://query.prod.cms.rt.microsoft.com/cms/api/am/binary/RE4OCI2" async></script>
                hi = new WinInet_HttpItem();
                hi.Accept = $"*/*";
                hi.AcceptEncoding = $"gzip, deflate, br";
                hi.AcceptLanguage = $"zh-CN,zh;q=0.9";

                hi.Url = locationUrl;
                hi.HttpMethod = WinInet_HttpMethod.GET;
                hi.Cookie = loginInfo.Cookies_Browser_Str;
                hi.Referrer = $"https://outlook.live.com/";

                hr = hh.GetHtml(hi);

                //Cookie合并更新
                if (hr.Cookies != null && hr.Cookies.Count > 0)
                    loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);

                //取cobrandid
                //{cobrandid:"ab0455a0-8d03-46b9-b18b-df2f57b9e44c",nlp:"1"}
                cobrandid = GetMidStr(hr.HtmlString, "cobrandid:\"", "\"");
                nlp = GetMidStr(hr.HtmlString, "nlp:\"", "\"");

                if (string.IsNullOrEmpty(cobrandid) || string.IsNullOrEmpty(nlp))
                {
                    loginInfo.Login_ErrorMsg = $"第1个跳转失败(在 取cobrandid)";
                    return loginInfo;
                }

                //跳转连接
                //https://outlook.live.com/owa/?cobrandid=ab0455a0-8d03-46b9-b18b-df2f57b9e44c&nlp=1&deeplink=owa/0/?state=1&redirectTo=aHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8
                hi = new WinInet_HttpItem();
                hi.Accept = $"*/*";
                hi.AcceptEncoding = $"gzip, deflate, br";
                hi.AcceptLanguage = $"zh-CN,zh;q=0.9";

                hi.Url =
                    $"https://outlook.live.com/owa/?cobrandid={cobrandid}&nlp={nlp}&deeplink=owa/0/?state=1&redirectTo={redirectTo}";
                hi.HttpMethod = WinInet_HttpMethod.GET;
                hi.Cookie = loginInfo.Cookies_Browser_Str;
                hi.Referrer = $"https://outlook.live.com/";

                hr = hh.GetHtml(hi);

                //Cookie合并更新
                if (hr.Cookies != null && hr.Cookies.Count > 0)
                    loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);

                //https://login.live.com/login.srf?wa=wsignin1.0&rpsnv=22&ct=1711771048&rver=7.0.6738.0&wp=MBI_SSL&wreply=https%3a%2f%2foutlook.live.com%2fowa%2f%3fcobrandid%3dab0455a0-8d03-46b9-b18b-df2f57b9e44c%26nlp%3d1%26deeplink%3dowa%252f0%252f%253fstate%253d1%26redirectTo%3daHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8%26RpsCsrfState%3dac41bb73-fb2d-f91e-8683-3638a276c21a&id=292841&aadredir=1&CBCXT=out&lw=1&fl=dob%2cflname%2cwld&cobrandid=ab0455a0-8d03-46b9-b18b-df2f57b9e44c
                Console.WriteLine($"第1个跳转 model.Html >>> {hr.LocationUrl}");
                if (!hr.LocationUrl.StartsWith("https://login.live.com/login.srf?"))
                {
                    loginInfo.Login_ErrorMsg = $"第1个跳转失败(在 https://outlook.live.com/owa/?cobrandid=)";
                    return loginInfo;
                }
            }

            //第3次连接
            hi = new WinInet_HttpItem();
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.AcceptEncoding = $"gzip, deflate, br";
            hi.AcceptLanguage = $"zh-CN,zh;q=0.9";

            hi.Url = hr.LocationUrl;
            hi.HttpMethod = WinInet_HttpMethod.GET;
            hi.Cookie = loginInfo.Cookies_Browser_Str;
            hi.Referrer = $"https://outlook.live.com/";

            hr = hh.GetHtml(hi);

            //Cookie合并更新
            if (hr.Cookies != null && hr.Cookies.Count > 0)
                loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);

            Console.WriteLine($"第1个跳转 model.Html >>> {hr.LocationUrl}");
            if (string.IsNullOrEmpty(hr.HtmlString) || !hr.HtmlString.Contains("<form name=\"fmHF\""))
            {
                loginInfo.Login_ErrorMsg = $"第1个跳转失败";
                return loginInfo;
            }

            #endregion

            #region 第2个跳转

            //<form name="fmHF" id="fmHF" action="https://outlook.live.com/owa/0/?state=1&redirectTo=aHR0cHM6Ly9vdXRsb29rLmxpdmUuY29tL21haWwvMC8&RpsCsrfState=21972af5-7ff9-dfab-e0df-de3fa9d23ee3&wa=wsignin1.0" method="post" target="_self">
            //<input type="hidden" name="NAPExp" id="NAPExp" value="Thu, 06-Jun-2024 21:09:47 GMT">
            //<input type="hidden" name="wbids" id="wbids" value="0">
            //<input type="hidden" name="pprid" id="pprid" value="c823c24db5dd0c0b">
            //<input type="hidden" name="wbid" id="wbid" value="MSFT">
            //<input type="hidden" name="NAP" id="NAP" value="V%3D1.9%26E%3D1d1c%26C%3DCWu5hSqthdb4MmGunhbNWxUjt2aNKOB9QKzUq839bfqip1ROIhfSVA%26W%3D1">
            //<input type="hidden" name="ANON" id="ANON" value="A%3D4811D3FF5DE17D589F546EAFFFFFFFFF%26E%3D1d76%26W%3D1">
            //<input type="hidden" name="ANONExp" id="ANONExp" value="Sat, 14-Sep-2024 21:09:47 GMT">
            //<input type="hidden" name="t" id="t" value="GABWAgMAAAAMgAAAFQAgcOKK+Av7YbzAXRD8ww1og9KPkb8Y2PTfeNjPu/JPb8kAAfTmq8rF+2usf40zWGm1JlYz2UphSQs9QJ9xjO9HKGcJBP5b8QEPKEvYxZrbTXCUrEJpx28aWPfvkRvcVx8QER1RlnnBJrYC8IlPYEmsZ0XCOKlnLF33zeNVqGwxdRhTFndD57A6uyA2Lu2gu/sMMmGMrz0Jnr663TwTPYZIVBSANYWWYPzDAl5CGs044zur8bPHzB/NBCLU/FatvtkZMjyxGVyGYvJfFhrz+WZwysmN7eJFE29ypenvBeORPoVAu9/NoQEYBx/CnXbEVjtkXeHYG6ydNrrtC1oP+pr+uG9zpspndSqA/WkXvYodKYSgc5iWecVtUg0krRtpaKcphqAjAX4AIwEAAAMAfujxRirt3WUNKshl6XcEAAoTIAAYGgBlZHVhcmRvMDA3MjI4MjdAZ21haWwuY29tAGIAACZlZHVhcmRvMDA3MjI4MjclZ21haWwuY29tQHBhc3Nwb3J0LmNvbQAAALRCUgAAAAAAAAQWAgAAj3NVQAAGQQAGdmFzY2tpAAh6YXZhc2NraQAAAAAAAAAAAAAAAAAAAAAAALXdDAvII8JNAAAq7d1lDtE+ZgAAAAAAAAAAAAAAAA8AMTEzLjczLjE1My4xMzMABAEAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAMhUqL9tDNe6AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwAAAA==">
            //</form>

            //提取参数
            hi = new WinInet_HttpItem();
            hi.Url = $"{GetMidStr(hr.HtmlString, "action=\"", "\"")}";
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            hi.HttpMethod = WinInet_HttpMethod.POST;
            hi.PostDataType = WinInet_PostDataType.Byte;

            //post数据
            string tempStr = $"{GetMidStr(hr.HtmlString, "<form name=\"fmHF\"", "</form>")}";
            List<string> sList = GetMidStrList(tempStr, "<input type=\"hidden\"", ">");
            postdata = string.Join("&",
                sList.Select(s =>
                    $"{GetMidStr(s, "name=\"", "\"")}={HttpUtility.UrlEncode(GetMidStr(s, $"value=\"", "\""), Encoding.UTF8)}"));
            //List<string> sList = new List<string>() { "NAPExp", "wbids", "pprid", "wbid", "NAP", "ANON", "ANONExp", "t" };
            //postdata = string.Join("&", sList.Select(s => $"{s}={HttpUtility.UrlEncode(GetMidStr(tempStr, $"id=\"{s}\" value=\"", "\""), Encoding.UTF8)}"));

            hi.PostBytes = Encoding.UTF8.GetBytes(postdata);
            hi.Cookie = loginInfo.Cookies_Browser_Str;
            hi.Referrer = $"https://login.live.com";
            hi.OtherHeaders.Add(new WinInet_Header() { Name = $"Origin", Value = $"https://login.live.com" });
            hi.OtherHeaders.Add(new WinInet_Header()
                { Name = $"Content-Type", Value = $"application/x-www-form-urlencoded" });

            hr = hh.GetHtml(hi);

            //Cookie合并更新
            loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);

            Console.WriteLine($"第2个跳转 model.Location >>> {hr.LocationUrl}");
            if (string.IsNullOrEmpty(hr.LocationUrl))
            {
                loginInfo.Login_ErrorMsg = $"第2个跳转失败";
                return loginInfo;
            }

            #endregion

            #region 第3个跳转

            if (hr.LocationUrl != "https://outlook.live.com/mail/0/")
            {
                hi = new WinInet_HttpItem();
                hi.Url = hr.LocationUrl;
                hi.Referrer = $"https://login.live.com/";
                hi.Cookie = loginInfo.Cookies_Browser_Str;

                hr = hh.GetHtml(hi);

                //Cookie合并更新
                loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);

                Console.WriteLine($"第3个跳转 model.Location >>> {hr.LocationUrl}");
                if ((!hr.HeadersString.Contains("HTTP/1.1 200 OK") || hr.Cookies == null || hr.Cookies.Count == 0) &&
                    hr.LocationUrl != "https://outlook.live.com/mail/0/")
                {
                    loginInfo.Login_ErrorMsg = $"第3个跳转失败";
                    return loginInfo;
                }
            }

            #endregion

            #region 获取登录权限

            hi = new WinInet_HttpItem();
            hi.Url = $"https://outlook.live.com/owa/0/startupdata.ashx?app=Mail&n=0";
            hi.HttpMethod = WinInet_HttpMethod.POST;
            hi.Cookie = loginInfo.Cookies_Browser_Str;
            hi.Referrer = "https://outlook.live.com";

            hi.OtherHeaders.Add(new WinInet_Header() { Name = $"Origin", Value = $"https://outlook.live.com" });
            hi.OtherHeaders.Add(new WinInet_Header()
                { Name = $"Content-Type", Value = $"application/x-www-form-urlencoded" });

            hi.OtherHeaders.Add(new WinInet_Header() { Name = $"action", Value = $"StartupData" });

            ckFind = loginInfo.Cookies_Browser.Where(ck => ck.Name.ToString().ToLower() == "x-owa-canary")
                .FirstOrDefault();
            x_owa_canary = ckFind == null ? string.Empty : ckFind.Value.ToString().Trim();
            if (string.IsNullOrEmpty(x_owa_canary)) x_owa_canary = "X-OWA-CANARY_cookie_is_null_or_empty";

            hi.OtherHeaders.Add(new WinInet_Header() { Name = $"x-owa-canary", Value = x_owa_canary });

            hr = hh.GetHtml(hi);

            //Cookie合并更新
            loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);

            Console.WriteLine($"获取登录权限 model.Html >>> {hr.HtmlString}");

            #endregion

            try
            {
                userInfo = JObject.Parse(hr.HtmlString);
                var startupdata = JsonConvert.DeserializeObject<OutlookStartupdata>(hr.HtmlString);
                loginInfo.EmailAccount = startupdata.owaUserConfig.SessionSettings.LogonEmailAddress;
            }
            catch
            {
            }

            if (userInfo == null) return loginInfo;
            if (string.IsNullOrEmpty(hr.HtmlString))
            {
                loginInfo.Login_ErrorMsg = $"获取访问API需要的用户登录关键数据失败";
                return loginInfo;
            }

            //保存登录数据
            loginInfo.Login_Success = true;
            loginInfo.Login_ErrorMsg = "登录成功";
            loginInfo.UserInfo_JObject = userInfo;
            loginInfo.UserInfo_JsonStr = hr.HtmlString;

            //x-owa-canary(这里需要重新获取，保存)
            x_owa_canary = string.Empty;
            ckFind = loginInfo.Cookies_Browser.Where(ck => ck.Name.ToString().ToLower() == "x-owa-canary")
                .FirstOrDefault();
            x_owa_canary = ckFind == null ? string.Empty : ckFind.Value.ToString().Trim();
            loginInfo.Postdata_X_OWA_CANARY = x_owa_canary;

            //获取邮件列表的参数
            jsonPath = "findFolders.Header.ServerVersionInfo.Version";
            if (userInfo.SelectToken(jsonPath) != null)
                loginInfo.Postdata_FindConversation_Header_RequestServerVersion =
                    userInfo.SelectToken(jsonPath).ToString().Trim();
            jsonPath = "findFolders.Body.ResponseMessages.Items[0].RootFolder.Folders[0].FolderId.Id";
            if (userInfo.SelectToken(jsonPath) != null)
                loginInfo.Postdata_FindConversation_Body_ParentFolderId_BaseFolderId_Id =
                    userInfo.SelectToken(jsonPath).ToString().Trim();
            jsonPath = "findConversation.Body.SearchFolderId.Id";
            if (userInfo.SelectToken(jsonPath) != null)
                loginInfo.Postdata_FindConversation_Body_SearchFolderId_Id =
                    userInfo.SelectToken(jsonPath).ToString().Trim();

            //查询邮件内容的参数
            jsonPath = "findFolders.Body.ResponseMessages.Items[0].RootFolder.Folders";
            if (userInfo.SelectToken(jsonPath) != null)
            {
                jt_Find = userInfo.SelectToken(jsonPath);
                //FolderClass : "IPF.Journal"
                jt_Find = jt_Find
                    .Where(jt => jt["FolderClass"] != null && jt["FolderClass"].ToString().Trim() == "IPF.Journal")
                    .FirstOrDefault();
                if (jt_Find != null)
                {
                    jsonPath = "FolderId.Id";
                    if (jt_Find.SelectToken(jsonPath) != null)
                        loginInfo.Postdata_GetConversationItems_FolderId_Id =
                            jt_Find.SelectToken(jsonPath).ToString().Trim();

                    jsonPath = "ParentFolderId.Id";
                    if (jt_Find.SelectToken(jsonPath) != null)
                        loginInfo.Postdata_GetConversationItems_ParentFolderId_Id =
                            jt_Find.SelectToken(jsonPath).ToString().Trim();
                }
            }

            return loginInfo;
        }

        public static bool LogOut(OutlookMail_LoginInfo loginInfo, out string errorMsg)
        {
            bool isSuccess = false;
            errorMsg = string.Empty;

            WinInet_HttpHelper hh = new WinInet_HttpHelper();
            WinInet_HttpItem hi = null;
            WinInet_HttpResult hr = null;
            string nextUrl = string.Empty;
            // hh.UserAgent_Global =
            //     linkedin.Ua == null ? CreateUa() : linkedin.Ua; //设置当前WinInet_HttpHelper对象下使用的局部UserAgent

            #region 请求注销连接（第1个连接）

            hi = new WinInet_HttpItem();
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.AcceptEncoding = $"gzip, deflate, br";
            hi.AcceptLanguage = "zh-CN,zh;q=0.9";

            hi.Url = $"https://outlook.live.com/owa/logoff.owa";
            hi.Cookie = loginInfo.Cookies_Browser_Str;

            hr = hh.GetHtml(hi);

            //<html><head></head><body>.<script type="text/javascript">
            //window.location.href = "https://login.live.com/logout.srf?ct=1710923499&rver=7.0.6738.0&id=292841&ru=https:%2F%2Foutlook.live.com%2Fowa%2Fcsignout.aspx%3F%253f%253fumkt%3Des-ES%26exch%3D1%26RpsCsrfState%3D456c87fa-d751-142f-b80f-02fa128fe264";
            //</script></body></html>
            if (!hr.HtmlString.Contains("window.location.href") ||
                !hr.HtmlString.Contains("https://login.live.com/logout.srf?"))
            {
                loginInfo.Login_ErrorMsg = $"注销失败(第1连接请求失败)";
                return isSuccess;
            }

            //Cookie合并更新
            loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);

            #endregion

            #region 请求第2个连接

            nextUrl =
                $"https://login.live.com/logout.srf?{GetMidStr(hr.HtmlString, "https://login.live.com/logout.srf?", "\"")}";

            hi = new WinInet_HttpItem();
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.AcceptEncoding = $"gzip, deflate, br";
            hi.AcceptLanguage = "zh-CN,zh;q=0.9";

            hi.Url = nextUrl;
            hi.Cookie = loginInfo.Cookies_Browser_Str;

            hr = hh.GetHtml(hi);

            //Location: https://account.live.com/logout.aspx?
            if (string.IsNullOrEmpty(hr.LocationUrl))
            {
                loginInfo.Login_ErrorMsg = $"注销失败(第2连接请求失败)";
                return isSuccess;
            }

            //Cookie合并更新
            loginInfo.Cookies_Browser = WinInet_HttpHelper.UpdateCookies(loginInfo.Cookies_Browser, hr.Cookies);

            #endregion

            //判断是否注销成功
            isSuccess = hr.LocationUrl.Contains("https://account.live.com/logout.aspx?");
            errorMsg = $"注销{(isSuccess ? "成功" : "失败")}";

            return isSuccess;
        }

        static Random _rand = new Random();

        public static string CreateUa()
        {
            string[] strArray1 =
            {
                "WOW64",
                "Win64;x64"
            };
            string[] strArray2 = new string[13];
            strArray2[0] = "Mozilla/5.0 (Windows NT ";
            int num = _rand.Next(6, 10);
            strArray2[1] = num.ToString();
            strArray2[2] = ".";
            num = _rand.Next(0, 9);
            strArray2[3] = num.ToString();
            strArray2[4] = "; ";
            strArray2[5] = strArray1[_rand.Next(0, 1)];
            strArray2[6] = ") AppleWebKit/537.36 (KHTML, like Gecko) Chrome/";
            num = _rand.Next(76, 96);
            strArray2[7] = num.ToString();
            strArray2[8] = ".0.";
            num = _rand.Next(1000, 5000);
            strArray2[9] = num.ToString();
            strArray2[10] = ".";
            num = _rand.Next(0, 124);
            strArray2[11] = num.ToString();
            strArray2[12] = " Safari/537.36";
            string str = string.Concat(strArray2);
            return str;
            //     string[] mobileDevices =
            //     {
            //         "iPhone", "iPad", "iPod", "Android", "Windows Phone", "BlackBerry"
            //     };
            //
            //     string[] mobileBrowsers =
            //     {
            //         "Mobile Safari", "Android WebKit", "Windows Phone", "BlackBerry"
            //     };
            //
            //     Random random = new Random();
            //     string device = mobileDevices[random.Next(mobileDevices.Length)];
            //     string browser = mobileBrowsers[random.Next(mobileBrowsers.Length)];
            //     string version = $"{random.Next(1, 10)}.{random.Next(0, 10)}";
            //
            //     return
            //         $"Mozilla/5.0 ({device}; CPU OS {version} like Mac OS X) AppleWebKit/601.1.46 (KHTML, like Gecko) Version/{version} {browser}/{version}";
        }

        public static List<Mail> Email_GetOutlookList(string cookie, string ua)
        {
            List<Mail> mailList = new List<Mail>();
            try
            {
                var outlookMailLoginInfo1 = GetLoginInfo(cookie, ua);

                string aaa = "";
                var outlookMailLoginInfo =
                    GetOutlookMailList(outlookMailLoginInfo1, ua, out aaa, 0, 25);
                foreach (var outlookMailInfo in outlookMailLoginInfo)
                {
                    var sender = outlookMailInfo.sender;
                    var preview = outlookMailInfo.Mail_PreviewContent;
                    var title = outlookMailInfo.Main_Title;
                    var id__ = outlookMailInfo.Mail_Id;
                    var listCookie = outlookMailLoginInfo1.Cookies_Browser;
                    var Cookies_Browser_Str = outlookMailLoginInfo1.Cookies_Browser_Str;
                    var Postdata_X_OWA_CANARY = outlookMailLoginInfo1.Postdata_X_OWA_CANARY;
                    mailList.Add(new Mail
                    {
                        sender = sender,
                        preview = preview,
                        title = title,
                        id = id__,
                        Main_TotalData_JsonStr = outlookMailInfo.Main_TotalData_JsonStr,
                        Cookies_Browser = listCookie,
                        Cookies_Browser_Str = Cookies_Browser_Str,
                        Postdata_X_OWA_CANARY = Postdata_X_OWA_CANARY
                    });
                }
            }
            catch
            {
                //log.Error(ex.Message); 
            }

            return mailList;
        }

        /// <summary>
        /// 获取邮件列表
        /// </summary>
        /// <param name="loginInfo">登录信息</param>
        /// <param name="pageIndex">页索引，0开始</param>
        /// <param name="pageSize">每页数量，默认25</param>
        /// <returns></returns>
        public static List<OutlookMailInfo> GetOutlookMailList(OutlookMail_LoginInfo loginInfo,
            string ua, out string errorMsg,
            int pageIndex = 0, int pageSize = 25)
        {
            List<OutlookMailInfo> mailInfos = new List<OutlookMailInfo>();
            errorMsg = string.Empty;

            JToken jt_Find;
            JObject jo_x_owa_urlpostdata = null;
            string x_owa_urlpostdata;

            WinInet_HttpHelper hh = new WinInet_HttpHelper();
            WinInet_HttpItem hi = null;
            WinInet_HttpResult hr = null;
            hh.UserAgent_Global = ua; //设置当前WinInet_HttpHelper对象下使用的局部UserAgent
            // if (LinkedinProxy.Setting.EmailProxy)
            // {
            //     hh.ProxyInfo_Global = GetProxyInfo(proxy);
            // }

            hi = new WinInet_HttpItem();
            hi.Accept = $"*/*";
            hi.AcceptEncoding = $"gzip, deflate, br";
            hi.AcceptLanguage = "zh-CN,zh;q=0.9";

            hi.Url = $"https://outlook.live.com/owa/0/service.svc?action=FindConversation&app=Mail";
            hi.Cookie = loginInfo.Cookies_Browser_Str;

            hi.OtherHeaders.Add(new WinInet_Header() { Name = $"action", Value = $"FindConversation" });
            hi.OtherHeaders.Add(
                new WinInet_Header() { Name = $"x-owa-canary", Value = loginInfo.Postdata_X_OWA_CANARY });

            //x-owa-urlpostdata
            string postdata =
                "{\"__type\":\"FindConversationJsonRequest:#Exchange\",\"Header\":{\"__type\":\"JsonRequestHeaders:#Exchange\",\"RequestServerVersion\":\"V2018_01_08\",\"TimeZoneContext\":{\"__type\":\"TimeZoneContext:#Exchange\",\"TimeZoneDefinition\":{\"__type\":\"TimeZoneDefinitionType:#Exchange\",\"Id\":\"SA Pacific Standard Time\"}}},\"Body\":{\"ParentFolderId\":{\"__type\":\"TargetFolderId:#Exchange\",\"BaseFolderId\":{\"__type\":\"FolderId:#Exchange\",\"Id\":\"AQMkADAwATY0MDABLWM3MmEtZmVjYi0wMAItMDAKAC4AAAOVL4AtofOUTYeGMzMAemPMggEAptylQ92uakeklXSM4qW32QAAAgEMAAAA\"}},\"ConversationShape\":{\"__type\":\"ConversationResponseShape:#Exchange\",\"BaseShape\":\"IdOnly\"},\"ShapeName\":\"ReactConversationListView\",\"Paging\":{\"__type\":\"IndexedPageView:#Exchange\",\"BasePoint\":\"Beginning\",\"Offset\":0,\"MaxEntriesReturned\":25},\"ViewFilter\":\"All\",\"SortOrder\":[{\"__type\":\"SortResults:#Exchange\",\"Order\":\"Descending\",\"Path\":{\"__type\":\"PropertyUri:#Exchange\",\"FieldURI\":\"ConversationLastDeliveryOrRenewTime\"}},{\"__type\":\"SortResults:#Exchange\",\"Order\":\"Descending\",\"Path\":{\"__type\":\"PropertyUri:#Exchange\",\"FieldURI\":\"ConversationLastDeliveryTime\"}}],\"FocusedViewFilter\":0}}";
            jo_x_owa_urlpostdata = JObject.Parse(postdata);
            //jo_x_owa_urlpostdata["Header"]["RequestServerVersion"] = loginInfo.Postdata_FindConversation_Header_RequestServerVersion;
            jo_x_owa_urlpostdata["Body"]["ParentFolderId"]["BaseFolderId"]["Id"] =
                loginInfo.Postdata_FindConversation_Body_ParentFolderId_BaseFolderId_Id;
            //jo_x_owa_urlpostdata["Body"]["SearchFolderId"]["Id"] = loginInfo.Postdata_FindConversation_Body_SearchFolderId_Id;
            //设置页码pageIndex
            jo_x_owa_urlpostdata["Body"]["Paging"]["Offset"] = pageIndex * pageSize;
            //设置页的记录数量pageSize
            jo_x_owa_urlpostdata["Body"]["Paging"]["MaxEntriesReturned"] = pageSize;

            x_owa_urlpostdata = HttpUtility.UrlEncode(JsonConvert.SerializeObject(jo_x_owa_urlpostdata));

            //处理json值有空格的情况(不处理的话，真的提交不上)
            jt_Find = jo_x_owa_urlpostdata.SelectToken("Header.TimeZoneContext.TimeZoneDefinition.Id");
            x_owa_urlpostdata = x_owa_urlpostdata.Replace(HttpUtility.UrlEncode(jt_Find.ToString().Trim()),
                jt_Find.ToString().Trim());

            hi.OtherHeaders.Add(new WinInet_Header() { Name = $"x-owa-urlpostdata", Value = x_owa_urlpostdata });

            hr = hh.GetHtml(hi);

            Console.WriteLine($"获取邮件列表[pageIndex:{pageIndex + 1},pageSize:{pageSize}] model.Html >>> {hr.HtmlString}");

            JObject joResult = null;
            try
            {
                joResult = JObject.Parse(hr.HtmlString);
            }
            catch
            {
            }

            if (joResult == null || joResult.SelectToken("Body.Conversations") == null)
            {
                errorMsg = $"获取邮件列表失败";
                return mailInfos;
            }

            //整理邮件列表信息
            JToken jtMails = joResult.SelectToken("Body.Conversations");
            mailInfos = jtMails.Select(jt =>
            {
                OutlookMailInfo mInfo = new OutlookMailInfo();
                // item.UniqueSenders[0]
                mInfo.sender = jt.SelectToken("UniqueSenders")[0].ToString().Trim();
                string jPath;
                //邮件ID
                jPath = "ConversationId.Id";
                if (jt.SelectToken(jPath) != null) mInfo.Mail_Id = jt.SelectToken(jPath).ToString().Trim();
                //邮件标题
                jPath = "ConversationTopic";
                mInfo.Main_Title = jt.SelectToken(jPath).ToString().Trim();
                //是否已读
                jPath = "GlobalUnreadCount";
                mInfo.IsReaded = jt.SelectToken(jPath).ToString().Trim() == "0"; //0是已读，1是未读
                //邮件日期
                jPath = "LastDeliveryTime";
                string jValue = jt.SelectToken(jPath).ToString().Trim();
                int sIndex = jValue.LastIndexOf("-");
                if (sIndex > -1)
                {
                    DateTime dt;
                    if (DateTime.TryParse(jValue.Substring(0, sIndex), out dt)) mInfo.Main_RevDate = dt;
                }

                //预览内容(这个要用响应原文来取)
                jPath = "Preview";
                mInfo.Mail_PreviewContent = jt.SelectToken(jPath).ToString().Trim();

                //原始数据(Json字符串，按需要获取，会吃内存哦)
                mInfo.Main_TotalData_JsonStr = jt.ToString();

                return mInfo;
            }).Where(m => m != null).ToList();

            return mailInfos;
        }

        /// <summary>
        /// 获取邮件内容
        /// </summary>
        /// <param name="loginInfo">登录信息</param>
        /// <param name="outlookMail">邮件信息</param>
        /// <returns></returns>
        public static void GetOutlookMailHtmlDetail(OutlookMailLoginInfo loginInfo, OutlookMailInfo outlookMail,
            out string errorMsg)
        {
            errorMsg = string.Empty;

            WinInet_HttpHelper hh = new WinInet_HttpHelper();
            WinInet_HttpItem hi = null;
            WinInet_HttpResult hr = null;
            // hh.UserAgent_Global =  linkedin.Ua == null ? CreateUa() : linkedin.Ua;//设置当前WinInet_HttpHelper对象下使用的局部UserAgent

            hi = new WinInet_HttpItem();
            hi.Accept = $"*/*";
            hi.AcceptEncoding = $"gzip, deflate, br";
            hi.AcceptLanguage = "zh-CN,zh;q=0.9";

            hi.Url = $"https://outlook.live.com/owa/0/service.svc?action=GetConversationItems&app=Mail";

            hi.HttpMethod = WinInet_HttpMethod.POST;
            //postdata
            string postdata =
                "{\"__type\":\"GetConversationItemsJsonRequest:#Exchange\",\"Header\":{\"__type\":\"JsonRequestHeaders:#Exchange\",\"RequestServerVersion\":\"V2017_08_18\",\"TimeZoneContext\":{\"__type\":\"TimeZoneContext:#Exchange\",\"TimeZoneDefinition\":{\"__type\":\"TimeZoneDefinitionType:#Exchange\",\"Id\":\"SA Pacific Standard Time\"}}},\"Body\":{\"__type\":\"GetConversationItemsRequest:#Exchange\",\"Conversations\":[{\"__type\":\"ConversationRequestType:#Exchange\",\"ConversationId\":{\"__type\":\"ItemId:#Exchange\",\"Id\":\"AQQkADAwATY0MDABLWM3MmEtZmVjYi0wMAItMDAKABAA1/b5Bqh+UUOR7cUKx1n84w==\"},\"SyncState\":\"\"}],\"ItemShape\":{\r\n\"__type\":\"ItemResponseShape:#Exchange\",\"BaseShape\":\"IdOnly\",\"AddBlankTargetToLinks\":true,\"BlockContentFromUnknownSenders\":false,\"BlockExternalImagesIfSenderUntrusted\":true,\"ClientSupportsIrm\":true,\"CssScopeClassName\":\"\",\"FilterHtmlContent\":true,\"FilterInlineSafetyTips\":true,\"InlineImageCustomDataTemplate\":\"{id}\",\"InlineImageUrlTemplate\":\"\",\"MaximumBodySize\":0,\"MaximumRecipientsToReturn\":20,\"ImageProxyCapability\":\"OwaAndConnectorsProxy\",\"AdditionalProperties\":[\r\n\r\n],\"InlineImageUrlOnLoadTemplate\":\"\",\"ExcludeBindForInlineAttachments\":true,\"CalculateOnlyFirstBody\":true,\"BodyShape\":\"UniqueFragment\"\r\n},\"ShapeName\":\"ItemPart\",\"SortOrder\":\"DateOrderDescending\",\"MaxItemsToReturn\":20,\"Action\":\"ReturnRootNode\",\"FoldersToIgnore\":[{\"__type\":\"FolderId:#Exchange\",\"Id\":\"AQMkADAwATY0MDABLWM3MmEtZmVjYi0wMAItMDAKAC4AAAOVL4AtofOUTYeGMzMAemPMggEAptylQ92uakeklXSM4qW32QAAAgEQAAAA\"},{\"__type\":\"FolderId:#Exchange\",\"Id\":\"AQMkADAwATY0MDABLWM3MmEtZmVjYi0wMAItMDAKAC4AAAOVL4AtofOUTYeGMzMAemPMggEAptylQ92uakeklXSM4qW32QAAAgELAAAA\"}],\"ReturnSubmittedItems\":true,\"ReturnDeletedItems\":true}}";
            JObject jo_postdata = JObject.Parse(postdata);
            jo_postdata["Body"]["Conversations"][0]["ConversationId"]["Id"] = outlookMail.Mail_Id;
            jo_postdata["Body"]["FoldersToIgnore"][0]["Id"] = loginInfo.Postdata_GetConversationItems_FolderId_Id;
            jo_postdata["Body"]["FoldersToIgnore"][1]["Id"] = loginInfo.Postdata_GetConversationItems_ParentFolderId_Id;
            hi.PostDataType = WinInet_PostDataType.Byte;
            hi.PostBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jo_postdata));

            //cookie
            hi.Cookie = loginInfo.Cookies_Browser_Str;

            hi.OtherHeaders.Add(new WinInet_Header() { Name = $"action", Value = $"GetConversationItems" });
            hi.OtherHeaders.Add(new WinInet_Header()
                { Name = $"Content-Type", Value = $"application/json; charset=utf-8" });
            hi.OtherHeaders.Add(
                new WinInet_Header() { Name = $"x-owa-canary", Value = loginInfo.Postdata_X_OWA_CANARY });

            //提交
            hr = hh.GetHtml(hi);

            string jtPath =
                "Body.ResponseMessages.Items[0].Conversation.ConversationNodes[0].Items[0].UniqueBody.Value";
            JObject joResult = null;
            try
            {
                joResult = JObject.Parse(hr.HtmlString);
            }
            catch
            {
            }

            if (joResult == null || joResult.SelectToken(jtPath) == null)
            {
                errorMsg = $"获取邮件Html失败";
                return;
            }

            outlookMail.Mail_HtmlContent = joResult.SelectToken(jtPath).ToString().Trim();
        }

        #region 辅助方法

        /// <summary>
        /// 从指定位置开始寻找文本
        /// </summary>
        /// <param name="index">开始搜索的位置</param>
        /// <returns>返回在源文本中的索引位置</returns>
        public static int FindStrAfterIndex(string sourceStr, string findStr, int index)
        {
            if (sourceStr.Length == 0 || findStr.Length == 0 || index < 0 || index > sourceStr.Length - 1) return -1;
            string partOfSourceStr = sourceStr.Substring(index, sourceStr.Length - index);
            if (partOfSourceStr.Length == 0) return -1;
            int result = partOfSourceStr.IndexOf(findStr);
            return result == -1 ? -1 : result + index;
        }

        /// <summary>
        /// 寻找中间文本
        /// </summary>
        /// <param name="sourceStr">源文本</param>
        /// <param name="frontStr">前面的文本</param>
        /// <param name="backStr">后面的文本</param>
        /// <returns>返回两个文本中间的部分</returns>
        public static string GetMidStr(string sourceStr, string frontStr, string backStr)
        {
            if (sourceStr.Length == 0) return string.Empty;
            int frontIndex = sourceStr.IndexOf(frontStr);
            int backIndex = FindStrAfterIndex(sourceStr, backStr, frontIndex + frontStr.Length);
            int midStrIndex = 0;
            int midStrLength = 0;
            if (frontIndex > -1 && backIndex > -1)
            {
                midStrIndex = frontIndex + frontStr.Length;
                midStrLength = backIndex - midStrIndex;
                return sourceStr.Substring(midStrIndex, midStrLength);
            }
            else return string.Empty;
        }

        /// <summary>
        /// 在指定位置开始，寻找中间文本
        /// </summary>
        /// <param name="sourceStr">源文本</param>
        /// <param name="frontStr">前面的文本</param>
        /// <param name="backStr">后面的文本</param>
        /// <param name="index">指定位置开始寻找</param>
        /// <returns>返回两个文本中间的部分</returns>
        public static string GetMidStr(string sourceStr, string frontStr, string backStr, int index)
        {
            if (sourceStr.Length == 0) return string.Empty;
            if (index > -1 && index < sourceStr.Length) sourceStr = sourceStr.Substring(index);
            else return string.Empty;
            int frontIndex = sourceStr.IndexOf(frontStr);
            int backIndex = FindStrAfterIndex(sourceStr, backStr, frontIndex + frontStr.Length);
            int midStrIndex = 0;
            int midStrLength = 0;
            if (frontIndex > -1 && backIndex > -1)
            {
                midStrIndex = frontIndex + frontStr.Length;
                midStrLength = backIndex - midStrIndex;
                return sourceStr.Substring(midStrIndex, midStrLength);
            }
            else return string.Empty;
        }

        /// <summary>
        /// 批量寻找中间文本
        /// </summary>
        /// <param name="sourceStr">源文本</param>
        /// <param name="frontStr">前面的文本</param>
        /// <param name="backStr">后面的文本</param>
        /// <returns></returns>
        public static List<string> GetMidStrList(string sourceStr, string frontStr, string backStr)
        {
            List<string> list = new List<string>();
            if (sourceStr.Length > 0)
            {
                int SearchIndex = 0;
                string resultStr = string.Empty;
                while (SearchIndex > -1 && SearchIndex < sourceStr.Length)
                {
                    resultStr = GetMidStr(sourceStr, frontStr, backStr, SearchIndex);
                    if (resultStr == string.Empty)
                    {
                        int frontIndex = sourceStr.IndexOf(frontStr, SearchIndex);
                        int backIndex = FindStrAfterIndex(sourceStr, backStr, frontIndex + frontStr.Length);
                        if (frontIndex == -1 || backIndex == -1 || frontIndex > backIndex) break;
                    }

                    list.Add(resultStr);
                    string tempstr = frontStr + resultStr + backStr;
                    SearchIndex = FindStrAfterIndex(sourceStr, tempstr, SearchIndex) + tempstr.Length;
                }
            }

            return list;
        }

        /// <summary>
        /// 从指定位置开始,批量寻找中间文本
        /// </summary>
        /// <param name="sourceStr">源文本</param>
        /// <param name="frontStr">前面的文本</param>
        /// <param name="backStr">后面的文本</param>
        /// <param name="index">从指定位置开始</param>
        /// <returns></returns>
        public static List<string> GetMidStrList(string sourceStr, string frontStr, string backStr, int index)
        {
            List<string> list = new List<string>();
            if (sourceStr.Length > 0)
            {
                int SearchIndex = 0;
                if (index > -1 && index < sourceStr.Length) SearchIndex = index;
                string resultStr = string.Empty;
                while (SearchIndex > -1 && SearchIndex < sourceStr.Length)
                {
                    resultStr = GetMidStr(sourceStr, frontStr, backStr, SearchIndex);
                    if (resultStr == string.Empty)
                    {
                        int frontIndex = sourceStr.IndexOf(frontStr, SearchIndex);
                        int backIndex = FindStrAfterIndex(sourceStr, backStr, frontIndex + frontStr.Length);
                        if (frontIndex == -1 || backIndex == -1 || frontIndex > backIndex) break;
                    }

                    list.Add(resultStr);
                    SearchIndex = FindStrAfterIndex(sourceStr, backStr, SearchIndex) + backStr.Length;
                }
            }

            return list;
        }

        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors errors)
        {
            return true;
        }

        private static int GetUnixTimeStamp(DateTime dateTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long unixTimeStampInTicks = (dateTime.ToUniversalTime() - unixStart).Ticks;
            return (int)(unixTimeStampInTicks / TimeSpan.TicksPerSecond);
        }

        private static DateTime GetDateTimeFromUnixTimeStamp(int unixTimeStamp)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long timeStampInTicks = unixTimeStamp * TimeSpan.TicksPerSecond;
            return new DateTime(unixStart.Ticks + timeStampInTicks, DateTimeKind.Utc);
        }

        private static JArray GetNewCookie(JArray oldCookies, CookieCollection newCookies)
        {
            JArray jaNewCookies = new JArray();

            //复制旧Ck
            for (int i = 0; i < oldCookies.Count; i++)
            {
                jaNewCookies.Add(oldCookies[i]);
            }

            //合并新CK
            if (newCookies != null)
            {
                foreach (Cookie ck in newCookies)
                {
                    JToken jt = jaNewCookies
                        .Where(jta => jta["name"].ToString() == ck.Name && jta["domain"].ToString() == ck.Domain)
                        .FirstOrDefault();
                    if (jt == null)
                    {
                        jt = new JObject();
                        jt["name"] = ck.Name;
                        jt["value"] = ck.Value;
                        jt["domain"] = ck.Domain;
                        jt["path"] = ck.Path;
                        jt["secure"] = ck.Secure;
                        jt["httpOnly"] = ck.HttpOnly;
                        jt["sameSite"] = "unspecified";
                        jt["expiry"] = GetUnixTimeStamp(ck.Expires);

                        jaNewCookies.Add(jt);
                    }
                    else jt["value"] = ck.Value;
                }
            }

            return jaNewCookies;
        }

        static CookieCollection GetCookiesByHeaderString(string responseHeaders)
        {
            CookieCollection cookies = new CookieCollection();

            if (string.IsNullOrEmpty(responseHeaders)) return cookies;

            // 按行拆分响应头
            string[] headerLines = responseHeaders.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            // 遍历每一行
            foreach (string line in headerLines)
            {
                // 查找 Set-Cookie 头
                if (line.StartsWith("Set-Cookie:"))
                {
                    // 提取 Cookie 字符串
                    string cookieString = line.Substring("Set-Cookie:".Length).Trim();

                    // 拆分成多个 Cookie 字符串（如果有多个）
                    string[] cookieParts = cookieString.Split(';');

                    // 解析每个 Cookie 字符串并添加到 CookieCollection 中
                    Cookie cookie = new Cookie();
                    foreach (string part in cookieParts)
                    {
                        string[] keyValue = part.Split(new char[] { '=' }, 2);
                        string key = keyValue[0].Trim();
                        string value = keyValue.Length > 1 ? keyValue[1].Trim() : null;

                        switch (key.ToLower())
                        {
                            case "expires":
                                if (!string.IsNullOrEmpty(value))
                                    cookie.Expires = DateTime.Parse(value);
                                break;
                            case "path":
                                cookie.Path = value;
                                break;
                            case "domain":
                                cookie.Domain = value;
                                break;
                            case "secure":
                                cookie.Secure = true;
                                break;
                            case "httponly":
                                cookie.HttpOnly = true;
                                break;
                            case "samesite":
                                break;
                            default:
                                cookie.Name = key;
                                cookie.Value = value;
                                break;
                        }
                    }

                    cookies.Add(cookie);
                }
            }

            return cookies;
        }

        /// <summary>
        /// 永久删除
        /// </summary>
        /// <param name="loginInfo">登录信息</param>
        /// <param name="outlookMail">邮件信息</param>
        /// <param name="errorMsg"></param>
        public static bool DeleteMailInfo(OutlookMail_LoginInfo loginInfo, OutlookMailInfo outlookMail, string Ua,
            out string errorMsg)
        {
            string JsonStr_x_owa_urlpostdata_MoveToDeletedItems =
                "{\"__type\":\"DeleteItemJsonRequest:#Exchange\",\"Header\":{\"__type\":\"JsonRequestHeaders:#Exchange\",\"RequestServerVersion\":\"V2018_01_08\",\"TimeZoneContext\":{\"__type\":\"TimeZoneContext:#Exchange\",\"TimeZoneDefinition\":{\"__type\":\"TimeZoneDefinitionType:#Exchange\",\"Id\":\"E. South America Standard Time\"}}},\"Body\":{\"__type\":\"DeleteItemRequest:#Exchange\",\"ItemIds\":[{\"__type\":\"ItemId:#Exchange\",\"Id\":\"\"}],\"DeleteType\":\"MoveToDeletedItems\",\"SuppressReadReceipts\":true,\"ReturnMovedItemIds\":true,\"SendMeetingCancellations\":\"SendToNone\",\"AffectedTaskOccurrences\":\"AllOccurrences\"}}";
            string JsonStr_x_owa_urlpostdata_SoftDelete =
                "{\"__type\":\"DeleteItemJsonRequest:#Exchange\",\"Header\":{\"__type\":\"JsonRequestHeaders:#Exchange\",\"RequestServerVersion\":\"V2018_01_08\",\"TimeZoneContext\":{\"__type\":\"TimeZoneContext:#Exchange\",\"TimeZoneDefinition\":{\"__type\":\"TimeZoneDefinitionType:#Exchange\",\"Id\":\"E. South America Standard Time\"}}},\"Body\":{\"__type\":\"DeleteItemRequest:#Exchange\",\"ItemIds\":[{\"__type\":\"ItemId:#Exchange\",\"Id\":\"\"}],\"DeleteType\":\"SoftDelete\",\"SuppressReadReceipts\":true,\"ReturnMovedItemIds\":true,\"SendMeetingCancellations\":\"SendToNone\",\"AffectedTaskOccurrences\":\"AllOccurrences\"}}";
            bool isSuccess = false;
            errorMsg = string.Empty;

            WinInet_HttpHelper hh = new WinInet_HttpHelper();
            WinInet_HttpItem hi = null;
            WinInet_HttpResult hr = null;
            hh.UserAgent_Global = Ua == null ? CreateUa() : Ua; //设置当前WinInet_HttpHelper对象下使用的局部UserAgent

            JObject jo_Mail_TotalData = null;
            JToken jt_ItemId = null;
            JObject jo_postdata = null;
            JObject joResult = null;
            string jtPath = string.Empty;
            string mailId_Changed = string.Empty;
            JToken jt_Find;
            string x_owa_urlpostdata;

            #region 移动到回收站

            hi = new WinInet_HttpItem();
            hi.Accept = $"*/*";
            hi.AcceptEncoding = $"gzip, deflate, br";
            hi.AcceptLanguage = "zh-CN,zh;q=0.9";

            hi.Url = $"https://outlook.live.com/owa/0/service.svc?action=DeleteItem&app=Mail";

            hi.HttpMethod = WinInet_HttpMethod.POST;
            hi.ContentType = $"application/json; charset=utf-8";

            //postdata
            try
            {
                jo_Mail_TotalData = JObject.Parse(outlookMail.Main_TotalData_JsonStr);
                if (jo_Mail_TotalData != null) jt_ItemId = jo_Mail_TotalData.SelectToken("ItemIds[0].Id");
            }
            catch
            {
            }

            if (jt_ItemId == null || string.IsNullOrEmpty(jt_ItemId.ToString().Trim()))
            {
                errorMsg = "删除失败(邮件Id提取失败)";
                return isSuccess;
            }

            jo_postdata = JObject.Parse(JsonStr_x_owa_urlpostdata_MoveToDeletedItems);
            jo_postdata["Body"]["ItemIds"][0]["Id"] = jt_ItemId.ToString().Trim();
            x_owa_urlpostdata = HttpUtility.UrlEncode(JsonConvert.SerializeObject(jo_postdata));

            //处理json值有空格的情况(不处理的话，真的提交不上)
            jt_Find = jo_postdata.SelectToken("Header.TimeZoneContext.TimeZoneDefinition.Id");
            x_owa_urlpostdata = x_owa_urlpostdata.Replace(HttpUtility.UrlEncode(jt_Find.ToString().Trim()),
                jt_Find.ToString().Trim());

            //cookie
            hi.Cookie = loginInfo.Cookies_Browser_Str;

            hi.OtherHeaders.Add(new WinInet_Header() { Name = $"x-owa-urlpostdata", Value = $"{x_owa_urlpostdata}" });
            hi.OtherHeaders.Add(new WinInet_Header() { Name = $"action", Value = $"DeleteItem" });
            hi.OtherHeaders.Add(
                new WinInet_Header() { Name = $"x-owa-canary", Value = loginInfo.Postdata_X_OWA_CANARY });

            //提交
            hr = hh.GetHtml(hi);

            jtPath = "Body.ResponseMessages.Items[0].MovedItemId.Id";
            try
            {
                joResult = JObject.Parse(hr.HtmlString);
            }
            catch
            {
            }

            if (joResult == null || joResult.SelectToken(jtPath) == null)
            {
                errorMsg = $"删除失败(移动到回收站失败)";
                return isSuccess;
            }

            mailId_Changed = joResult.SelectToken(jtPath).ToString().Trim();

            #endregion

            #region 永久删除

            hi = new WinInet_HttpItem();
            hi.Accept = $"*/*";
            hi.AcceptEncoding = $"gzip, deflate, br";
            hi.AcceptLanguage = "zh-CN,zh;q=0.9";

            hi.Url = $"https://outlook.live.com/owa/0/service.svc?action=DeleteItem&app=Mail";

            hi.HttpMethod = WinInet_HttpMethod.POST;
            hi.ContentType = $"application/json; charset=utf-8";

            //postdata
            jo_postdata = JObject.Parse(JsonStr_x_owa_urlpostdata_SoftDelete);
            jo_postdata["Body"]["ItemIds"][0]["Id"] = mailId_Changed;
            x_owa_urlpostdata = HttpUtility.UrlEncode(JsonConvert.SerializeObject(jo_postdata));

            //处理json值有空格的情况(不处理的话，真的提交不上)
            jt_Find = jo_postdata.SelectToken("Header.TimeZoneContext.TimeZoneDefinition.Id");
            x_owa_urlpostdata = x_owa_urlpostdata.Replace(HttpUtility.UrlEncode(jt_Find.ToString().Trim()),
                jt_Find.ToString().Trim());

            //cookie
            hi.Cookie = loginInfo.Cookies_Browser_Str;

            hi.OtherHeaders.Add(new WinInet_Header() { Name = $"x-owa-urlpostdata", Value = $"{x_owa_urlpostdata}" });
            hi.OtherHeaders.Add(new WinInet_Header() { Name = $"action", Value = $"DeleteItem" });
            hi.OtherHeaders.Add(
                new WinInet_Header() { Name = $"x-owa-canary", Value = loginInfo.Postdata_X_OWA_CANARY });

            //提交
            hr = hh.GetHtml(hi);

            jtPath = "Body.ResponseMessages.Items[0].ResponseClass";
            try
            {
                joResult = JObject.Parse(hr.HtmlString);
            }
            catch
            {
            }

            if (joResult == null || joResult.SelectToken(jtPath) == null ||
                joResult.SelectToken(jtPath).ToString().Trim().ToLower() != "success")
            {
                errorMsg = $"删除失败(永久删除失败)";
                return isSuccess;
            }

            #endregion

            isSuccess = true;

            return isSuccess;
        }

        #endregion
    }

    #region 实体类创建

    /// <summary>
    /// 用户登录数据类
    /// </summary>
    public class OutlookMailLoginInfo
    {
        /// <summary>
        /// 登录是否成功
        /// </summary>
        public bool Login_Success { get; set; } = false;

        /// <summary>
        /// 登录错误信息
        /// </summary>
        public string Login_ErrorMsg { get; set; } = string.Empty;

        public string EmailAccount { get; set; } = string.Empty;

        /// <summary>
        /// Json格式的原始Cookie
        /// </summary>
        public string Cookie_Json_Old { get; set; } = string.Empty;

        /// <summary>
        /// 浏览器Cookie的Json格式
        /// </summary>
        public List<Cookie> Cookies_Browser { get; set; } = null;

        /// <summary>
        /// Http请求使用的Cookie
        /// </summary>
        public string Cookies_Browser_Str
        {
            get
            {
                //domain=.login.live.com;
                if (this.Cookies_Browser == null || this.Cookies_Browser.Count == 0) return string.Empty;
                else
                    return string.Join("; ",
                        this.Cookies_Browser.Select(ck => $"{ck.Name}={ck.Value}; domain={ck.Domain}"));
            }
        }

        /// <summary>
        /// 用户登录数据(json对象)
        /// </summary>
        public JObject UserInfo_JObject { get; set; } = null;

        /// <summary>
        /// 用户登录数据(json字符串)
        /// </summary>
        public string UserInfo_JsonStr { get; set; } = string.Empty;

        #region 用户访问API的参数

        public string Postdata_X_OWA_CANARY { get; set; } = string.Empty;
        public string Postdata_FindConversation_Header_RequestServerVersion { get; set; } = string.Empty;
        public string Postdata_FindConversation_Body_ParentFolderId_BaseFolderId_Id { get; set; } = string.Empty;
        public string Postdata_FindConversation_Body_SearchFolderId_Id { get; set; } = string.Empty;

        public string Postdata_GetConversationItems_FolderId_Id { get; set; } = string.Empty;
        public string Postdata_GetConversationItems_ParentFolderId_Id { get; set; } = string.Empty;

        #endregion
    }

    /// <summary>
    /// 邮件信息
    /// </summary>
    public class OutlookMailInfo
    {
        /// <summary>
        /// 邮件ID
        /// </summary>
        public string Mail_Id { get; set; } = string.Empty;

        public string sender { get; set; } = string.Empty;

        /// <summary>
        /// 标题
        /// </summary>
        public string Main_Title { get; set; } = string.Empty;

        /// <summary>
        /// 是否已读
        /// </summary>
        public bool IsReaded { get; set; } = false;

        /// <summary>
        /// 邮件日期
        /// </summary>
        public DateTime Main_RevDate { get; set; }

        /// <summary>
        /// 预览内容
        /// </summary>
        public string Mail_PreviewContent { get; set; } = string.Empty;

        /// <summary>
        /// 邮件完整内容
        /// </summary>
        public string Mail_HtmlContent { get; set; } = string.Empty;

        /// <summary>
        /// 原始数据(Json字符串)
        /// </summary>
        public string Main_TotalData_JsonStr { get; set; } = string.Empty;
    }

    public class OutlookMail_LoginInfo
    {
        /// <summary>
        /// 登录是否成功
        /// </summary>
        public bool Login_Success { get; set; } = false;

        /// <summary>
        /// 登录错误信息
        /// </summary>
        public string Login_ErrorMsg { get; set; } = string.Empty;

        public string EmailAccount { get; set; } = string.Empty;

        /// <summary>
        /// Json格式的原始Cookie
        /// </summary>
        public string Cookie_Json_Old { get; set; } = string.Empty;

        /// <summary>
        /// 浏览器Cookie的Json格式
        /// </summary>
        public List<Cookie> Cookies_Browser { get; set; } = null;

        /// <summary>
        /// Http请求使用的Cookie
        /// </summary>
        public string Cookies_Browser_Str
        {
            get
            {
                //domain=.login.live.com;
                if (this.Cookies_Browser == null || this.Cookies_Browser.Count == 0) return string.Empty;
                else
                    return string.Join("; ",
                        this.Cookies_Browser.Select(ck => $"{ck.Name}={ck.Value}; domain={ck.Domain}"));
            }
        }

        /// <summary>
        /// 用户登录数据(json对象)
        /// </summary>
        public JObject UserInfo_JObject { get; set; } = null;

        /// <summary>
        /// 用户登录数据(json字符串)
        /// </summary>
        public string UserInfo_JsonStr { get; set; } = string.Empty;

        #region 用户访问API的参数

        public string Postdata_X_OWA_CANARY { get; set; } = string.Empty;
        public string Postdata_FindConversation_Header_RequestServerVersion { get; set; } = string.Empty;
        public string Postdata_FindConversation_Body_ParentFolderId_BaseFolderId_Id { get; set; } = string.Empty;
        public string Postdata_FindConversation_Body_SearchFolderId_Id { get; set; } = string.Empty;

        public string Postdata_GetConversationItems_FolderId_Id { get; set; } = string.Empty;
        public string Postdata_GetConversationItems_ParentFolderId_Id { get; set; } = string.Empty;

        #endregion
    }

    #endregion
}