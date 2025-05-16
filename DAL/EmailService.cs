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
using System.Security.Claims;
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
    public class EmailService
    {
        public string Email_GetGoolge(string cookie)
        {
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;

            hi = new HttpItem();
            hi.URL = $"https://mail.google.com/mail/u/0/feed/atom";
            // hi.UserAgent = account.UserAgent;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Referer = hi.URL;
            hi.Allowautoredirect = false;

            hi.Timeout = 20000;
            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = cookie;

            hr = hh.GetHtml(hi);
            var midStr = StringHelper.GetMidStr(hr.Html, "<title>Gmail - Inbox for ", "</title>");
            return midStr;
        }

        public List<Mail> Email_GetGoolgeList(string cookie,string ua)
        {
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;

            hi = new HttpItem();
            hi.URL = $"https://mail.google.com/mail/u/0/feed/atom";
            hi.UserAgent = ua;
            hi.Accept = $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Referer = hi.URL;
            hi.Allowautoredirect = false;

            hi.Timeout = 20000;
            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = cookie;

            hr = hh.GetHtml(hi);

            List<Mail> mailList = new List<Mail>();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(hr.Html);
            var nodes = doc.DocumentNode.SelectNodes("/feed/entry");

            for (int i = 0; i < nodes.Count; i++)
            {
                var item = nodes[i];
                try
                {
                    var sender = item.SelectSingleNode("author/name")?.InnerText;
                    if (sender != null)
                    {
                        var title = item.SelectSingleNode("title")?.InnerText;
                        var preview = item.SelectSingleNode("summary")?.InnerText;
                        var previewHtml = item.SelectSingleNode("summary")?.InnerHtml;
                        var id = item.SelectSingleNode("link").Attributes["href"].Value.Replace("&amp;", "&");
                        mailList.Add(new Mail
                        {
                            sender = sender,
                            title = title,
                            preview = preview,
                            previewHtml = previewHtml,
                            id = id
                        });
                    }
                }
                catch
                {
                }
            }

            return mailList;
        }

        public List<Mail> Email_GetYahooList(string cookie,string ua)
        {
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;
            var emailGetYahooList = new List<Mail>();
            hi = new HttpItem();
            hi.URL = $"https://mail.yahoo.com/";
            hi.UserAgent = ua;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("authority", "mail.yahoo.com");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Header.Add("sec-ch-ua", "\"Chromium\";v=\"122\", \"Not)A;Brand\";v=\"24\", \"Google Chrome\";v=\"122\"");
            //Cookie
            hi.Cookie = cookie;

            hr = hh.GetHtml(hi);
            if (string.IsNullOrEmpty(hr.Html))
            {
                return emailGetYahooList;
            }

            string Postdata_AppId = StringHelper.GetMidStr(hr.Html, "\"appId\":\"", "\"");
            string Postdata_MailWssid = StringHelper.GetMidStr(hr.Html, "\"mailWssid\":\"", "\"");
            string SelectedMailboxId = StringHelper.GetMidStr(hr.Html, "\"selectedMailbox\":{\"id\":\"", "\"");
            string tempStr = StringHelper.GetMidStr(hr.Html, "\"guidHash\":\"", "\"");
            string Postdata_Ymreqid = "";
            if (!string.IsNullOrEmpty(tempStr))
            {
                Postdata_Ymreqid =
                    $"{tempStr.Substring(0, 8)}-{tempStr.Substring(8, 4)}-{tempStr.Substring(12, 4)}-{GetRandomStr(4)}-{GetRandomStr(10)}";
            }

            hi = new HttpItem();
            hi.URL = $"https://mail.yahoo.com/d/folders/1";
            hi.UserAgent = ua;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("authority", "mail.yahoo.com");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Header.Add("sec-ch-ua", "\"Chromium\";v=\"122\", \"Not)A;Brand\";v=\"24\", \"Google Chrome\";v=\"122\"");
            hi.Referer = hi.URL;
            hi.Allowautoredirect = false;
            hi.Header.Add("Sec-Fetch-Site", "none");
            hi.Method = "GET";
            //Cookie
            hi.Cookie = cookie;

            hr = hh.GetHtml(hi);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(hr.Html);

            var ul = doc.DocumentNode.SelectSingleNode(
                "//*[@id='mail-app-component']/div[1]/div/div/div[2]/div/div/div[4]/div/div[1]/ul");
            if (ul == null)
            {
                ul = doc.DocumentNode.SelectSingleNode(
                    "//*[@id='mail-app-component']/div[1]/div/div/div[2]/div/div[1]/div[3]/div/div[1]/ul");
            }

            if (ul == null)
            {
                throw new Exception("无法获取邮件");
            }

            doc.LoadHtml(ul.InnerHtml);
            var li = doc.DocumentNode.SelectNodes("li");

            for (int i = 0; i < li.Count; i++)
            {
                var id = li[i].SelectSingleNode("a")?.Attributes["href"]?.Value;
                var title = li[i].SelectSingleNode("a/div/div[3]/div[1]/div[1]/span[1]")?.InnerText;
                var preview = li[i].SelectSingleNode("a/div/div[3]/div[1]/div[2]/div")?.InnerText;
                var sender = li[i].SelectSingleNode("a/div/div[1]//strong")?.InnerText;
                if (title != null)
                {
                    emailGetYahooList.Add(new Mail
                    {
                        id = id,
                        title = title,
                        preview = preview,
                        sender = sender,
                        // Cookies_Browser = listCookie,
                        // Cookies_Browser_Str = Cookies_Browser_Str,
                        Postdata_AppId = Postdata_AppId,
                        Postdata_MailWssid = Postdata_MailWssid,
                        SelectedMailboxId = SelectedMailboxId,
                        Postdata_Ymreqid = Postdata_Ymreqid,
                    });
                }
            }

            return emailGetYahooList;
        }

        public static string GetRandomStr(int length)
        {
            byte[] r = new byte[length];
            Random rand = new Random((int)(DateTime.Now.Ticks % 1000000));
            int ran;
            //生成8字节原始数据
            for (int i = 0; i < length; i++)
            {
                //while循环剔除非字母和数字的随机数
                do
                {
                    //数字范围是ASCII码中字母数字和一些符号
                    ran = rand.Next(48, 122);
                    r[i] = Convert.ToByte(ran);
                } while ((ran >= 58 && ran <= 64) || (ran >= 91 && ran <= 96));
            }

            //转换成8位String类型               
            string randomID = Encoding.ASCII.GetString(r);
            return randomID;
        }

        public string Email_GetYahoo(string cookie,string ua)
        {
            HttpHelper hh = new HttpHelper();
            HttpItem hi = null;
            HttpResult hr = null;

            hi = new HttpItem();
            hi.URL = $"https://mail.yahoo.com/d/folders/1";
            hi.UserAgent = ua;
            hi.Accept =
                $"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            hi.Header.Add("Accept-Encoding", "gzip");
            hi.Header.Add("authority", "mail.yahoo.com");
            hi.Header.Add("Accept-Language", "zh-HK,zh;q=0.9");
            hi.Header.Add("sec-ch-ua", "\"Chromium\";v=\"122\", \"Not)A;Brand\";v=\"24\", \"Google Chrome\";v=\"122\"");
            hi.Referer = hi.URL;
            hi.Allowautoredirect = true;

            hi.Timeout = 20000;
            hi.Header.Add("Sec-Fetch-Site", "none");

            //Cookie
            hi.Cookie = cookie;

            hr = hh.GetHtml(hi);
            var midStr = StringHelper.GetMidStr(hr.Html, "\"email\":\"", "\",\"isPrimary\"");

            return midStr;
        }
    }
}