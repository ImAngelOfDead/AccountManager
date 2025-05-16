using AccountManager.Common;
using AccountManager.Models;
using BookingService.Common;
using CsharpHttpHelper;
using Jurassic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenPop.Pop3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinInetHelperCSharp;

namespace AccountManager.DAL
{
    /// <summary>
    /// FacebookAPI
    /// </summary>
    public class CodingPlatformService
    {
        public CodingPlatformService()
        {
        }

        public string CreateTaskByCapsolver(string blob, Account_FBOrIns account)
        {
            string taskId = "";
            string CapSolverClientKey = "CAP-83D2466E316AAF04462FCE30F8E6409E";
            string blobJson = "{\"blob\": \"" + blob + "\"}";
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;

            #region 平台打码

            hi = new HttpItem();
            hi.URL = $"https://api.capsolver.com/createTask";
            hi.Allowautoredirect = false;
            hi.Method = "POST";
            hi.ContentType = $"application/json";
            JObject jo_posttask = new JObject();
            jo_posttask["type"] = "FunCaptchaTaskProxyLess";
            jo_posttask["websiteURL"] = "www.linkedin.com";
            jo_posttask["websitePublicKey"] = "3117BF26-4762-4F5A-8ED9-A85E69209A46";
            jo_posttask["data"] = blobJson;

            jo_postdata = new JObject();
            jo_postdata["clientKey"] = CapSolverClientKey;
            jo_postdata["task"] = jo_posttask;

            hi.Postdata = jo_postdata.ToString();
            //代理
            // if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //判断结果
            jr = null;
            try
            {
                jr = JObject.Parse(hr.Html);
                if (jr["status"].ToString().Equals("idle"))
                {
                    taskId = jr["taskId"].ToString();
                }
            }
            catch
            {
            }

            #endregion

            return taskId;
        }
        
        public string GetTaskResultByCapsolver(string taskId,Account_FBOrIns account)
        {
            string token = "";
            string CapSolverClientKey = "CAP-83D2466E316AAF04462FCE30F8E6409E";
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            JObject jo_postdata = null;
            JObject jr = null;

            #region 平台打码

            hi = new HttpItem();
            hi.URL = $"https://api.capsolver.com/getTaskResult";
            hi.Allowautoredirect = false;
            hi.Method = "POST";
            hi.ContentType = $"application/json";
            jo_postdata = new JObject();
            jo_postdata["taskId"] = taskId;
            jo_postdata["clientKey"] = CapSolverClientKey;

            hi.Postdata = jo_postdata.ToString();
            //代理
            if (account.WebProxy != null) hi.WebProxy = account.WebProxy;

            hr = hh.GetHtml(hi);

            //判断结果
            jr = null;
            try
            {
                jr = JObject.Parse(hr.Html);
                if (jr["status"].ToString().Equals("ready"))
                {
                    token = jr["solution"]["token"].ToString();
                }
            }
            catch
            {
            }

            #endregion

            return token;
        }
    }
}