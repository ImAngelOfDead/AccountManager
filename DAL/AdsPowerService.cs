using System;
using CsharpHttpHelper;
using Newtonsoft.Json.Linq;

namespace AccountManager.DAL;

public class AdsPowerService
{
    #region 浏览器

    //启动浏览器
    public JObject ADS_StartBrowser(string user_id)
    {
        // lock (obj)
        // {
            JObject jo_Result = new JObject();
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;

            #region 先访问目标页面

            hi = new HttpItem();
            hi.URL = $"http://local.adspower.net:50325/api/v1/browser/start?clear_cache_after_closing=1&user_id=" +
                     user_id;

            hr = hh.GetHtml(hi);
            var html = hr.Html;
            try
            {
                jo_Result = JObject.Parse(html);
            }
            catch
            {
            }

            #endregion

            return jo_Result;
        // }
    }

    //关闭浏览器
    public JObject ADS_StopBrowser(string user_id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        #region 先访问目标页面

        hi = new HttpItem();
        hi.URL = $"http://local.adspower.net:50325/api/v1/browser/stop?user_id=" + user_id;

        hr = hh.GetHtml(hi);
        var html = hr.Html;
        try
        {
            jo_Result = JObject.Parse(html);
        }
        catch
        {
        }

        #endregion

        return jo_Result;
    }

    public JObject ADS_UserDelete(string user_id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        #region 先访问目标页面

        hi = new HttpItem();
        hi.URL = $"http://local.adspower.net:50325/api/v1/user/delete";

        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        JArray jArray = new JArray();
        jArray.Add(user_id);
        jo_postdata["user_ids"] = jArray;
        hi.ContentType = "application/json; charset=utf-8";

        hi.Postdata = jo_postdata.ToString();
        hr = hh.GetHtml(hi);
        var html = hr.Html;
        try
        {
            jo_Result = JObject.Parse(html);
        }
        catch
        {
        }

        #endregion

        return jo_Result;
    }

    //检查浏览器的启动状态
    public JObject ADS_ActiveBrowser(string user_id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        #region 先访问目标页面

        hi = new HttpItem();
        hi.URL = $"http://local.adspower.net:50325/api/v1/browser/active?user_id=" + user_id;

        hr = hh.GetHtml(hi);
        var html = hr.Html;
        try
        {
            jo_Result = JObject.Parse(html);
        }
        catch
        {
        }

        #endregion

        return jo_Result;
    }

    //查询当前设备所有已打开的浏览器
    public JObject ADS_LocalActiveBrowser()
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        #region 先访问目标页面

        hi = new HttpItem();
        hi.URL = $"http://local.adspower.net:50325/api/v1/browser/local-active";

        hr = hh.GetHtml(hi);
        var html = hr.Html;
        try
        {
            jo_Result = JObject.Parse(html);
        }
        catch
        {
        }

        #endregion

        return jo_Result;
    }

    #endregion

    #region 分组管理

    //查询分组
    public JObject ADS_GroupList()
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        #region 先访问目标页面

        hi = new HttpItem();
        hi.URL = $"http://local.adspower.net:50325/api/v1/group/list?page_size=1000";

        hr = hh.GetHtml(hi);
        var html = hr.Html;
        try
        {
            jo_Result = JObject.Parse(html);
        }
        catch
        {
        }

        #endregion

        return jo_Result;
    }

    #endregion

    #region 环境管理

//新建浏览器
    public JObject ADS_UserList(string groupId)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        #region 先访问目标页面

        hi = new HttpItem();
        hi.URL = $"http://local.adspower.net:50325/api/v1/user/list?page_size=1000&group_id=" + groupId;

        hr = hh.GetHtml(hi);
        var html = hr.Html;
        try
        {
            jo_Result = JObject.Parse(html);
        }
        catch
        {
        }

        #endregion

        return jo_Result;
    }

    private static object obj = new object();

    //新建浏览器
    public JObject ADS_UserCreate(string groupName, string cookie, string UserAgent)
    {
        // lock (obj)
        // {
            string group_id = String.Empty;
            var adsGroupList = this.ADS_GroupList();
            var jToken = adsGroupList["data"]["list"];
            foreach (var token in jToken)
            {
                if (token["group_name"].ToString().Equals(groupName))
                {
                    group_id = token["group_id"].ToString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(group_id))
            {
                var adsGroupCreate = this.ADS_GroupCreate(groupName);
                group_id = adsGroupCreate["data"]["group_id"].ToString();
            }

            JObject jo_Result = new JObject();
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;

            #region 先访问目标页面

            hi = new HttpItem();
            hi.URL = $"http://local.adspower.net:50325/api/v1/user/create";
            hi.Method = "POST";
            JObject jo_postdata = new JObject();
            jo_postdata["group_id"] = group_id;
            jo_postdata["cookie"] = cookie;
            jo_postdata["fingerprint_config"] = GetFingerprintConfig(UserAgent);
            jo_postdata["user_proxy_config"] = GetUserProxyConfig(groupName);
            if (groupName.Equals("IN"))
            {
                if (Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Type_922)
                {
                    jo_postdata["country"] = "hk";
                }
            }
            else if (groupName.Equals("IN_RE"))
            {
                if (Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Type_922)
                {
                    jo_postdata["country"] = "hk";
                }
            }
            else if (groupName.Equals("EM"))
            {
                if (Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Type_922)
                {
                    jo_postdata["country"] = "hk";
                }
            }

            hi.ContentType = "application/json; charset=utf-8";

            hi.Postdata = jo_postdata.ToString();
            hr = hh.GetHtml(hi);
            var html = hr.Html;
            try
            {
                jo_Result = JObject.Parse(html);
            }
            catch
            {
            }

            #endregion

            return jo_Result;
        // }
    }

    //更新浏览器环境
    public JObject ADS_UserUpdate(string user_id, string UserAgent)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        #region 先访问目标页面

        hi = new HttpItem();
        hi.URL = $"http://local.adspower.net:50325/api/v1/user/update";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata["user_id"] = user_id;
        jo_postdata["fingerprint_config"] = GetFingerprintConfig(UserAgent);
        hi.ContentType = "application/json; charset=utf-8";
        hi.Postdata = jo_postdata.ToString();
        hr = hh.GetHtml(hi);
        var html = hr.Html;
        try
        {
            jo_Result = JObject.Parse(html);
        }
        catch
        {
        }

        #endregion

        return jo_Result;
    }

    public JObject ADS_GroupCreate(string group_name)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        #region 先访问目标页面

        hi = new HttpItem();
        hi.URL = $"http://local.adspower.net:50325/api/v1/group/create";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata["group_name"] = group_name;
        jo_postdata["remark"] = group_name + "[注册，勿动]";
        hi.ContentType = "application/json; charset=utf-8";
        hi.Postdata = jo_postdata.ToString();
        hr = hh.GetHtml(hi);
        var html = hr.Html;
        try
        {
            jo_Result = JObject.Parse(html);
        }
        catch
        {
        }

        #endregion

        return jo_Result;
    }

    public JObject GetFingerprintConfig(string UserAgent)
    {
        JObject jo_postdata = new JObject();
        jo_postdata["automatic_timezone"] = 1;
        jo_postdata["language_switch"] = 1;
        jo_postdata["timezone"] = string.Empty;
        jo_postdata["webrtc"] = "proxy";
        jo_postdata["webgl"] = 3;
        jo_postdata["ua"] = UserAgent;
        jo_postdata["location"] = "block";
        JArray jArray = new JArray();
        jArray.Add("en-US");
        jo_postdata["language"] = jArray;
        jo_postdata["location_switch"] = 1;
        jo_postdata["language_switch"] = 0;
        return jo_postdata;
    }

    public JObject GetUserProxyConfig(string groupName)
    {
        JObject jo_postdata = new JObject();

        if (groupName.Equals("IN"))
        {
            if (Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_IsUse)
            {
                jo_postdata["proxy_soft"] = "luminati";
                jo_postdata["proxy_type"] = "socks5";
                var strings = Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Url.ToString().Split(':');
                jo_postdata["proxy_host"] = strings[0];
                jo_postdata["proxy_port"] = strings[1];
                jo_postdata["proxy_user"] = Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_UserName;
                jo_postdata["proxy_password"] = Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Pwd;
                jo_postdata["proxy_url"] = string.Empty;
                jo_postdata["global_config"] = string.Empty;
            }

            if (Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Type_922)
            {
                jo_postdata["proxy_soft"] = "922S5auto";
            }
        }
        else if (groupName.Equals("IN_RE"))
        {
            if (Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_IsUse)
            {
                jo_postdata["proxy_soft"] = "luminati";
                jo_postdata["proxy_type"] = "socks5";
                var strings = Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Url.ToString().Split(':');
                jo_postdata["proxy_host"] = strings[0];
                jo_postdata["proxy_port"] = strings[1];
                jo_postdata["proxy_user"] = Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_UserName;
                jo_postdata["proxy_password"] = Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Pwd;
                jo_postdata["proxy_url"] = string.Empty;
                jo_postdata["global_config"] = string.Empty;
            }

            if (Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Type_922)
            {
                jo_postdata["proxy_soft"] = "922S5auto";
            }
        }
        else if (groupName.Equals("EM"))
        {
            if (Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_IsUse)
            {
                jo_postdata["proxy_soft"] = "luminati";
                jo_postdata["proxy_type"] = "socks5";
                var strings = Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Url.ToString().Split(':');
                jo_postdata["proxy_host"] = strings[0];
                jo_postdata["proxy_port"] = strings[1];
                jo_postdata["proxy_user"] = Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_UserName;
                jo_postdata["proxy_password"] = Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Pwd;
                jo_postdata["proxy_url"] = string.Empty;
                jo_postdata["global_config"] = string.Empty;
            }

            if (Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Type_922)
            {
                jo_postdata["proxy_soft"] = "922S5auto";
            }
        }


        return jo_postdata;
    }

    #endregion
}