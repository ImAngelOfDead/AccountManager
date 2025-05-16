using AccountManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using AccountManager.Common;
using AccountManager.DAL;
using BookingService.Common;
using CsharpHttpHelper;
using Newtonsoft.Json.Linq;

namespace AccountManager
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // BitBrowserService bitBrowserService = new BitBrowserService();
            // bitBrowserService = new BitBrowserService();
            // string groupId = String.Empty;
            // var bitGroupList = bitBrowserService.BIT_GroupList(0, 10);
            // if (bitGroupList != null)
            // {
            //     var jToken = bitGroupList["data"]["list"];
            //     if (jToken.Count() > 0)
            //     {
            //         foreach (var token in jToken)
            //         {
            //             if (token["groupName"].ToString().Equals("EM"))
            //             {
            //                 groupId = token["id"].ToString();
            //                 break;
            //             }
            //         }
            //     }
            //
            //     if (string.IsNullOrEmpty(groupId))
            //     {
            //         var bitGroupAdd = bitBrowserService.BIT_GroupAdd("EM", 99);
            //         if (bitGroupAdd != null)
            //         {
            //             if ((bool)bitGroupAdd["success"])
            //             {
            //                 groupId = bitGroupAdd["data"]["id"].ToString();
            //             }
            //         }
            //     }
            // }
            //
            // var adsUserCreate = bitBrowserService.BIT_BrowserUpdate(null, null, groupId,
            //     "https://signup.live.com/signup", "live.com", "微软注册" + UUID.StrSnowId);
            // Debug.Write("122121");
            // SmsActivateService smsActivateService = new SmsActivateService();
            // smsActivateService.SMS_GetPhoneNum();
            // LinkedinService linkedinService = new LinkedinService();
            // linkedinService.IN_Re();
            // AdsPowerService adsPowerService = new AdsPowerService();
            // var adsUserCreate = adsPowerService.ADS_UserCreate();
            // var user_id = adsUserCreate["data"]["id"].ToString();
            // adsPowerService.ADS_StartBrowser(user_id);
            // adsPowerService.ADS_StopBrowser(user_id);
            // adsPowerService.ADS_UserDelete(user_id);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Frm_AccountManager());
        }

        public static Setting setting = null;
    }
}