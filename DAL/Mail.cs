using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;

namespace AccountManager.DAL
{
    public class Mail
    {
        public string id;
        public string title;
        public string sender;
        public string preview;
        public string previewHtml;
        public string content;
        public string Main_TotalData_JsonStr;
        public string Cookies_Browser_Str;
        public string Postdata_X_OWA_CANARY;
        public string Postdata_AppId;
        public string Postdata_MailWssid;
        public string SelectedMailboxId;
        public string Postdata_Ymreqid;
        public List<Cookie> Cookies_Browser;
    }

    public enum MailType
    {
        Gmail,
        Outlook,
        Yahoo
    }
}