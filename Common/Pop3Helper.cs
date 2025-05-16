using Newtonsoft.Json;
using OpenPop.Pop3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Message = OpenPop.Mime.Message;

namespace AccountManager.Common
{
    public static class Pop3Helper
    {
        /// <summary>
        /// 获取连接的客户端
        /// </summary>
        /// <param name="mailName"></param>
        /// <param name="mailPwd"></param>
        /// <returns></returns>
        public static Pop3Client GetPop3Client(string mailName, string mailPwd)
        {
            string[] arr = mailName.Split('@');
            if (arr.Length != 2) return null;

            // 邮件服务器信息
            string host = $"pop.{arr[1].Trim()}";
            int port = 995;
            bool useSsl = true;

            Pop3Client client = null;

            try
            {
                client = new Pop3Client();
                client.Connect(host, port, useSsl); // 使用加密连接
                client.Authenticate(mailName, mailPwd);
            }
            catch { }

            return client;
        }
        /// <summary>
        /// 获取邮件数量
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static int GetMessageCount(Pop3Client client)
        {
            if (client == null) return 0;
            else return client.GetMessageCount();
        }
        /// <summary>
        /// 获取邮件内容
        /// </summary>
        /// <param name="client"></param>
        /// <param name="mIndexList"></param>
        /// <returns></returns>
        public static List<Pop3MailMessage> GetMessageByIndex(Pop3Client client)
        {
            List<Pop3MailMessage> msg_pop3_List = new List<Pop3MailMessage>();

            try
            {
                int msgCount = client.GetMessageCount();
                int msgCountMax = 4;
                int msgIndex = 0;

                Message msg = null;
                Pop3MailMessage msg_pop3 = null;
                for (int i = 0; i < msgCountMax; i++)
                {
                    msgIndex = msgCount - i;
                    if (msgIndex < 1) break;

                    msg = null;
                    try { msg = client.GetMessage(msgIndex); } catch { }
                    if (msg == null) continue;

                    string html = "";
                    if (msg.MessagePart.IsText) html = msg.MessagePart.GetBodyAsText();
                    else if (msg.MessagePart.IsMultiPart)
                    {
                        foreach (var part in msg.FindAllTextVersions())
                        {
                            html += part.GetBodyAsText();
                        }
                    }

                    msg_pop3 = new Pop3MailMessage();
                    msg_pop3.Subject = msg.Headers.Subject;
                    msg_pop3.From = msg.Headers.From.ToString();
                    msg_pop3.To = $"{string.Join(", ", msg.Headers.To)}";
                    msg_pop3.DateSent = msg.Headers.DateSent;
                    msg_pop3.Html = html;
                    msg_pop3.Message = msg;

                    msg_pop3_List.Add(msg_pop3);
                }
            }
            catch { }

            return msg_pop3_List;
        }
        /// <summary>
        /// 停止连接
        /// </summary>
        /// <param name="client"></param>
        public static void Pop3ClientDisconnect(Pop3Client client)
        {
            if (client == null) client.Disconnect();
        }
    }
    public class Pop3MailMessage
    {
        /// <summary>
        /// 主题
        /// </summary>
        public string Subject { get; set; } = string.Empty;
        /// <summary>
        /// 发件人
        /// </summary>
        public string From { get; set; } = string.Empty;
        /// <summary>
        /// 收件人
        /// </summary>
        public string To { get; set; } = string.Empty;
        /// <summary>
        /// 收件日期
        /// </summary>
        public DateTime DateSent { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Html { get; set; } = string.Empty;
        [JsonIgnore]
        public Message Message { get; set; } = null;
    }
}
