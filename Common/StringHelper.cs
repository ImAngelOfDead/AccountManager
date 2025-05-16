using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AccountManager.Common
{
    public static class StringHelper
    {
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

        //Guid 获取随机数种子
        public static int GetRandomGuid()
        {
            return Guid.NewGuid().GetHashCode();
        }

        //返回0.8991527960220353 16-18位随机小数
        public static string GetRandJs()
        {
            Random rand = new Random(GetRandomGuid());
            return rand.NextDouble().ToString();
        }

        /// <summary>
        /// MD5加密字符串
        /// </summary>
        /// <param name="input">字符串
        /// <returns>md5加密结果</returns>
        public static string MD5(string input)
        {
            // input = "123456z";
            var md5 = new MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] result = md5.ComputeHash(data);
            String ret = "";
            ;
            for (int i = 0; i < result.Length; i++)
                ret += result[i].ToString("x2");
            return ret;
        }

        public static string GetRealCookie(string cookie, string keyName)
        {
            List<string> strList = StringHelper.GetMidStrList(cookie, keyName + "=", ";");
            return strList.Count > 0 ? (keyName + "=" + strList[strList.Count - 1] + ";") : string.Empty;
        }

        /// <summary>
        /// 取十三位时间戳
        /// </summary>
        /// <param name="nowTime">给的时间值
        /// <returns>返回13位时间戳</returns>
        public static string GetUnixTime(DateTime nowTime)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));

            long unixTime = (long)Math.Round((nowTime - startTime).TotalMilliseconds, MidpointRounding.AwayFromZero);

            return unixTime.ToString();
        }

        /// <summary>        
        /// 时间戳转为C#格式时间        
        /// </summary>        
        /// <param name=”timeStamp”></param>        
        /// <returns></returns>        
        public static DateTime ConvertStringToDateTime(string timeStamp)
        {
            if (timeStamp.Length == 10)
            {
                Int64 begtime = Convert.ToInt64(timeStamp) * 10000000;
                DateTime dt_1970 = new DateTime(1970, 1, 1, 8, 0, 0);
                long tricks_1970 = dt_1970.Ticks; //1970年1月1日刻度
                long time_tricks = tricks_1970 + begtime; //日志日期刻度
                DateTime dt = new DateTime(time_tricks); //转化为DateTime
                return dt;
            }
            else
            {
                DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
                long lTime = long.Parse(timeStamp + "0000");
                TimeSpan toNow = new TimeSpan(lTime);
                return dtStart.Add(toNow);
            }
        }

        /// <summary>
        /// 从Json格式的CK转换到CookieCollection
        /// </summary>
        /// <param name="cookieJsonStr"></param>
        /// <returns></returns>
        public static CookieCollection GetCookieCollectionByCookieJsonStr(string cookieJsonStr)
        {
            CookieCollection cks = new CookieCollection();

            JArray jaCookie = null;
            try
            {
                jaCookie = JArray.Parse(cookieJsonStr);
            }
            catch
            {
            }

            ;
            if (jaCookie == null || jaCookie.Count == 0) return cks;

            try
            {
                for (int i = 0; i < jaCookie.Count; i++)
                {
                    JToken jt = jaCookie[i];

                    if (jt["name"].ToString().ToLower().Trim() == "i_user") continue;

                    Cookie ck = new Cookie();
                    ck.Name = jt["name"].ToString().Trim();
                    ck.Value = jt["value"].ToString().Trim();
                    ck.Domain = jt["domain"] == null ? string.Empty : jt["domain"].ToString().Trim();
                    if (jt["domain"] == null)
                        ck.Domain = jt["domian"] == null ? string.Empty : jt["domian"].ToString().Trim();
                    ck.Path = jt["path"] == null ? string.Empty : jt["path"].ToString().Trim();
                    ck.Secure = jt["secure"] == null || jt["secure"].ToString().Trim().ToLower() != "true"
                        ? false
                        : true;
                    ck.HttpOnly = jt["httpOnly"] == null || jt["httpOnly"].ToString().Trim().ToLower() != "true"
                        ? false
                        : true;
                    if (jt["expiry"] != null && !string.IsNullOrEmpty(jt["expiry"].ToString().Trim()))
                        ck.Expires = ConvertStringToDateTime(jt["expiry"].ToString().Trim() + "000");

                    cks.Add(ck);
                }
            }
            catch
            {
                cks = new CookieCollection();
            }

            return cks;
        }

        /// <summary>
        /// 从Json格式的CK转换到CookieCollection
        /// </summary>
        /// <param name="cookieJsonStr"></param>
        /// <returns></returns>
        public static string GetCookieJsonStrByCookieCollection(CookieCollection cks)
        {
            string cookieJsonStr = string.Empty;

            JArray jaCookie = new JArray();

            for (int i = 0; i < cks.Count; i++)
            {
                JObject jock = new JObject();
                Cookie ck = cks[i];

                jock["name"] = ck.Name;
                jock["value"] = ck.Value;
                jock["domain"] = ck.Domain;
                jock["path"] = ck.Path;
                jock["secure"] = ck.Secure;
                jock["httpOnly"] = ck.HttpOnly;
                jock["expiry"] = StringHelper.GetUnixTime(DateTime.Now).Substring(0, 10);

                jaCookie.Add(jock);
            }

            cookieJsonStr = JsonConvert.SerializeObject(jaCookie);

            return cookieJsonStr;
        }

        /// <summary>
        /// 更新Cookie
        /// </summary>
        /// <param name="oldCookieStr"></param>
        /// <param name="newCookies"></param>
        /// <returns></returns>
        public static string UpdateCookie(string oldCookieStr, System.Net.CookieCollection newCookies)
        {
            Dictionary<string, string> dic_OldCookies = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(oldCookieStr))
            {
                List<string> cks = oldCookieStr.Split(';').Where(s => !string.IsNullOrEmpty(s) && s.Trim().Length > 0)
                    .Select(s => s.Trim()).ToList();
                cks.ForEach(ck =>
                {
                    int fIndex = ck.IndexOf("=");
                    if (fIndex > -1)
                    {
                        string ckName = ck.Substring(0, fIndex);
                        string ckValue = ck.Substring(fIndex + 1);
                        if (!string.IsNullOrEmpty(ckName))
                        {
                            if (dic_OldCookies.ContainsKey(ckName)) dic_OldCookies[ckName] = ckValue;
                            else dic_OldCookies.Add(ckName, ckValue);
                        }
                    }
                });
            }

            if (newCookies != null && newCookies.Count > 0)
            {
                foreach (System.Net.Cookie ck in newCookies)
                {
                    if (dic_OldCookies.ContainsKey(ck.Name)) dic_OldCookies[ck.Name] = ck.Value;
                    else dic_OldCookies.Add(ck.Name, ck.Value);
                }
            }

            string retCookieStr = string.Join("; ", dic_OldCookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            return retCookieStr;
        }

        /// <summary>
        /// 更新Cookie
        /// </summary>
        /// <param name="oldCookieStr"></param>
        /// <param name="newCookieStr"></param>
        /// <returns></returns>
        public static string UpdateCookie(string oldCookieStr, string newCookieStr)
        {
            Dictionary<string, string> dic_OldCookies = new Dictionary<string, string>();
            Dictionary<string, string> dic_NewCookies = new Dictionary<string, string>();
            List<string> cks = null;

            if (!string.IsNullOrEmpty(oldCookieStr))
            {
                cks = oldCookieStr.Split(';').Where(s => !string.IsNullOrEmpty(s) && s.Trim().Length > 0)
                    .Select(s => s.Trim()).ToList();
                cks.ForEach(ck =>
                {
                    int fIndex = ck.IndexOf("=");
                    if (fIndex > -1)
                    {
                        string ckName = ck.Substring(0, fIndex);
                        string ckValue = ck.Substring(fIndex + 1);
                        if (!string.IsNullOrEmpty(ckName))
                        {
                            if (dic_OldCookies.ContainsKey(ckName)) dic_OldCookies[ckName] = ckValue;
                            else dic_OldCookies.Add(ckName, ckValue);
                        }
                    }
                });
            }

            if (!string.IsNullOrEmpty(newCookieStr))
            {
                cks = newCookieStr.Split(';').Where(s => !string.IsNullOrEmpty(s) && s.Trim().Length > 0)
                    .Select(s => s.Trim()).ToList();
                cks.ForEach(ck =>
                {
                    int fIndex = ck.IndexOf("=");
                    if (fIndex > -1)
                    {
                        string ckName = ck.Substring(0, fIndex);
                        string ckValue = ck.Substring(fIndex + 1);
                        if (!string.IsNullOrEmpty(ckName))
                        {
                            if (dic_NewCookies.ContainsKey(ckName)) dic_NewCookies[ckName] = ckValue;
                            else dic_NewCookies.Add(ckName, ckValue);
                        }
                    }
                });
            }

            foreach (var kvp in dic_NewCookies)
            {
                if (dic_OldCookies.ContainsKey(kvp.Key)) dic_OldCookies[kvp.Key] = kvp.Value;
                else dic_OldCookies.Add(kvp.Key, kvp.Value);
            }

            string retCookieStr = string.Join("; ", dic_OldCookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            return retCookieStr;
        }

        /// <summary>
        /// 合并更新Cookie
        /// </summary>
        /// <param name="oldCookies"></param>
        /// <param name="newCookies"></param>
        /// <returns></returns>
        public static CookieCollection UpdateCookies(CookieCollection oldCookies, CookieCollection newCookies)
        {
            if (oldCookies == null) oldCookies = new CookieCollection();
            if (newCookies == null || newCookies.Count == 0) return oldCookies;
            for (int i = 0; i < newCookies.Count; i++)
            {
                Cookie ckn = newCookies[i];
                Cookie cko = oldCookies.Cast<Cookie>()
                    .Where(ck => ck.Name.ToLower() == ckn.Name.ToLower() && ck.Domain == ckn.Domain).FirstOrDefault();
                if (cko == null) oldCookies.Add(ckn);
                else
                {
                    cko.Value = ckn.Value;
                    cko.Expires = ckn.Expires;
                }
            }

            return oldCookies;
        }

        /// <summary>
        /// 合并更新Cookie
        /// </summary>
        /// <param name="oldCookies"></param>
        /// <param name="newCookies"></param>
        /// <returns></returns>
        public static CookieCollection UpdateCookies(CookieCollection oldCookies, string newCookieStr)
        {
            if (oldCookies == null) oldCookies = new CookieCollection();
            if (string.IsNullOrEmpty(newCookieStr)) return oldCookies;

            List<string> ckNames_Other = new List<string>()
                { "domain", "domian", "expires", "Max-Age", "path", "httponly", "secure", "SameSite" };
            List<string> cks_All = newCookieStr.Split(';').Select(s =>
                    s.Trim().Replace("httponly,", string.Empty).Replace("SameSite=None,", string.Empty)
                        .Replace("secure,", string.Empty).Replace("HttpOnly,", string.Empty)
                        .Replace("Secure,", string.Empty).Replace("path=/,", string.Empty)
                        .Replace("Path=/,", string.Empty))
                .ToList();
            List<string> cks = cks_All.Where(s =>
                    s.Contains("=") && ckNames_Other.Where(ckn => s.ToLower().Contains($"{ckn.ToLower()}")).Count() ==
                    0)
                .ToList();

            string format = "ddd, dd-MMM-yyyy HH:mm:ss 'GMT'";
            DateTime dt;
            for (int i = 0; i < cks.Count; i++)
            {
                int index = cks_All.IndexOf($"{cks[i]}");
                if (index == -1) continue;

                Cookie ckNew = new Cookie();
                string[] arr = cks[i].Split('=');
                if (arr.Length > 1)
                {
                    ckNew.Name = arr[0].Trim();
                    string value = "";
                    for (int j = 1; j < arr.Length; j++)
                    {
                        if (string.IsNullOrEmpty(value))
                        {
                            value += arr[j].Trim();
                        }
                        else
                        {
                            value += "=" + arr[j].Trim();
                        }
                    }

                    ckNew.Value = value;
                }
                else
                {
                    ckNew.Name = arr[0].Trim();
                    ckNew.Value = arr[1].Trim();
                }


                int eIndex = cks_All.FindIndex(index, cka => cka.Contains("Expires=") || cka.Contains("expires="));
                string expiresStr = cks_All[eIndex].Replace("Expires=", "").Replace("expires=", "");
                if (DateTime.TryParseExact(expiresStr, format, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out dt)) ckNew.Expires = dt;

                int dIndex = cks_All.FindIndex(index, cka => cka.Contains("Domain=") || cka.Contains("domain="));
                string domainStr = cks_All[dIndex].Replace("Domain=", "").Replace("domain=", "");
                ckNew.Domain = domainStr;

                Cookie cko = oldCookies.Cast<Cookie>().Where(ck =>
                        ck.Name.Trim().ToLower() == ckNew.Name.Trim().ToLower() && ck.Domain == ckNew.Domain)
                    .FirstOrDefault();
                if (cko == null) oldCookies.Add(ckNew);
                else
                {
                    cko.Value = ckNew.Value;
                    cko.Expires = ckNew.Expires;
                }
            }

            return oldCookies;
        }

        public static CookieCollection UpdateCookiesIN(CookieCollection oldCookies, string newCookieStr)
        {
            if (oldCookies == null) oldCookies = new CookieCollection();
            if (string.IsNullOrEmpty(newCookieStr)) return oldCookies;

            List<string> ckNames_Other = new List<string>()
                { "domain", "domian", "expires", "Max-Age", "path", "httponly", "secure", "SameSite" };
            List<string> cks_All = newCookieStr.Split(';').Select(s =>
                    s.Trim().Replace("httponly,", string.Empty).Replace("SameSite=None,", string.Empty)
                        .Replace("secure,", string.Empty).Replace("HttpOnly,", string.Empty)
                        .Replace("Secure,", string.Empty).Replace("path=/,", string.Empty)
                        .Replace("Path=/,", string.Empty))
                .ToList();
            List<string> cks = cks_All.Where(s =>
                    s.Contains("=") && ckNames_Other.Where(ckn => s.ToLower().Contains($"{ckn.ToLower()}")).Count() ==
                    0)
                .ToList();

            string format = "ddd, dd-MMM-yyyy HH:mm:ss 'GMT'";
            DateTime dt;
            for (int i = 0; i < cks.Count; i++)
            {
                int index = cks_All.IndexOf($"{cks[i]}");
                if (index == -1) continue;

                Cookie ckNew = new Cookie();
                string[] arr = cks[i].Split('=');
                ckNew.Name = arr[0].Trim();
                ckNew.Value = arr[1];

                int eIndex = cks_All.FindIndex(index, cka => cka.Contains("Expires=") || cka.Contains("expires="));
                string expiresStr = cks_All[eIndex].Replace("Expires=", "").Replace("expires=", "");
                if (DateTime.TryParseExact(expiresStr, format, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out dt)) ckNew.Expires = dt;

                int dIndex = cks_All.FindIndex(index, cka => cka.Contains("Domain=") || cka.Contains("domain="));
                string domainStr = cks_All[dIndex].Replace("Domain=", "").Replace("domain=", "");
                ckNew.Domain = domainStr;

                Cookie cko = oldCookies.Cast<Cookie>().Where(ck =>
                        ck.Name.Trim().ToLower() == ckNew.Name.Trim().ToLower() && ck.Domain == ckNew.Domain)
                    .FirstOrDefault();
                if (cko == null) oldCookies.Add(ckNew);
                else
                {
                    cko.Value = ckNew.Value;
                    cko.Expires = ckNew.Expires;
                }
            }

            return oldCookies;
        }

        public static string MergeCookie(string oldCookie, string newCookie)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();

            var arr = oldCookie.Split(';');
            foreach (var item in arr)
            {
                if (item.Trim() == "") continue;
                var tmp = item.Trim().Split(new char[] { '=' }, 2);
                if (item == "") continue;
                string key = tmp[0], val = tmp[1];
                if (dic.ContainsKey(tmp[0]))
                {
                    dic[key] = val;
                }
                else
                {
                    dic.Add(key, val);
                }
            }

            arr = newCookie.Split(';');
            foreach (var item in arr)
            {
                if (item.Trim() == "") continue;
                var tmp = item.Trim().Split(new char[] { '=' }, 2);

                string key = tmp[0], val = tmp[1];
                if (dic.ContainsKey(tmp[0]))
                {
                    dic[key] = val;
                }
                else
                {
                    dic.Add(key, val);
                }
            }

            var cookie = "";
            foreach (var item in dic)
            {
                string key = item.Key, val = item.Value;
                if (val != "" && val != "delete me")
                {
                    cookie += $"{key}={val}; ";
                }
            }

            return cookie;
        }

        public static void GetRASEncryptKey(out string publicKey, string privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                publicKey = rsa.ToXmlString(false);
                privateKey = rsa.ToXmlString(true);
            }
        }

        public static string RASEncrypt(string publicKey, string data)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);

                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] encryptedBytes = rsa.Encrypt(dataBytes, false);

                return Convert.ToBase64String(encryptedBytes);
            }
        }

        public static string RASDecrypt(string privateKey, string encryptedData)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);

                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
                byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, false);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }

        public static string UrlEncode(string str)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char c in str)
            {
                if (HttpUtility.UrlEncode(c.ToString()).Length > 1)
                {
                    builder.Append(HttpUtility.UrlEncode(c.ToString()).ToUpper());
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        [DllImport("shell32.dll", ExactSpelling = true)]
        public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl,
            [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr ILCreateFromPath([MarshalAs(UnmanagedType.LPTStr)] string pszPath);

        /// <summary>
        /// 在Windows资源管理器打开文件夹，并选中指定的文件或文件夹
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="filesToSelect">要选中的文件或文件夹路径</param>
        public static void OpenFolderAndSelectFiles(string folderPath, params string[] filesToSelect)
        {
            IntPtr dir = ILCreateFromPath(folderPath);

            var filesToSelectIntPtrs = new IntPtr[filesToSelect.Length];
            for (int i = 0; i < filesToSelect.Length; i++)
            {
                filesToSelectIntPtrs[i] = ILCreateFromPath(filesToSelect[i]);
            }

            SHOpenFolderAndSelectItems(dir, (uint)filesToSelect.Length, filesToSelectIntPtrs, 0);
        }

        /// <summary>
        /// Usc2转Ansi
        /// </summary>
        /// <param name="tmp"></param>
        /// <returns></returns>
        public static string Usc2ConvertToAnsi(string tmp)
        {
            return System.Web.HttpUtility.UrlDecode(Regex.Unescape(tmp));
        }

        /// <summary>
        /// 获取随机数，解决重复问题
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int GetRandomNumber(int min, int max)
        {
            Guid guid = Guid.NewGuid(); //每次都是全新的ID
            string sGuid = guid.ToString();
            int seed = DateTime.Now.Millisecond;
            for (int i = 0; i < sGuid.Length; i++)
            {
                switch (sGuid[i])
                {
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                        seed = seed + 1;
                        break;
                    case 'h':
                    case 'i':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'm':
                    case 'n':
                        seed = seed + 2;
                        break;
                    case 'o':
                    case 'p':
                    case 'q':
                    case 'r':
                    case 's':
                    case 't':
                        seed = seed + 3;
                        break;
                    case 'u':
                    case 'v':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                        seed = seed + 3;
                        break;
                    default:
                        seed = seed + 4;
                        break;
                }
            }

            Random random = new Random(seed);
            return random.Next(min, max);
        }

        /// <summary>
        /// 取随机字符串
        /// </summary>
        /// <param name="daXie">是否需要大写字母</param>
        /// <param name="xiaoXie">是否需要小写字母</param>
        /// <param name="number">是否需要数字</param>
        /// <param name="spChar">是否需要特殊字符</param>
        /// <param name="minLength">最小长度</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns></returns>
        public static string GetRandomString(bool daXie, bool xiaoXie, bool number, bool spChar, int minLength,
            int maxLength)
        {
            string rStr = string.Empty;
            if (!daXie && !xiaoXie && !number && !spChar) return rStr;

            string str_daXie = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string str_xiaoXie = "abcdefghijklmnopqrstuvwxyz";
            string str_number = "0123456789";
            string str_spChar = "!#%&()*+-.<=>@[]$_{}";

            List<string> sList = new List<string>();
            if (daXie) sList.Add(str_daXie);
            if (xiaoXie) sList.Add(str_xiaoXie);
            if (number) sList.Add(str_number);
            if (spChar) sList.Add(str_spChar);

            int getLength = GetRandomNumber(minLength, maxLength + 1);
            int typeIndex = 0;
            int charIndex = 0;
            for (int i = 0; i < getLength; i++)
            {
                typeIndex = GetRandomNumber(0, sList.Count);
                charIndex = GetRandomNumber(0, sList[typeIndex].Length);
                rStr += sList[typeIndex][charIndex].ToString();
            }

            return rStr;
        }

        /// <summary>
        /// 生成随机UA
        /// </summary>
        /// <returns></returns>
        public static string CreateRandomUserAgent()
        {
            string[] strArray1 = { "WOW64", "Win64;x64" };
            string[] strArray2 = new string[13];
            strArray2[0] = "Mozilla/5.0 (Windows NT ";
            int num = GetRandomNumber(6, 11);
            strArray2[1] = num.ToString();
            strArray2[2] = ".";
            num = GetRandomNumber(0, 10);
            strArray2[3] = num.ToString();
            strArray2[4] = "; ";
            strArray2[5] = strArray1[GetRandomNumber(0, 2)];
            strArray2[6] = ") AppleWebKit/537.36 (KHTML, like Gecko) Chrome/";
            num = GetRandomNumber(76, 96);
            strArray2[7] = num.ToString();
            strArray2[8] = ".0.";
            num = GetRandomNumber(1000, 5000);
            strArray2[9] = num.ToString();
            strArray2[10] = ".";
            num = GetRandomNumber(0, 124);
            strArray2[11] = num.ToString();
            strArray2[12] = " Safari/537.36";
            string str = string.Concat(strArray2);
            return str;
        }

        public static string GenerateSurname()
        {
            string name = string.Empty;
            string[] currentConsonant;
            string[] vowels = "a,a,a,a,a,e,e,e,e,e,e,e,e,e,e,e,i,i,i,o,o,o,u,y,ee,ee,ea,ea,ey,eau,eigh,oa,oo,ou,ough,ay"
                .Split(',');
            string[] commonConsonants = "s,s,s,s,t,t,t,t,t,n,n,r,l,d,sm,sl,sh,sh,th,th,th".Split(',');
            string[] averageConsonants = "sh,sh,st,st,b,c,f,g,h,k,l,m,p,p,ph,wh".Split(',');
            string[] middleConsonants =
                "x,ss,ss,ch,ch,ck,ck,dd,kn,rt,gh,mm,nd,nd,nn,pp,ps,tt,ff,rr,rk,mp,ll".Split(','); //Can't start
            string[] rareConsonants = "j,j,j,v,v,w,w,w,z,qu,qu".Split(',');
            Random rng = new Random(Guid.NewGuid().GetHashCode()); //http://codebetter.com/blogs/59496.aspx
            int[] lengthArray = new int[] { 2, 2, 2, 2, 2, 2, 3, 3, 3, 4 }; //Favor shorter names but allow longer ones
            int length = lengthArray[rng.Next(lengthArray.Length)];
            for (int i = 0; i < length; i++)
            {
                int letterType = rng.Next(1000);
                if (letterType < 775) currentConsonant = commonConsonants;
                else if (letterType < 875 && i > 0) currentConsonant = middleConsonants;
                else if (letterType < 985) currentConsonant = averageConsonants;
                else currentConsonant = rareConsonants;
                name += currentConsonant[rng.Next(currentConsonant.Length)];
                name += vowels[rng.Next(vowels.Length)];
                if (name.Length > 4 && rng.Next(1000) < 800) break; //Getting long, must roll to save
                if (name.Length > 6 && rng.Next(1000) < 950) break; //Really long, roll again to save
                if (name.Length > 7) break; //Probably ridiculous, stop building and add ending
            }

            int endingType = rng.Next(1000);
            if (name.Length > 6)
                endingType -= (name.Length * 25); //Don't add long endings if already long
            else
                endingType += (name.Length * 10); //Favor long endings if short
            if (endingType < 400)
            {
            } // Ends with vowel
            else if (endingType < 775) name += commonConsonants[rng.Next(commonConsonants.Length)];
            else if (endingType < 825) name += averageConsonants[rng.Next(averageConsonants.Length)];
            else if (endingType < 840) name += "ski";
            else if (endingType < 860) name += "son";
            else if (Regex.IsMatch(name, "(.+)(ay|e|ee|ea|oo)$") || name.Length < 5)
            {
                name = "Mc" + name.Substring(0, 1).ToUpper() + name.Substring(1);
                return name;
            }
            else name += "ez";

            name = name.Substring(0, 1).ToUpper() + name.Substring(1); //Capitalize first letter
            return name;
        }
        
    }
}