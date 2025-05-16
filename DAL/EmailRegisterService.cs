using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using AccountManager.Common;
using AccountManager.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace AccountManager.DAL;

public class EmailRegisterService
{
    /// <summary>
    /// 注册微软
    /// </summary>
    /// <param name="account"></param>
    /// <param name="driverSet"></param>
    /// <returns></returns>
    public JObject EM_RegisterMSSelenium(Account_FBOrIns account)
    {
        JObject jo_Result = new JObject();
        jo_Result["Success"] = false;
        jo_Result["ErrorMsg"] = string.Empty;
        account.Account_Id = UUID.StrSnowId;
        ChromeDriver driverSet = null;
        AdsPowerService adsPowerService = null;
        BitBrowserService bitBrowserService = null;
        openADS:
        if (Program.setting.Setting_EM.ADSPower)
        {
            account.Running_Log = "初始化ADSPower";
            adsPowerService = new AdsPowerService();
            if (!string.IsNullOrEmpty(account.user_id))
            {
                try
                {
                    account.Running_Log = "验证环境是否打开";
                    Thread.Sleep(5000);
                    var adsActiveBrowser = adsPowerService.ADS_ActiveBrowser(account.user_id);
                    if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                    {
                        var adsUserCreate =
                            adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                        account.user_id = adsUserCreate["data"]["id"].ToString();
                    }
                }
                catch (Exception e)
                {
                    Debug.Write(e.Message);
                }
            }
            else
            {
                account.Running_Log = "获取user_id";
                var adsUserCreate = adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                account.user_id = adsUserCreate["data"]["id"].ToString();
                account.Running_Log = "获取user_id=[" + account.user_id + "]";
            }

            #region 实例化ADS API

            account.Running_Log = "打开ADS";
            var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
            if (adsStartBrowser["code"].ToString().Equals("-1"))
            {
                goto openADS;
            }

            account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
            account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
            account.Running_Log =
                "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

            #endregion
        }
        else if (Program.setting.Setting_EM.BitBrowser)
        {
            account.Running_Log = "初始化BitBrowser";
            bitBrowserService = new BitBrowserService();
            if (!string.IsNullOrEmpty(account.user_id))
            {
                try
                {
                    account.Running_Log = "验证环境是否打开";
                    Thread.Sleep(5000);
                    var adsActiveBrowser = bitBrowserService.BIT_BrowserPidsAlive(account.user_id);
                    if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                    {
                        var adsUserCreate =
                            adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                        account.user_id = adsUserCreate["data"]["id"].ToString();
                    }
                }
                catch (Exception e)
                {
                    Debug.Write(e.Message);
                }

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
                if (adsStartBrowser["code"].ToString().Equals("-1"))
                {
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
                account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }
            else
            {
                account.Running_Log = "查询分组";
                bitBrowserService = new BitBrowserService();
                string groupId = String.Empty;
                var bitGroupList = bitBrowserService.BIT_GroupList(0, 10);
                if (bitGroupList != null)
                {
                    var jToken = bitGroupList["data"]["list"];
                    if (jToken.Count() > 0)
                    {
                        foreach (var token in jToken)
                        {
                            if (token["groupName"].ToString().Equals("EM"))
                            {
                                groupId = token["id"].ToString();
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(groupId))
                    {
                        var bitGroupAdd = bitBrowserService.BIT_GroupAdd("EM", 99);
                        if (bitGroupAdd != null)
                        {
                            if ((bool)bitGroupAdd["success"])
                            {
                                groupId = bitGroupAdd["data"]["id"].ToString();
                            }
                        }
                    }
                }

                var adsUserCreate = bitBrowserService.BIT_BrowserUpdate(account.UserAgent, account.Facebook_CK, groupId,
                    "https://signup.live.com/", "live.com", "live_" + account.Account_Id);
                account.user_id = adsUserCreate["data"]["id"].ToString();
                account.Running_Log = "获取user_id=[" + account.user_id + "]";

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = bitBrowserService.BIT_BrowserOpen(account.user_id);
                if (!(bool)adsStartBrowser["success"])
                {
                    bitBrowserService.BIT_BrowserDelete(account.user_id);
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["http"].ToString();
                account.webdriver = adsStartBrowser["data"]["driver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }
        }


        try
        {
            ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
            if (driverSet == null)
            {
                driverSet = chromeDriverSetting.GetDriverSetting("EM", account.Account_Id, account.selenium,
                    account.webdriver);
            }

            driverSet.Navigate().GoToUrl(
                "https://signup.live.com/signup");
            Thread.Sleep(5000);

            //创建邮箱
            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='___3cifbr0 f11xvspe f1bsjrm3 f3rmtva f1ern45e f1n71otn f1h8hb77 f1deefiw fryk5ur fv6z6zc f1cio4g9 f1mwb9y5 f1ynmygo f121g8nd f1i82eaq f8491dx f1nbblvp fifp7yv f1ov4xf1 f1asdtw4 f1rs8wju f1mtyjhi f1edxzt f3a8s8z']")))
            {
                driverSet.FindElement(By.CssSelector(
                        "[class='___3cifbr0 f11xvspe f1bsjrm3 f3rmtva f1ern45e f1n71otn f1h8hb77 f1deefiw fryk5ur fv6z6zc f1cio4g9 f1mwb9y5 f1ynmygo f121g8nd f1i82eaq f8491dx f1nbblvp fifp7yv f1ov4xf1 f1asdtw4 f1rs8wju f1mtyjhi f1edxzt f3a8s8z']"))
                    .Click();
            }

            Thread.Sleep(5000);

            var emailAddress = StringHelper.GetRandomString(false, true, false, false, 10, 10);
            Random randomEmail = new Random();
            emailAddress += randomEmail.Next(1000, 9999);
            //创建邮箱
            if (CheckIsExists(driverSet,
                    By.Id("usernameInput")))
            {
                driverSet.FindElement(By.Id("usernameInput"))
                    .SendKeys(emailAddress);
            }

            Thread.Sleep(5000);

            //下一步
            if (CheckIsExists(driverSet,
                    By.Id("nextButton")))
            {
                driverSet.FindElement(By.Id("nextButton"))
                    .Click();
            }

            Thread.Sleep(5000);
            var password = StringHelper.GetRandomString(true, true, true, false, 10, 10);
            //创建密码
            if (CheckIsExists(driverSet,
                    By.Id("Password")))
            {
                driverSet.FindElement(By.Id("Password"))
                    .SendKeys(password);
            }

            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='___ifdorr0 ftiweyp f1lmfglv fibjyge f1abmfm4 f9yszdx']")))
            {
                if (driverSet.FindElement(By.CssSelector(
                        "[class='___ifdorr0 ftiweyp f1lmfglv fibjyge f1abmfm4 f9yszdx']")).Text
                    .Equals("A password is required"))
                {
                    password = StringHelper.GetRandomString(true, true, true, false, 10, 10);
                    driverSet.FindElement(By.Id("Password"))
                        .SendKeys(password);
                }
            }

            Thread.Sleep(5000);
            //下一步
            if (CheckIsExists(driverSet,
                    By.Id("nextButton")))
            {
                driverSet.FindElement(By.Id("nextButton"))
                    .Click();
            }

            Thread.Sleep(5000);
            var generateSurname = StringHelper.GenerateSurname();
            //First Name
            if (CheckIsExists(driverSet,
                    By.Id("firstNameInput")))
            {
                driverSet.FindElement(By.Id("firstNameInput"))
                    .SendKeys("Mc");
            }

            //Last Name
            if (CheckIsExists(driverSet,
                    By.Id("lastNameInput")))
            {
                driverSet.FindElement(By.Id("lastNameInput"))
                    .SendKeys(generateSurname);
            }

            Thread.Sleep(5000);
            //提交姓名 下一步
            if (CheckIsExists(driverSet,
                    By.Id("nextButton")))
            {
                driverSet.FindElement(By.Id("nextButton"))
                    .Click();
            }

            Thread.Sleep(5000);
            List<string> birthMonthList = new List<string>();
            birthMonthList.Add("January");
            birthMonthList.Add("February");
            birthMonthList.Add("March");
            birthMonthList.Add("April");
            birthMonthList.Add("May");
            birthMonthList.Add("June");
            birthMonthList.Add("July");
            birthMonthList.Add("August");
            birthMonthList.Add("September");
            birthMonthList.Add("October");
            birthMonthList.Add("November");
            birthMonthList.Add("December");
            Random random = new Random();
            int index = random.Next(birthMonthList.Count);
            var birthMonthText = birthMonthList[index];
            //输入月份
            if (CheckIsExists(driverSet,
                    By.Id("BirthMonth")))
            {
                var findElement = driverSet.FindElement(By.Id("BirthMonth"));
                SelectElement selectObj = new SelectElement(findElement);
                selectObj.SelectByText(birthMonthText);
            }

            // BirthDay
            List<string> birthDayList = new List<string>();
            for (int i = 0; i < 28; i++)
            {
                birthDayList.Add(i + "");
            }

            index = random.Next(birthDayList.Count);
            var birthDayText = birthDayList[index];
            //天
            if (CheckIsExists(driverSet,
                    By.Id("BirthDay")))
            {
                var findElement = driverSet.FindElement(By.Id("BirthDay"));
                SelectElement selectObj = new SelectElement(findElement);
                selectObj.SelectByText(birthDayText);
            }

            //年
            Random ran = new Random();
            int year = ran.Next(1905, 2000);
            if (CheckIsExists(driverSet,
                    By.Id("BirthYear")))
            {
                driverSet.FindElement(By.Id("BirthYear"))
                    .SendKeys(year + "");
            }

            Thread.Sleep(5000);
            //提交年月日 下一步
            if (CheckIsExists(driverSet,
                    By.Id("nextButton")))
            {
                driverSet.FindElement(By.Id("nextButton"))
                    .Click();
            }

            Thread.Sleep(5000);
            //打码
            bool expression = true;
            int ii = 0;
            do
            {
                if (ii > 5)
                {
                    break;
                }

                Thread.Sleep(5000);
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='ext-secondary ext-button ___46a6l70 f17hdyk f1oudy f1d4dqg0 f16643v7 f1ugb8du f7y26xe f13hfvcj fm07rh1 f1apsahp fd0rex f1cpir1z f16eno2h f18r37t4 fzjldvh f1qt38gl f8rakl9 f1g0fpsx f16h1fbs fsgvd33 fmuajgt f17m94t f9q4yqu fhe0td7 fwbpk35 f1wcl2ob f1ltk4hd f1oyfet3 f1k5fftb flu9u7w fa4qi57 f11zj0ky f43o6hn f14894vr f1uush98 fr10sow f1qd3bm6 ftxr058 f1x8m22p f18kyeoj f7uvj51 f1emwz7l fz1xuqi fsrzjhw fur62vr f1f2bxve f19rxy1v f1ks5t5n fg209rd f1hvg9fg f1ik4u3u fd6720t f1u5eihr ftlxw82 fj7y92t f154ob9o fb1y507 f16qlskp f15dqc6l fk9yu7v f1a94zgw fblkvk0 f2ud54c f1rx6zpj f1yeerbk f1apeehu fc5iy9t f1w0w9a7 f4rf09w f1lbyfsq f1jvmnke ffu7u5y fr5cd8s fu7zm6 f1l3iklw f1wctfe5 fr4vimk f171xskp f1mtrtxf ft29jt3 f1dkakdg f7ua2bh f1nxs5xn f1ern45e f1n71otn f1h8hb77 f1deefiw fxdtvjf fytdu2e f14t3ns0 f10ra9hq f11qrl6u f1y2xyjm fjlbh76 f10pi13n f6dzj5z f17mccla fz5stix f1p9o1ba f1sil6mw fmrv4ls f1cmbuwj f1cyt9o8 fusgiwz fz0id56 fayajf8 f8491dx f593xy7 fnmhfyr f1e35ql2 fatbyko f1grzc83 fb0xa7e fljg2da f1c2uykm f1eqj1rd f7n145z f1dcjnth ff472gp f4yyc7m fggejwh ft2aflc f9f7vaa fmjaa5u flutoqy f12qb2w f1s9iqzn f1o2wvfq fkbkaou fjk9nze f10kbna7']")))
                {
                    driverSet.FindElement(By.CssSelector(
                            "[class='ext-secondary ext-button ___46a6l70 f17hdyk f1oudy f1d4dqg0 f16643v7 f1ugb8du f7y26xe f13hfvcj fm07rh1 f1apsahp fd0rex f1cpir1z f16eno2h f18r37t4 fzjldvh f1qt38gl f8rakl9 f1g0fpsx f16h1fbs fsgvd33 fmuajgt f17m94t f9q4yqu fhe0td7 fwbpk35 f1wcl2ob f1ltk4hd f1oyfet3 f1k5fftb flu9u7w fa4qi57 f11zj0ky f43o6hn f14894vr f1uush98 fr10sow f1qd3bm6 ftxr058 f1x8m22p f18kyeoj f7uvj51 f1emwz7l fz1xuqi fsrzjhw fur62vr f1f2bxve f19rxy1v f1ks5t5n fg209rd f1hvg9fg f1ik4u3u fd6720t f1u5eihr ftlxw82 fj7y92t f154ob9o fb1y507 f16qlskp f15dqc6l fk9yu7v f1a94zgw fblkvk0 f2ud54c f1rx6zpj f1yeerbk f1apeehu fc5iy9t f1w0w9a7 f4rf09w f1lbyfsq f1jvmnke ffu7u5y fr5cd8s fu7zm6 f1l3iklw f1wctfe5 fr4vimk f171xskp f1mtrtxf ft29jt3 f1dkakdg f7ua2bh f1nxs5xn f1ern45e f1n71otn f1h8hb77 f1deefiw fxdtvjf fytdu2e f14t3ns0 f10ra9hq f11qrl6u f1y2xyjm fjlbh76 f10pi13n f6dzj5z f17mccla fz5stix f1p9o1ba f1sil6mw fmrv4ls f1cmbuwj f1cyt9o8 fusgiwz fz0id56 fayajf8 f8491dx f593xy7 fnmhfyr f1e35ql2 fatbyko f1grzc83 fb0xa7e fljg2da f1c2uykm f1eqj1rd f7n145z f1dcjnth ff472gp f4yyc7m fggejwh ft2aflc f9f7vaa fmjaa5u flutoqy f12qb2w f1s9iqzn f1o2wvfq fkbkaou fjk9nze f10kbna7']"))
                        .Click();
                    expression = false;
                }

                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='ms-Button ms-Button--primary root-122']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='ms-Button ms-Button--primary root-122']"))
                        .Click();
                    expression = false;
                }

                ii++;
            } while (expression);

            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://signup.live.com/signup"))
            {
                jo_Result["ErrorMsg"] = "注册失败";
                jo_Result["Success"] = false;
                return jo_Result;
            }

            driverSet.Navigate().GoToUrl("https://outlook.live.com/mail/0/");
            Thread.Sleep(5000);
            if (CheckIsExists(driverSet,
                    By.Id("acceptButton")))
            {
                driverSet.FindElement(By.Id("acceptButton"))
                    .Click();
            }

            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://outlook.live.com/mail/0/"))
            {
                account.New_Mail_Name = emailAddress + "@outlook.com";
                account.New_Mail_Pwd = password;
                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Facebook_CK = strJson;
                }

                if (Program.setting.Setting_EM.Account_List == null)
                {
                    Program.setting.Setting_EM.Account_List = new List<Account_FBOrIns>();
                }

                Program.setting.Setting_EM.Account_List.Add(account);
            }
            else
            {
                jo_Result["ErrorMsg"] = "注册失败";
                jo_Result["Success"] = false;
                return jo_Result;
            }
        }
        catch (Exception e)
        {
            if (e.Message.Contains("The HTTP request to the remote WebDriver server for URL"))
            {
                jo_Result["ErrorMsg"] = "超时";
                jo_Result["Success"] = false;
                return jo_Result;
            }
            else
            {
                jo_Result["ErrorMsg"] = "其他错误";
                jo_Result["Success"] = false;
                return jo_Result;
            }
        }
        finally
        {
            if (Program.setting.Setting_EM.ADSPower)
            {
                adsPowerService.ADS_UserDelete(account.user_id);
            }
            else if (Program.setting.Setting_EM.BitBrowser)
            {
                bitBrowserService.BIT_BrowserDelete(account.user_id);
            }

            try
            {
                driverSet?.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Quit();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        jo_Result["Success"] = true;
        jo_Result["ErrorMsg"] = "注册微软:操作成功";
        return jo_Result;
    }

    /// <summary>
    /// 注册谷歌
    /// </summary>
    /// <param name="account"></param>
    /// <param name="driverSet"></param>
    /// <returns></returns>
    public JObject EM_RegisterGoogleSelenium(Account_FBOrIns account, MailInfo mailRu)
    {
        JObject jo_Result = new JObject();
        jo_Result["Success"] = false;
        jo_Result["ErrorMsg"] = string.Empty;
        account.Account_Id = UUID.StrSnowId;
        ChromeDriver driverSet = null;
        AdsPowerService adsPowerService = null;
        BitBrowserService bitBrowserService = null;

        Match match;
        string regexPattern;
        try
        {
            openADS:
            if (Program.setting.Setting_EM.ADSPower)
            {
                account.Running_Log = "初始化ADSPower";
                adsPowerService = new AdsPowerService();
                if (!string.IsNullOrEmpty(account.user_id))
                {
                    try
                    {
                        account.Running_Log = "验证环境是否打开";
                        Thread.Sleep(5000);
                        var adsActiveBrowser = adsPowerService.ADS_ActiveBrowser(account.user_id);
                        if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                        {
                            var adsUserCreate =
                                adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                            account.user_id = adsUserCreate["data"]["id"].ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Write(e.Message);
                    }
                }
                else
                {
                    account.Running_Log = "获取user_id";
                    var adsUserCreate = adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                    account.user_id = adsUserCreate["data"]["id"].ToString();
                    account.Running_Log = "获取user_id=[" + account.user_id + "]";
                }

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
                if (adsStartBrowser["code"].ToString().Equals("-1"))
                {
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
                account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }
            else if (Program.setting.Setting_EM.BitBrowser)
            {
                account.Account_Id = string.Empty;
                account.Running_Log = "初始化BitBrowser";
                bitBrowserService = new BitBrowserService();
                JObject bitGroupList = null;
                string groupId = String.Empty;
                try
                {
                    account.Running_Log = "查询分组";
                    bitGroupList = bitBrowserService.BIT_GroupList(0, 10);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                if (bitGroupList != null)
                {
                    var jToken = bitGroupList["data"]["list"];
                    if (jToken.Count() > 0)
                    {
                        foreach (var token in jToken)
                        {
                            if (token["groupName"].ToString().Equals("EM"))
                            {
                                groupId = token["id"].ToString();
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(groupId))
                    {
                        var bitGroupAdd = bitBrowserService.BIT_GroupAdd("EM", 99);
                        if (bitGroupAdd != null)
                        {
                            if ((bool)bitGroupAdd["success"])
                            {
                                groupId = bitGroupAdd["data"]["id"].ToString();
                            }
                        }
                    }
                }

                var adsUserCreate = bitBrowserService.BIT_BrowserUpdate(account.UserAgent, account.Facebook_CK, groupId,
                    "https://www.google.com/account/about/?hl=en-US", "google.com", "google_" + account.Account_Id);
                account.user_id = adsUserCreate["data"]["id"].ToString();
                account.Running_Log = "获取user_id=[" + account.user_id + "]";

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = bitBrowserService.BIT_BrowserOpen(account.user_id);
                if (!(bool)adsStartBrowser["success"])
                {
                    bitBrowserService.BIT_BrowserDelete(account.user_id);
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["http"].ToString();
                account.webdriver = adsStartBrowser["data"]["driver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";
                #endregion
            }

            ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
            if (driverSet == null)
            {
                driverSet = chromeDriverSetting.GetDriverSetting("Google", account.Account_Id, account.selenium,
                    account.webdriver);
            }

            // taskName = "RegisterGoogle";
            // task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            // if (task != null && task.IsSelected)
            // {

            #region 注册Google

            driverSet.Navigate().GoToUrl(
                "https://www.google.com/account/about/?hl=en-US");
            Thread.Sleep(5000);

            //创建邮箱
            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='h-c-header__cta-li-link h-c-header__cta-li-link--secondary button-standard-mobile']")))
            {
                driverSet.FindElement(By.CssSelector(
                        "[class='h-c-header__cta-li-link h-c-header__cta-li-link--secondary button-standard-mobile']"))
                    .Click();
            }

            Thread.Sleep(5000);
            var firstName = StringHelper.GenerateSurname();
            var lastName = StringHelper.GenerateSurname();
            //First Name
            if (CheckIsExists(driverSet,
                    By.Id("firstName")))
            {
                driverSet.FindElement(By.Id("firstName"))
                    .SendKeys(firstName);
            }

            //Last Name
            if (CheckIsExists(driverSet,
                    By.Id("lastName")))
            {
                driverSet.FindElement(By.Id("lastName"))
                    .SendKeys(lastName);
            }

            Thread.Sleep(5000);

            //点击下一步
            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
            {
                driverSet.FindElement(By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                    .Click();
            }

            Thread.Sleep(5000);
            List<string> birthMonthList = new List<string>();
            birthMonthList.Add("January");
            birthMonthList.Add("February");
            birthMonthList.Add("March");
            birthMonthList.Add("April");
            birthMonthList.Add("May");
            birthMonthList.Add("June");
            birthMonthList.Add("July");
            birthMonthList.Add("August");
            birthMonthList.Add("September");
            birthMonthList.Add("October");
            birthMonthList.Add("November");
            birthMonthList.Add("December");
            Random random = new Random();
            int index = random.Next(birthMonthList.Count);
            var birthMonthText = birthMonthList[index];
            //输入月份
            if (CheckIsExists(driverSet,
                    By.Id("month")))
            {
                var findElement = driverSet.FindElement(By.Id("month"));
                SelectElement selectObj = new SelectElement(findElement);
                selectObj.SelectByText(birthMonthText);
            }

            // BirthDay
            List<string> birthDayList = new List<string>();
            for (int i = 0; i < 28; i++)
            {
                birthDayList.Add(i + "");
            }

            index = random.Next(birthDayList.Count);
            var birthDayText = birthDayList[index];
            //天
            if (CheckIsExists(driverSet,
                    By.Id("day")))
            {
                driverSet.FindElement(By.Id("day")).SendKeys(birthDayText);
            }

            //年
            Random ran = new Random();
            int year = ran.Next(1905, 2000);
            if (CheckIsExists(driverSet,
                    By.Id("year")))
            {
                driverSet.FindElement(By.Id("year"))
                    .SendKeys(year + "");
            }

            Thread.Sleep(5000);
            //gender
            if (CheckIsExists(driverSet,
                    By.Id("gender")))
            {
                var findElement = driverSet.FindElement(By.Id("gender"));
                SelectElement selectObj = new SelectElement(findElement);
                selectObj.SelectByIndex(1);
            }

            Thread.Sleep(5000);
            //点击下一步提交
            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
            {
                driverSet.FindElement(By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                    .Click();
            }

            Thread.Sleep(5000);
            //选择自定义邮箱地址
            if (CheckIsExists(driverSet,
                    By.XPath(
                        "/html/body/div[1]/div[1]/div[2]/c-wiz/div/div[2]/div/div/div/form/span/section/div/div/div[1]/div[1]/div/span/div[3]/div/div[1]/div")))
            {
                driverSet.FindElement(By.XPath(
                        "/html/body/div[1]/div[1]/div[2]/c-wiz/div/div[2]/div/div/div/form/span/section/div/div/div[1]/div[1]/div/span/div[3]/div/div[1]/div"))
                    .Click();
            }
            else if (CheckIsExists(driverSet,
                         By.XPath(
                             "/html/body/div[1]/div[1]/div[2]/div/div/div[2]/div/div/div/form/span/section/div/div/div[1]/div[1]/div/span/div[3]/div/div[1]/div")))
            {
                driverSet.FindElement(By.XPath(
                        "/html/body/div[1]/div[1]/div[2]/div/div/div[2]/div/div/div/form/span/section/div/div/div[1]/div[1]/div/span/div[3]/div/div[1]/div"))
                    .Click();
            }

            Thread.Sleep(5000);
            re_emailAddress:
            //输入生成邮箱
            // var emailAddress = StringHelper.GetRandomString(false, true, false, false, 10, 10);

            var emailAddress = firstName + lastName;
            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='whsOnd zHQkBf']")))
            {
                driverSet.FindElement(By.CssSelector(
                    "[class='whsOnd zHQkBf']")).Clear();
                driverSet.FindElement(By.CssSelector(
                        "[class='whsOnd zHQkBf']"))
                    .SendKeys(emailAddress);
                account.New_Mail_Name = emailAddress + "@gmail.com";
            }

            Thread.Sleep(5000);
            //点击下一步提交
            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
            {
                driverSet.FindElement(By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                    .Click();
            }

            Thread.Sleep(5000);
            if (driverSet.PageSource.Contains("This username isn't allowed. Try again."))
            {
                goto re_emailAddress;
            }

            var password = StringHelper.GetRandomString(true, true, true, false, 10, 10);
            //点击下一步提交
            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='whsOnd zHQkBf']")))
            {
                var readOnlyCollection = driverSet.FindElements(By.CssSelector(
                    "[class='whsOnd zHQkBf']"));
                foreach (var webElement in readOnlyCollection)
                {
                    webElement.SendKeys(password);
                }

                account.New_Mail_Pwd = password;
            }

            Thread.Sleep(5000);
            //点击下一步提交
            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
            {
                driverSet.FindElement(By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                    .Click();
            }

            Thread.Sleep(5000);
            if (driverSet.PageSource.Contains("Sorry, we could not create your Google Account."))
            {
                jo_Result["ErrorMsg"] = "创建邮箱失败";
                return jo_Result;
            }

            //phoneNumberId
            if (CheckIsExists(driverSet,
                    By.Id("phoneNumberId")))
            {
                int i = 0;
                BindPhone :
                if (i > 3)
                {
                    jo_Result["ErrorMsg"] = "手机验证失败";
                    return jo_Result;
                }

                i++;
                SmsActivateService smsActivateService = new SmsActivateService();
                var smsGetPhoneNum = smsActivateService.SMS_GetPhoneNum("go", Program.setting.Setting_EM.Country);
                if (smsGetPhoneNum["ErrorMsg"] != null &&
                    !string.IsNullOrEmpty(smsGetPhoneNum["ErrorMsg"].ToString()))
                {
                    jo_Result["ErrorMsg"] = "获取手机失败";
                    return jo_Result;
                }

                driverSet.FindElement(By.Id("phoneNumberId")).Clear();
                Thread.Sleep(1000);
                driverSet.FindElement(By.Id("phoneNumberId"))
                    .SendKeys(smsGetPhoneNum["Number"].ToString());
                //点击下一步提交
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
                {
                    driverSet.FindElement(By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                        .Click();
                }

                Thread.Sleep(5000);
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='Ekjuhf Jj6Lae']")))
                {
                    if (driverSet.FindElement(By.CssSelector(
                            "[class='Ekjuhf Jj6Lae']")).Text.Contains("This phone number has been used too many times"))
                    {
                        goto BindPhone;
                    }
                }

                if (driverSet.PageSource.Contains(
                        "This phone number format is not recognized. Please check the country and number."))
                {
                    goto BindPhone;
                }

                if (driverSet.PageSource.Contains("This phone number cannot be used for verification."))
                {
                    goto BindPhone;
                }

                var sms_code = string.Empty;
                int hh = 0;
                bool isGetCode = true;
                do
                {
                    Thread.Sleep(10000);
                    if (hh > 20)
                    {
                        isGetCode = false;
                    }

                    var smsGetCode = smsActivateService.SMS_GetCode(smsGetPhoneNum["phoneId"].ToString());
                    if (smsGetCode["Code"] != null)
                    {
                        if (string.IsNullOrEmpty(smsGetCode["Code"].ToString()))
                        {
                            Thread.Sleep(10000);
                        }
                        else
                        {
                            sms_code = smsGetCode["Code"].ToString();
                            break;
                        }
                    }

                    hh++;
                } while (isGetCode);

                if (string.IsNullOrEmpty(sms_code))
                {
                    jo_Result["ErrorMsg"] = "获取手机验证码失败";
                    return jo_Result;
                }

                //code
                if (CheckIsExists(driverSet,
                        By.Id("code")))
                {
                    driverSet.FindElement(By.Id("code"))
                        .SendKeys(sms_code);
                    Thread.Sleep(5000);
                }

                //点击下一步
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
                {
                    driverSet.FindElement(By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                        .Click();
                    Thread.Sleep(5000);
                }

                if (Program.setting.Setting_EM.RecoveryEmail)
                {
                    //辅助邮箱
                    if (CheckIsExists(driverSet,
                            By.Id("recoveryEmailId")))
                    {
                        driverSet.FindElement(By.Id("recoveryEmailId"))
                            .SendKeys(mailRu.Mail_Name);
                        account.Recovery_Email = mailRu.Mail_Name;
                        account.Recovery_Email_Password = mailRu.Mail_Pwd;
                        Thread.Sleep(5000);
                    }

                    //点击下一步
                    if (CheckIsExists(driverSet,
                            By.CssSelector(
                                "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-INsAgc VfPpkd-LgbsSe-OWXEXe-dgl2Hf Rj2Mlf OLiIxf PDpWxe P62QJc LQeN7 BqKGqe pIzcPc TrZEUc lw1w4b']")))
                    {
                        var readOnlyCollection = driverSet.FindElements(By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-INsAgc VfPpkd-LgbsSe-OWXEXe-dgl2Hf Rj2Mlf OLiIxf PDpWxe P62QJc LQeN7 BqKGqe pIzcPc TrZEUc lw1w4b']"));
                        if (readOnlyCollection.Count > 0)
                        {
                            readOnlyCollection[1].Click();
                        }
                    }

                    Thread.Sleep(5000);
                }
                else
                {
                    //点击下一步
                    if (CheckIsExists(driverSet,
                            By.CssSelector(
                                "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-INsAgc VfPpkd-LgbsSe-OWXEXe-dgl2Hf Rj2Mlf OLiIxf PDpWxe P62QJc LQeN7 BqKGqe pIzcPc TrZEUc lw1w4b']")))
                    {
                        driverSet.FindElement(By.CssSelector(
                                "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-INsAgc VfPpkd-LgbsSe-OWXEXe-dgl2Hf Rj2Mlf OLiIxf PDpWxe P62QJc LQeN7 BqKGqe pIzcPc TrZEUc lw1w4b']"))
                            .Click();
                    }

                    Thread.Sleep(5000);
                }

                //点击下一步
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
                {
                    driverSet.FindElement(By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                        .Click();
                }

                Thread.Sleep(5000);

                //我同意
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
                {
                    driverSet.FindElement(By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                        .Click();
                }

                Thread.Sleep(10000);
                if (driverSet.Url.Contains("https://myaccount.google.com/?utm_source="))
                {
                    if (CheckIsExists(driverSet,
                            By.CssSelector(
                                "[class='RlFDUe kzvTY paynGb']")))
                    {
                        var readOnlyCollection = driverSet.FindElements(By.CssSelector(
                            "[class='RlFDUe kzvTY paynGb']"));
                        if (readOnlyCollection.Count > 0)
                        {
                            readOnlyCollection[0].Click();
                        }
                    }

                    Thread.Sleep(5000);
                    if (CheckIsExists(driverSet,
                            By.CssSelector(
                                "[class='mUIrbf-LgbsSe mUIrbf-LgbsSe-OWXEXe-dgl2Hf Rmjmjf']")))
                    {
                        var readOnlyCollection = driverSet.FindElements(By.CssSelector(
                            "[class='mUIrbf-LgbsSe mUIrbf-LgbsSe-OWXEXe-dgl2Hf Rmjmjf']"));
                        if (readOnlyCollection.Count > 0)
                        {
                            readOnlyCollection[0].Click();
                        }
                    }

                    Thread.Sleep(5000);
                    if (CheckIsExists(driverSet,
                            By.CssSelector(
                                "[class='pYTkkf-Bz112c-LgbsSe wMI9H Qd9OXe']")))
                    {
                        var readOnlyCollection = driverSet.FindElements(By.CssSelector(
                            "[class='pYTkkf-Bz112c-LgbsSe wMI9H Qd9OXe']"));
                        if (readOnlyCollection.Count > 0)
                        {
                            readOnlyCollection[0].Click();
                        }

                        Thread.Sleep(5000);
                    }

                    if (CheckIsExists(driverSet,
                            By.CssSelector(
                                "[class='UywwFc-LgbsSe UywwFc-LgbsSe-OWXEXe-dgl2Hf wMI9H']")))
                    {
                        var readOnlyCollection = driverSet.FindElements(By.CssSelector(
                            "[class='UywwFc-LgbsSe UywwFc-LgbsSe-OWXEXe-dgl2Hf wMI9H']"));
                        if (readOnlyCollection.Count > 0)
                        {
                            readOnlyCollection[0].Click();
                        }

                        Thread.Sleep(5000);
                    }

                    string emailCode = string.Empty;

                    #region 去邮箱提取验证码

                    var timeSpan = 500;
                    var timeCount = 0;
                    var timeOut = 25000;
                    List<Pop3MailMessage> msgList;
                    Pop3MailMessage pop3MailMessage;
                    DateTime sendCodeTime = DateTime.Parse("1970-01-01");
                    account.Running_Log = $"绑邮箱:提取邮箱验证码[{mailRu.Mail_Name}]";

                    pop3MailMessage = null;
                    while (pop3MailMessage == null && timeCount < timeOut)
                    {
                        Thread.Sleep(timeSpan);
                        Application.DoEvents();
                        timeCount += timeSpan;

                        if (mailRu.Pop3Client != null && mailRu.Pop3Client.Connected)
                            try
                            {
                                mailRu.Pop3Client.Disconnect();
                            }
                            catch
                            {
                            }

                        mailRu.Pop3Client = Pop3Helper.GetPop3Client(mailRu.Mail_Name, mailRu.Mail_Pwd);
                        if (mailRu.Pop3Client == null) continue;

                        msgList = Pop3Helper.GetMessageByIndex(mailRu.Pop3Client);
                        pop3MailMessage = msgList.Where(m =>
                            m.DateSent >= sendCodeTime &&
                            m.From.Contains("<noreply@google.com>")).FirstOrDefault();
                    }

                    if (mailRu.Pop3Client == null)
                    {
                        jo_Result["ErrorMsg"] = "绑邮箱:Pop3连接失败";
                        jo_Result["IsMailUsed"] = true;
                        jo_Result["Success"] = false;
                        return jo_Result;
                    }

                    // if (pop3MailMessage == null)
                    // {
                    //     jo_Result["ErrorMsg"] = "绑邮箱:没有找到指定的邮件信息";
                    //     jo_Result["IsMailUsed"] = true;
                    //     jo_Result["Success"] = false;
                    //     return jo_Result;
                    // }

                    var htmlEmail = pop3MailMessage.Subject;
                    regexPattern = @"\b\d{6}\b";

                    match = Regex.Match(htmlEmail, regexPattern);

                    if (match.Success)
                    {
                        emailCode = match.Value;
                    }

                    #endregion

                    if (!string.IsNullOrEmpty(emailCode))
                    {
                        if (CheckIsExists(driverSet,
                                By.CssSelector(
                                    "[class='VfPpkd-fmcmS-wGMbrd ']")))
                        {
                            driverSet.FindElement(By.CssSelector(
                                "[class='VfPpkd-fmcmS-wGMbrd ']")).Clear();
                            driverSet.FindElement(By.CssSelector(
                                "[class='VfPpkd-fmcmS-wGMbrd ']")).SendKeys(emailCode);
                            Thread.Sleep(5000);
                        }

                        if (CheckIsExists(driverSet,
                                By.CssSelector(
                                    "[class='UywwFc-LgbsSe UywwFc-LgbsSe-OWXEXe-dgl2Hf wMI9H']")))
                        {
                            driverSet.FindElement(By.CssSelector(
                                "[class='UywwFc-LgbsSe UywwFc-LgbsSe-OWXEXe-dgl2Hf wMI9H']")).Click();
                            Thread.Sleep(5000);
                        }

                        //处理邮箱的绑定问题
                        lock (Program.setting.Setting_EM.Lock_Mail_ForBind_List)
                        {
                            mailRu.IsLocked = true;
                            mailRu.Is_Used = true;
                        }

                        var cookieJar = driverSet.Manage().Cookies.AllCookies;
                        if (cookieJar.Count > 0)
                        {
                            string strJson = JsonConvert.SerializeObject(cookieJar);
                            account.Facebook_CK = strJson;
                        }
                    }
                }
            }

            #endregion

            var taskName = "RegisterGoogleAndLinkedin";
            var task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                #region MyRegion

                var newPassword = string.Empty;
                account.Running_Log = "IN:访问注册页面";
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
                Thread.Sleep(15000);
                ReadOnlyCollection<IWebElement> webElementIframes;
                if (driverSet.Url.Contains(
                        "https://www.linkedin.com/feed/?trk=guest_homepage-basic_google-one-tap-submit"))
                {
                    Thread.Sleep(5000);
                    goto create_password;
                }
                else if (driverSet.Url.Contains("https://www.linkedin.com/checkpoint/challenge"))
                {
                    Thread.Sleep(5000);
                    if (driverSet.PageSource.Contains("Your account has been temporarily restricted as a precaution"))
                    {
                        jo_Result["ErrorMsg"] = "IN:注册失败,封号";
                        jo_Result["Success"] = false;
                        return jo_Result;
                    }
                }
                else if (driverSet.Url.Contains("https://www.linkedin.com/uas/login-submit"))
                {
                    Thread.Sleep(5000);
                    driverSet.SwitchTo().DefaultContent();
                    webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                    foreach (var webElementIframe in webElementIframes)
                    {
                        if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                        {
                            driverSet.SwitchTo().Frame(webElementIframe);
                            Thread.Sleep(5000);
                            break;
                        }
                    }

                    // continue-as
                    if (CheckIsExists(driverSet, By.Id("continue-as")))
                    {
                        driverSet.FindElement(By.Id("continue-as")).Click();
                        Thread.Sleep(5000);
                    }

                    driverSet.SwitchTo().DefaultContent();
                    webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                    foreach (var webElementIframe in webElementIframes)
                    {
                        if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                        {
                            driverSet.SwitchTo().Frame(webElementIframe);
                            Thread.Sleep(5000);
                            break;
                        }
                    }

                    // continue-as
                    if (CheckIsExists(driverSet, By.Id("continue-as")))
                    {
                        driverSet.FindElement(By.Id("continue-as")).Click();
                        Thread.Sleep(5000);
                    }


                    driverSet.SwitchTo().DefaultContent();
                    webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                    foreach (var webElementIframe in webElementIframes)
                    {
                        if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                        {
                            driverSet.SwitchTo().Frame(webElementIframe);
                            Thread.Sleep(5000);
                            break;
                        }
                    }

                    // continue-as
                    if (CheckIsExists(driverSet, By.Id("continue-as")))
                    {
                        driverSet.FindElement(By.Id("continue-as")).Click();
                        Thread.Sleep(5000);
                    }
                }
                else if (driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/profile-location/new/"))
                {
                    Thread.Sleep(5000);
                    goto create_password;
                }
                else
                {
                    driverSet.SwitchTo().DefaultContent();
                    webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                    foreach (var webElementIframe in webElementIframes)
                    {
                        if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                        {
                            driverSet.SwitchTo().Frame(webElementIframe);
                            Thread.Sleep(5000);
                            break;
                        }
                    }

                    // continue-as
                    if (CheckIsExists(driverSet, By.Id("continue-as")))
                    {
                        driverSet.FindElement(By.Id("continue-as")).Click();
                        Thread.Sleep(5000);
                    }

                    driverSet.SwitchTo().DefaultContent();
                    webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                    foreach (var webElementIframe in webElementIframes)
                    {
                        if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                        {
                            driverSet.SwitchTo().Frame(webElementIframe);
                            Thread.Sleep(5000);
                            break;
                        }
                    }

                    // continue-as
                    if (CheckIsExists(driverSet, By.Id("continue-as")))
                    {
                        driverSet.FindElement(By.Id("continue-as")).Click();
                        Thread.Sleep(5000);
                    }


                    driverSet.SwitchTo().DefaultContent();
                    webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                    foreach (var webElementIframe in webElementIframes)
                    {
                        if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                        {
                            driverSet.SwitchTo().Frame(webElementIframe);
                            Thread.Sleep(5000);
                            break;
                        }
                    }

                    // continue-as
                    if (CheckIsExists(driverSet, By.Id("continue-as")))
                    {
                        driverSet.FindElement(By.Id("continue-as")).Click();
                        Thread.Sleep(5000);
                    }
                }


                driverSet.SwitchTo().DefaultContent();
                account.Running_Log = "IN:点击注册领英按钮";
                re_button:
                int iii = 0;
                bool isContain = true;
                try
                {
                    //点击注册按钮

                    driverSet.SwitchTo().DefaultContent();
                    account.Running_Log = "IN:点击注册领英按钮";
                    if (CheckIsExists(driverSet,
                            By.Id("join-form-submit")))
                    {
                        driverSet.FindElement(By.Id("join-form-submit"))
                            .Click();
                        Thread.Sleep(5000);
                    }

                    iii++;
                    if (iii > 4)
                    {
                        isContain = false;
                    }
                }
                catch
                {
                    isContain = false;
                }

                if (isContain)
                {
                    Thread.Sleep(5000);
                    if (driverSet.PageSource.Contains("Sorry, something went wrong. Please try again."))
                    {
                        goto re_button;
                    }
                }

                for (int i = 0; i < 5; i++)
                {
                    bool isVerify = false;
                    try
                    {
                        webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                        foreach (var webElementIframe in webElementIframes)
                        {
                            if (webElementIframe.GetAttribute("title").Contains("Captcha Challenge"))
                            {
                                isVerify = true;
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        isVerify = false;
                    }


                    if (!isVerify)
                    {
                        break;
                    }

                    Thread.Sleep(5000);
                }

                Thread.Sleep(5000);
                if (driverSet.Url.Contains("https://www.linkedin.com/feed") ||
                    driverSet.Url.Contains(
                        "https://www.linkedin.com/onboarding/start/profile-location/new/?source=coreg"))
                {
                    goto create_password;
                }

                driverSet.SwitchTo().DefaultContent();
                webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                foreach (var webElementIframe in webElementIframes)
                {
                    if (webElementIframe.GetAttribute("title").Contains("Security verification"))
                    {
                        driverSet.SwitchTo().Frame(webElementIframe);
                        Thread.Sleep(5000);
                        break;
                    }
                }

                if (CheckIsExists(driverSet,
                        By.Id("select-register-phone-country")))
                {
                    jo_Result["ErrorMsg"] = "IN:注册失败,需要绑定手机";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                Thread.Sleep(5000);

                //等待打码 或者sms 接码
                account.Running_Log = "IN:等待打码 或者sms 接码";

                Thread.Sleep(15000);
                if (driverSet.Url.Contains("https://www.linkedin.com/feed/") ||
                    driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/profile-location/new/") ||
                    driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/?source=coreg"))
                {
                    account.Running_Log = "IN:创建成功";
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Running_Log = $"IN:提取cookie";
                        account.Facebook_CK = strJson;
                    }

                    goto create_password;
                }

                Thread.Sleep(5000);
                account.Running_Log = "IN:跳转主页";
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
                Thread.Sleep(5000);
                if (driverSet.Url.Contains("https://www.linkedin.com/feed/") ||
                    driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/profile-location/new/"))
                {
                    account.Running_Log = "IN:创建成功";
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Running_Log = $"IN:提取cookie";
                        account.Facebook_CK = strJson;
                    }
                }
                else if (driverSet.Url.Contains("https://www.linkedin.com/uas/login-submit"))
                {
                    if (CheckIsExists(driverSet,
                            By.Id("join-form-submit")))
                    {
                        driverSet.FindElement(By.Id("join-form-submit"))
                            .Click();
                    }

                    Thread.Sleep(5000);
                }

                create_password:

                if (driverSet.Url.Contains(
                        "https://www.linkedin.com/zh-cn/customer/signup?trk=guest_homepage-basic_nav-header-join"))
                {
                    jo_Result["ErrorMsg"] = "IN:注册失败";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                //创建密码
                account.Running_Log = "IN:创建密码";
                driverSet.Navigate()
                    .GoToUrl(
                        "https://www.linkedin.com/passwordReset");
                Thread.Sleep(5000);
                //忘记密码 下一步
                account.Running_Log = "IN:忘记密码 下一步";
                if (CheckIsExists(driverSet,
                        By.Id("username")))
                {
                    driverSet.FindElement(By.Id("username")).Clear();
                    driverSet.FindElement(By.Id("username")).SendKeys(account.New_Mail_Name);
                    Thread.Sleep(5000);
                }

                if (CheckIsExists(driverSet,
                        By.Id("reset-password-submit-button")))
                {
                    driverSet.FindElement(By.Id("reset-password-submit-button")).Click();
                    Thread.Sleep(5000);
                }

                //等待打码 或者sms 接码
                account.Running_Log = "IN:修改密码,等待打码 或者sms 接码";
                Thread.Sleep(10000);


                for (int i = 0; i < 5; i++)
                {
                    bool isVerify = false;
                    try
                    {
                        webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                        foreach (var webElementIframe in webElementIframes)
                        {
                            if (webElementIframe.GetAttribute("title").Contains("Captcha Challenge"))
                            {
                                isVerify = true;
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        isVerify = false;
                    }


                    if (!isVerify)
                    {
                        break;
                    }

                    Thread.Sleep(5000);
                }

                var pinLinkedin = string.Empty;

                driverSet.SwitchTo().NewWindow(WindowType.Tab);
                bool isGetCode = true;
                int h = 0;
                do
                {
                    if (h > 8)
                    {
                        break;
                    }

                    driverSet.Navigate().GoToUrl("https://mail.google.com/mail/u/0/#inbox");
                    Thread.Sleep(15000);

                    //获取验证码
                    if (CheckIsExists(driverSet, By.TagName("span")))
                    {
                        var readOnlyCollection = driverSet.FindElements(By.TagName("span"));
                        foreach (var element in readOnlyCollection)
                        {
                            try
                            {
                                if (element.Text.Contains("PIN"))
                                {
                                    regexPattern = @"\b\d{6}\b";

                                    match = Regex.Match(element.Text, regexPattern);

                                    if (match.Success)
                                    {
                                        pinLinkedin = match.Value;
                                        isGetCode = false;
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    h++;
                } while (isGetCode);


                if (string.IsNullOrEmpty(pinLinkedin))
                {
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }

                    jo_Result["ErrorMsg"] = "IN:创建密码，获取验证码为空";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                Thread.Sleep(5000);
                try
                {
                    driverSet.Close();
                }
                catch
                {
                }

                //返回当前窗口
                if (driverSet.WindowHandles.Count > 0)
                {
                    foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                    {
                        driverSet.SwitchTo().Window(driverSetWindowHandle);
                        break;
                    }

                    Thread.Sleep(5000);
                }

                //输入验证码
                account.Running_Log = "IN:输入验证码" + pinLinkedin;
                if (CheckIsExists(driverSet,
                        By.Name("pin")))
                {
                    driverSet.FindElement(By.Name("pin"))
                        .SendKeys(pinLinkedin);
                    Thread.Sleep(5000);
                }

                //提交验证码
                account.Running_Log = "IN:提交验证码";
                if (CheckIsExists(driverSet,
                        By.Id("pin-submit-button")))
                {
                    driverSet.FindElement(By.Id("pin-submit-button")).Click();
                    Thread.Sleep(5000);
                }

                newPassword = this.GetNewPassword_EM();
                //输入新密码
                account.Running_Log = "IN:输入新密码" + newPassword;
                if (CheckIsExists(driverSet,
                        By.Id("newPassword")))
                {
                    driverSet.FindElement(By.Id("newPassword")).SendKeys(newPassword);
                }

                //确认新密码
                account.Running_Log = "IN:确认新密码" + newPassword;
                if (CheckIsExists(driverSet,
                        By.Id("confirmPassword")))
                {
                    driverSet.FindElement(By.Id("confirmPassword")).SendKeys(newPassword);
                    account.Facebook_Pwd = newPassword;
                    Thread.Sleep(5000);
                }


                //提交新密码
                account.Running_Log = "IN:提交新密码";
                if (CheckIsExists(driverSet,
                        By.Id("reset-password-submit-button")))
                {
                    driverSet.FindElement(By.Id("reset-password-submit-button")).Click();
                    Thread.Sleep(5000);
                }

                if (driverSet.PageSource.Contains("Your password has been changed"))
                {
                    account.Running_Log = "IN:修改密码成功";
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }
                }

                Thread.Sleep(5000);
                account.Running_Log = "IN:跳转主页";
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
                Thread.Sleep(5000);
                if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
                {
                    loginLinkedin:
                    if (CheckIsExists(driverSet,
                            By.Id("password")))
                    {
                        driverSet.FindElement(By.Id("password"))
                            .SendKeys(account.Facebook_Pwd);
                        Thread.Sleep(3000);
                    }

                    //提交登录
                    if (CheckIsExists(driverSet,
                            By.CssSelector("[class='btn__primary--large from__button--floating']")))
                    {
                        driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                            .Click();
                    }

                    Thread.Sleep(10000);
                    driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                    Thread.Sleep(3000);
                    if (CheckIsExists(driverSet,
                            By.Id("password")))
                    {
                        goto loginLinkedin;
                    }

                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }
                }

                if (driverSet.Url.Contains("https://www.linkedin.com/feed/"))
                {
                    account.Running_Log = "IN:创建成功";
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Running_Log = $"IN:提取cookie";
                        account.Facebook_CK = strJson;
                    }
                }

                driverSet.Navigate().GoToUrl(
                    "https://www.linkedin.com/mynetwork/invite-connect/connections/");
                Thread.Sleep(5000);
                if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
                {
                    int i = 0;
                    loginLinkedin:
                    i++;
                    if (i > 5)
                    {
                        jo_Result["ErrorMsg"] = "IN:登录失败";
                        jo_Result["Success"] = false;
                        return jo_Result;
                    }

                    if (CheckIsExists(driverSet,
                            By.Id("password")))
                    {
                        driverSet.FindElement(By.Id("password"))
                            .SendKeys(account.Facebook_Pwd);
                        Thread.Sleep(3000);
                    }

                    //提交登录
                    if (CheckIsExists(driverSet,
                            By.CssSelector("[class='btn__primary--large from__button--floating']")))
                    {
                        driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                            .Click();
                        Thread.Sleep(10000);
                    }

                    driverSet.Navigate().GoToUrl("https://www.linkedin.com/feed/");
                    Thread.Sleep(3000);
                    if (!driverSet.Url.Contains("https://www.linkedin.com/feed/"))
                    {
                        goto loginLinkedin;
                    }

                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }
                }

                account.Running_Log = $"IN:获取好友信息";
                account.HaoYouCount =
                    StringHelper.GetMidStr(driverSet.PageSource, "totalResultCount\":", ",\"secondaryFilterCluster");
                if (driverSet.PageSource.Contains("urn:li:fsd_profile:"))
                {
                    account.Running_Log = $"IN:FsdProfile";
                    account.FsdProfile =
                        StringHelper.GetMidStr(driverSet.PageSource, $"urn:li:fsd_profile:", $"\"");
                    var accountName = StringHelper.GetMidStr(driverSet.PageSource, "https://www.linkedin.com/in/",
                        "/recent-activity");
                    account.AccountName = "https://www.linkedin.com/in/" + accountName + "/";
                }

                driverSet.Navigate().GoToUrl(account.AccountName);
                account.AccountName = driverSet.Url;
                account.Running_Log = $"IN:AccountName" + account.AccountName;

                Thread.Sleep(5000);

                //切换到新的窗口
                if (driverSet.WindowHandles.Count > 0)
                {
                    foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                    {
                        driverSet.SwitchTo().Window(driverSetWindowHandle);
                        break;
                    }

                    Thread.Sleep(5000);
                }

                taskName = string.Empty;
                task = null;
                taskName = "RegisterINGoogleBindEmail";
                task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
                if (task != null && task.IsSelected)
                {
                    account.Running_Log = "IN:绑定邮箱";
                    driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                    Thread.Sleep(5000);
                    if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
                    {
                        //点击绑定邮箱按钮
                        if (CheckIsExists(driverSet, By.Id("password")))
                        {
                            driverSet.FindElement(By.Id("password")).SendKeys(account.Facebook_Pwd);

                            Thread.Sleep(5000);
                        }

                        //点击登录按钮
                        if (CheckIsExists(driverSet,
                                By.CssSelector("[class='btn__primary--large from__button--floating']")))
                        {
                            driverSet.FindElement(
                                    By.CssSelector("[class='btn__primary--large from__button--floating']"))
                                .Click();
                        }

                        Thread.Sleep(15000);
                        if (driverSet.Url.Contains("https://www.linkedin.com/checkpoint/challenge/"))
                        {
                            jo_Result["ErrorMsg"] = "IN:账号失效";
                            jo_Result["Success"] = false;
                            return jo_Result;
                        }
                    }

                    var webElementIframesEmail = driverSet.FindElements(By.TagName("iframe"));
                    foreach (var webElementIframe in webElementIframesEmail)
                    {
                        if (webElementIframe.GetAttribute("class").Contains("settings-iframe--frame"))
                        {
                            driverSet.SwitchTo().Frame(webElementIframe);
                            Thread.Sleep(5000);
                            break;
                        }
                    }

                    //点击绑定邮箱按钮
                    if (CheckIsExists(driverSet, By.Id("add-email-btn")))
                    {
                        driverSet.FindElement(By.Id("add-email-btn")).Click();

                        Thread.Sleep(5000);
                    }

                    //检查是否跳转
                    if (CheckIsExists(driverSet, By.Id("epc-pin-input")))
                    {
                        var googlePin = string.Empty;
                        driverSet.SwitchTo().NewWindow(WindowType.Tab);
                        Thread.Sleep(5000);
                        isGetCode = true;
                        h = 0;
                        do
                        {
                            if (h > 8)
                            {
                                break;
                            }

                            driverSet.Navigate().GoToUrl("https://mail.google.com/mail/u/0/#inbox");
                            Thread.Sleep(15000);

                            //获取验证码
                            if (CheckIsExists(driverSet, By.TagName("span")))
                            {
                                var readOnlyCollection = driverSet.FindElements(By.TagName("span"));
                                foreach (var element in readOnlyCollection)
                                {
                                    try
                                    {
                                        if (element.Text.Contains("PIN"))
                                        {
                                            regexPattern = @"\b\d{6}\b";

                                            match = Regex.Match(element.Text, regexPattern);

                                            if (match.Success)
                                            {
                                                googlePin = match.Value;
                                                isGetCode = false;
                                                break;
                                            }
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }
                            }

                            h++;
                        } while (isGetCode);


                        if (string.IsNullOrEmpty(googlePin))
                        {
                            jo_Result["ErrorMsg"] = "IN:绑定邮箱Google验证码获取失败";
                            jo_Result["Success"] = false;
                            return jo_Result;
                        }

                        try
                        {
                            driverSet.Close();
                        }
                        catch
                        {
                        }

                        //切换到新的窗口
                        if (driverSet.WindowHandles.Count > 0)
                        {
                            foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                            {
                                driverSet.SwitchTo().Window(driverSetWindowHandle);
                                break;
                            }

                            Thread.Sleep(5000);
                        }

                        webElementIframesEmail = driverSet.FindElements(By.TagName("iframe"));
                        foreach (var webElementIframe in webElementIframesEmail)
                        {
                            if (webElementIframe.GetAttribute("class").Contains("settings-iframe--frame"))
                            {
                                driverSet.SwitchTo().Frame(webElementIframe);
                                Thread.Sleep(5000);
                                break;
                            }
                        }

                        driverSet.FindElement(By.Id("epc-pin-input")).SendKeys(googlePin);

                        Thread.Sleep(5000);
                    }

                    //点击提交按钮
                    if (CheckIsExists(driverSet, By.Id("pin-submit-button")))
                    {
                        driverSet.FindElement(By.Id("pin-submit-button")).Click();

                        Thread.Sleep(5000);
                    }

                    //输入新邮箱
                    if (CheckIsExists(driverSet, By.Id("add-email")))
                    {
                        driverSet.FindElement(By.Id("add-email")).SendKeys(mailRu.Mail_Name);
                        account.RU_Mail_Name = mailRu.Mail_Name;
                        Thread.Sleep(5000);
                    }

                    //输入密码
                    if (CheckIsExists(driverSet, By.Id("enter-password")))
                    {
                        driverSet.FindElement(By.Id("enter-password")).SendKeys(account.Facebook_Pwd);
                        account.RU_Mail_Pwd = mailRu.Mail_Pwd;
                        Thread.Sleep(5000);
                    }

                    //提交绑定
                    if (CheckIsExists(driverSet, By.CssSelector("[class='submit-btn btn']")))
                    {
                        driverSet.FindElement(By.CssSelector("[class='submit-btn btn']")).Click();

                        Thread.Sleep(5000);
                    }

                    #region 去邮箱提取验证码

                    var timeSpan = 500;
                    var timeCount = 0;
                    var timeOut = 25000;
                    List<Pop3MailMessage> msgList;
                    Pop3MailMessage pop3MailMessage;
                    DateTime sendCodeTime = DateTime.Parse("1970-01-01");
                    account.Running_Log = $"绑邮箱:提取邮箱验证码[{mailRu.Mail_Name}]";

                    pop3MailMessage = null;
                    while (pop3MailMessage == null && timeCount < timeOut)
                    {
                        Thread.Sleep(timeSpan);
                        Application.DoEvents();
                        timeCount += timeSpan;

                        if (mailRu.Pop3Client != null && mailRu.Pop3Client.Connected)
                            try
                            {
                                mailRu.Pop3Client.Disconnect();
                            }
                            catch
                            {
                            }

                        mailRu.Pop3Client = Pop3Helper.GetPop3Client(mailRu.Mail_Name, mailRu.Mail_Pwd);
                        if (mailRu.Pop3Client == null) continue;

                        msgList = Pop3Helper.GetMessageByIndex(mailRu.Pop3Client);
                        pop3MailMessage = msgList.Where(m =>
                            m.DateSent >= sendCodeTime &&
                            m.From.Contains("<security-noreply@linkedin.com>")).FirstOrDefault();
                    }

                    if (mailRu.Pop3Client == null)
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

                    var confirmCode = StringHelper.GetMidStr(pop3MailMessage.Html,
                        "https://www.linkedin.com/comm/psettings/email/confirm", "\n").Trim();
                    if (string.IsNullOrEmpty(confirmCode))
                    {
                        jo_Result["ErrorMsg"] = "提取邮件验证码失败";
                        return jo_Result;
                    }

                    confirmCode = "https://www.linkedin.com/comm/psettings/email/confirm" +
                                  confirmCode;

                    #endregion

                    driverSet.Navigate().GoToUrl(confirmCode);
                    Thread.Sleep(5000);

                    //输入密码
                    if (CheckIsExists(driverSet, By.Id("password")))
                    {
                        driverSet.FindElement(By.Id("password")).SendKeys(account.Facebook_Pwd);

                        Thread.Sleep(5000);
                    }

                    //点击登录
                    if (CheckIsExists(driverSet,
                            By.CssSelector("[class='btn__primary--large from__button--floating']")))
                    {
                        driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                            .Click();

                        Thread.Sleep(5000);
                    }

                    driverSet.Navigate().GoToUrl("https://www.linkedin.com");
                    Thread.Sleep(15000);
                    if (driverSet.Url.Contains("https://www.linkedin.com/feed"))
                    {
                        //处理邮箱的绑定问题
                        lock (Program.setting.Setting_EM.Lock_Mail_ForBind_List)
                        {
                            mailRu.IsLocked = true;
                            mailRu.Is_Used = true;
                        }

                        account.Running_Log = "IN:绑定邮箱成功";
                        var cookieJar = driverSet.Manage().Cookies.AllCookies;
                        if (cookieJar.Count > 0)
                        {
                            string strJson = JsonConvert.SerializeObject(cookieJar);
                            account.Running_Log = $"IN:提取cookie";
                            account.Facebook_CK = strJson;
                        }
                    }

                    account.Running_Log = "IN:设置主邮箱";
                    driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                    Thread.Sleep(5000);
                    webElementIframesEmail = driverSet.FindElements(By.TagName("iframe"));
                    foreach (var webElementIframe in webElementIframesEmail)
                    {
                        if (webElementIframe.GetAttribute("class").Contains("settings-iframe--frame"))
                        {
                            driverSet.SwitchTo().Frame(webElementIframe);
                            Thread.Sleep(5000);
                            break;
                        }
                    }

                    //点击切换主邮箱按钮
                    if (CheckIsExists(driverSet, By.CssSelector("[class='isPrimary tertiary-btn']")))
                    {
                        driverSet.FindElement(By.CssSelector("[class='isPrimary tertiary-btn']")).Click();

                        Thread.Sleep(5000);
                    }

                    //输入密码
                    if (CheckIsExists(driverSet, By.Id("password")))
                    {
                        driverSet.FindElement(By.Id("password")).SendKeys(account.Facebook_Pwd);

                        Thread.Sleep(5000);
                    }

                    //点击切换主邮箱按钮
                    if (CheckIsExists(driverSet, By.CssSelector("[class='submit-button btn']")))
                    {
                        driverSet.FindElement(By.CssSelector("[class='submit-button btn']")).Click();

                        Thread.Sleep(5000);
                    }
                }

                #endregion
            }
        }
        catch (Exception e)
        {
            if (e.Message.Contains("The HTTP request to the remote WebDriver server for URL"))
            {
                jo_Result["ErrorMsg"] = "超时";
                jo_Result["Success"] = false;
                return jo_Result;
            }
            else
            {
                jo_Result["ErrorMsg"] = "其他错误";
                jo_Result["Success"] = false;
                return jo_Result;
            }
        }
        finally
        {
            if (Program.setting.Setting_EM.ADSPower)
            {
                adsPowerService.ADS_UserDelete(account.user_id);
            }
            else if (Program.setting.Setting_EM.BitBrowser)
            {
                bitBrowserService.BIT_BrowserDelete(account.user_id);
            }

            try
            {
                driverSet?.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Quit();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        jo_Result["Success"] = true;
        jo_Result["ErrorMsg"] = "注册谷歌:操作成功";
        return jo_Result;
    }

    /// <summary>
    /// 注册雅虎
    /// </summary>
    /// <param name="account"></param>
    /// <param name="driverSet"></param>
    /// <returns></returns>
    [Obsolete("Obsolete")]
    public JObject EM_RegisterYaSelenium(Account_FBOrIns account)
    {
        JObject jo_Result = new JObject();
        jo_Result["Success"] = false;
        jo_Result["ErrorMsg"] = string.Empty;
        account.Account_Id = UUID.StrSnowId;
        ChromeDriver driverSet = null;
        AdsPowerService adsPowerService = null;
        BitBrowserService bitBrowserService = null;
        openADS:
        if (Program.setting.Setting_EM.ADSPower)
        {
            account.Running_Log = "初始化ADSPower";
            adsPowerService = new AdsPowerService();
            if (!string.IsNullOrEmpty(account.user_id))
            {
                try
                {
                    account.Running_Log = "验证环境是否打开";
                    Thread.Sleep(5000);
                    var adsActiveBrowser = adsPowerService.ADS_ActiveBrowser(account.user_id);
                    if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                    {
                        var adsUserCreate =
                            adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                        account.user_id = adsUserCreate["data"]["id"].ToString();
                    }
                }
                catch (Exception e)
                {
                    Debug.Write(e.Message);
                }
            }
            else
            {
                account.Running_Log = "获取user_id";
                var adsUserCreate = adsPowerService.ADS_UserCreate("IN", account.Facebook_CK, account.UserAgent);
                account.user_id = adsUserCreate["data"]["id"].ToString();
                account.Running_Log = "获取user_id=[" + account.user_id + "]";
            }

            #region 实例化ADS API

            account.Running_Log = "打开ADS";
            var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
            if (adsStartBrowser["code"].ToString().Equals("-1"))
            {
                goto openADS;
            }

            account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
            account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
            account.Running_Log =
                "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

            #endregion
        }
        else if (Program.setting.Setting_EM.BitBrowser)
        {
            account.Running_Log = "初始化BitBrowser";
            bitBrowserService = new BitBrowserService();
            if (!string.IsNullOrEmpty(account.user_id))
            {
                try
                {
                    account.Running_Log = "验证环境是否打开";
                    Thread.Sleep(5000);
                    var adsActiveBrowser = bitBrowserService.BIT_BrowserPidsAlive(account.user_id);
                    if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                    {
                        var adsUserCreate =
                            adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                        account.user_id = adsUserCreate["data"]["id"].ToString();
                    }
                }
                catch (Exception e)
                {
                    Debug.Write(e.Message);
                }

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
                if (adsStartBrowser["code"].ToString().Equals("-1"))
                {
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
                account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }
            else
            {
                account.Running_Log = "查询分组";
                bitBrowserService = new BitBrowserService();
                string groupId = String.Empty;
                var bitGroupList = bitBrowserService.BIT_GroupList(0, 10);
                if (bitGroupList != null)
                {
                    var jToken = bitGroupList["data"]["list"];
                    if (jToken.Count() > 0)
                    {
                        foreach (var token in jToken)
                        {
                            if (token["groupName"].ToString().Equals("EM"))
                            {
                                groupId = token["id"].ToString();
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(groupId))
                    {
                        var bitGroupAdd = bitBrowserService.BIT_GroupAdd("EM", 99);
                        if (bitGroupAdd != null)
                        {
                            if ((bool)bitGroupAdd["success"])
                            {
                                groupId = bitGroupAdd["data"]["id"].ToString();
                            }
                        }
                    }
                }

                var adsUserCreate = bitBrowserService.BIT_BrowserUpdate(account.UserAgent, account.Facebook_CK, groupId,
                    "https://login.yahoo.com/account/create", "yahoo.com", "yahoo_" + account.Account_Id);
                account.user_id = adsUserCreate["data"]["id"].ToString();
                account.Running_Log = "获取user_id=[" + account.user_id + "]";

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = bitBrowserService.BIT_BrowserOpen(account.user_id);
                if (!(bool)adsStartBrowser["success"])
                {
                    bitBrowserService.BIT_BrowserDelete(account.user_id);
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["http"].ToString();
                account.webdriver = adsStartBrowser["data"]["driver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }
        }

        try
        {
            ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
            driverSet = chromeDriverSetting.GetDriverSetting("Yahoo", account.Account_Id, account.selenium,
                account.webdriver);
            Thread.Sleep(5000);

            //点击注册
            if (CheckIsExists(driverSet, By.CssSelector("[class='_yb_1o6ljpm  _yb_12i5op7 undefined']")))
            {
                driverSet.FindElement(By.CssSelector("[class='_yb_1o6ljpm  _yb_12i5op7 undefined']")).Click();
            }

            Thread.Sleep(5000);
            //创建邮箱
            if (CheckIsExists(driverSet, By.Id("createacc")))
            {
                driverSet.FindElement(By.Id("createacc")).Click();
            }

            Thread.Sleep(5000);
            var generateSurname = StringHelper.GenerateSurname();
            //First Name
            if (CheckIsExists(driverSet, By.Id("usernamereg-firstName")))
            {
                driverSet.FindElement(By.Id("usernamereg-firstName")).SendKeys("Mc");
            }

            //Last Name
            if (CheckIsExists(driverSet, By.Id("usernamereg-lastName")))
            {
                driverSet.FindElement(By.Id("usernamereg-lastName")).SendKeys(generateSurname);
            }

            //输入生成邮箱
            var emailAddress = StringHelper.GetRandomString(false, true, false, false, 10, 10);

            Random randomEmail = new Random();
            emailAddress += randomEmail.Next(1000, 9999);
            if (CheckIsExists(driverSet,
                    By.Id("usernamereg-userId")))
            {
                driverSet.FindElement(By.Id("usernamereg-userId")).Clear();
                driverSet.FindElement(By.Id("usernamereg-userId"))
                    .SendKeys(emailAddress);
            }

            var password = StringHelper.GetRandomString(true, true, true, false, 10, 10);
            //点击下一步提交
            if (CheckIsExists(driverSet, By.Id("usernamereg-password")))
            {
                driverSet.FindElement(By.Id("usernamereg-password")).SendKeys(password);
            }

            Thread.Sleep(5000);
            List<string> birthMonthList = new List<string>();
            birthMonthList.Add("January");
            birthMonthList.Add("February");
            birthMonthList.Add("March");
            birthMonthList.Add("April");
            birthMonthList.Add("May");
            birthMonthList.Add("June");
            birthMonthList.Add("July");
            birthMonthList.Add("August");
            birthMonthList.Add("September");
            birthMonthList.Add("October");
            birthMonthList.Add("November");
            birthMonthList.Add("December");
            Random random = new Random();
            int index = random.Next(birthMonthList.Count);
            var birthMonthText = birthMonthList[index];
            //输入月份
            if (CheckIsExists(driverSet,
                    By.Id("usernamereg-month")))
            {
                var findElement = driverSet.FindElement(By.Id("usernamereg-month"));
                SelectElement selectObj = new SelectElement(findElement);
                selectObj.SelectByText(birthMonthText);
            }

            // BirthDay
            List<string> birthDayList = new List<string>();
            for (int i = 0; i < 28; i++)
            {
                birthDayList.Add(i + "");
            }

            index = random.Next(birthDayList.Count);
            var birthDayText = birthDayList[index];
            //天
            if (CheckIsExists(driverSet, By.Id("usernamereg-day")))
            {
                driverSet.FindElement(By.Id("usernamereg-day")).SendKeys(birthDayText);
            }

            //年
            Random ran = new Random();
            int year = ran.Next(1905, 2000);
            if (CheckIsExists(driverSet, By.Id("usernamereg-year")))
            {
                driverSet.FindElement(By.Id("usernamereg-year")).SendKeys(year + "");
            }

            Thread.Sleep(5000);
            //点击下一步提交
            if (CheckIsExists(driverSet, By.Id("reg-submit-button")))
            {
                driverSet.FindElement(By.Id("reg-submit-button")).Click();
            }

            Thread.Sleep(5000);


            //phoneNumberId
            if (CheckIsExists(driverSet,
                    By.Id("usernamereg-phone")))
            {
                int i = 0;
                BindPhone :
                if (i > 3)
                {
                    jo_Result["ErrorMsg"] = "手机验证失败";
                    return jo_Result;
                }

                i++;
                SmsActivateService smsActivateService = new SmsActivateService();
                var smsGetPhoneNum = smsActivateService.SMS_GetPhoneNum("go", Program.setting.Setting_EM.Country);
                if (smsGetPhoneNum["ErrorMsg"] != null &&
                    !string.IsNullOrEmpty(smsGetPhoneNum["ErrorMsg"].ToString()))
                {
                    jo_Result["ErrorMsg"] = "获取手机失败";
                    return jo_Result;
                }

                driverSet.FindElement(By.Id("usernamereg-phone")).Clear();
                Thread.Sleep(1000);
                driverSet.FindElement(By.Id("usernamereg-phone"))
                    .SendKeys(smsGetPhoneNum["Number"].ToString());
                //点击下一步提交
                if (CheckIsExists(driverSet, By.Id("reg-sms-button")))
                {
                    driverSet.FindElement(By.Id("reg-sms-button")).Click();
                }

                Thread.Sleep(5000);

                var sms_code = string.Empty;
                int hh = 0;
                bool isGetCode = true;
                do
                {
                    Thread.Sleep(10000);
                    if (hh > 20)
                    {
                        isGetCode = false;
                    }

                    var smsGetCode = smsActivateService.SMS_GetCode(smsGetPhoneNum["phoneId"].ToString());
                    if (smsGetCode["Code"] != null)
                    {
                        if (string.IsNullOrEmpty(smsGetCode["Code"].ToString()))
                        {
                            Thread.Sleep(10000);
                        }
                        else
                        {
                            sms_code = smsGetCode["Code"].ToString();
                            break;
                        }
                    }

                    hh++;
                } while (isGetCode);

                //code
                if (CheckIsExists(driverSet,
                        By.Id("verification-code-field")))
                {
                    driverSet.FindElement(By.Id("verification-code-field"))
                        .SendKeys(sms_code);
                }

                Thread.Sleep(5000);
                //点击下一步
                if (CheckIsExists(driverSet, By.Id("verify-code-button")))
                {
                    driverSet.FindElement(By.Id("verify-code-button")).Click();
                }

                Thread.Sleep(5000);

                //点击下一步
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
                {
                    driverSet.FindElement(By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                        .Click();
                }

                Thread.Sleep(5000);

                //我同意
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
                {
                    driverSet.FindElement(By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                        .Click();
                }

                Thread.Sleep(5000);
                if (driverSet.Url.Contains("https://myaccount.google.com/?utm_source="))
                {
                    account.New_Mail_Name = emailAddress + "@gmail.com";
                    account.New_Mail_Pwd = password;
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }

                    if (Program.setting.Setting_EM.Account_List == null)
                    {
                        Program.setting.Setting_EM.Account_List = new List<Account_FBOrIns>();
                    }

                    Program.setting.Setting_EM.Account_List.Add(account);
                }

                Debug.Write("111");
            }
        }
        catch (Exception e)
        {
            if (e.Message.Contains("The HTTP request to the remote WebDriver server for URL"))
            {
                jo_Result["ErrorMsg"] = "超时";
                jo_Result["Success"] = false;
                return jo_Result;
            }
            else
            {
                jo_Result["ErrorMsg"] = "其他错误";
                jo_Result["Success"] = false;
                return jo_Result;
            }
        }
        finally
        {
            if (Program.setting.Setting_EM.ADSPower)
            {
                adsPowerService.ADS_UserDelete(account.user_id);
            }
            else if (Program.setting.Setting_EM.BitBrowser)
            {
                bitBrowserService.BIT_BrowserDelete(account.user_id);
            }

            try
            {
                driverSet?.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Quit();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        jo_Result["Success"] = true;
        jo_Result["ErrorMsg"] = "注册谷歌:操作成功";
        return jo_Result;
    }

    public JObject EM_LoginGoogle(Account_FBOrIns account, MailInfo mail, ChromeDriver driverSet)
    {
        JObject jo_Result = new JObject();
        jo_Result["Success"] = false;
        jo_Result["ErrorMsg"] = string.Empty;
        account.Running_Log = "IN:登录Google";
        driverSet.Navigate().GoToUrl(
            "https://accounts.google.com/");
        Thread.Sleep(8000);
        //输入google账号
        account.Running_Log = "IN:输入google账号";
        if (CheckIsExists(driverSet,
                By.Id("identifierId")))
        {
            driverSet.FindElement(By.Id("identifierId"))
                .SendKeys(mail.Mail_Name);

            Thread.Sleep(8000);
        }

        account.New_Mail_Name = mail.Mail_Name;
        account.Running_Log = "IN:输入google账号";

        //点击下一步
        account.Running_Log = "IN:点击下一步";
        if (CheckIsExists(driverSet,
                By.CssSelector(
                    "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
        {
            driverSet.FindElement(By.CssSelector(
                    "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                .Click();

            Thread.Sleep(8000);
        }

        //判断是否打码
        account.Running_Log = "IN:判断是否打码";
        if (CheckIsExists(driverSet,
                By.CssSelector(
                    "[class='dMNVAe']")))
        {
            if (driverSet.FindElement(By.CssSelector(
                    "[class='dMNVAe']")).Text.Contains("Confirm you’re not a robot"))
            {
                Thread.Sleep(10000);
                //点击下一步
                account.Running_Log = "IN:输入辅助邮箱,点击下一步";
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
                {
                    try
                    {
                        driverSet.FindElement(By.CssSelector(
                                "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                            .Click();
                    }
                    catch
                    {
                        jo_Result["ErrorMsg"] = "登录谷歌，打码超时";
                        jo_Result["Success"] = false;
                        return jo_Result;
                    }
                }
            }

            Thread.Sleep(8000);
        }

        //密码
        account.Running_Log = "IN:输入Google密码";
        if (CheckIsExists(driverSet,
                By.Name("Passwd")))
        {
            driverSet.FindElement(By.Name("Passwd"))
                .SendKeys(mail.Mail_Pwd);
        }

        account.New_Mail_Pwd = mail.Mail_Pwd;
        //点击下一步
        account.Running_Log = "IN:输入Google密码,点击下一步";
        if (CheckIsExists(driverSet,
                By.CssSelector(
                    "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
        {
            driverSet.FindElement(By.CssSelector(
                    "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                .Click();

            Thread.Sleep(8000);
        }

        account.Recovery_Email = mail.VerifyMail_Name;
        account.Recovery_Email_Password = mail.VerifyMail_Pwd;
        if (driverSet.Url.Contains("https://myaccount.google.com/?pli=1"))
        {
            var cookieJar = driverSet.Manage().Cookies.AllCookies;
            if (cookieJar.Count > 0)
            {
                string strJson = JsonConvert.SerializeObject(cookieJar);
                account.Running_Log = $"IN:提取cookie";
                account.Old_Mail_CK = strJson;
            }

            jo_Result["ErrorMsg"] = "登录成功";
            jo_Result["Success"] = true;
            return jo_Result;
        }

        //确认辅助邮箱
        account.Running_Log = "IN:确认辅助邮箱登录";
        if (CheckIsExists(driverSet,
                By.CssSelector("[class='l5PPKe']")))
        {
            foreach (var findElement in driverSet.FindElements(By.CssSelector(
                         "[class='l5PPKe']")))
            {
                if (findElement.Text.Contains("Confirm your recovery email"))
                {
                    findElement.Click();
                    break;
                }
            }

            Thread.Sleep(8000);
        }
        else if (CheckIsExists(driverSet,
                     By.CssSelector("[class='VV3oRb YZVTmd SmR8']")))
        {
            foreach (var findElement in driverSet.FindElements(By.CssSelector(
                         "[class='VV3oRb YZVTmd SmR8']")))
            {
                if (findElement.Text.Contains("Confirm your recovery email"))
                {
                    findElement.Click();
                    break;
                }
            }

            Thread.Sleep(8000);
        }
        else
        {
            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 BqKGqe eR0mzb TrZEUc lw1w4b']")))
            {
                driverSet.FindElement(By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 BqKGqe eR0mzb TrZEUc lw1w4b']"))
                    .Click();

                Thread.Sleep(10000);
            }

            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 k97fxb yu6jOd']")))
            {
                driverSet.FindElement(By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 k97fxb yu6jOd']"))
                    .Click();

                Thread.Sleep(8000);
            }
        }

        if (CheckIsExists(driverSet,
                By.Id("knowledge-preregistered-email-response")))
        {
            driverSet.FindElement(By.Id("knowledge-preregistered-email-response"))
                .SendKeys(mail.VerifyMail_Name);

            Thread.Sleep(8000);
        }

        //点击下一步
        account.Running_Log = "IN:输入辅助邮箱,点击下一步";
        if (CheckIsExists(driverSet,
                By.CssSelector(
                    "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
        {
            driverSet.FindElement(By.CssSelector(
                    "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                .Click();

            Thread.Sleep(10000);
        }


        //点击取消创建密码安全
        account.Running_Log = "IN:点击取消创建密码安全";
        if (CheckIsExists(driverSet,
                By.CssSelector(
                    "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 BqKGqe eR0mzb TrZEUc lw1w4b']")))
        {
            driverSet.FindElement(By.CssSelector(
                    "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 BqKGqe eR0mzb TrZEUc lw1w4b']"))
                .Click();
            Thread.Sleep(8000);
            //点击取消设置
            account.Running_Log = "IN:点击取消设置";
            if (CheckIsExists(driverSet,
                    By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 k97fxb yu6jOd']")))
            {
                driverSet.FindElement(By.CssSelector(
                        "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 k97fxb yu6jOd']"))
                    .Click();

                Thread.Sleep(5000);
            }
        }

        if (driverSet.Url.Contains("https://myaccount.google.com"))
        {
            var cookieJar = driverSet.Manage().Cookies.AllCookies;
            if (cookieJar.Count > 0)
            {
                string strJson = JsonConvert.SerializeObject(cookieJar);
                account.Running_Log = $"IN:提取cookie";
                account.Old_Mail_CK = strJson;
            }

            jo_Result["ErrorMsg"] = "登录成功";
            jo_Result["Success"] = true;
            return jo_Result;
        }
        else
        {
            jo_Result["ErrorMsg"] = "登录失败";
            jo_Result["Success"] = false;
            return jo_Result;
        }
    }

    /// <summary>
    /// 注册领英
    /// </summary>
    /// <param name="account"></param>
    /// <param name="mail"></param>
    /// <returns></returns>
    public JObject EM_RegisterLinkedinSeleniumByLoginGoolge(Account_FBOrIns account, MailInfo mailGoogle,
        MailInfo mailRu)
    {
        JObject jo_Result = new JObject();
        jo_Result["Success"] = false;
        jo_Result["ErrorMsg"] = string.Empty;
        account.Account_Id = UUID.StrSnowId;
        ChromeDriver driverSet = null;
        AdsPowerService adsPowerService = null;
        BitBrowserService bitBrowserService = null;

        try
        {
            openADS:
            if (Program.setting.Setting_EM.ADSPower)
            {
                account.Running_Log = "初始化ADSPower";
                adsPowerService = new AdsPowerService();
                if (!string.IsNullOrEmpty(account.user_id))
                {
                    try
                    {
                        account.Running_Log = "验证环境是否打开";
                        Thread.Sleep(5000);
                        var adsActiveBrowser = adsPowerService.ADS_ActiveBrowser(account.user_id);
                        if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                        {
                            var adsUserCreate =
                                adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                            account.user_id = adsUserCreate["data"]["id"].ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Write(e.Message);
                    }
                }
                else
                {
                    account.Running_Log = "获取user_id";
                    var adsUserCreate = adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                    account.user_id = adsUserCreate["data"]["id"].ToString();
                    account.Running_Log = "获取user_id=[" + account.user_id + "]";
                }

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
                if (adsStartBrowser["code"].ToString().Equals("-1"))
                {
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
                account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }
            else if (Program.setting.Setting_EM.BitBrowser)
            {
                account.Account_Id = string.Empty;
                account.Running_Log = "初始化BitBrowser";
                bitBrowserService = new BitBrowserService();
                JObject bitGroupList = null;
                string groupId = String.Empty;
                try
                {
                    account.Running_Log = "查询分组";
                    bitGroupList = bitBrowserService.BIT_GroupList(0, 10);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                if (bitGroupList != null)
                {
                    var jToken = bitGroupList["data"]["list"];
                    if (jToken.Count() > 0)
                    {
                        foreach (var token in jToken)
                        {
                            if (token["groupName"].ToString().Equals("EM"))
                            {
                                groupId = token["id"].ToString();
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(groupId))
                    {
                        var bitGroupAdd = bitBrowserService.BIT_GroupAdd("EM", 99);
                        if (bitGroupAdd != null)
                        {
                            if ((bool)bitGroupAdd["success"])
                            {
                                groupId = bitGroupAdd["data"]["id"].ToString();
                            }
                        }
                    }
                }

                var adsUserCreate = bitBrowserService.BIT_BrowserUpdate(account.UserAgent, account.Facebook_CK, groupId,
                    "https://accounts.google.com/", "google.com", "linkedin_" + account.Account_Id);
                account.user_id = adsUserCreate["data"]["id"].ToString();
                account.Running_Log = "获取user_id=[" + account.user_id + "]";

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = bitBrowserService.BIT_BrowserOpen(account.user_id);
                if (!(bool)adsStartBrowser["success"])
                {
                    bitBrowserService.BIT_BrowserDelete(account.user_id);
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["http"].ToString();
                account.webdriver = adsStartBrowser["data"]["driver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }

            ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
            if (driverSet == null)
            {
                driverSet = chromeDriverSetting.GetDriverSetting("Google", account.Account_Id, account.selenium,
                    account.webdriver);
            }

            var newPassword = string.Empty;

            var emLoginGoogle = EM_LoginGoogle(account, mailGoogle, driverSet);
            bool isSuccess = Convert.ToBoolean(emLoginGoogle["Success"].ToString());
            if (!isSuccess)
            {
                return emLoginGoogle;
            }

            account.Running_Log = "IN:访问注册页面";
            driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
            Thread.Sleep(15000);
            ReadOnlyCollection<IWebElement> webElementIframes;
            if (driverSet.Url.Contains("https://www.linkedin.com/feed/?trk=guest_homepage-basic_google-one-tap-submit"))
            {
                Thread.Sleep(5000);
                goto create_password;
            }
            else if (driverSet.Url.Contains("https://www.linkedin.com/checkpoint/challenge"))
            {
                Thread.Sleep(5000);
                if (driverSet.PageSource.Contains("Your account has been temporarily restricted as a precaution"))
                {
                    jo_Result["ErrorMsg"] = "IN:注册失败,封号";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }
            }
            else if (driverSet.Url.Contains("https://www.linkedin.com/uas/login-submit"))
            {
                Thread.Sleep(5000);
                driverSet.SwitchTo().DefaultContent();
                webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                foreach (var webElementIframe in webElementIframes)
                {
                    if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                    {
                        driverSet.SwitchTo().Frame(webElementIframe);
                        Thread.Sleep(5000);
                        break;
                    }
                }

                // continue-as
                if (CheckIsExists(driverSet, By.Id("continue-as")))
                {
                    driverSet.FindElement(By.Id("continue-as")).Click();
                    Thread.Sleep(5000);
                }

                driverSet.SwitchTo().DefaultContent();
                webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                foreach (var webElementIframe in webElementIframes)
                {
                    if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                    {
                        driverSet.SwitchTo().Frame(webElementIframe);
                        Thread.Sleep(5000);
                        break;
                    }
                }

                // continue-as
                if (CheckIsExists(driverSet, By.Id("continue-as")))
                {
                    driverSet.FindElement(By.Id("continue-as")).Click();
                    Thread.Sleep(5000);
                }


                driverSet.SwitchTo().DefaultContent();
                webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                foreach (var webElementIframe in webElementIframes)
                {
                    if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                    {
                        driverSet.SwitchTo().Frame(webElementIframe);
                        Thread.Sleep(5000);
                        break;
                    }
                }

                // continue-as
                if (CheckIsExists(driverSet, By.Id("continue-as")))
                {
                    driverSet.FindElement(By.Id("continue-as")).Click();
                    Thread.Sleep(5000);
                }
            }
            else if (driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/profile-location/new/"))
            {
                Thread.Sleep(5000);
                goto create_password;
            }
            else
            {
                driverSet.SwitchTo().DefaultContent();
                webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                foreach (var webElementIframe in webElementIframes)
                {
                    if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                    {
                        driverSet.SwitchTo().Frame(webElementIframe);
                        Thread.Sleep(5000);
                        break;
                    }
                }

                // continue-as
                if (CheckIsExists(driverSet, By.Id("continue-as")))
                {
                    driverSet.FindElement(By.Id("continue-as")).Click();
                    Thread.Sleep(5000);
                }

                driverSet.SwitchTo().DefaultContent();
                webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                foreach (var webElementIframe in webElementIframes)
                {
                    if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                    {
                        driverSet.SwitchTo().Frame(webElementIframe);
                        Thread.Sleep(5000);
                        break;
                    }
                }

                // continue-as
                if (CheckIsExists(driverSet, By.Id("continue-as")))
                {
                    driverSet.FindElement(By.Id("continue-as")).Click();
                    Thread.Sleep(5000);
                }


                driverSet.SwitchTo().DefaultContent();
                webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                foreach (var webElementIframe in webElementIframes)
                {
                    if (webElementIframe.GetAttribute("title").Contains("Sign in with Google Dialog"))
                    {
                        driverSet.SwitchTo().Frame(webElementIframe);
                        Thread.Sleep(5000);
                        break;
                    }
                }

                // continue-as
                if (CheckIsExists(driverSet, By.Id("continue-as")))
                {
                    driverSet.FindElement(By.Id("continue-as")).Click();
                    Thread.Sleep(5000);
                }
            }


            driverSet.SwitchTo().DefaultContent();
            account.Running_Log = "IN:点击注册领英按钮";
            re_button:
            int iii = 0;
            bool isContain = true;
            try
            {
                //点击注册按钮

                driverSet.SwitchTo().DefaultContent();
                account.Running_Log = "IN:点击注册领英按钮";
                if (CheckIsExists(driverSet,
                        By.Id("join-form-submit")))
                {
                    driverSet.FindElement(By.Id("join-form-submit"))
                        .Click();
                    Thread.Sleep(5000);
                }

                iii++;
                if (iii > 4)
                {
                    isContain = false;
                }
            }
            catch
            {
                isContain = false;
            }

            if (isContain)
            {
                Thread.Sleep(5000);
                if (driverSet.PageSource.Contains("Sorry, something went wrong. Please try again."))
                {
                    goto re_button;
                }
            }

            for (int i = 0; i < 5; i++)
            {
                bool isVerify = false;
                try
                {
                    webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                    foreach (var webElementIframe in webElementIframes)
                    {
                        if (webElementIframe.GetAttribute("title").Contains("Captcha Challenge"))
                        {
                            isVerify = true;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    isVerify = false;
                }


                if (!isVerify)
                {
                    break;
                }

                Thread.Sleep(5000);
            }

            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://www.linkedin.com/feed"))
            {
                goto create_password;
            }

            driverSet.SwitchTo().DefaultContent();
            webElementIframes = driverSet.FindElements(By.TagName("iframe"));
            foreach (var webElementIframe in webElementIframes)
            {
                if (webElementIframe.GetAttribute("title").Contains("Security verification"))
                {
                    driverSet.SwitchTo().Frame(webElementIframe);
                    Thread.Sleep(5000);
                    break;
                }
            }

            if (CheckIsExists(driverSet,
                    By.Id("select-register-phone-country")))
            {
                jo_Result["ErrorMsg"] = "IN:注册失败,需要绑定手机";
                jo_Result["Success"] = false;
                return jo_Result;
            }

            Thread.Sleep(5000);

            //等待打码 或者sms 接码
            account.Running_Log = "IN:等待打码 或者sms 接码";

            Thread.Sleep(15000);
            if (driverSet.Url.Contains("https://www.linkedin.com/feed/") ||
                driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/profile-location/new/") ||
                driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/?source=coreg"))
            {
                account.Running_Log = "IN:创建成功";
                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Running_Log = $"IN:提取cookie";
                    account.Facebook_CK = strJson;
                }

                goto create_password;
            }

            Thread.Sleep(5000);
            account.Running_Log = "IN:跳转主页";
            driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://www.linkedin.com/feed/") ||
                driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/profile-location/new/"))
            {
                account.Running_Log = "IN:创建成功";
                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Running_Log = $"IN:提取cookie";
                    account.Facebook_CK = strJson;
                }
            }
            else if (driverSet.Url.Contains("https://www.linkedin.com/uas/login-submit"))
            {
                if (CheckIsExists(driverSet,
                        By.Id("join-form-submit")))
                {
                    driverSet.FindElement(By.Id("join-form-submit"))
                        .Click();
                }

                Thread.Sleep(5000);
            }

            create_password:
            Match match;
            string regexPattern;
            if (account.IsAuthorization)
            {
                if (driverSet.Url.Contains(
                        "https://www.linkedin.com/zh-cn/customer/signup?trk=guest_homepage-basic_nav-header-join"))
                {
                    jo_Result["ErrorMsg"] = "IN:注册失败";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                //创建密码
                account.Running_Log = "IN:创建密码";
                driverSet.Navigate()
                    .GoToUrl(
                        "https://www.linkedin.com/passwordReset");
                Thread.Sleep(5000);
                //忘记密码 下一步
                account.Running_Log = "IN:忘记密码 下一步";
                if (CheckIsExists(driverSet,
                        By.Id("username")))
                {
                    driverSet.FindElement(By.Id("username")).Clear();
                    driverSet.FindElement(By.Id("username")).SendKeys(account.New_Mail_Name);
                    Thread.Sleep(5000);
                }

                if (CheckIsExists(driverSet,
                        By.Id("reset-password-submit-button")))
                {
                    driverSet.FindElement(By.Id("reset-password-submit-button")).Click();
                    Thread.Sleep(5000);
                }

                //等待打码 或者sms 接码
                account.Running_Log = "IN:修改密码,等待打码 或者sms 接码";
                Thread.Sleep(10000);


                for (int i = 0; i < 5; i++)
                {
                    bool isVerify = false;
                    try
                    {
                        webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                        foreach (var webElementIframe in webElementIframes)
                        {
                            if (webElementIframe.GetAttribute("title").Contains("Captcha Challenge"))
                            {
                                isVerify = true;
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        isVerify = false;
                    }


                    if (!isVerify)
                    {
                        break;
                    }

                    Thread.Sleep(5000);
                }

                var pinLinkedin = string.Empty;

                driverSet.SwitchTo().NewWindow(WindowType.Tab);
                bool isGetCode = true;
                int h = 0;
                do
                {
                    if (h > 8)
                    {
                        break;
                    }

                    driverSet.Navigate().GoToUrl("https://mail.google.com/mail/u/0/#inbox");
                    Thread.Sleep(15000);

                    //获取验证码
                    if (CheckIsExists(driverSet, By.TagName("span")))
                    {
                        var readOnlyCollection = driverSet.FindElements(By.TagName("span"));
                        foreach (var element in readOnlyCollection)
                        {
                            try
                            {
                                if (element.Text.Contains("PIN"))
                                {
                                    regexPattern = @"\b\d{6}\b";

                                    match = Regex.Match(element.Text, regexPattern);

                                    if (match.Success)
                                    {
                                        pinLinkedin = match.Value;
                                        isGetCode = false;
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    h++;
                } while (isGetCode);


                if (string.IsNullOrEmpty(pinLinkedin))
                {
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }

                    jo_Result["ErrorMsg"] = "IN:创建密码，获取验证码为空";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                Thread.Sleep(5000);
                try
                {
                    driverSet.Close();
                }
                catch
                {
                }

                //返回当前窗口
                if (driverSet.WindowHandles.Count > 0)
                {
                    foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                    {
                        driverSet.SwitchTo().Window(driverSetWindowHandle);
                        break;
                    }

                    Thread.Sleep(5000);
                }

                //输入验证码
                account.Running_Log = "IN:输入验证码" + pinLinkedin;
                if (CheckIsExists(driverSet,
                        By.Name("pin")))
                {
                    driverSet.FindElement(By.Name("pin"))
                        .SendKeys(pinLinkedin);
                    Thread.Sleep(5000);
                }

                //提交验证码
                account.Running_Log = "IN:提交验证码";
                if (CheckIsExists(driverSet,
                        By.Id("pin-submit-button")))
                {
                    driverSet.FindElement(By.Id("pin-submit-button")).Click();
                    Thread.Sleep(5000);
                }

                newPassword = this.GetNewPassword_EM();
                //输入新密码
                account.Running_Log = "IN:输入新密码" + newPassword;
                if (CheckIsExists(driverSet,
                        By.Id("newPassword")))
                {
                    driverSet.FindElement(By.Id("newPassword")).SendKeys(newPassword);
                }

                //确认新密码
                account.Running_Log = "IN:确认新密码" + newPassword;
                if (CheckIsExists(driverSet,
                        By.Id("confirmPassword")))
                {
                    driverSet.FindElement(By.Id("confirmPassword")).SendKeys(newPassword);
                    account.Facebook_Pwd = newPassword;
                    Thread.Sleep(5000);
                }


                //提交新密码
                account.Running_Log = "IN:提交新密码";
                if (CheckIsExists(driverSet,
                        By.Id("reset-password-submit-button")))
                {
                    driverSet.FindElement(By.Id("reset-password-submit-button")).Click();
                    Thread.Sleep(5000);
                }

                if (driverSet.PageSource.Contains("Your password has been changed"))
                {
                    account.Running_Log = "IN:修改密码成功";
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }
                }
            }

            Thread.Sleep(5000);
            account.Running_Log = "IN:跳转主页";
            driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
            {
                loginLinkedin:
                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password"))
                        .SendKeys(account.Facebook_Pwd);
                    Thread.Sleep(3000);
                }

                //提交登录
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='btn__primary--large from__button--floating']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                        .Click();
                }

                Thread.Sleep(10000);
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                Thread.Sleep(3000);
                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    goto loginLinkedin;
                }

                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Facebook_CK = strJson;
                }
            }

            if (driverSet.Url.Contains("https://www.linkedin.com/feed/"))
            {
                account.Running_Log = "IN:创建成功";
                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Running_Log = $"IN:提取cookie";
                    account.Facebook_CK = strJson;
                }
            }

            driverSet.Navigate().GoToUrl(
                "https://www.linkedin.com/mynetwork/invite-connect/connections/");
            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
            {
                int i = 0;
                loginLinkedin:
                i++;
                if (i > 5)
                {
                    jo_Result["ErrorMsg"] = "IN:登录失败";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password"))
                        .SendKeys(account.Facebook_Pwd);
                    Thread.Sleep(3000);
                }

                //提交登录
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='btn__primary--large from__button--floating']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                        .Click();
                    Thread.Sleep(10000);
                }

                driverSet.Navigate().GoToUrl("https://www.linkedin.com/feed/");
                Thread.Sleep(3000);
                if (!driverSet.Url.Contains("https://www.linkedin.com/feed/"))
                {
                    goto loginLinkedin;
                }

                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Facebook_CK = strJson;
                }
            }

            account.Running_Log = $"IN:获取好友信息";
            account.HaoYouCount =
                StringHelper.GetMidStr(driverSet.PageSource, "totalResultCount\":", ",\"secondaryFilterCluster");
            if (driverSet.PageSource.Contains("urn:li:fsd_profile:"))
            {
                account.Running_Log = $"IN:FsdProfile";
                account.FsdProfile =
                    StringHelper.GetMidStr(driverSet.PageSource, $"urn:li:fsd_profile:", $"\"");
                var accountName = StringHelper.GetMidStr(driverSet.PageSource, "https://www.linkedin.com/in/",
                    "/recent-activity");
                account.AccountName = "https://www.linkedin.com/in/" + accountName + "/";
            }

            driverSet.Navigate().GoToUrl(account.AccountName);
            account.AccountName = driverSet.Url;
            account.Running_Log = $"IN:AccountName" + account.AccountName;

            Thread.Sleep(5000);

            //切换到新的窗口
            if (driverSet.WindowHandles.Count > 0)
            {
                foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                {
                    driverSet.SwitchTo().Window(driverSetWindowHandle);
                    break;
                }

                Thread.Sleep(5000);
            }

            string taskName = string.Empty;
            TaskInfo task = null;
            taskName = "RegisterINGoogleBindEmail";
            task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "IN:绑定邮箱";
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                Thread.Sleep(5000);
                if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
                {
                    //点击绑定邮箱按钮
                    if (CheckIsExists(driverSet, By.Id("password")))
                    {
                        driverSet.FindElement(By.Id("password")).SendKeys(account.Facebook_Pwd);

                        Thread.Sleep(5000);
                    }

                    //点击登录按钮
                    if (CheckIsExists(driverSet,
                            By.CssSelector("[class='btn__primary--large from__button--floating']")))
                    {
                        driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                            .Click();
                    }

                    Thread.Sleep(15000);
                    if (driverSet.Url.Contains("https://www.linkedin.com/checkpoint/challenge/"))
                    {
                        jo_Result["ErrorMsg"] = "IN:账号失效";
                        jo_Result["Success"] = false;
                        return jo_Result;
                    }
                }

                var webElementIframesEmail = driverSet.FindElements(By.TagName("iframe"));
                foreach (var webElementIframe in webElementIframesEmail)
                {
                    if (webElementIframe.GetAttribute("class").Contains("settings-iframe--frame"))
                    {
                        driverSet.SwitchTo().Frame(webElementIframe);
                        Thread.Sleep(5000);
                        break;
                    }
                }

                //点击绑定邮箱按钮
                if (CheckIsExists(driverSet, By.Id("add-email-btn")))
                {
                    driverSet.FindElement(By.Id("add-email-btn")).Click();

                    Thread.Sleep(5000);
                }

                //检查是否跳转
                if (CheckIsExists(driverSet, By.Id("epc-pin-input")))
                {
                    var googlePin = string.Empty;
                    driverSet.SwitchTo().NewWindow(WindowType.Tab);
                    Thread.Sleep(5000);
                    bool isGetCode = true;
                    int h = 0;
                    do
                    {
                        if (h > 8)
                        {
                            break;
                        }

                        driverSet.Navigate().GoToUrl("https://mail.google.com/mail/u/0/#inbox");
                        Thread.Sleep(15000);

                        //获取验证码
                        if (CheckIsExists(driverSet, By.TagName("span")))
                        {
                            var readOnlyCollection = driverSet.FindElements(By.TagName("span"));
                            foreach (var element in readOnlyCollection)
                            {
                                try
                                {
                                    if (element.Text.Contains("PIN"))
                                    {
                                        regexPattern = @"\b\d{6}\b";

                                        match = Regex.Match(element.Text, regexPattern);

                                        if (match.Success)
                                        {
                                            googlePin = match.Value;
                                            isGetCode = false;
                                            break;
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }

                        h++;
                    } while (isGetCode);


                    if (string.IsNullOrEmpty(googlePin))
                    {
                        jo_Result["ErrorMsg"] = "IN:绑定邮箱Google验证码获取失败";
                        jo_Result["Success"] = false;
                        return jo_Result;
                    }

                    try
                    {
                        driverSet.Close();
                    }
                    catch
                    {
                    }

                    //切换到新的窗口
                    if (driverSet.WindowHandles.Count > 0)
                    {
                        foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                        {
                            driverSet.SwitchTo().Window(driverSetWindowHandle);
                            break;
                        }

                        Thread.Sleep(5000);
                    }

                    webElementIframesEmail = driverSet.FindElements(By.TagName("iframe"));
                    foreach (var webElementIframe in webElementIframesEmail)
                    {
                        if (webElementIframe.GetAttribute("class").Contains("settings-iframe--frame"))
                        {
                            driverSet.SwitchTo().Frame(webElementIframe);
                            Thread.Sleep(5000);
                            break;
                        }
                    }

                    driverSet.FindElement(By.Id("epc-pin-input")).SendKeys(googlePin);

                    Thread.Sleep(5000);
                }

                //点击提交按钮
                if (CheckIsExists(driverSet, By.Id("pin-submit-button")))
                {
                    driverSet.FindElement(By.Id("pin-submit-button")).Click();

                    Thread.Sleep(5000);
                }

                //输入新邮箱
                if (CheckIsExists(driverSet, By.Id("add-email")))
                {
                    driverSet.FindElement(By.Id("add-email")).SendKeys(mailRu.Mail_Name);
                    account.RU_Mail_Name = mailRu.Mail_Name;
                    Thread.Sleep(5000);
                }

                //输入密码
                if (CheckIsExists(driverSet, By.Id("enter-password")))
                {
                    driverSet.FindElement(By.Id("enter-password")).SendKeys(account.Facebook_Pwd);
                    account.RU_Mail_Pwd = mailRu.Mail_Pwd;
                    Thread.Sleep(5000);
                }

                //提交绑定
                if (CheckIsExists(driverSet, By.CssSelector("[class='submit-btn btn']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='submit-btn btn']")).Click();

                    Thread.Sleep(5000);
                }

                #region 去邮箱提取验证码

                var timeSpan = 500;
                var timeCount = 0;
                var timeOut = 25000;
                List<Pop3MailMessage> msgList;
                Pop3MailMessage pop3MailMessage;
                DateTime sendCodeTime = DateTime.Parse("1970-01-01");
                account.Running_Log = $"绑邮箱:提取邮箱验证码[{mailRu.Mail_Name}]";

                pop3MailMessage = null;
                while (pop3MailMessage == null && timeCount < timeOut)
                {
                    Thread.Sleep(timeSpan);
                    Application.DoEvents();
                    timeCount += timeSpan;

                    if (mailRu.Pop3Client != null && mailRu.Pop3Client.Connected)
                        try
                        {
                            mailRu.Pop3Client.Disconnect();
                        }
                        catch
                        {
                        }

                    mailRu.Pop3Client = Pop3Helper.GetPop3Client(mailRu.Mail_Name, mailRu.Mail_Pwd);
                    if (mailRu.Pop3Client == null) continue;

                    msgList = Pop3Helper.GetMessageByIndex(mailRu.Pop3Client);
                    pop3MailMessage = msgList.Where(m =>
                        m.DateSent >= sendCodeTime &&
                        m.From.Contains("<security-noreply@linkedin.com>")).FirstOrDefault();
                }

                if (mailRu.Pop3Client == null)
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

                var confirmCode = StringHelper.GetMidStr(pop3MailMessage.Html,
                    "https://www.linkedin.com/comm/psettings/email/confirm", "\n").Trim();
                if (string.IsNullOrEmpty(confirmCode))
                {
                    jo_Result["ErrorMsg"] = "提取邮件验证码失败";
                    return jo_Result;
                }

                confirmCode = "https://www.linkedin.com/comm/psettings/email/confirm" +
                              confirmCode;

                #endregion

                driverSet.Navigate().GoToUrl(confirmCode);
                Thread.Sleep(5000);

                //输入密码
                if (CheckIsExists(driverSet, By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password")).SendKeys(account.Facebook_Pwd);

                    Thread.Sleep(5000);
                }

                //点击登录
                if (CheckIsExists(driverSet, By.CssSelector("[class='btn__primary--large from__button--floating']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                        .Click();

                    Thread.Sleep(5000);
                }

                driverSet.Navigate().GoToUrl("https://www.linkedin.com");
                Thread.Sleep(15000);
                if (driverSet.Url.Contains("https://www.linkedin.com/feed"))
                {
                    //处理邮箱的绑定问题
                    lock (Program.setting.Setting_EM.Lock_Mail_ForBind_List)
                    {
                        mailRu.IsLocked = true;
                        mailRu.Is_Used = true;
                    }

                    account.Running_Log = "IN:绑定邮箱成功";
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Running_Log = $"IN:提取cookie";
                        account.Facebook_CK = strJson;
                    }
                }

                account.Running_Log = "IN:设置主邮箱";
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                Thread.Sleep(5000);
                webElementIframesEmail = driverSet.FindElements(By.TagName("iframe"));
                foreach (var webElementIframe in webElementIframesEmail)
                {
                    if (webElementIframe.GetAttribute("class").Contains("settings-iframe--frame"))
                    {
                        driverSet.SwitchTo().Frame(webElementIframe);
                        Thread.Sleep(5000);
                        break;
                    }
                }

                //点击切换主邮箱按钮
                if (CheckIsExists(driverSet, By.CssSelector("[class='isPrimary tertiary-btn']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='isPrimary tertiary-btn']")).Click();

                    Thread.Sleep(5000);
                }

                //输入密码
                if (CheckIsExists(driverSet, By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password")).SendKeys(account.Facebook_Pwd);

                    Thread.Sleep(5000);
                }

                //点击切换主邮箱按钮
                if (CheckIsExists(driverSet, By.CssSelector("[class='submit-button btn']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='submit-button btn']")).Click();

                    Thread.Sleep(5000);
                }
            }
        }
        catch (Exception e)
        {
            if (e.Message.Contains("The HTTP request to the remote WebDriver server for URL"))
            {
                jo_Result["Success"] = false;
                return jo_Result;
            }
            else
            {
                jo_Result["Success"] = false;
                return jo_Result;
            }
        }
        finally
        {
            if (Program.setting.Setting_EM.ADSPower)
            {
                adsPowerService.ADS_UserDelete(account.user_id);
            }
            else if (Program.setting.Setting_EM.BitBrowser)
            {
                try
                {
                    bitBrowserService.BIT_BrowserClose(account.user_id);
                    Thread.Sleep(5000);
                    var bitBrowserDetail = bitBrowserService.BIT_BrowserDetail(account.user_id);
                    Thread.Sleep(5000);
                    if (!string.IsNullOrEmpty(bitBrowserDetail["data"]["cookie"].ToString()))
                    {
                        account.Facebook_CK = bitBrowserDetail["data"]["cookie"].ToString();
                    }
                }
                catch (Exception e)
                {
                }

                bitBrowserService.BIT_BrowserDelete(account.user_id);
            }

            try
            {
                driverSet?.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Quit();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        jo_Result["Success"] = true;
        jo_Result["ErrorMsg"] = "注册:操作成功";
        return jo_Result;
    }

    /// <summary>
    /// 增加粉丝
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public JObject EM_AddFans(Account_FBOrIns account)
    {
        JObject jo_Result = new JObject();
        jo_Result["Success"] = false;
        jo_Result["ErrorMsg"] = string.Empty;
        List<Account_FBOrIns> accountFbOrInsList = null;
        List<Account_FBOrIns> accountAddFans = null;
        if (Program.setting.Setting_IN.Account_List != null &&
            Program.setting.Setting_IN.Account_List.Count > 0)
        {
            accountFbOrInsList = Program.setting.Setting_IN.Account_List;
        }

        if (accountFbOrInsList == null)
        {
            jo_Result["Success"] = false;
            jo_Result["ErrorMsg"] = "Cookie为空";
            return jo_Result;
        }

        var businessLinkedinCookie = account.Facebook_CK;

        #region 两个账号同时登录

        var linkedinLoginInfo = LinkedinService.LoginByJsonCookie(businessLinkedinCookie, account.UserAgent);

        if (!linkedinLoginInfo.Login_Success)
        {
            throw new Exception("登录失败");
        }

        #endregion

        foreach (var accountFbOrIns in accountFbOrInsList)
        {
            try
            {
                var operateResult = LinkedinService.AddFriend11(linkedinLoginInfo, accountFbOrIns);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Cookie失效"))
                {
                    throw e;
                }

                Debug.WriteLine(e.Message);
            }
        }

        return jo_Result;
    }

    /// <summary>
    /// 确认粉丝
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public JObject EM_BindEmail(Account_FBOrIns account, MailInfo mailnew)
    {
        JObject jo_Result = new JObject();
        jo_Result["Success"] = false;
        jo_Result["ErrorMsg"] = string.Empty;
        account.Account_Id = UUID.StrSnowId;
        ChromeDriver driverSet = null;
        AdsPowerService adsPowerService = null;
        BitBrowserService bitBrowserService = null;
        try
        {
            openADS:
            if (Program.setting.Setting_EM.ADSPower)
            {
                account.Running_Log = "初始化ADSPower";
                adsPowerService = new AdsPowerService();
                if (!string.IsNullOrEmpty(account.user_id))
                {
                    try
                    {
                        account.Running_Log = "验证环境是否打开";
                        Thread.Sleep(5000);
                        var adsActiveBrowser = adsPowerService.ADS_ActiveBrowser(account.user_id);
                        if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                        {
                            var adsUserCreate =
                                adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                            account.user_id = adsUserCreate["data"]["id"].ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Write(e.Message);
                    }
                }
                else
                {
                    account.Running_Log = "获取user_id";
                    var adsUserCreate = adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                    account.user_id = adsUserCreate["data"]["id"].ToString();
                    account.Running_Log = "获取user_id=[" + account.user_id + "]";
                }

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
                if (adsStartBrowser["code"].ToString().Equals("-1"))
                {
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
                account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }
            else if (Program.setting.Setting_EM.BitBrowser)
            {
                account.Account_Id = string.Empty;
                account.Running_Log = "初始化BitBrowser";
                bitBrowserService = new BitBrowserService();
                JObject bitGroupList = null;
                string groupId = String.Empty;
                try
                {
                    account.Running_Log = "查询分组";
                    bitGroupList = bitBrowserService.BIT_GroupList(0, 10);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                if (bitGroupList != null)
                {
                    var jToken = bitGroupList["data"]["list"];
                    if (jToken.Count() > 0)
                    {
                        foreach (var token in jToken)
                        {
                            if (token["groupName"].ToString().Equals("EM"))
                            {
                                groupId = token["id"].ToString();
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(groupId))
                    {
                        var bitGroupAdd = bitBrowserService.BIT_GroupAdd("EM", 99);
                        if (bitGroupAdd != null)
                        {
                            if ((bool)bitGroupAdd["success"])
                            {
                                groupId = bitGroupAdd["data"]["id"].ToString();
                            }
                        }
                    }
                }

                var adsUserCreate = bitBrowserService.BIT_BrowserUpdate(account.UserAgent, account.Facebook_CK, groupId,
                    "https://www.linkedin.com/mypreferences/d/manage-email-addresses", "linkedin.com",
                    "linkedin_" + account.Account_Id);
                account.user_id = adsUserCreate["data"]["id"].ToString();
                account.Running_Log = "获取user_id=[" + account.user_id + "]";

                #region 实例化ADS API

                account.Running_Log = "打开ADS";
                var adsStartBrowser = bitBrowserService.BIT_BrowserOpen(account.user_id);
                if (!(bool)adsStartBrowser["success"])
                {
                    bitBrowserService.BIT_BrowserDelete(account.user_id);
                    goto openADS;
                }

                account.selenium = adsStartBrowser["data"]["http"].ToString();
                account.webdriver = adsStartBrowser["data"]["driver"].ToString();
                account.Running_Log =
                    "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

                #endregion
            }

            ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
            if (driverSet == null)
            {
                driverSet = chromeDriverSetting.GetDriverSetting("Google", account.Account_Id, account.selenium,
                    account.webdriver);
            }

            driverSet.SwitchTo().NewWindow(WindowType.Tab);
            Thread.Sleep(5000);
            MailInfo mailGoogle = new MailInfo();
            mailGoogle.Mail_Name = account.New_Mail_Name;
            mailGoogle.Mail_Pwd = account.New_Mail_Pwd;
            mailGoogle.VerifyMail_Name = account.Recovery_Email;
            var emLoginGoogle = EM_LoginGoogle(account, mailGoogle, driverSet);
            bool isSuccess = Convert.ToBoolean(emLoginGoogle["Success"].ToString());
            if (!isSuccess)
            {
                return emLoginGoogle;
            }
            else
            {
                try
                {
                    driverSet.Close();
                }
                catch
                {
                }
            }

            //切换到新的窗口
            if (driverSet.WindowHandles.Count > 0)
            {
                foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                {
                    driverSet.SwitchTo().Window(driverSetWindowHandle);
                    break;
                }

                Thread.Sleep(5000);
            }

            account.Running_Log = "IN:绑定邮箱";
            driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
            {
                //点击绑定邮箱按钮
                if (CheckIsExists(driverSet, By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password")).SendKeys(account.Facebook_Pwd);

                    Thread.Sleep(5000);
                }

                //点击登录按钮
                if (CheckIsExists(driverSet, By.CssSelector("[class='btn__primary--large from__button--floating']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                        .Click();
                }

                Thread.Sleep(15000);
                if (driverSet.Url.Contains("https://www.linkedin.com/checkpoint/challenge/"))
                {
                    jo_Result["ErrorMsg"] = "IN:账号失效";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }
            }

            var webElementIframesEmail = driverSet.FindElements(By.TagName("iframe"));
            foreach (var webElementIframe in webElementIframesEmail)
            {
                if (webElementIframe.GetAttribute("class").Contains("settings-iframe--frame"))
                {
                    driverSet.SwitchTo().Frame(webElementIframe);
                    Thread.Sleep(5000);
                    break;
                }
            }

            //点击绑定邮箱按钮
            if (CheckIsExists(driverSet, By.Id("add-email-btn")))
            {
                driverSet.FindElement(By.Id("add-email-btn")).Click();

                Thread.Sleep(5000);
            }

            //检查是否跳转
            string regexPattern;
            Match match;
            if (CheckIsExists(driverSet, By.Id("epc-pin-input")))
            {
                var googlePin = string.Empty;
                driverSet.SwitchTo().NewWindow(WindowType.Tab);
                bool isGetCode = true;
                int h = 0;
                do
                {
                    if (h > 8)
                    {
                        break;
                    }

                    driverSet.Navigate().GoToUrl("https://mail.google.com/mail/u/0/#inbox");
                    Thread.Sleep(20000);

                    //获取验证码
                    if (CheckIsExists(driverSet, By.TagName("span")))
                    {
                        var readOnlyCollection = driverSet.FindElements(By.TagName("span"));
                        foreach (var element in readOnlyCollection)
                        {
                            try
                            {
                                if (element.Text.Contains("PIN"))
                                {
                                    regexPattern = @"\b\d{6}\b";

                                    match = Regex.Match(element.Text, regexPattern);

                                    if (match.Success)
                                    {
                                        googlePin = match.Value;
                                        isGetCode = false;
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    h++;
                } while (isGetCode);


                if (string.IsNullOrEmpty(googlePin))
                {
                    jo_Result["ErrorMsg"] = "IN:绑定邮箱Google验证码获取失败";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                try
                {
                    driverSet.Close();
                }
                catch
                {
                }

                //切换到新的窗口
                if (driverSet.WindowHandles.Count > 0)
                {
                    foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                    {
                        driverSet.SwitchTo().Window(driverSetWindowHandle);
                        break;
                    }

                    Thread.Sleep(8000);
                }

                webElementIframesEmail = driverSet.FindElements(By.TagName("iframe"));
                foreach (var webElementIframe in webElementIframesEmail)
                {
                    if (webElementIframe.GetAttribute("class").Contains("settings-iframe--frame"))
                    {
                        driverSet.SwitchTo().Frame(webElementIframe);
                        Thread.Sleep(8000);
                        break;
                    }
                }

                driverSet.FindElement(By.Id("epc-pin-input")).SendKeys(googlePin);

                Thread.Sleep(8000);
            }

            //点击提交按钮
            if (CheckIsExists(driverSet, By.Id("pin-submit-button")))
            {
                driverSet.FindElement(By.Id("pin-submit-button")).Click();

                Thread.Sleep(8000);
            }

            //输入新邮箱
            if (CheckIsExists(driverSet, By.Id("add-email")))
            {
                driverSet.FindElement(By.Id("add-email")).SendKeys(mailnew.Mail_Name);
                account.RU_Mail_Name = mailnew.Mail_Name;
                Thread.Sleep(8000);
            }

            //输入密码
            if (CheckIsExists(driverSet, By.Id("enter-password")))
            {
                driverSet.FindElement(By.Id("enter-password")).SendKeys(account.Facebook_Pwd);
                account.RU_Mail_Pwd = mailnew.Mail_Pwd;
                Thread.Sleep(8000);
            }

            //提交绑定
            if (CheckIsExists(driverSet, By.CssSelector("[class='submit-btn btn']")))
            {
                driverSet.FindElement(By.CssSelector("[class='submit-btn btn']")).Click();

                Thread.Sleep(8000);
            }

            #region 去邮箱提取验证码

            var timeSpan = 500;
            var timeCount = 0;
            var timeOut = 25000;
            List<Pop3MailMessage> msgList;
            Pop3MailMessage pop3MailMessage;
            DateTime sendCodeTime = DateTime.Parse("1970-01-01");
            account.Running_Log = $"绑邮箱:提取邮箱验证码[{mailnew.Mail_Name}]";

            pop3MailMessage = null;
            while (pop3MailMessage == null && timeCount < timeOut)
            {
                Thread.Sleep(timeSpan);
                Application.DoEvents();
                timeCount += timeSpan;

                if (mailnew.Pop3Client != null && mailnew.Pop3Client.Connected)
                    try
                    {
                        mailnew.Pop3Client.Disconnect();
                    }
                    catch
                    {
                    }

                mailnew.Pop3Client = Pop3Helper.GetPop3Client(mailnew.Mail_Name, mailnew.Mail_Pwd);
                if (mailnew.Pop3Client == null) continue;

                msgList = Pop3Helper.GetMessageByIndex(mailnew.Pop3Client);
                pop3MailMessage = msgList.Where(m =>
                    m.DateSent >= sendCodeTime &&
                    m.From.Contains("<security-noreply@linkedin.com>")).FirstOrDefault();
            }

            if (mailnew.Pop3Client == null)
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

            var confirmCode = StringHelper.GetMidStr(pop3MailMessage.Html,
                "https://www.linkedin.com/comm/psettings/email/confirm", "\n").Trim();
            if (string.IsNullOrEmpty(confirmCode))
            {
                jo_Result["ErrorMsg"] = "提取邮件验证码失败";
                return jo_Result;
            }

            confirmCode = "https://www.linkedin.com/comm/psettings/email/confirm" +
                          confirmCode;

            #endregion

            driverSet.Navigate().GoToUrl(confirmCode);
            Thread.Sleep(5000);

            //输入密码
            if (CheckIsExists(driverSet, By.Id("password")))
            {
                driverSet.FindElement(By.Id("password")).SendKeys(account.Facebook_Pwd);

                Thread.Sleep(5000);
            }

            //点击登录
            if (CheckIsExists(driverSet, By.CssSelector("[class='btn__primary--large from__button--floating']")))
            {
                driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']")).Click();

                Thread.Sleep(5000);
            }

            driverSet.Navigate().GoToUrl("https://www.linkedin.com");
            Thread.Sleep(15000);
            if (driverSet.Url.Contains("https://www.linkedin.com/feed"))
            {
                //处理邮箱的绑定问题
                lock (Program.setting.Setting_EM.Lock_Mail_ForBind_List)
                {
                    mailnew.IsLocked = true;
                    mailnew.Is_Used = true;
                }

                account.Running_Log = "IN:绑定邮箱成功";
                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Running_Log = $"IN:提取cookie";
                    account.Facebook_CK = strJson;
                }
            }

            account.Running_Log = "IN:设置主邮箱";
            driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
            Thread.Sleep(5000);
            webElementIframesEmail = driverSet.FindElements(By.TagName("iframe"));
            foreach (var webElementIframe in webElementIframesEmail)
            {
                if (webElementIframe.GetAttribute("class").Contains("settings-iframe--frame"))
                {
                    driverSet.SwitchTo().Frame(webElementIframe);
                    Thread.Sleep(5000);
                    break;
                }
            }

            //点击切换主邮箱按钮
            if (CheckIsExists(driverSet, By.CssSelector("[class='isPrimary tertiary-btn']")))
            {
                driverSet.FindElement(By.CssSelector("[class='isPrimary tertiary-btn']")).Click();

                Thread.Sleep(5000);
            }

            //输入密码
            if (CheckIsExists(driverSet, By.Id("password")))
            {
                driverSet.FindElement(By.Id("password")).SendKeys(account.Facebook_Pwd);

                Thread.Sleep(5000);
            }

            //点击切换主邮箱按钮
            if (CheckIsExists(driverSet, By.CssSelector("[class='submit-button btn']")))
            {
                driverSet.FindElement(By.CssSelector("[class='submit-button btn']")).Click();

                Thread.Sleep(5000);
            }

            string taskName = string.Empty;
            TaskInfo task = null;
            taskName = "OpenTwoFA";
            task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "IN:开启2FA";
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/two-factor-authentication");
                Thread.Sleep(5000);

                webElementIframesEmail = driverSet.FindElements(By.TagName("iframe"));
                foreach (var webElementIframe in webElementIframesEmail)
                {
                    if (webElementIframe.GetAttribute("class").Contains("settings-iframe--frame"))
                    {
                        driverSet.SwitchTo().Frame(webElementIframe);
                        Thread.Sleep(5000);
                        break;
                    }
                }

                //点击开启2Fa按钮
                if (CheckIsExists(driverSet, By.CssSelector("[class='about-btn btn']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='about-btn btn']")).Click();

                    Thread.Sleep(5000);
                }

                //获取邮箱验证码

                if (CheckIsExists(driverSet, By.Id("epc-pin-input")))
                {
                    #region 去邮箱提取验证码

                    account.Running_Log = $"开启2Fa:提取邮箱验证码[{mailnew.Mail_Name}]";
                    var emailCode = String.Empty;
                    pop3MailMessage = null;
                    while (pop3MailMessage == null && timeCount < timeOut)
                    {
                        Thread.Sleep(timeSpan);
                        Application.DoEvents();
                        timeCount += timeSpan;

                        if (mailnew.Pop3Client != null && mailnew.Pop3Client.Connected)
                            try
                            {
                                mailnew.Pop3Client.Disconnect();
                            }
                            catch
                            {
                            }

                        mailnew.Pop3Client = Pop3Helper.GetPop3Client(mailnew.Mail_Name, mailnew.Mail_Pwd);
                        if (mailnew.Pop3Client == null) continue;

                        msgList = Pop3Helper.GetMessageByIndex(mailnew.Pop3Client);
                        pop3MailMessage = msgList.Where(m =>
                            m.DateSent >= sendCodeTime && m.Subject.Contains("PIN") &&
                            m.From.Contains("<security-noreply@linkedin.com>")).FirstOrDefault();
                    }

                    if (mailnew.Pop3Client == null)
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

                    var htmlEmail = pop3MailMessage.Subject;
                    regexPattern = @"\b\d{6}\b";

                    match = Regex.Match(htmlEmail, regexPattern);

                    if (match.Success)
                    {
                        emailCode = match.Value;
                    }


                    if (string.IsNullOrEmpty(emailCode))
                    {
                        jo_Result["ErrorMsg"] = "验证码为空";
                        return jo_Result;
                    }

                    #endregion

                    //输入验证码
                    if (CheckIsExists(driverSet, By.Id("epc-pin-input")))
                    {
                        driverSet.FindElement(By.Id("epc-pin-input")).SendKeys(emailCode);

                        Thread.Sleep(8000);
                    }

                    //提交验证码
                    if (CheckIsExists(driverSet, By.Id("pin-submit-button")))
                    {
                        driverSet.FindElement(By.Id("pin-submit-button")).Click();

                        Thread.Sleep(8000);
                    }
                }

                //输入验证码
                if (CheckIsExists(driverSet, By.Id("authAppBtn")))
                {
                    driverSet.FindElement(By.Id("authAppBtn")).Click();

                    Thread.Sleep(8000);
                }

                //输入密码
                if (CheckIsExists(driverSet, By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password")).SendKeys(account.Facebook_Pwd);

                    Thread.Sleep(8000);
                }

                //提交验证码
                if (CheckIsExists(driverSet, By.CssSelector("[class='submit-button btn']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='submit-button btn']")).Click();

                    Thread.Sleep(8000);
                }

                //获取2FA
                if (CheckIsExists(driverSet, By.CssSelector("[class='text-secret-key']")))
                {
                    account.TwoFA_Dynamic_SecretKey =
                        driverSet.FindElement(By.CssSelector("[class='text-secret-key']")).Text;
                    Thread.Sleep(8000);
                }

                //提交2FA
                if (CheckIsExists(driverSet, By.CssSelector("[class='authenticator-setup-btn btn']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='authenticator-setup-btn btn']")).Click();
                    Thread.Sleep(8000);
                }

                // TwoFactorAuthNet.TwoFactorAuth tfa = new TwoFactorAuthNet.TwoFactorAuth("LinkedIn");
                // var code = tfa.GetCode(account.TwoFA_Dynamic_SecretKey);
                // //输入6位码
                // if (CheckIsExists(driverSet, By.Id("code-input")))
                // {
                //     driverSet.FindElement(By.Id("code-input")).SendKeys(code);
                //     Thread.Sleep(8000);
                // }

                //确认6位码
                if (CheckIsExists(driverSet, By.CssSelector("[class='authenticator-verify-btn btn']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='authenticator-verify-btn btn']")).Click();
                    Thread.Sleep(8000);
                }
            }
        }
        catch (Exception e)
        {
            if (e.Message.Contains("The HTTP request to the remote WebDriver server for URL"))
            {
                jo_Result["Success"] = false;
                return jo_Result;
            }
            else
            {
                jo_Result["Success"] = false;
                return jo_Result;
            }
        }
        finally
        {
            if (Program.setting.Setting_EM.ADSPower)
            {
                adsPowerService.ADS_UserDelete(account.user_id);
            }
            else if (Program.setting.Setting_EM.BitBrowser)
            {
                try
                {
                    bitBrowserService.BIT_BrowserClose(account.user_id);
                    Thread.Sleep(5000);
                    var bitBrowserDetail = bitBrowserService.BIT_BrowserDetail(account.user_id);
                    Thread.Sleep(5000);
                    if (!string.IsNullOrEmpty(bitBrowserDetail["data"]["cookie"].ToString()))
                    {
                        account.Facebook_CK = bitBrowserDetail["data"]["cookie"].ToString();
                    }
                }
                catch (Exception e)
                {
                }

                bitBrowserService.BIT_BrowserDelete(account.user_id);
            }

            try
            {
                driverSet?.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Quit();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        jo_Result["Success"] = true;
        jo_Result["ErrorMsg"] = "IN:操作成功";
        return jo_Result;
    }

    /// <summary>
    /// 确认粉丝
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public JObject EM_ConfirmFans(Account_FBOrIns account)
    {
        return null;
    }

    /// <summary>
    /// 注册领英
    /// </summary>
    /// <param name="account"></param>
    /// <param name="mail"></param>
    /// <returns></returns>
    public JObject EM_RegisterLinkedinSeleniumByLoginGoolge1(Account_FBOrIns account, MailInfo mail)
    {
        JObject jo_Result = new JObject();
        jo_Result["Success"] = false;
        jo_Result["ErrorMsg"] = string.Empty;
        account.Account_Id = UUID.StrSnowId;
        ChromeDriver driverSet = null;
        AdsPowerService adsPowerService = null;
        BitBrowserService bitBrowserService = null;
        openADS:
        if (Program.setting.Setting_EM.ADSPower)
        {
            account.Running_Log = "初始化ADSPower";
            adsPowerService = new AdsPowerService();
            if (!string.IsNullOrEmpty(account.user_id))
            {
                try
                {
                    account.Running_Log = "验证环境是否打开";
                    Thread.Sleep(5000);
                    var adsActiveBrowser = adsPowerService.ADS_ActiveBrowser(account.user_id);
                    if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                    {
                        var adsUserCreate =
                            adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                        account.user_id = adsUserCreate["data"]["id"].ToString();
                    }
                }
                catch (Exception e)
                {
                    Debug.Write(e.Message);
                }
            }
            else
            {
                account.Running_Log = "获取user_id";
                var adsUserCreate = adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                account.user_id = adsUserCreate["data"]["id"].ToString();
                account.Running_Log = "获取user_id=[" + account.user_id + "]";
            }

            #region 实例化ADS API

            account.Running_Log = "打开ADS";
            var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
            if (adsStartBrowser["code"].ToString().Equals("-1"))
            {
                goto openADS;
            }

            account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
            account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
            account.Running_Log =
                "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

            #endregion
        }
        else if (Program.setting.Setting_EM.BitBrowser)
        {
            account.Account_Id = string.Empty;
            account.Running_Log = "初始化BitBrowser";
            bitBrowserService = new BitBrowserService();
            JObject bitGroupList = null;
            string groupId = String.Empty;
            try
            {
                account.Running_Log = "查询分组";
                bitGroupList = bitBrowserService.BIT_GroupList(0, 10);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (bitGroupList != null)
            {
                var jToken = bitGroupList["data"]["list"];
                if (jToken.Count() > 0)
                {
                    foreach (var token in jToken)
                    {
                        if (token["groupName"].ToString().Equals("EM"))
                        {
                            groupId = token["id"].ToString();
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(groupId))
                {
                    var bitGroupAdd = bitBrowserService.BIT_GroupAdd("EM", 99);
                    if (bitGroupAdd != null)
                    {
                        if ((bool)bitGroupAdd["success"])
                        {
                            groupId = bitGroupAdd["data"]["id"].ToString();
                        }
                    }
                }
            }

            var adsUserCreate = bitBrowserService.BIT_BrowserUpdate(account.UserAgent, account.Facebook_CK, groupId,
                "https://accounts.google.com/", "google.com", "linkedin_" + account.Account_Id);
            account.user_id = adsUserCreate["data"]["id"].ToString();
            account.Running_Log = "获取user_id=[" + account.user_id + "]";

            #region 实例化ADS API

            account.Running_Log = "打开ADS";
            var adsStartBrowser = bitBrowserService.BIT_BrowserOpen(account.user_id);
            if (!(bool)adsStartBrowser["success"])
            {
                bitBrowserService.BIT_BrowserDelete(account.user_id);
                goto openADS;
            }

            account.selenium = adsStartBrowser["data"]["http"].ToString();
            account.webdriver = adsStartBrowser["data"]["driver"].ToString();
            account.Running_Log =
                "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

            #endregion
        }

        try
        {
            ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
            if (driverSet == null)
            {
                driverSet = chromeDriverSetting.GetDriverSetting("Google", account.Account_Id, account.selenium,
                    account.webdriver);
            }

            var newPassword = string.Empty;

            var emLoginGoogle = EM_LoginGoogle(account, mail, driverSet);
            bool isSuccess = Convert.ToBoolean(emLoginGoogle["Success"].ToString());
            if (!isSuccess)
            {
                return emLoginGoogle;
            }

            account.Running_Log = "IN:访问注册页面";
            openAuth:
            driverSet.Navigate().GoToUrl(
                "https://www.linkedin.com/");
            Thread.Sleep(5000);
            driverSet.Navigate().GoToUrl(
                "https://www.linkedin.com/signup?trk=guest_homepage-basic_nav-header-join");
            Thread.Sleep(5000);
            driverSet.Navigate().Refresh();
            if (driverSet.Url.Contains("https://www.linkedin.cn/"))
            {
                jo_Result["ErrorMsg"] = "挂代理失败";
                jo_Result["Success"] = false;
                return jo_Result;
            }

            ReadOnlyCollection<IWebElement> webElementIframes;


            account.Running_Log = "IN:开启授权注册";
            driverSet.SwitchTo().DefaultContent();
            Thread.Sleep(5000);
            webElementIframes = driverSet.FindElements(By.TagName("iframe"));
            foreach (var webElementIframe in webElementIframes)
            {
                if (webElementIframe.GetAttribute("id").Contains("gsi_"))
                {
                    driverSet.SwitchTo().Frame(webElementIframe);
                    Thread.Sleep(5000);
                    break;
                }
            }

            // if (!CheckIsExists(driverSet,
            //         By.CssSelector("[class='nsm7Bb-HzV7m-LgbsSe jVeSEe i5vt6e-Ia7Qfc JGcpL-RbRzK']")))
            // {
            //   goto openAuth;
            // }
            account.Running_Log = "IN:点击google 授权按钮";

            //点击google 授权
            if (CheckIsExists(driverSet,
                    By.CssSelector("[class='nsm7Bb-HzV7m-LgbsSe jVeSEe i5vt6e-Ia7Qfc JGcpL-RbRzK']")))
            {
                driverSet.FindElement(
                        By.CssSelector("[class='nsm7Bb-HzV7m-LgbsSe jVeSEe i5vt6e-Ia7Qfc JGcpL-RbRzK']"))
                    .Click();

                Thread.Sleep(5000);
            }
            else if (CheckIsExists(driverSet,
                         By.CssSelector("[class='nsm7Bb-HzV7m-LgbsSe-BPrWId']")))
            {
                driverSet.FindElement(By.CssSelector("[class='nsm7Bb-HzV7m-LgbsSe-BPrWId']"))
                    .Click();

                Thread.Sleep(5000);
            }

            //切换到新的窗口
            if (driverSet.WindowHandles.Count > 0)
            {
                foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                {
                    if (driverSet.CurrentWindowHandle != driverSetWindowHandle)
                    {
                        driverSet.SwitchTo().Window(driverSetWindowHandle);
                        break;
                    }
                }

                Thread.Sleep(5000);
            }

            //点击google邮箱授权
            account.Running_Log = "IN:点击google授权";
            if (CheckIsExists(driverSet,
                    By.CssSelector("[class='fFW7wc-ibnC6b-K4efff']")))
            {
                driverSet.FindElement(By.CssSelector("[class='fFW7wc-ibnC6b-K4efff']"))
                    .Click();
                Thread.Sleep(5000);
            }

            //点击googleConfirm
            account.Running_Log = "IN:点击google授权确认";
            if (CheckIsExists(driverSet,
                    By.Id("confirm_yes")))
            {
                driverSet.FindElement(By.Id("confirm_yes"))
                    .Click();
                Thread.Sleep(5000);
            }

            //返回当前窗口
            if (driverSet.WindowHandles.Count > 0)
            {
                foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                {
                    driverSet.SwitchTo().Window(driverSetWindowHandle);
                    break;
                }

                Thread.Sleep(5000);
            }


            //是否创建过领英
            account.Running_Log = "IN:是否创建成功";
            if (driverSet.Url.Contains("https://www.linkedin.com/feed/?trk=registration-frontend"))
            {
                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Running_Log = $"IN:提取cookie";
                    account.Facebook_CK = strJson;
                }

                goto create_password;
            }

            account.Running_Log = "IN:是否创建过领英";
            //点是否注册
            if (CheckIsExists(driverSet,
                    By.CssSelector("[class='join-with-google__title']")))
            {
                var findElement = driverSet.FindElement(By.CssSelector("[class='join-with-google__title']"));
                if (!findElement.Text.Contains("Join LinkedIn"))
                {
                    var findElement1 = driverSet.FindElement(By.CssSelector("[class='main__subtitle']"));
                    if (!findElement1.Text.Contains("Make the most of your professional life"))
                    {
                        goto create_password;
                    }
                }
            }
            else if (CheckIsExists(driverSet,
                         By.CssSelector("[class='header__content__heading ']")))
            {
                var findElement = driverSet.FindElement(By.CssSelector("[class='header__content__heading ']"));
                if (findElement.Text.Contains("Sign in"))
                {
                    goto create_password;
                }
            }
            else if (CheckIsExists(driverSet,
                         By.CssSelector("[class='main__subtitle ']")))
            {
                var findElement1 = driverSet.FindElement(By.CssSelector("[class='main__subtitle ']"));
                if (!findElement1.Text.Contains("Make the most of your professional life"))
                {
                    goto create_password;
                }
            }

            if (driverSet.Url.Contains("https://www.linkedin.com/feed"))
            {
                goto create_password;
            }

            Thread.Sleep(5000);

            re_button:
            bool isContain = true;
            try
            {
                //点击注册按钮

                driverSet.SwitchTo().DefaultContent();
                account.Running_Log = "IN:点击注册领英按钮";
                if (CheckIsExists(driverSet,
                        By.Id("join-form-submit")))
                {
                    driverSet.FindElement(By.Id("join-form-submit"))
                        .Click();
                    Thread.Sleep(5000);
                }
            }
            catch
            {
                isContain = false;
            }

            if (isContain)
            {
                Thread.Sleep(5000);
                if (driverSet.PageSource.Contains("Sorry, something went wrong. Please try again."))
                {
                    goto re_button;
                }
            }


            //等待打码 或者sms 接码
            account.Running_Log = "IN:等待打码 或者sms 接码";

            Thread.Sleep(15000);
            if (driverSet.Url.Contains("https://www.linkedin.com/feed/") ||
                driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/profile-location/new/") ||
                driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/?source=coreg"))
            {
                account.Running_Log = "IN:创建成功";
                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Running_Log = $"IN:提取cookie";
                    account.Facebook_CK = strJson;
                }

                goto create_password;
            }

            driverSet.SwitchTo().DefaultContent();
            Thread.Sleep(5000);
            webElementIframes = driverSet.FindElements(By.TagName("iframe"));
            driverSet.SwitchTo().Frame(webElementIframes[1]);

            phoneNumber:
            if (CheckIsExists(driverSet,
                    By.Id("select-register-phone-country")))
            {
                account.Running_Log = "IN:开始接码";
                var findElement = driverSet.FindElement(By.Id("select-register-phone-country"));
                SelectElement selectObj = new SelectElement(findElement);
                selectObj.SelectByValue("hk");

                int hhh = 0;
                BindPhone :
                account.Running_Log = "IN:调用接码平台获取电话号码" + hhh;
                hhh++;
                if (hhh > 5)
                {
                    jo_Result["ErrorMsg"] = "获取手机号码失败        ";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                SmsActivateService smsActivateService = new SmsActivateService();
                var smsGetPhoneNum = smsActivateService.SMS_GetPhoneNum("tn", Program.setting.Setting_EM.Country);
                if (smsGetPhoneNum["ErrorMsg"] != null &&
                    !string.IsNullOrEmpty(smsGetPhoneNum["ErrorMsg"].ToString()))
                {
                    jo_Result["ErrorMsg"] = "绑定电话错误";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                //输入收集号码
                account.Running_Log = "IN:输入手机号码";
                if (CheckIsExists(driverSet,
                        By.Id("register-verification-phone-number")))
                {
                    driverSet.FindElement(
                        By.Id("register-verification-phone-number")).SendKeys(smsGetPhoneNum["Number"].ToString());
                }

                Thread.Sleep(5000);
                //提交手机号码
                account.Running_Log = "IN:提交手机号码";
                if (CheckIsExists(driverSet,
                        By.Id("register-phone-submit-button")))
                {
                    driverSet.FindElement(By.Id("register-phone-submit-button"))
                        .Click();
                }

                Thread.Sleep(5000);
                if (driverSet.PageSource.Contains("You can’t use this phone number. Please try a different one") ||
                    driverSet.PageSource.Contains("Something unexpected happened. Please try again.") ||
                    driverSet.PageSource.Contains("Oops, this isn’t a valid phone number. Try entering it again."))
                {
                    goto BindPhone;
                }

                account.Running_Log = "IN:调用接码平台获取验证码";
                var sms_code = string.Empty;
                int hh = 0;
                bool isGetCode = true;
                do
                {
                    Thread.Sleep(10000);
                    if (hh > 5)
                    {
                        isGetCode = false;
                    }

                    var smsGetCode = smsActivateService.SMS_GetCode(smsGetPhoneNum["phoneId"].ToString());
                    if (smsGetCode["Code"] != null)
                    {
                        if (string.IsNullOrEmpty(smsGetCode["Code"].ToString()))
                        {
                            Thread.Sleep(10000);
                        }
                        else
                        {
                            sms_code = smsGetCode["Code"].ToString();
                            break;
                        }
                    }

                    hh++;
                } while (isGetCode);

                if (string.IsNullOrEmpty(sms_code))
                {
                    jo_Result["ErrorMsg"] = "手机验证码获取失败";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                //验证码
                account.Running_Log = "IN:调用接码平台获取验证码=" + sms_code;
                if (CheckIsExists(driverSet,
                        By.Id("input__phone_verification_pin")))
                {
                    driverSet.FindElement(By.Id("input__phone_verification_pin"))
                        .SendKeys(sms_code);
                    Thread.Sleep(5000);
                }
            }
            else
            {
                try
                {
                    if (driverSet.PageSource.Contains("Your noCAPTCHA user response code is missing or invalid."))
                    {
                        jo_Result["ErrorMsg"] = "打码失败";
                        jo_Result["Success"] = false;
                        return jo_Result;
                    }

                    driverSet.SwitchTo().DefaultContent();
                    if (driverSet.PageSource.Contains("Someone’s already using that email."))
                    {
                        jo_Result["ErrorMsg"] = "邮箱占用";
                        jo_Result["Success"] = false;
                        return jo_Result;
                    }
                }
                catch (Exception e)
                {
                }
            }

            if (CheckIsExists(driverSet,
                    By.Id("select-register-phone-country")))
            {
                goto phoneNumber;
                Thread.Sleep(5000);
            }

            //提交验证码
            account.Running_Log = "IN:提交验证码";
            if (CheckIsExists(driverSet,
                    By.Id("register-phone-submit-button")))
            {
                driverSet.FindElement(By.Id("register-phone-submit-button"))
                    .Click();
            }

            Thread.Sleep(5000);
            account.Running_Log = "IN:跳转主页";
            driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://www.linkedin.com/feed/") ||
                driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/profile-location/new/"))
            {
                account.Running_Log = "IN:创建成功";
                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Running_Log = $"IN:提取cookie";
                    account.Facebook_CK = strJson;
                }
            }
            else if (driverSet.Url.Contains("https://www.linkedin.com/uas/login-submit"))
            {
                if (CheckIsExists(driverSet,
                        By.Id("join-form-submit")))
                {
                    driverSet.FindElement(By.Id("join-form-submit"))
                        .Click();
                }

                Thread.Sleep(5000);
                goto phoneNumber;
            }

            create_password:

            if (driverSet.Url.Contains("https://www.linkedin.com/uas/login-submit"))
            {
                jo_Result["ErrorMsg"] = "IN:跳转失败";
                jo_Result["Success"] = false;
                return jo_Result;
            }

            if (account.IsAuthorization)
            {
                if (driverSet.Url.Contains(
                        "https://www.linkedin.com/zh-cn/customer/signup?trk=guest_homepage-basic_nav-header-join"))
                {
                    jo_Result["ErrorMsg"] = "IN:注册失败";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                //创建密码
                account.Running_Log = "IN:创建密码";
                driverSet.Navigate()
                    .GoToUrl(
                        "https://www.linkedin.com/passwordReset");
                Thread.Sleep(5000);
                //忘记密码 下一步
                account.Running_Log = "IN:忘记密码 下一步";
                if (CheckIsExists(driverSet,
                        By.Id("username")))
                {
                    driverSet.FindElement(By.Id("username")).Clear();
                    driverSet.FindElement(By.Id("username")).SendKeys(account.New_Mail_Name);
                    Thread.Sleep(5000);
                }

                if (CheckIsExists(driverSet,
                        By.Id("reset-password-submit-button")))
                {
                    driverSet.FindElement(By.Id("reset-password-submit-button")).Click();
                    Thread.Sleep(5000);
                }

                //等待打码 或者sms 接码
                account.Running_Log = "IN:修改密码,等待打码 或者sms 接码";
                Thread.Sleep(10000);
                var pinLinkedin = string.Empty;
                bool expression = true;
                int kk = 0;
                do
                {
                    if (kk > 5)
                    {
                        break;
                    }

                    if (driverSet.Url.Contains("https://www.linkedin.com/checkpoint/challenge"))
                    {
                        expression = false;
                        break;
                    }

                    Thread.Sleep(5000);
                    kk++;
                } while (expression);

                driverSet.SwitchTo().NewWindow(WindowType.Tab);
                Thread.Sleep(5000);
                bool isGetCode = true;
                int h = 0;
                do
                {
                    if (h > 8)
                    {
                        break;
                    }

                    driverSet.Navigate().GoToUrl("https://mail.google.com/mail/u/0/#inbox");
                    Thread.Sleep(15000);

                    //获取验证码
                    if (CheckIsExists(driverSet, By.TagName("span")))
                    {
                        var readOnlyCollection = driverSet.FindElements(By.TagName("span"));
                        foreach (var element in readOnlyCollection)
                        {
                            try
                            {
                                if (element.Text.Contains("PIN"))
                                {
                                    string regexPattern = @"\b\d{6}\b";

                                    Match match = Regex.Match(element.Text, regexPattern);

                                    if (match.Success)
                                    {
                                        pinLinkedin = match.Value;
                                        isGetCode = false;
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    h++;
                } while (isGetCode);


                if (string.IsNullOrEmpty(pinLinkedin))
                {
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }

                    jo_Result["ErrorMsg"] = "IN:创建密码，获取验证码为空";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                Thread.Sleep(5000);
                //返回当前窗口
                if (driverSet.WindowHandles.Count > 0)
                {
                    foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                    {
                        driverSet.SwitchTo().Window(driverSetWindowHandle);
                        break;
                    }

                    Thread.Sleep(5000);
                }

                //输入验证码
                account.Running_Log = "IN:输入验证码" + pinLinkedin;
                if (CheckIsExists(driverSet,
                        By.Name("pin")))
                {
                    driverSet.FindElement(By.Name("pin"))
                        .SendKeys(pinLinkedin);
                    Thread.Sleep(5000);
                }

                //提交验证码
                account.Running_Log = "IN:提交验证码";
                if (CheckIsExists(driverSet,
                        By.Id("pin-submit-button")))
                {
                    driverSet.FindElement(By.Id("pin-submit-button")).Click();
                    Thread.Sleep(5000);
                }

                newPassword = this.GetNewPassword_EM();
                //输入新密码
                account.Running_Log = "IN:输入新密码" + newPassword;
                if (CheckIsExists(driverSet,
                        By.Id("newPassword")))
                {
                    driverSet.FindElement(By.Id("newPassword")).SendKeys(newPassword);
                }

                //确认新密码
                account.Running_Log = "IN:确认新密码" + newPassword;
                if (CheckIsExists(driverSet,
                        By.Id("confirmPassword")))
                {
                    driverSet.FindElement(By.Id("confirmPassword")).SendKeys(newPassword);
                    account.Facebook_Pwd = newPassword;
                    Thread.Sleep(5000);
                }


                //提交新密码
                account.Running_Log = "IN:提交新密码";
                if (CheckIsExists(driverSet,
                        By.Id("reset-password-submit-button")))
                {
                    driverSet.FindElement(By.Id("reset-password-submit-button")).Click();
                    Thread.Sleep(5000);
                }

                if (driverSet.PageSource.Contains("Your password has been changed"))
                {
                    account.Running_Log = "IN:修改密码成功";
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }
                }
            }

            Thread.Sleep(5000);
            account.Running_Log = "IN:跳转主页";
            driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
            {
                loginLinkedin:
                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password"))
                        .SendKeys(account.Facebook_Pwd);
                    Thread.Sleep(3000);
                }

                //提交登录
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='btn__primary--large from__button--floating']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                        .Click();
                }

                Thread.Sleep(10000);
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                Thread.Sleep(3000);
                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    goto loginLinkedin;
                }

                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Facebook_CK = strJson;
                }
            }

            if (driverSet.Url.Contains("https://www.linkedin.com/feed/"))
            {
                account.Running_Log = "IN:创建成功";
                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Running_Log = $"IN:提取cookie";
                    account.Facebook_CK = strJson;
                }
            }

            driverSet.Navigate().GoToUrl(
                "https://www.linkedin.com/mynetwork/invite-connect/connections/");
            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
            {
                loginLinkedin:
                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password"))
                        .SendKeys(account.Facebook_Pwd);
                    Thread.Sleep(3000);
                }

                //提交登录
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='btn__primary--large from__button--floating']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                        .Click();
                    Thread.Sleep(10000);
                }

                driverSet.Navigate().GoToUrl("https://www.linkedin.com/feed/");
                Thread.Sleep(3000);
                if (!driverSet.Url.Contains("https://www.linkedin.com/feed/"))
                {
                    goto loginLinkedin;
                }

                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Facebook_CK = strJson;
                }
            }

            account.Running_Log = $"IN:获取好友信息";
            account.HaoYouCount =
                StringHelper.GetMidStr(driverSet.PageSource, "totalResultCount\":", ",\"secondaryFilterCluster");
            if (driverSet.PageSource.Contains("urn:li:fsd_profile:"))
            {
                account.Running_Log = $"IN:FsdProfile";
                account.FsdProfile =
                    StringHelper.GetMidStr(driverSet.PageSource, $"urn:li:fsd_profile:", $"\"");
                var accountName = StringHelper.GetMidStr(driverSet.PageSource, "https://www.linkedin.com/in/",
                    "/recent-activity");
                account.AccountName = "https://www.linkedin.com/in/" + accountName + "/";
            }

            driverSet.Navigate().GoToUrl(account.AccountName);
            account.AccountName = driverSet.Url;

            account.Running_Log = $"IN:AccountName" + account.AccountName;
        }
        catch (Exception e)
        {
            if (e.Message.Contains("The HTTP request to the remote WebDriver server for URL"))
            {
                jo_Result["Success"] = false;
                return jo_Result;
            }
            else
            {
                jo_Result["Success"] = false;
                return jo_Result;
            }
        }
        finally
        {
            if (Program.setting.Setting_EM.ADSPower)
            {
                adsPowerService.ADS_UserDelete(account.user_id);
            }
            else if (Program.setting.Setting_EM.BitBrowser)
            {
                bitBrowserService.BIT_BrowserDelete(account.user_id);
            }

            try
            {
                driverSet?.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Quit();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        jo_Result["Success"] = true;
        jo_Result["ErrorMsg"] = "注册:操作成功";
        return jo_Result;
    }

    /// <summary>
    /// 注册领英
    /// </summary>
    /// <param name="account"></param>
    /// <param name="mail"></param>
    /// <returns></returns>
    public JObject EM_RegisterLinkedinSelenium(Account_FBOrIns account, MailInfo mail)
    {
        JObject jo_Result = new JObject();
        jo_Result["Success"] = false;
        jo_Result["ErrorMsg"] = string.Empty;
        account.Account_Id = UUID.StrSnowId;
        ChromeDriver driverSet = null;
        AdsPowerService adsPowerService = null;
        BitBrowserService bitBrowserService = null;
        openADS:
        if (Program.setting.Setting_EM.ADSPower)
        {
            account.Running_Log = "初始化ADSPower";
            adsPowerService = new AdsPowerService();
            if (!string.IsNullOrEmpty(account.user_id))
            {
                try
                {
                    account.Running_Log = "验证环境是否打开";
                    Thread.Sleep(5000);
                    var adsActiveBrowser = adsPowerService.ADS_ActiveBrowser(account.user_id);
                    if (adsActiveBrowser["data"]["status"]!.ToString().Equals("Inactive"))
                    {
                        var adsUserCreate =
                            adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                        account.user_id = adsUserCreate["data"]["id"].ToString();
                    }
                }
                catch (Exception e)
                {
                    Debug.Write(e.Message);
                }
            }
            else
            {
                account.Running_Log = "获取user_id";
                var adsUserCreate = adsPowerService.ADS_UserCreate("EM", account.Facebook_CK, account.UserAgent);
                account.user_id = adsUserCreate["data"]["id"].ToString();
                account.Running_Log = "获取user_id=[" + account.user_id + "]";
            }

            #region 实例化ADS API

            account.Running_Log = "打开ADS";
            var adsStartBrowser = adsPowerService.ADS_StartBrowser(account.user_id);
            if (adsStartBrowser["code"].ToString().Equals("-1"))
            {
                goto openADS;
            }

            account.selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
            account.webdriver = adsStartBrowser["data"]["webdriver"].ToString();
            account.Running_Log =
                "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

            #endregion
        }
        else if (Program.setting.Setting_EM.BitBrowser)
        {
            account.Account_Id = string.Empty;
            account.Running_Log = "初始化BitBrowser";
            bitBrowserService = new BitBrowserService();
            JObject bitGroupList = null;
            string groupId = String.Empty;
            try
            {
                account.Running_Log = "查询分组";
                bitGroupList = bitBrowserService.BIT_GroupList(0, 10);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (bitGroupList != null)
            {
                var jToken = bitGroupList["data"]["list"];
                if (jToken.Count() > 0)
                {
                    foreach (var token in jToken)
                    {
                        if (token["groupName"].ToString().Equals("EM"))
                        {
                            groupId = token["id"].ToString();
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(groupId))
                {
                    var bitGroupAdd = bitBrowserService.BIT_GroupAdd("EM", 99);
                    if (bitGroupAdd != null)
                    {
                        if ((bool)bitGroupAdd["success"])
                        {
                            groupId = bitGroupAdd["data"]["id"].ToString();
                        }
                    }
                }
            }

            var adsUserCreate = bitBrowserService.BIT_BrowserUpdate(account.UserAgent, account.Facebook_CK, groupId,
                "https://www.linkedin.com", "linkedin.com", "linkedin_" + account.Account_Id);
            account.user_id = adsUserCreate["data"]["id"].ToString();
            account.Running_Log = "获取user_id=[" + account.user_id + "]";

            #region 实例化ADS API

            account.Running_Log = "打开ADS";
            var adsStartBrowser = bitBrowserService.BIT_BrowserOpen(account.user_id);
            if (!(bool)adsStartBrowser["success"])
            {
                bitBrowserService.BIT_BrowserDelete(account.user_id);
                goto openADS;
            }

            account.selenium = adsStartBrowser["data"]["http"].ToString();
            account.webdriver = adsStartBrowser["data"]["driver"].ToString();
            account.Running_Log =
                "成功打开ADS环境[selenium=" + account.selenium + "][webdriver=" + account.webdriver + "]";

            #endregion
        }

        try
        {
            ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
            if (driverSet == null)
            {
                driverSet = chromeDriverSetting.GetDriverSetting("Google", account.Account_Id, account.selenium,
                    account.webdriver);
            }

            var newPassword = string.Empty;
            DateTime sendCodeTime = DateTime.Parse("1970-01-01");
            account.Running_Log = "IN:访问注册页面";
            driverSet.Navigate().GoToUrl(
                "https://www.linkedin.com/");
            Thread.Sleep(5000);
            driverSet.Navigate().GoToUrl(
                "https://www.linkedin.com/signup?trk=guest_homepage-basic_nav-header-join");
            Thread.Sleep(5000);
            driverSet.Navigate().Refresh();
            if (driverSet.Url.Contains("https://www.linkedin.cn/"))
            {
                jo_Result["ErrorMsg"] = "挂代理失败";
                jo_Result["Success"] = false;
                return jo_Result;
            }

            ReadOnlyCollection<IWebElement> webElementIframes;
            if (account.IsAuthorization)
            {
                openAuth:
                account.Running_Log = "IN:开启授权注册";
                driverSet.SwitchTo().DefaultContent();
                Thread.Sleep(5000);
                webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                driverSet.SwitchTo().Frame(webElementIframes[0]);
                Thread.Sleep(5000);

                account.Running_Log = "IN:点击google 授权按钮";
                try
                {
                    //点击google 授权
                    if (CheckIsExists(driverSet,
                            By.CssSelector("[class='nsm7Bb-HzV7m-LgbsSe-BPrWId']")))
                    {
                        driverSet.FindElement(By.CssSelector("[class='nsm7Bb-HzV7m-LgbsSe-BPrWId']"))
                            .Click();
                    }
                }
                catch
                {
                    goto openAuth;
                }


                Thread.Sleep(5000);

                //切换到新的窗口
                if (driverSet.WindowHandles.Count > 0)
                {
                    foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                    {
                        if (driverSet.CurrentWindowHandle != driverSetWindowHandle)
                        {
                            driverSet.SwitchTo().Window(driverSetWindowHandle);
                            break;
                        }
                    }
                }

                Thread.Sleep(8000);
                login_google:

                account.Running_Log = "IN:输入google账号";
                //输入google账号
                if (CheckIsExists(driverSet,
                        By.Id("identifierId")))
                {
                    driverSet.FindElement(By.Id("identifierId"))
                        .SendKeys(mail.Mail_Name);
                }

                account.New_Mail_Name = mail.Mail_Name;
                Thread.Sleep(5000);
                //点击下一步
                account.Running_Log = "IN:点击下一步";
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
                {
                    driverSet.FindElement(By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                        .Click();
                }

                Thread.Sleep(5000);
                //密码
                account.Running_Log = "IN:输入Google密码";
                if (CheckIsExists(driverSet,
                        By.Name("Passwd")))
                {
                    driverSet.FindElement(By.Name("Passwd"))
                        .SendKeys(mail.Mail_Pwd);
                }

                account.New_Mail_Pwd = mail.Mail_Pwd;
                Thread.Sleep(5000);
                //点击下一步
                account.Running_Log = "IN:输入Google密码,点击下一步";
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
                {
                    driverSet.FindElement(By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                        .Click();
                }

                Thread.Sleep(5000);
                //确认辅助邮箱
                account.Running_Log = "IN:确认辅助邮箱登录";
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='l5PPKe']")))
                {
                    foreach (var findElement in driverSet.FindElements(By.CssSelector(
                                 "[class='l5PPKe']")))
                    {
                        if (findElement.Text.Contains("Confirm your recovery email"))
                        {
                            findElement.Click();
                            break;
                        }
                    }

                    Thread.Sleep(5000);

                    if (CheckIsExists(driverSet,
                            By.Id("knowledge-preregistered-email-response")))
                    {
                        driverSet.FindElement(By.Id("knowledge-preregistered-email-response"))
                            .SendKeys(mail.VerifyMail_Name);
                    }

                    Thread.Sleep(5000);
                    //点击下一步
                    account.Running_Log = "IN:输入辅助邮箱,点击下一步";
                    if (CheckIsExists(driverSet,
                            By.CssSelector(
                                "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']")))
                    {
                        driverSet.FindElement(By.CssSelector(
                                "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-k8QpJ VfPpkd-LgbsSe-OWXEXe-dgl2Hf nCP5yc AjY5Oe DuMIQc LQeN7 BqKGqe Jskylb TrZEUc lw1w4b']"))
                            .Click();
                    }

                    Thread.Sleep(5000);
                }

                account.Recovery_Email = mail.VerifyMail_Name;

                //点击取消创建密码安全
                account.Running_Log = "IN:点击取消创建密码安全";
                if (CheckIsExists(driverSet,
                        By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 BqKGqe eR0mzb TrZEUc lw1w4b']")))
                {
                    driverSet.FindElement(By.CssSelector(
                            "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 BqKGqe eR0mzb TrZEUc lw1w4b']"))
                        .Click();
                    Thread.Sleep(5000);
                    //点击取消设置
                    account.Running_Log = "IN:点击取消设置";
                    if (CheckIsExists(driverSet,
                            By.CssSelector(
                                "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 k97fxb yu6jOd']")))
                    {
                        driverSet.FindElement(By.CssSelector(
                                "[class='VfPpkd-LgbsSe VfPpkd-LgbsSe-OWXEXe-dgl2Hf ksBjEc lKxP2d LQeN7 k97fxb yu6jOd']"))
                            .Click();
                    }

                    Thread.Sleep(5000);
                }

                account.Running_Log = "IN:关闭登录Google窗口";
                try
                {
                    driverSet.Close();
                    Thread.Sleep(5000);
                }
                catch (Exception e)
                {
                }

                //返回当前窗口
                if (driverSet.WindowHandles.Count > 0)
                {
                    foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                    {
                        driverSet.SwitchTo().Window(driverSetWindowHandle);
                        break;
                    }
                }

                Thread.Sleep(5000);
                //是否创建过领英
                account.Running_Log = "IN:是否创建过领英";
                //点是否注册
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='join-with-google__title']")))
                {
                    var findElement = driverSet.FindElement(By.CssSelector("[class='join-with-google__title']"));
                    if (!findElement.Text.Contains("Join LinkedIn"))
                    {
                        var findElement1 = driverSet.FindElement(By.CssSelector("[class='main__subtitle']"));
                        if (!findElement1.Text.Contains("Make the most of your professional life"))
                        {
                            goto create_password;
                        }
                    }
                }
                else if (CheckIsExists(driverSet,
                             By.CssSelector("[class='header__content__heading ']")))
                {
                    var findElement = driverSet.FindElement(By.CssSelector("[class='header__content__heading ']"));
                    if (findElement.Text.Contains("Sign in"))
                    {
                        goto create_password;
                    }
                }
                else if (CheckIsExists(driverSet,
                             By.CssSelector("[class='main__subtitle ']")))
                {
                    var findElement1 = driverSet.FindElement(By.CssSelector("[class='main__subtitle ']"));
                    if (!findElement1.Text.Contains("Make the most of your professional life"))
                    {
                        goto create_password;
                    }
                }

                if (driverSet.Url.Contains("https://www.linkedin.com/feed"))
                {
                    goto create_password;
                }

                Thread.Sleep(5000);

                driverSet.SwitchTo().DefaultContent();
                Thread.Sleep(5000);
                webElementIframes = driverSet.FindElements(By.TagName("iframe"));
                driverSet.SwitchTo().Frame(webElementIframes[0]);
                Thread.Sleep(5000);
                //点击google授权
                account.Running_Log = "IN:点击google授权";
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='nsm7Bb-HzV7m-LgbsSe-BPrWId']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='nsm7Bb-HzV7m-LgbsSe-BPrWId']"))
                        .Click();
                }
                else
                {
                    goto re_button;
                }

                Thread.Sleep(5000);
                //切换到新的窗口
                if (driverSet.WindowHandles.Count > 0)
                {
                    foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                    {
                        if (driverSet.CurrentWindowHandle != driverSetWindowHandle)
                        {
                            driverSet.SwitchTo().Window(driverSetWindowHandle);
                            break;
                        }
                    }
                }

                Thread.Sleep(5000);

                //点击google 授权
                if (CheckIsExists(driverSet,
                        By.Id("identifierId")))
                {
                    goto login_google;
                }

                Thread.Sleep(5000);
                bool button = true;
                do
                {
                    if (!CheckIsExists(driverSet,
                            By.CssSelector("[class='fFW7wc-ibnC6b-K4efff']")))
                    {
                        driverSet.Navigate().Refresh();
                    }
                    else
                    {
                        button = false;
                    }

                    Thread.Sleep(3000);
                } while (button);

                //点击google邮箱授权
                account.Running_Log = "IN:点击google授权";
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='fFW7wc-ibnC6b-K4efff']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='fFW7wc-ibnC6b-K4efff']"))
                        .Click();
                }

                Thread.Sleep(5000);
                //点击googleConfirm
                account.Running_Log = "IN:点击google授权确认";
                if (CheckIsExists(driverSet,
                        By.Id("confirm_yes")))
                {
                    driverSet.FindElement(By.Id("confirm_yes"))
                        .Click();
                }

                Thread.Sleep(5000);
                //返回当前窗口
                if (driverSet.WindowHandles.Count > 0)
                {
                    foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                    {
                        driverSet.SwitchTo().Window(driverSetWindowHandle);
                        break;
                    }
                }

                Thread.Sleep(5000);
                re_button:
                bool isContain = true;
                try
                {
                    //点击注册按钮

                    driverSet.SwitchTo().DefaultContent();
                    account.Running_Log = "IN:点击注册领英按钮";
                    if (CheckIsExists(driverSet,
                            By.Id("join-form-submit")))
                    {
                        driverSet.FindElement(By.Id("join-form-submit"))
                            .Click();
                    }

                    Thread.Sleep(5000);
                }
                catch
                {
                    isContain = false;
                }

                if (isContain)
                {
                    Thread.Sleep(5000);
                    if (driverSet.PageSource.Contains("Sorry, something went wrong. Please try again."))
                    {
                        goto re_button;
                    }
                }
            }
            else
            {
                //输入邮箱
                account.Running_Log = "IN:输入邮箱";
                if (CheckIsExists(driverSet,
                        By.Id("email-address")))
                {
                    driverSet.FindElement(By.Id("email-address"))
                        .SendKeys(mail.Mail_Name);
                }

                account.New_Mail_Name = mail.Mail_Name;
                Thread.Sleep(5000);
                newPassword = this.GetNewPassword_EM();
                //输入密码
                account.Running_Log = "IN:输入密码";
                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password"))
                        .SendKeys(newPassword);
                }

                Thread.Sleep(5000);
                //点击提交
                account.Running_Log = "IN:输入密码，点击提交";
                if (CheckIsExists(driverSet,
                        By.Id("join-form-submit")))
                {
                    driverSet.FindElement(By.Id("join-form-submit"))
                        .Click();
                }

                Thread.Sleep(5000);

                var generateSurname = StringHelper.GenerateSurname();
                //First Name
                account.Running_Log = "IN:输入First Name";
                if (CheckIsExists(driverSet,
                        By.Id("first-name")))
                {
                    driverSet.FindElement(By.Id("first-name"))
                        .SendKeys("Mc");
                }

                //Last Name
                account.Running_Log = "IN:输入Last Name";
                if (CheckIsExists(driverSet,
                        By.Id("last-name")))
                {
                    driverSet.FindElement(By.Id("last-name"))
                        .SendKeys(generateSurname);
                }

                Thread.Sleep(5000);
                //注册提交
                account.Running_Log = "IN:注册提交";
                if (CheckIsExists(driverSet,
                        By.Id("join-form-submit")))
                {
                    driverSet.FindElement(By.Id("join-form-submit"))
                        .Click();
                }

                Thread.Sleep(10000);
            }


            //等待打码 或者sms 接码
            account.Running_Log = "IN:等待打码 或者sms 接码";

            driverSet.SwitchTo().DefaultContent();
            Thread.Sleep(15000);
            webElementIframes = driverSet.FindElements(By.TagName("iframe"));
            driverSet.SwitchTo().Frame(webElementIframes[1]);

            phoneNumber:
            if (CheckIsExists(driverSet,
                    By.Id("select-register-phone-country")))
            {
                account.Running_Log = "IN:开始接码";
                var findElement = driverSet.FindElement(By.Id("select-register-phone-country"));
                SelectElement selectObj = new SelectElement(findElement);
                selectObj.SelectByValue("hk");
                int hhh = 0;
                BindPhone :
                account.Running_Log = "IN:调用接码平台获取电话号码";
                hhh++;
                if (hhh > 5)
                {
                    jo_Result["ErrorMsg"] = "获取手机号码失败        ";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                SmsActivateService smsActivateService = new SmsActivateService();
                var smsGetPhoneNum = smsActivateService.SMS_GetPhoneNum("tn", Program.setting.Setting_EM.Country);
                if (smsGetPhoneNum["ErrorMsg"] != null &&
                    !string.IsNullOrEmpty(smsGetPhoneNum["ErrorMsg"].ToString()))
                {
                    jo_Result["ErrorMsg"] = "绑定电话错误";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                //输入收集号码
                account.Running_Log = "IN:输入手机号码";
                if (CheckIsExists(driverSet,
                        By.Id("register-verification-phone-number")))
                {
                    driverSet.FindElement(
                        By.Id("register-verification-phone-number")).SendKeys(smsGetPhoneNum["Number"].ToString());
                }

                Thread.Sleep(5000);
                //提交手机号码
                account.Running_Log = "IN:提交手机号码";
                if (CheckIsExists(driverSet,
                        By.Id("register-phone-submit-button")))
                {
                    driverSet.FindElement(By.Id("register-phone-submit-button"))
                        .Click();
                }

                Thread.Sleep(5000);
                if (driverSet.PageSource.Contains("You can’t use this phone number. Please try a different one") ||
                    driverSet.PageSource.Contains("Something unexpected happened. Please try again."))
                {
                    goto BindPhone;
                }

                account.Running_Log = "IN:调用接码平台获取验证码";
                var sms_code = string.Empty;
                int hh = 0;
                bool isGetCode = true;
                do
                {
                    Thread.Sleep(10000);
                    if (hh > 5)
                    {
                        isGetCode = false;
                    }

                    var smsGetCode = smsActivateService.SMS_GetCode(smsGetPhoneNum["phoneId"].ToString());
                    if (smsGetCode["Code"] != null)
                    {
                        if (string.IsNullOrEmpty(smsGetCode["Code"].ToString()))
                        {
                            Thread.Sleep(10000);
                        }
                        else
                        {
                            sms_code = smsGetCode["Code"].ToString();
                            break;
                        }
                    }

                    hh++;
                } while (isGetCode);

                if (string.IsNullOrEmpty(sms_code))
                {
                    jo_Result["ErrorMsg"] = "验证码获取失败";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                //验证码
                account.Running_Log = "IN:调用接码平台获取验证码=" + sms_code;
                if (CheckIsExists(driverSet,
                        By.Id("input__phone_verification_pin")))
                {
                    driverSet.FindElement(By.Id("input__phone_verification_pin"))
                        .SendKeys(sms_code);
                }

                Thread.Sleep(5000);
            }
            else
            {
                try
                {
                    if (driverSet.PageSource.Contains("Your noCAPTCHA user response code is missing or invalid."))
                    {
                        jo_Result["ErrorMsg"] = "打码失败";
                        jo_Result["Success"] = false;
                        return jo_Result;
                    }

                    driverSet.SwitchTo().DefaultContent();
                    if (driverSet.PageSource.Contains("Someone’s already using that email."))
                    {
                        lock (Program.setting.Setting_IN.Lock_Mail_ForBind_List)
                        {
                            mail.IsLocked = true;
                            mail.Is_Used = true;
                        }

                        jo_Result["ErrorMsg"] = "邮箱占用";
                        jo_Result["Success"] = false;
                        return jo_Result;
                    }
                }
                catch (Exception e)
                {
                }
            }

            if (CheckIsExists(driverSet,
                    By.Id("select-register-phone-country")))
            {
                goto phoneNumber;
            }

            Thread.Sleep(5000);
            //提交验证码
            account.Running_Log = "IN:提交验证码";
            if (CheckIsExists(driverSet,
                    By.Id("register-phone-submit-button")))
            {
                driverSet.FindElement(By.Id("register-phone-submit-button"))
                    .Click();
            }

            Thread.Sleep(5000);
            account.Running_Log = "IN:跳转主页";
            driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://www.linkedin.com/feed/") ||
                driverSet.Url.Contains("https://www.linkedin.com/onboarding/start/profile-location/new/"))
            {
                account.Running_Log = "IN:创建成功";
                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Running_Log = $"IN:提取cookie";
                    account.Facebook_CK = strJson;
                }
            }
            else if (driverSet.Url.Contains("https://www.linkedin.com/uas/login-submit"))
            {
                if (CheckIsExists(driverSet,
                        By.Id("join-form-submit")))
                {
                    driverSet.FindElement(By.Id("join-form-submit"))
                        .Click();
                }

                goto phoneNumber;
            }

            create_password:
            Thread.Sleep(5000);
            if (account.IsAuthorization)
            {
                //创建密码
                account.Running_Log = "IN:创建密码";
                driverSet.Navigate()
                    .GoToUrl(
                        "https://www.linkedin.com/passwordReset");
                Thread.Sleep(5000);
                //忘记密码 下一步
                account.Running_Log = "IN:忘记密码 下一步";
                if (CheckIsExists(driverSet,
                        By.Id("username")))
                {
                    driverSet.FindElement(By.Id("username")).Clear();
                    driverSet.FindElement(By.Id("username")).SendKeys(account.New_Mail_Name);
                }

                Thread.Sleep(5000);
                if (CheckIsExists(driverSet,
                        By.Id("reset-password-submit-button")))
                {
                    driverSet.FindElement(By.Id("reset-password-submit-button")).Click();
                }

                Thread.Sleep(5000);
                //等待打码 或者sms 接码
                account.Running_Log = "IN:等待打码 或者sms 接码";
                Thread.Sleep(13000);
                var pinLinkedin = string.Empty;
                driverSet.SwitchTo().NewWindow(WindowType.Tab);
                Thread.Sleep(5000);
                bool isGetCode = true;
                int h = 0;
                do
                {
                    if (h > 3)
                    {
                        break;
                    }

                    driverSet.Navigate().GoToUrl("https://mail.google.com/mail/u/0/#inbox");
                    Thread.Sleep(18000);

                    //获取验证码
                    if (CheckIsExists(driverSet, By.TagName("span")))
                    {
                        var readOnlyCollection = driverSet.FindElements(By.TagName("span"));
                        foreach (var element in readOnlyCollection)
                        {
                            try
                            {
                                if (element.Text.Contains("PIN"))
                                {
                                    string regexPattern = @"\b\d{6}\b";

                                    Match match = Regex.Match(element.Text, regexPattern);

                                    if (match.Success)
                                    {
                                        pinLinkedin = match.Value;
                                        isGetCode = false;
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    h++;
                } while (isGetCode);


                if (string.IsNullOrEmpty(pinLinkedin))
                {
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }

                    jo_Result["ErrorMsg"] = "IN:创建密码，获取验证码为空";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                Thread.Sleep(5000);
                //返回当前窗口
                if (driverSet.WindowHandles.Count > 0)
                {
                    foreach (var driverSetWindowHandle in driverSet.WindowHandles)
                    {
                        driverSet.SwitchTo().Window(driverSetWindowHandle);
                        break;
                    }
                }

                Thread.Sleep(5000);
                //输入验证码
                account.Running_Log = "IN:输入验证码" + pinLinkedin;
                if (CheckIsExists(driverSet,
                        By.Name("pin")))
                {
                    driverSet.FindElement(By.Name("pin"))
                        .SendKeys(pinLinkedin);
                }

                Thread.Sleep(5000);
                //提交验证码
                account.Running_Log = "IN:提交验证码";
                if (CheckIsExists(driverSet,
                        By.Id("pin-submit-button")))
                {
                    driverSet.FindElement(By.Id("pin-submit-button")).Click();
                }

                Thread.Sleep(5000);
                newPassword = this.GetNewPassword_EM();
                //输入新密码
                account.Running_Log = "IN:输入新密码" + newPassword;
                if (CheckIsExists(driverSet,
                        By.Id("newPassword")))
                {
                    driverSet.FindElement(By.Id("newPassword")).SendKeys(newPassword);
                }

                //确认新密码
                account.Running_Log = "IN:确认新密码" + newPassword;
                if (CheckIsExists(driverSet,
                        By.Id("confirmPassword")))
                {
                    driverSet.FindElement(By.Id("confirmPassword")).SendKeys(newPassword);
                }


                Thread.Sleep(5000);
                //提交新密码
                account.Running_Log = "IN:提交新密码";
                if (CheckIsExists(driverSet,
                        By.Id("reset-password-submit-button")))
                {
                    driverSet.FindElement(By.Id("reset-password-submit-button")).Click();
                }

                Thread.Sleep(5000);
                if (driverSet.PageSource.Contains("Your password has been changed"))
                {
                    account.Running_Log = "IN:修改密码成功";
                    account.Facebook_Pwd = newPassword;
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }
                }
            }
            else
            {
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                Thread.Sleep(5000);

                driverSet.SwitchTo().DefaultContent();
                Thread.Sleep(5000);
                var settings = driverSet.FindElement(By.CssSelector("[class='settings-iframe--frame']"));
                driverSet.SwitchTo().Frame(settings);
                Thread.Sleep(5000);
                //发送邮箱链接
                account.Running_Log = "IN:发送邮箱链接";
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='send-verification tertiary-btn']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='send-verification tertiary-btn']"))
                        .Click();
                }

                Thread.Sleep(5000);

                #region 去邮箱提取验证码

                int timeSpan = 0;
                int timeCount = 0;
                int timeOut = 0;
                Thread.Sleep(5000);
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

                    if (mail.Pop3Client != null && mail.Pop3Client.Connected)
                        try
                        {
                            mail.Pop3Client.Disconnect();
                        }
                        catch
                        {
                        }

                    mail.Pop3Client = Pop3Helper.GetPop3Client(mail.Mail_Name, mail.Mail_Pwd);
                    if (mail.Pop3Client == null) continue;

                    msgList = Pop3Helper.GetMessageByIndex(mail.Pop3Client);
                    pop3MailMessage = msgList.Where(m =>
                        m.DateSent >= sendCodeTime &&
                        m.From.Contains("<security-noreply@linkedin.com>")).FirstOrDefault();
                }

                if (mail.Pop3Client == null)
                {
                    jo_Result["ErrorMsg"] = "pop3获取失败";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                if (pop3MailMessage == null)
                {
                    jo_Result["ErrorMsg"] = "pop3获取失败";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                var confirmCode = StringHelper.GetMidStr(pop3MailMessage.Html,
                    "https://www.linkedin.com/comm/psettings/email/confirm", "\n").Trim();
                if (string.IsNullOrEmpty(confirmCode))
                {
                    jo_Result["ErrorMsg"] = "邮箱连接获取失败";
                    jo_Result["Success"] = false;
                    return jo_Result;
                }

                confirmCode = "https://www.linkedin.com/comm/psettings/email/confirm" +
                              confirmCode;

                #endregion

                account.Running_Log = "IN:发送邮箱链接" + confirmCode;
                driverSet.Navigate().GoToUrl(confirmCode);
            }

            Thread.Sleep(5000);
            account.Running_Log = "IN:跳转主页";
            driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
            {
                loginLinkedin:
                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password"))
                        .SendKeys(account.Facebook_Pwd);
                }

                Thread.Sleep(3000);
                //提交登录
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='btn__primary--large from__button--floating']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                        .Click();
                }

                Thread.Sleep(10000);
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                Thread.Sleep(3000);
                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    goto loginLinkedin;
                }

                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Facebook_CK = strJson;
                }
            }

            if (driverSet.Url.Contains("https://www.linkedin.com/feed/"))
            {
                account.Running_Log = "IN:创建成功";
                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Running_Log = $"IN:提取cookie";
                    account.Facebook_CK = strJson;
                }
            }

            driverSet.Navigate().GoToUrl(
                "https://www.linkedin.com/mynetwork/invite-connect/connections/");
            Thread.Sleep(5000);
            if (driverSet.Url.Contains("https://www.linkedin.com/uas/login?session_redirect="))
            {
                loginLinkedin:
                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password"))
                        .SendKeys(account.Facebook_Pwd);
                }

                Thread.Sleep(3000);
                //提交登录
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='btn__primary--large from__button--floating']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='btn__primary--large from__button--floating']"))
                        .Click();
                }

                Thread.Sleep(10000);
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/feed/");
                Thread.Sleep(3000);
                if (!driverSet.Url.Contains("https://www.linkedin.com/feed/"))
                {
                    goto loginLinkedin;
                }

                var cookieJar = driverSet.Manage().Cookies.AllCookies;
                if (cookieJar.Count > 0)
                {
                    string strJson = JsonConvert.SerializeObject(cookieJar);
                    account.Facebook_CK = strJson;
                }
            }

            account.Running_Log = $"IN:获取好友信息";
            account.HaoYouCount =
                StringHelper.GetMidStr(driverSet.PageSource, "totalResultCount\":", ",\"secondaryFilterCluster");
            if (driverSet.PageSource.Contains("urn:li:fsd_profile:"))
            {
                account.Running_Log = $"IN:FsdProfile";
                account.FsdProfile =
                    StringHelper.GetMidStr(driverSet.PageSource, $"urn:li:fsd_profile:", $"\"");
                var accountName = StringHelper.GetMidStr(driverSet.PageSource, "https://www.linkedin.com/in/",
                    "/recent-activity");
                account.AccountName = "https://www.linkedin.com/in/" + accountName + "/";
            }

            driverSet.Navigate().GoToUrl(account.AccountName);
            account.AccountName = driverSet.Url;

            account.Running_Log = $"IN:AccountName" + account.AccountName;
        }
        catch (Exception e)
        {
            if (e.Message.Contains("The HTTP request to the remote WebDriver server for URL"))
            {
                jo_Result["Success"] = false;
                return jo_Result;
            }
            else
            {
                jo_Result["Success"] = false;
                return jo_Result;
            }
        }
        finally
        {
            if (Program.setting.Setting_EM.ADSPower)
            {
                adsPowerService.ADS_UserDelete(account.user_id);
            }
            else if (Program.setting.Setting_EM.BitBrowser)
            {
                bitBrowserService.BIT_BrowserDelete(account.user_id);
            }

            try
            {
                driverSet?.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Quit();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            try
            {
                driverSet?.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        jo_Result["Success"] = true;
        jo_Result["ErrorMsg"] = "注册:操作成功";
        return jo_Result;
    }
    //生成一个新的密码

    private string GetNewPassword_EM()
    {
        string newPwd = string.Empty;
        if (Program.setting.Setting_EM.ForgotPwdSetting_Front_Mode == 0)
            newPwd = StringHelper.GetRandomString(true, true, true, true, 10, 10);
        else newPwd = Program.setting.Setting_EM.ForgotPwdSetting_Front_Custom_Content.Trim();

        if (Program.setting.Setting_EM.ForgotPwdSetting_After_IsAddDate)
            newPwd += $"{DateTime.Now.ToString("MMdd")}";

        return newPwd;
    }

    #region 判定元素是否存在

    public static bool CheckIsExists(ChromeDriver chromeDriver, By by)
    {
        try
        {
            int i = 0;
            while (i < 3)
            {
                if (chromeDriver.FindElements(by).Count > 0)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(2000);
                    i = i + 1;
                }
            }

            return chromeDriver.FindElements(by).Count > 0;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    #endregion
}