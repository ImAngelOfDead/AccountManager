using System.Diagnostics;
using AccountManager.Common;
using AccountManager.Models;
using BookingService.Common;
using CsharpHttpHelper;
using Newtonsoft.Json.Linq;

namespace AccountManager.DAL;

public class SmsActivateService
{
    public SmsActivateService()
    {
    }

    public JObject SMS_GetCode(string id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;
        JObject jo_postdata = null;
        JObject jr = null;
        var randomUserAgent = StringHelper.CreateRandomUserAgent();

        #region 先访问目标页面

        hi = new HttpItem();

        string url =
            "https://api.sms-activate.io/stubs/handler_api.php?api_key=$api_key&action=getStatus&id=$id";
        url = url.Replace("$id", id);
        url = url.Replace("$api_key", Program.setting.Setting_EM.TokenSms.ToString());
        hi.URL = url;
        hi.UserAgent = randomUserAgent;
        hi.Allowautoredirect = true;


        hr = hh.GetHtml(hi);
        string html = hr.Html;
        if (html.Contains("STATUS_OK"))
        {
            string replaceCode = html.Replace("STATUS_OK:", "");
            jo_Result["Code"] = replaceCode;
            this.SMS_UpdateStatus(id);
        }

        #endregion

        return jo_Result;
    }

    public JObject SMS_GetPhoneNum(string service,string country)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;
        JObject jo_postdata = null;
        JObject jr = null;
        var randomUserAgent = StringHelper.CreateRandomUserAgent();

        #region 先访问目标页面

        hi = new HttpItem();

        string url =
            "https://api.sms-activate.io/stubs/handler_api.php?api_key=$api_key&action=getNumber&service=$service&country=$country";
        url = url.Replace("$api_key", Program.setting.Setting_EM.TokenSms.ToString());
        url = url.Replace("$service", service);
        url = url.Replace("$country", country);
        hi.URL = url;
        hi.UserAgent = randomUserAgent;
        hi.Allowautoredirect = true;


        hr = hh.GetHtml(hi);
        string html = hr.Html;
        if (html.Contains("NO_NUMBERS"))
        {
            jo_Result["ErrorMsg"] = "接码平台没有电话号码";
            return jo_Result;
        }
        string[] strArray = html.Replace("ACCESS_NUMBER:", "").Split(':');
        if (strArray.Length == 2)
        {
            jo_Result.Add("phoneId", strArray[0]);
            jo_Result.Add("Number", strArray[1]);
        }
        else
        {
            jo_Result["ErrorMsg"] = "接码平台没有电话号码";
            return jo_Result;
        }

        #endregion

        return jo_Result;
    }

    public JObject SMS_UpdateStatus(string id)
    {
        JObject jo_Result = new JObject();
        HttpHelper hh = new HttpHelper();
        HttpItem hi = null;
        HttpResult hr = null;
        JObject jo_postdata = null;
        JObject jr = null;

        #region 先访问目标页面

        hi = new HttpItem();
        string url =
            "https://api.sms-activate.io/stubs/handler_api.php?api_key=$api_key&action=setStatus&id=$id&status=3";
        url = url.Replace("$api_key", Program.setting.Setting_EM.TokenSms.ToString());
        url = url.Replace("$id", id);
        hi.URL = url;

        hr = hh.GetHtml(hi);

        #endregion

        return jo_Result;
    }
}