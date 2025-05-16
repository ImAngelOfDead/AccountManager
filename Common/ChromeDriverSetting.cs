using System;
using System.IO;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace AccountManager.Common;

public class ChromeDriverSetting
{
    private static readonly object Obj = new object();
    private readonly Random _rand = new Random();

    #region 设置浏览器 UA 缓存 代理

    [Obsolete("Obsolete")]
    public ChromeDriver GetDriverSetting(string type, string accountid, string selenium, string webdriver)
    {
        if (string.IsNullOrEmpty(webdriver))
        {
            ChromeOptions options = new ChromeOptions();

            var UserDataDir = CreateCookieDir(accountid);
            var ua = CreateUa();
            var capsolver = CreateCapsolver();
            if (capsolver.Length > 0)
            {
                options.AddExtension(capsolver);
            }

            if (type.Equals("IN"))
            {
                if (Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_IsUse ||
                    Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Type_922)
                {
                    string proxyPath = CreateProxy(accountid, type);
                    if (proxyPath.Length > 0)
                    {
                        options.AddExtension(proxyPath);
                    }
                }
            }

            if (type.Equals("IN_RE"))
            {
                if (Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_IsUse ||
                    Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Type_922)
                {
                    string proxyPath = CreateProxy(accountid, type);
                    if (proxyPath.Length > 0)
                    {
                        options.AddExtension(proxyPath);
                    }
                }
            }

            // options.AddArgument("--headless"); //隐藏浏览器
            // options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
            options.AddArguments("--user-data-dir=" + UserDataDir);
            options.AddArguments("--disable-infobars");
            // // 隐身模式（无痕模式）
            // options.AddArguments("--incognito");
            options.AddArguments("--User-Agent=" + ua);
            options.AddArguments("--no-sandbox");
            options.AddArguments("--window-size=1320,1080");
            options.AddArguments("--disable-gpu");
            options.AddArguments("--disable-notifications");
            options.AddArguments("--disable-blink-features=AutomationControlled");
            options.AddArguments("--disable-webrtc-encryption[13]");
            options.AddArguments("--disable-webrtc-hw-decoding[13]");
            options.AddArguments("--disable-webrtc-hw-encoding[13]");
            options.AddArguments("--disable-webrtc-multiple-routes[13]");
            options.AddArguments("--disable-xss-auditor");
            options.AddArguments("--disable-web-security");
            // options.AddArguments("--disable-features=Video"); 
            // options.AddArguments("--host-resovler-rule"); 
            options.AddArguments("--lang=en_US"); // 设置语言
            options.AddExcludedArgument("enable-automation");

            ChromeDriverService defaultService = ChromeDriverService.CreateDefaultService(Environment.CurrentDirectory);
            defaultService.HideCommandPromptWindow = true;
            ChromeDriver chromeDriver = new ChromeDriver(defaultService, options, TimeSpan.FromSeconds(100.0));
            return chromeDriver;
        }
        else
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArguments("--disable-infobars");
            options.AddArguments("--no-sandbox");
            options.AddArguments("--window-size=1320,1080");
            options.AddArguments("--disable-gpu");
            options.AddArguments("--disable-notifications");
            options.AddArguments("--disable-blink-features=AutomationControlled");
            options.AddArguments("--disable-webrtc-encryption[13]");
            options.AddArguments("--disable-webrtc-hw-decoding[13]");
            options.AddArguments("--disable-webrtc-hw-encoding[13]");
            options.AddArguments("--disable-webrtc-multiple-routes[13]");
            options.AddArguments("--disable-xss-auditor");
            options.AddArguments("--disable-web-security");
            options.DebuggerAddress = selenium;
            ChromeDriverService defaultService = ChromeDriverService.CreateDefaultService(webdriver);
            defaultService.HideCommandPromptWindow = true;
            ChromeDriver chromeDriver = new ChromeDriver(defaultService, options);
            return chromeDriver;
        }
    }

    private string CreateCookieDir(string accountid)
    {
        DirectoryInfo directoryInfo3 = new DirectoryInfo("C:\\Profile");
        if (!directoryInfo3.Exists)
            directoryInfo3.Create();
        string path = "C:\\Profile\\ProUser file" + accountid;
        DirectoryInfo directoryInfo4 = new DirectoryInfo(path);
        if (!directoryInfo4.Exists)
            directoryInfo4.Create();
        return path;
    }

    private string CreateCapsolver()
    {
        string targetPath = string.Empty;
        string path = "C:\\Extension\\";
        DirectoryInfo directoryInfo = new DirectoryInfo(path);
        if (!directoryInfo.Exists)
            directoryInfo.Create();

        string sourcePath = string.Empty;
        if (false)
        {
            sourcePath = Environment.CurrentDirectory + "\\Extension\\CapSolver.Browser.Extension_proxy.zip";
            targetPath = path + "Capsolver_proxy.zip";
        }
        else
        {
            sourcePath = Environment.CurrentDirectory + "\\Extension\\google_pro_1.1.48.zip";
            targetPath = path + "google_pro_1.1.48.zip";
        }

        if (!File.Exists(targetPath))
        {
            lock (Obj)
            {
                File.Copy(sourcePath, targetPath, true);
            }
        }

        return targetPath;
    }

    public string CreateUa()
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
        num = 122; //_rand.Next(88, 124);
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
    }

    private string CreateProxy(string accountid, string type)
    {
        string proxy = string.Empty;
        string path = "C:\\Extension\\";
        DirectoryInfo directoryInfo = new DirectoryInfo(path);
        if (!directoryInfo.Exists)
            directoryInfo.Create();
        proxy = path + "proxy" + accountid + ".zip";
        if (!File.Exists(proxy))
        {
            lock (Obj)
            {
                CreateProxyFile(type);
                CreateZip.CreateZipFile("Extension\\Proxy", proxy);
            }
        }

        return proxy;
    }

    private void CreateProxyFile(string type)
    {
        if (type.Equals("IN"))
        {
            if (Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Type_922)
            {
                //proxyAddress = "192.168.50.176:30000";
                // StringBuilder stringBuilder = new StringBuilder();
                // using (StreamReader streamReader =
                //        new StreamReader("Extension/proxySetting922/background.js", Encoding.GetEncoding("utf-8")))
                // {
                //     while (streamReader.ReadLine() is { } str)
                //         stringBuilder.AppendLine(str);
                //     streamReader.Close();
                // }
                //
                // string[] strArray = proxyAddress.Split(':');
                // stringBuilder.Replace("[$http$]", strArray[0]);
                // stringBuilder.Replace("[$port$]", strArray[1]);
                // File.WriteAllText("Extension/proxy/background.js", stringBuilder.ToString(), Encoding.UTF8);
            }
            else if (Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_IsUse)
            {
                StringBuilder stringBuilder = new StringBuilder();
                using (StreamReader streamReader =
                       new StreamReader("Extension/proxySetting/background.js", Encoding.GetEncoding("utf-8")))
                {
                    while (streamReader.ReadLine() is { } str)
                        stringBuilder.AppendLine(str);
                    streamReader.Close();
                }

                string[] strArray = Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Url.Split(':');
                stringBuilder.Replace("[$http$]", strArray[0]);
                stringBuilder.Replace("[$port$]", strArray[1]);


                stringBuilder.Replace("[$name$]", Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_UserName);
                stringBuilder.Replace("[$password$]", Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Pwd);

                File.WriteAllText("Extension/proxy/background.js", stringBuilder.ToString(), Encoding.UTF8);
            }
        }
        else if (type.Equals("IN"))
        {
            if (Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Type_922)
            {
                //proxyAddress = "192.168.50.176:30000";
                // StringBuilder stringBuilder = new StringBuilder();
                // using (StreamReader streamReader =
                //        new StreamReader("Extension/proxySetting922/background.js", Encoding.GetEncoding("utf-8")))
                // {
                //     while (streamReader.ReadLine() is { } str)
                //         stringBuilder.AppendLine(str);
                //     streamReader.Close();
                // }
                //
                // string[] strArray = proxyAddress.Split(':');
                // stringBuilder.Replace("[$http$]", strArray[0]);
                // stringBuilder.Replace("[$port$]", strArray[1]);
                // File.WriteAllText("Extension/proxy/background.js", stringBuilder.ToString(), Encoding.UTF8);
            }
            else if (Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_IsUse)
            {
                StringBuilder stringBuilder = new StringBuilder();
                using (StreamReader streamReader =
                       new StreamReader("Extension/proxySetting/background.js", Encoding.GetEncoding("utf-8")))
                {
                    while (streamReader.ReadLine() is { } str)
                        stringBuilder.AppendLine(str);
                    streamReader.Close();
                }

                string[] strArray = Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Url.Split(':');
                stringBuilder.Replace("[$http$]", strArray[0]);
                stringBuilder.Replace("[$port$]", strArray[1]);


                stringBuilder.Replace("[$name$]", Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_UserName);
                stringBuilder.Replace("[$password$]", Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Pwd);

                File.WriteAllText("Extension/proxy/background.js", stringBuilder.ToString(), Encoding.UTF8);
            }
        }
        // else if (type.Equals("Lunaproxy"))
        // {
        //     StringBuilder stringBuilder = new StringBuilder();
        //     using (StreamReader streamReader =
        //            new StreamReader("Extension/proxySetting/background.js", Encoding.GetEncoding("utf-8")))
        //     {
        //         while (streamReader.ReadLine() is { } str)
        //             stringBuilder.AppendLine(str);
        //         streamReader.Close();
        //     }
        //
        //     string[] strArray = proxyAddress.Split(':');
        //     stringBuilder.Replace("[$http$]", strArray[0]);
        //     stringBuilder.Replace("[$port$]", strArray[1]);
        //
        //
        //     stringBuilder.Replace("[$name$]", strArray[2]);
        //     stringBuilder.Replace("[$password$]", strArray[3]);
        //
        //     File.WriteAllText("Extension/proxy/background.js", stringBuilder.ToString(), Encoding.UTF8);
        // }
        // else if (type.Equals("NineTwo"))
        // {
        //     proxyAddress = "192.168.50.176:30000";
        //     StringBuilder stringBuilder = new StringBuilder();
        //     using (StreamReader streamReader =
        //            new StreamReader("Extension/proxySetting922/background.js", Encoding.GetEncoding("utf-8")))
        //     {
        //         while (streamReader.ReadLine() is { } str)
        //             stringBuilder.AppendLine(str);
        //         streamReader.Close();
        //     }
        //
        //     string[] strArray = proxyAddress.Split(':');
        //     stringBuilder.Replace("[$http$]", strArray[0]);
        //     stringBuilder.Replace("[$port$]", strArray[1]);
        //     File.WriteAllText("Extension/proxy/background.js", stringBuilder.ToString(), Encoding.UTF8);
        // }
    }

    #endregion
}