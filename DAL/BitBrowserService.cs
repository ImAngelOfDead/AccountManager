using CsharpHttpHelper;
using Newtonsoft.Json.Linq;

namespace AccountManager.DAL;

public class BitBrowserService
{
    #region 创建、更新修改窗口

    public JObject BIT_BrowserUpdate(string userAgent, string cookie, string groupId, string platform,
        string platformIcon, string name, bool abortImage = false)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/update";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata["groupId"] = groupId;
        jo_postdata["platform"] = platform;
        jo_postdata["platformIcon"] = platformIcon;
        jo_postdata["platformIcon"] = platformIcon;
        jo_postdata["name"] = name;
        jo_postdata["workbench"] = "disable";
        jo_postdata["cookie"] = cookie;
        jo_postdata["abortImage"] = abortImage;
        jo_postdata["stopWhileNetError"] = true;

        if (string.IsNullOrEmpty(Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Url))
        {
            jo_postdata["proxyMethod"] = 2;
            jo_postdata["proxyType"] = "noproxy";
        }
        else
        {
            jo_postdata["ipCheckService"] = "ipCheckService";
            jo_postdata["proxyMethod"] = 2;
            jo_postdata["proxyType"] = "socks5";
            var strings = Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Url.Split(':');
            jo_postdata["host"] = strings[0];
            jo_postdata["port"] = strings[1];
            jo_postdata["proxyUserName"] = Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_UserName;
            jo_postdata["proxyPassword"] = Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Pwd;
        }

        jo_postdata["syncTabs"] = false;
        jo_postdata["syncCookies"] = true;
        jo_postdata["syncIndexedDb"] = false;
        jo_postdata["syncLocalStorage"] = false;
        jo_postdata["syncBookmarks"] = false;
        jo_postdata["syncAuthorization"] = false;
        jo_postdata["credentialsEnableService"] = false;
        jo_postdata["syncHistory"] = false;
        jo_postdata["syncExtensions"] = true;
        jo_postdata["syncUserExtensions"] = true;
        jo_postdata["clearCacheFilesBeforeLaunch"] = false;
        jo_postdata["clearCacheWithoutExtensions"] = false;
        jo_postdata["clearCookiesBeforeLaunch"] = false;
        jo_postdata["clearHistoriesBeforeLaunch"] = false;
        jo_postdata["randomFingerprint"] = true;
        jo_postdata["browserFingerPrint"] = GetBrowserFingerPrint(userAgent);
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

        return jo_Result;
    }

    #endregion

    #region 更新浏览器窗口部分属性

    public JObject BIT_BrowserUpdatePartial(string user_id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/update/partial";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        JArray jArray = new JArray();
        jArray.Add(user_id);
        jo_postdata["ids"] = jArray;
        jo_postdata["name"] = "我修改了";
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

        return jo_Result;
    }

    #endregion

    #region 浏览器列表

    public JObject BIT_BrowserList(string groupId)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/list";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata["groupId"] = groupId;
        jo_postdata["page"] = 0;
        jo_postdata["pageSize"] = 10;
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

        return jo_Result;
    }

    #endregion

    #region 浏览器简略信息列表

    public JObject BIT_BrowserListConcise()
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/list/concise";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata["sortDirection"] = "desc";
        jo_postdata["sortProperties"] = "seq";
        jo_postdata["page"] = 0;
        jo_postdata["pageSize"] = 10;
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

        return jo_Result;
    }

    #endregion

    #region 获取给定浏览器的pids

    public JObject BIT_BrowserPids(string user_id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/pids";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        JArray jArray = new JArray();
        jArray.Add(user_id);
        jo_postdata["ids"] = jArray;
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

        return jo_Result;
    }

    #endregion

    #region 获取给定浏览器的pids，检测是否还alive

    public JObject BIT_BrowserPidsAlive(string user_id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/pids/alive";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        JArray jArray = new JArray();
        jArray.Add(user_id);
        jo_postdata["ids"] = jArray;
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

        return jo_Result;
    }

    #endregion

    #region 获取所有或者的浏览器的pids

    public JObject BIT_BrowserPidsAll()
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/pids/all";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
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

        return jo_Result;
    }

    #endregion

    #region 创建分组

    public JObject BIT_GroupAdd(string groupName, int sortNum)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/group/add";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata.Add("groupName", groupName);
        jo_postdata.Add("sortNum", sortNum);
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

        return jo_Result;
    }

    #endregion

    #region 修改分组

    public JObject BIT_GroupEdit(string groupId, string groupName, int sortNum)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/group/edit";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata.Add("groupName", groupName);
        jo_postdata.Add("sortNum", sortNum);
        jo_postdata.Add("id", groupId);
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

        return jo_Result;
    }

    #endregion


    #region 删除分组

    public JObject BIT_GroupDelete(string groupId)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/group/delete";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata.Add("id", groupId);
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

        return jo_Result;
    }

    #endregion

    #region 获取分组详情

    public JObject BIT_GroupDetail(string groupId)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/group/detail";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata.Add("id", groupId);
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

        return jo_Result;
    }

    #endregion

    private static readonly object Obj = new object();

    #region 获取分组list

    public JObject BIT_GroupList(int page, int pageSize)
    {
        lock (Obj)
        {
            JObject jo_Result = new JObject();
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;

            hi = new HttpItem();
            hi.URL = $"http://127.0.0.1:54345/group/list";
            hi.Method = "POST";
            JObject jo_postdata = new JObject();
            jo_postdata.Add("page", page);
            jo_postdata.Add("pageSize", pageSize);
            jo_postdata.Add("all", true);
            jo_postdata.Add("sortDirection", "asc");
            jo_postdata.Add("sortProperties", "sortNum");
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

            return jo_Result;
        }
    }

    #endregion

    #region 自定义排列窗口

    public JObject BIT_Windowbounds()
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/windowbounds";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata.Add("type", "box");
        jo_postdata.Add("startX", 0);
        jo_postdata.Add("startY", 0);
        jo_postdata.Add("width", 800);
        jo_postdata.Add("height", 500);
        jo_postdata.Add("col", 4);
        jo_postdata.Add("spaceX", 0);
        jo_postdata.Add("spaceY", 0);
        jo_postdata.Add("offsetX", 50);
        jo_postdata.Add("offsetY", 50);
        JArray jArray = new JArray();
        jArray.Add(4270);
        jArray.Add(4271);
        jo_postdata.Add("seqlist", jArray);
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

        return jo_Result;
    }

    #endregion

    #region 打开窗口

    //启动浏览器
    public JObject BIT_BrowserOpen(string user_id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/open";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata["id"] = user_id;
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

        return jo_Result;
    }

    #endregion

    #region 批量修改分组

    public JObject BIT_BrowserGroupUpdate(string groupId, string id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/group/update";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        // jo_postdata["args"] = new JObject();;
        jo_postdata["groupId"] = groupId;
        JArray jArray = new JArray();
        jArray.Add(id);
        jo_postdata["browserIds"] = jArray;
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

        return jo_Result;
    }

    #endregion

    #region 批量修改备注

    public JObject BIT_BrowserRemarkUpdate(string remark, string browserIds)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/remark/update";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata["remark"] = remark;
        var strings = browserIds.Split(',');
        JArray jArray = new JArray();
        if (strings.Length > 0)
        {
            foreach (var id in strings)
            {
                jArray.Add(id);
            }
        }

        jo_postdata["browserIds"] = jArray;
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


        return jo_Result;
    }

    #endregion

    #region 批量修改代理

    public JObject BIT_BrowserProxyUpdate(string ids, string ipCheckService, string proxyMethod,
        string proxyType, string host, string port, string proxyUserName, string proxyPassword, string ip, string city,
        string province, string country, bool isIpNoChange, string dynamicIpUrl, string dynamicIpChannel,
        bool isDynamicIpChangeIp, bool isGlobalProxyInfo, bool isIpv6)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/proxy/update";
        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata["ipCheckService"] = ipCheckService;
        jo_postdata["proxyMethod"] = proxyMethod;
        jo_postdata["proxyType"] = proxyType;
        jo_postdata["host"] = host;
        jo_postdata["port"] = port;
        jo_postdata["ip"] = ip;
        jo_postdata["city"] = city;
        jo_postdata["province"] = province;
        jo_postdata["country"] = country;
        jo_postdata["isIpNoChange"] = isIpNoChange;
        jo_postdata["proxyPassword"] = proxyPassword;
        jo_postdata["proxyUserName"] = proxyUserName;
        jo_postdata["dynamicIpUrl"] = dynamicIpUrl;
        jo_postdata["dynamicIpChannel"] = dynamicIpChannel;
        jo_postdata["isDynamicIpChangeIp"] = isDynamicIpChangeIp;
        jo_postdata["isGlobalProxyInfo"] = isGlobalProxyInfo;
        jo_postdata["isIpv6"] = isIpv6;
        var strings = ids.Split(',');
        JArray jArray = new JArray();
        if (strings.Length > 0)
        {
            foreach (var id in strings)
            {
                jArray.Add(id);
            }
        }

        jo_postdata["ids"] = jArray;
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

        return jo_Result;
    }

    #endregion

    #region 关闭浏览器

    public JObject BIT_BrowserClose(string id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/close";

        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        jo_postdata["id"] = id;
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


        return jo_Result;
    }

    #endregion

    #region 批量关闭浏览器窗口

    public JObject BIT_BrowserCloseByseqs(string seqs)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/close";

        hi.Method = "POST";
        JObject jo_postdata = new JObject();
        var strings = seqs.Split(',');
        JArray jArray = new JArray();
        if (strings.Length > 0)
        {
            foreach (var seq in strings)
            {
                jArray.Add(seq);
            }
        }

        jo_postdata["seqs"] = jArray;
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

        return jo_Result;
    }

    #endregion

    #region 获取用户信息

    public JObject BIT_UserInfo(string seqs)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/userInfo";

        hi.Method = "POST";
        hi.ContentType = "application/json; charset=utf-8";
        hr = hh.GetHtml(hi);
        var html = hr.Html;
        try
        {
            jo_Result = JObject.Parse(html);
        }
        catch
        {
        }

        return jo_Result;
    }

    #endregion

    #region 删除浏览器窗口

    public JObject BIT_BrowserDelete(string id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/delete";
        JObject jo_postdata = new JObject();
        jo_postdata["id"] = id;
        hi.Method = "POST";
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

        return jo_Result;
    }

    #endregion

    #region 批量删除浏览器窗口

    public JObject BIT_BrowserDeleteIds(string ids)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/delete/ids";
        JObject jo_postdata = new JObject();
        var strings = ids.Split(',');
        JArray jArray = new JArray();
        if (strings.Length > 0)
        {
            foreach (var seq in strings)
            {
                jArray.Add(seq);
            }
        }

        jo_postdata["ids"] = jArray;
        hi.Method = "POST";
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

        return jo_Result;
    }

    #endregion

    #region 获取浏览器窗口详情

    public JObject BIT_BrowserDetail(string id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/detail";
        JObject jo_postdata = new JObject();
        jo_postdata["id"] = id;
        hi.Method = "POST";
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

        return jo_Result;
    }

    #endregion

    #region 接口健康检测

    public JObject BIT_Health()
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/health";
        hi.ContentType = "application/json; charset=utf-8";
        hr = hh.GetHtml(hi);
        var html = hr.Html;
        try
        {
            jo_Result = JObject.Parse(html);
        }
        catch
        {
        }

        return jo_Result;
    }

    #endregion

    #region 重启窗口

    public JObject BIT_BrowserReopenAtPos(string ids)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/reopenAtPos";
        hi.ContentType = "application/json; charset=utf-8";
        JObject jo_postdata = new JObject();
        jo_postdata["all"] = false;
        var strings = ids.Split(',');
        JArray jArray = new JArray();
        if (strings.Length > 0)
        {
            foreach (var seq in strings)
            {
                jArray.Add(seq);
            }
        }

        jo_postdata["ids"] = jArray;
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

        return jo_Result;
    }

    #endregion

    #region 所有已打开窗口的端口

    public JObject BIT_browserports(string ids)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/ports";
        hi.ContentType = "application/json; charset=utf-8";
        hr = hh.GetHtml(hi);
        var html = hr.Html;
        try
        {
            jo_Result = JObject.Parse(html);
        }
        catch
        {
        }

        return jo_Result;
    }

    #endregion


    #region 代理检测

    public JObject BIT_Checkagent(string host, string port, string proxyType, string proxyUserName,
        string proxyPassword, string id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;

        hi = new HttpItem();
        hi.URL = $"http://127.0.0.1:54345/browser/ports";
        hi.ContentType = "application/json; charset=utf-8";
        JObject jo_postdata = new JObject();
        jo_postdata["host"] = host;
        jo_postdata["port"] = port;
        jo_postdata["proxyType"] = proxyType;
        jo_postdata["proxyUserName"] = proxyUserName;
        jo_postdata["proxyPassword"] = proxyPassword;
        jo_postdata["id"] = id;
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

        return jo_Result;
    }

    #endregion

    public JObject GetBrowserFingerPrint(string userAgent)
    {
        JObject jo_postdata = new JObject();
        jo_postdata["ostype"] = "PC";
        jo_postdata["os"] = "Win32";
        jo_postdata["userAgent"] = userAgent;
        jo_postdata["isIpCreateTimeZone"] = true;
        jo_postdata["ignoreHttpsErrors"] = false;
        jo_postdata["isIpCreatePosition"] = true;
        jo_postdata["clientRectNoiseEnabled"] = true;
        jo_postdata["isIpCreateLanguage"] = false;
        jo_postdata["enablePlugins"] = false;
        jo_postdata["isIpCreateDisplayLanguage"] = false;
        jo_postdata["webRTC"] = "0";
        jo_postdata["position"] = "1";
        jo_postdata["languages"] = "en-US";
        jo_postdata["openWidth"] = 1280;
        jo_postdata["openHeight"] = 720;
        jo_postdata["resolutionType"] = "0";
        jo_postdata["canvas"] = "0";
        jo_postdata["webGL"] = "0";
        jo_postdata["audioContext"] = "0";
        jo_postdata["speechVoices"] = "0";
        jo_postdata["portScanProtect"] = "0";
        jo_postdata["hardwareConcurrency"] = "4";
        jo_postdata["deviceMemory"] = "8";
        jo_postdata["doNotTrack"] = "1";
        jo_postdata["webGLMeta"] = "0";
        jo_postdata["webGLMeta"] = string.Empty;
        jo_postdata["webGLRender"] = string.Empty;
        return jo_postdata;
    }
}