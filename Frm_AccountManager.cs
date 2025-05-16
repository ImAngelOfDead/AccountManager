using AccountManager.DAL;
using AccountManager.Models;
using Amib.Threading;
using BookingService.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AccountManager.Common;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Path = System.IO.Path;

namespace AccountManager
{
    public partial class Frm_AccountManager : Form
    {
        public Frm_AccountManager()
        {
            InitializeComponent();

            #region DGV开启双缓冲,避免闪烁问题

            this.dgv_FB.DoubleBufferedDataGirdView(true);
            this.dgv_IG.DoubleBufferedDataGirdView(true);
            this.dgv_IN.DoubleBufferedDataGirdView(true);
            this.dgv_EM.DoubleBufferedDataGirdView(true);
            this.dgv_IN_RE.DoubleBufferedDataGirdView(true);

            #endregion
        }

        #region 软件初始化

        private void Frm_AccountManager_Load(object sender, EventArgs e)
        {
            this.SoftInit();
        }

        private void Frm_AccountManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.SoftClose();
        }


        //初始化方法
        private void SoftInit()
        {
            this.dgv_Main.AutoGenerateColumns = false;
            this.dgv_DomainList.AutoGenerateColumns = false;
            SetCountry_EM();
            SetCountry_IN_RE();
            //初始化Cbb列
            this.DgvCbbInit();

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            try
            {
                this.GetSetting();
                this.ShowSetting();
            }
            catch
            {
            }

            //启动刷新线程
            this.thread_UpdateDGV = new Thread(new ThreadStart(this.ThreadMethod_UpdateDGV));
            this.thread_UpdateDGV.IsBackground = true;
            this.thread_UpdateDGV.Start();

            //启动http服务程序，用于使用X86平台功能
            this.Method_StartX86HttpApp();
        }

        //关闭方法
        private void SoftClose()
        {
            if (this.thread_UpdateDGV != null) thread_UpdateDGV.Abort();

            this.SaveSetting_FromUser();
            this.SaveSetting_ToDisk();
        }

        #endregion

        #region 启动X86平台Http服务

        private void Method_StartX86HttpApp()
        {
            // string pName = $"HttpServerTool";
            //
            // if (Process.GetProcessesByName(pName).Count() > 0) return;
            //
            // Process process = new Process();
            // process.StartInfo.FileName = $@"{Application.StartupPath}\HttpServerTool_X86\{pName}.exe";
            // process.StartInfo.WorkingDirectory = $@"{Application.StartupPath}\HttpServerTool_X86";
            // process.StartInfo.CreateNoWindow = true;
            // process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            // process.Start();
        }

        #endregion

        #region 用户配置

        private void GetSetting()
        {
            //完善配置
            string dir = $@"{System.Windows.Forms.Application.StartupPath}\UserInfo";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string fileName = $@"{dir}\Setting.db";
            if (File.Exists(fileName))
            {
                string fileContent = string.Empty;
                try
                {
                    fileContent = SerializeObjectToString.ReadTxt(fileName);
                }
                catch
                {
                    fileContent = string.Empty;
                }

                Setting setInfo = null;
                if (!string.IsNullOrEmpty(fileContent))
                {
                    try
                    {
                        setInfo = JsonConvert.DeserializeObject<Setting>(fileContent);
                    }
                    catch
                    {
                    }
                }

                if (setInfo == null) setInfo = new Setting();

                Program.setting = setInfo;
            }
            else
            {
                Program.setting = new Setting();
            }
        }

        private void ShowSetting()
        {
            //加载国家区号列表
            Program.setting.Ja_CountrysPhoneNumInfo =
                JArray.Parse(Encoding.UTF8.GetString(Properties.Resources.CountrysPhoneNumInfo));
            //加载城市列表
            Program.setting.Ja_CitysInfo = JArray.Parse(Encoding.UTF8.GetString(Properties.Resources.cities_en));
            for (int i = 0; i < Program.setting.Ja_CitysInfo.Count; i++)
            {
                Program.setting.Ja_CitysInfo[i]["Value_JsonStr"] =
                    JsonConvert.SerializeObject(Program.setting.Ja_CitysInfo[i]);
            }

            this.Invoke(new Action(() =>
            {
                #region FB部分

                this.dgv_FB.AutoGenerateColumns = false;
                this.dgv_GMList_ForBind_FB.AutoGenerateColumns = false;
                this.dgv_TaskList_FB.AutoGenerateColumns = false;

                this.txt_ThreadCountMax_FB.Text = Program.setting.Setting_FB.ThreadCountMax.ToString();

                if (Program.setting.Setting_FB.Account_List != null &&
                    Program.setting.Setting_FB.Account_List.Count > 0)
                    this.dgv_FB.DataSource = Program.setting.Setting_FB.Account_List;
                if (Program.setting.Setting_FB.Mail_ForBind_List != null &&
                    Program.setting.Setting_FB.Mail_ForBind_List.Count > 0)
                    this.dgv_GMList_ForBind_FB.DataSource = Program.setting.Setting_FB.Mail_ForBind_List;

                //全局代理相关
                this.cb_Global_WebProxyInfo_FB.Checked = Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_IsUse;
                this.txt_Global_WebProxyInfo_FB_IPAddress.Text =
                    Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_Url;
                this.txt_Global_WebProxyInfo_FB_UserName.Text =
                    Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_UserName;
                this.txt_Global_WebProxyInfo_FB_Pwd.Text = Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_Pwd;

                //流程控制相关
                this.TaskList_FB_Init();

                //忘记密码相关
                if (Program.setting.Setting_FB.ForgotPwdSetting_Front_Mode == 0)
                    this.rb_ForgotPwdSetting_Front_Random_FB.Checked = true;
                else if (Program.setting.Setting_FB.ForgotPwdSetting_Front_Mode == 1)
                    this.rb_ForgotPwdSetting_Front_Custom_FB.Checked = true;
                else this.rb_ForgotPwdSetting_Front_Random_FB.Checked = true;
                this.txt_ForgotPwdSetting_Front_Custom_Content_FB.Text =
                    Program.setting.Setting_FB.ForgotPwdSetting_Front_Custom_Content.Trim();
                this.cb_ForgotPwdSetting_After_IsAddDate_FB.Checked =
                    Program.setting.Setting_FB.ForgotPwdSetting_After_IsAddDate;

                //统计相关
                this.ShowTongJiInfo_FB();
                this.ShowTongJiInfo_False_FB(1);

                this.cb_ForgotPwdSetting_LogMeOut_FB.Checked = Program.setting.Setting_FB.ForgotPwdSetting_LogMeOut;

                #endregion

                #region IG部分

                this.dgv_IG.AutoGenerateColumns = false;
                this.dgv_GMList_ForBind_IG.AutoGenerateColumns = false;
                this.dgv_TaskList_IG.AutoGenerateColumns = false;

                this.txt_ThreadCountMax_IG.Text = Program.setting.Setting_IG.ThreadCountMax.ToString();

                if (Program.setting.Setting_IG.Account_List != null &&
                    Program.setting.Setting_IG.Account_List.Count > 0)
                    this.dgv_IG.DataSource = Program.setting.Setting_IG.Account_List;
                if (Program.setting.Setting_IG.Mail_ForBind_List != null &&
                    Program.setting.Setting_IG.Mail_ForBind_List.Count > 0)
                    this.dgv_GMList_ForBind_IG.DataSource = Program.setting.Setting_IG.Mail_ForBind_List;

                //全局代理相关
                this.cb_Global_WebProxyInfo_IG.Checked = Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_IsUse;
                this.txt_Global_WebProxyInfo_IG_IPAddress.Text =
                    Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_Url;
                this.txt_Global_WebProxyInfo_IG_UserName.Text =
                    Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_UserName;
                this.txt_Global_WebProxyInfo_IG_Pwd.Text = Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_Pwd;

                //流程控制相关
                this.TaskList_IG_Init();

                //忘记密码相关
                if (Program.setting.Setting_IG.ForgotPwdSetting_Front_Mode == 0)
                    this.rb_ForgotPwdSetting_Front_Random_IG.Checked = true;
                else if (Program.setting.Setting_IG.ForgotPwdSetting_Front_Mode == 1)
                    this.rb_ForgotPwdSetting_Front_Custom_IG.Checked = true;
                else this.rb_ForgotPwdSetting_Front_Random_IG.Checked = true;
                this.txt_ForgotPwdSetting_Front_Custom_Content_IG.Text =
                    Program.setting.Setting_IG.ForgotPwdSetting_Front_Custom_Content.Trim();
                this.cb_ForgotPwdSetting_After_IsAddDate_IG.Checked =
                    Program.setting.Setting_IG.ForgotPwdSetting_After_IsAddDate;

                //统计相关
                this.ShowTongJiInfo_IG();
                this.ShowTongJiInfo_False_IG(1);

                this.cb_ForgotPwdSetting_LogMeOut_IG.Checked = Program.setting.Setting_IG.ForgotPwdSetting_LogMeOut;

                #endregion

                #region IN部分

                this.dgv_IN.AutoGenerateColumns = false;
                this.dgv_GMList_ForBind_IN.AutoGenerateColumns = false;
                this.dgv_TaskList_IN.AutoGenerateColumns = false;

                this.txt_ThreadCountMax_IN.Text = Program.setting.Setting_IN.ThreadCountMax.ToString();

                if (Program.setting.Setting_IN.Account_List != null &&
                    Program.setting.Setting_IN.Account_List.Count > 0)
                    this.dgv_IN.DataSource = Program.setting.Setting_IN.Account_List;
                if (Program.setting.Setting_IN.Mail_ForBind_List != null &&
                    Program.setting.Setting_IN.Mail_ForBind_List.Count > 0)
                    this.dgv_GMList_ForBind_IN.DataSource = Program.setting.Setting_IN.Mail_ForBind_List;

                //全局代理相关
                this.cb_Global_WebProxyInfo_IN.Checked = Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_IsUse;
                this.txt_Global_WebProxyInfo_IN_IPAddress.Text =
                    Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Url;
                this.txt_Global_WebProxyInfo_IN_UserName.Text =
                    Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_UserName;
                this.txt_Global_WebProxyInfo_IN_Pwd.Text = Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Pwd;
                this.cb_Global_922_proxy_IN.Checked =
                    Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Type_922;
                this.checkBox_protocol_IN.Checked = Program.setting.Setting_IN.Protocol;
                this.checkBox_selenium_IN.Checked = Program.setting.Setting_IN.Selenium;
                this.checkBox_ADSPower_IN.Checked = Program.setting.Setting_IN.ADSPower;
                //流程控制相关
                this.TaskList_IN_Init();

                //忘记密码相关
                if (Program.setting.Setting_IN.ForgotPwdSetting_Front_Mode == 0)
                    this.rb_ForgotPwdSetting_Front_Random_IN.Checked = true;
                else if (Program.setting.Setting_IN.ForgotPwdSetting_Front_Mode == 1)
                    this.rb_ForgotPwdSetting_Front_Custom_IN.Checked = true;
                else this.rb_ForgotPwdSetting_Front_Random_IN.Checked = true;
                this.txt_ForgotPwdSetting_Front_Custom_Content_IN.Text =
                    Program.setting.Setting_IN.ForgotPwdSetting_Front_Custom_Content.Trim();
                this.cb_ForgotPwdSetting_After_IsAddDate_IN.Checked =
                    Program.setting.Setting_IN.ForgotPwdSetting_After_IsAddDate;

                //统计相关
                this.ShowTongJiInfo_IN();
                this.ShowTongJiInfo_False_IN(1);

                this.cb_ForgotPwdSetting_LogMeOut_IN.Checked = Program.setting.Setting_IN.ForgotPwdSetting_LogMeOut;

                #endregion

                #region EM部分

                this.dgv_EM.AutoGenerateColumns = false;
                this.dgv_GMList_ForBind_EM.AutoGenerateColumns = false;
                this.dgv_TaskList_EM.AutoGenerateColumns = false;

                this.txt_ThreadCountMax_EM.Text = Program.setting.Setting_EM.ThreadCountMax.ToString();

                if (Program.setting.Setting_EM.Account_List != null &&
                    Program.setting.Setting_EM.Account_List.Count > 0)
                    this.dgv_EM.DataSource = Program.setting.Setting_EM.Account_List;
                if (Program.setting.Setting_EM.Mail_ForBind_List != null &&
                    Program.setting.Setting_EM.Mail_ForBind_List.Count > 0)
                    this.dgv_GMList_ForBind_EM.DataSource = Program.setting.Setting_EM.Mail_ForBind_List;

                this.cb_Global_WebProxyInfo_EM.Checked = Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_IsUse;
                this.txt_Global_WebProxyInfo_EM_IPAddress.Text =
                    Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Url;
                this.txt_Global_WebProxyInfo_EM_UserName.Text =
                    Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_UserName;
                this.txt_Global_WebProxyInfo_EM_Pwd.Text = Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Pwd;
                this.txt_ThreadCountMax_EM.Text = Program.setting.Setting_EM.ThreadCountMax.ToString();

                this.checkBox_protocol_EM.Checked = Program.setting.Setting_EM.Protocol;
                this.checkBox_selenium_EM.Checked = Program.setting.Setting_EM.Selenium;
                this.checkBox_ADSPower_EM.Checked = Program.setting.Setting_EM.ADSPower;
                this.checkBox_Bit_EM.Checked = Program.setting.Setting_EM.BitBrowser;
                this.checkBox_sms_activate_EM.Checked = Program.setting.Setting_EM.SmsActivate;
                this.checkBox_five_sim_EM.Checked = Program.setting.Setting_EM.FiveSim;
                this.checkBox_RecoveryEmail_EM.Checked = Program.setting.Setting_EM.RecoveryEmail;
                this.comboBox_country_EM.SelectedValue = Program.setting.Setting_EM.Country;

                this.textBox_token_sms_EM.Text = Program.setting.Setting_EM.TokenSms;
                this.textBox_token_sim_EM.Text = Program.setting.Setting_EM.TokenSis;
                this.label_sms_Balance_EM.Text = Program.setting.Setting_EM.SmsBalance;
                this.label_five_sim_Balance_EM.Text = Program.setting.Setting_EM.SisBalance;

                //密码相关
                if (Program.setting.Setting_EM.ForgotPwdSetting_Front_Mode == 0)
                    this.rb_ForgotPwdSetting_Front_Random_EM.Checked = true;
                else if (Program.setting.Setting_EM.ForgotPwdSetting_Front_Mode == 1)
                    this.rb_ForgotPwdSetting_Front_Custom_EM.Checked = true;
                else this.rb_ForgotPwdSetting_Front_Random_EM.Checked = true;
                this.txt_ForgotPwdSetting_Front_Custom_Content_EM.Text =
                    Program.setting.Setting_EM.ForgotPwdSetting_Front_Custom_Content.Trim();
                this.cb_ForgotPwdSetting_After_IsAddDate_EM.Checked =
                    Program.setting.Setting_EM.ForgotPwdSetting_After_IsAddDate;
                //流程控制相关
                this.TaskList_EM_Init();

                #endregion

                #region IN_RE部分

                this.dgv_IN_RE.AutoGenerateColumns = false;
                this.dgv_GMList_ForBind_IN_RE.AutoGenerateColumns = false;
                this.dgv_TaskList_IN_RE.AutoGenerateColumns = false;

                this.txt_ThreadCountMax_IN_RE.Text = Program.setting.Setting_IN_RE.ThreadCountMax.ToString();

                if (Program.setting.Setting_IN_RE.Account_List != null &&
                    Program.setting.Setting_IN_RE.Account_List.Count > 0)
                    this.dgv_IN_RE.DataSource = Program.setting.Setting_IN_RE.Account_List;
                if (Program.setting.Setting_IN_RE.Mail_ForBind_List != null &&
                    Program.setting.Setting_IN_RE.Mail_ForBind_List.Count > 0)
                    this.dgv_GMList_ForBind_IN_RE.DataSource = Program.setting.Setting_IN_RE.Mail_ForBind_List;
                this.radioButton_protocol_IN_RE.Checked = Program.setting.Setting_IN_RE.Protocol;
                this.radioButton_selenium_IN_RE.Checked = Program.setting.Setting_IN_RE.Selenium;
                this.radioButton_ADSPower_IN_RE.Checked = Program.setting.Setting_IN_RE.ADSPower;
                this.radioButton_bit_IN_RE.Checked = Program.setting.Setting_IN_RE.BitBrowser;
                this.checkBox_sms_activate_IN_RE.Checked = Program.setting.Setting_IN_RE.SmsActivate;
                this.checkBox_five_sim_IN_RE.Checked = Program.setting.Setting_IN_RE.FiveSim;
                this.comboBox_country_IN_RE.SelectedValue = Program.setting.Setting_IN_RE.Country;

                this.textBox_token_sms_EM.Text = Program.setting.Setting_EM.TokenSms;
                this.textBox_token_sim_EM.Text = Program.setting.Setting_EM.TokenSis;
                this.label_sms_Balance_EM.Text = Program.setting.Setting_EM.SmsBalance;
                this.label_five_sim_Balance_EM.Text = Program.setting.Setting_EM.SisBalance;
                //全局代理相关
                this.cb_Global_WebProxyInfo_IN_RE.Checked =
                    Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_IsUse;
                this.txt_Global_WebProxyInfo_IN_RE_IPAddress.Text =
                    Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Url;
                this.txt_Global_WebProxyInfo_IN_RE_UserName.Text =
                    Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_UserName;
                this.txt_Global_WebProxyInfo_IN_RE_Pwd.Text =
                    Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Pwd;

                //流程控制相关
                this.TaskList_IN_RE_Init();

                //忘记密码相关
                if (Program.setting.Setting_IN_RE.ForgotPwdSetting_Front_Mode == 0)
                    this.rb_ForgotPwdSetting_Front_Random_IN.Checked = true;
                else if (Program.setting.Setting_IN_RE.ForgotPwdSetting_Front_Mode == 1)
                    this.rb_ForgotPwdSetting_Front_Custom_IN_RE.Checked = true;
                else this.rb_ForgotPwdSetting_Front_Random_IN_RE.Checked = true;
                this.txt_ForgotPwdSetting_Front_Custom_Content_IN_RE.Text =
                    Program.setting.Setting_IN_RE.ForgotPwdSetting_Front_Custom_Content.Trim();
                this.cb_ForgotPwdSetting_After_IsAddDate_IN_RE.Checked =
                    Program.setting.Setting_IN_RE.ForgotPwdSetting_After_IsAddDate;

                #endregion

                #region ToolGetCk 部分

                this.dgv_DomainList.Rows.Clear();
                if (Program.setting.DomainInfos != null && Program.setting.DomainInfos.Count > 0)
                {
                    for (int i = 0; i < Program.setting.DomainInfos.Count; i++)
                    {
                        this.dgv_DomainList.Rows.Add(Program.setting.DomainInfos[i].DomainName
                            , Program.setting.DomainInfos[i].DomainListStr
                            , Program.setting.DomainInfos[i].CheckingMethod
                        );
                    }
                }

                this.txt_ThreadCount_ck_tool.Text = Program.setting.ThreadCountMax.ToString();
                this.checkBox_password_ck_tool.Checked = Program.setting.IsGetPassword;

                // this.dgv_DomainList.Rows.Clear();
                // if (Program.setting.Setting_CK.DomainInfos != null && Program.setting.Setting_CK.DomainInfos.Count > 0)
                // {
                //     for (int i = 0; i < Program.setting.Setting_CK.DomainInfos.Count; i++)
                //     {
                //         this.dgv_DomainList.Rows.Add(Program.setting.Setting_CK.DomainInfos[i].DomainName
                //             , Program.setting.Setting_CK.DomainInfos[i].DomainListStr
                //             , Program.setting.Setting_CK.DomainInfos[i].CheckingMethod
                //         );
                //     }
                // }
                //
                // this.txt_ThreadCount_ck_tool.Text = Program.setting.Setting_CK.ThreadCountMax.ToString();
                // this.checkBox_password_ck_tool.Checked = Program.setting.Setting_CK.IsGetPassword;

                #endregion
            }));
        }

        private void SaveSetting_FromUser()
        {
            int num;
            this.Invoke(new Action(() =>
            {
                #region FB部分

                if (int.TryParse(this.txt_ThreadCountMax_FB.Text.Trim(), out num))
                    Program.setting.Setting_FB.ThreadCountMax = num;
                Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_IsUse = this.cb_Global_WebProxyInfo_FB.Checked;
                Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_Url =
                    this.txt_Global_WebProxyInfo_FB_IPAddress.Text.Trim();
                Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_UserName =
                    this.txt_Global_WebProxyInfo_FB_UserName.Text.Trim();
                Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_Pwd =
                    this.txt_Global_WebProxyInfo_FB_Pwd.Text.Trim();

                //忘记密码相关
                if (this.rb_ForgotPwdSetting_Front_Random_FB.Checked)
                    Program.setting.Setting_FB.ForgotPwdSetting_Front_Mode = 0;
                else if (this.rb_ForgotPwdSetting_Front_Custom_FB.Checked)
                    Program.setting.Setting_FB.ForgotPwdSetting_Front_Mode = 1;
                else Program.setting.Setting_FB.ForgotPwdSetting_Front_Mode = 0;
                Program.setting.Setting_FB.ForgotPwdSetting_Front_Custom_Content =
                    this.txt_ForgotPwdSetting_Front_Custom_Content_FB.Text.Trim();
                Program.setting.Setting_FB.ForgotPwdSetting_After_IsAddDate =
                    this.cb_ForgotPwdSetting_After_IsAddDate_FB.Checked;

                //统计相关
                if (int.TryParse(this.txt_TongJi_WanChengShu_FB.Text.Trim(), out num))
                    Program.setting.Setting_FB.TongJi_False.WanChengShu = num;
                if (int.TryParse(this.txt_TongJi_FengHaoShu_FB.Text.Trim(), out num))
                    Program.setting.Setting_FB.TongJi_False.FengHaoShu = num;
                if (int.TryParse(this.txt_TongJi_WuXiao_FB.Text.Trim(), out num))
                    Program.setting.Setting_FB.TongJi_False.WuXiao = num;
                if (int.TryParse(this.txt_TongJi_YanZhengYouXiang_FB.Text.Trim(), out num))
                    Program.setting.Setting_FB.TongJi_False.YanZhengYouXiang = num;
                if (int.TryParse(this.txt_TongJi_YanZhengSheBei_FB.Text.Trim(), out num))
                    Program.setting.Setting_FB.TongJi_False.YanZhengSheBei = num;
                if (int.TryParse(this.txt_TongJi_QiTaCuoWu_FB.Text.Trim(), out num))
                    Program.setting.Setting_FB.TongJi_False.QiTaCuoWu = num;

                Program.setting.Setting_FB.ForgotPwdSetting_LogMeOut = this.cb_ForgotPwdSetting_LogMeOut_FB.Checked;

                #endregion

                #region IG部分

                if (int.TryParse(this.txt_ThreadCountMax_IG.Text.Trim(), out num))
                    Program.setting.Setting_IG.ThreadCountMax = num;
                Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_IsUse = this.cb_Global_WebProxyInfo_IG.Checked;
                Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_Url =
                    this.txt_Global_WebProxyInfo_IG_IPAddress.Text.Trim();
                Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_UserName =
                    this.txt_Global_WebProxyInfo_IG_UserName.Text.Trim();
                Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_Pwd =
                    this.txt_Global_WebProxyInfo_IG_Pwd.Text.Trim();

                //忘记密码相关
                if (this.rb_ForgotPwdSetting_Front_Random_IG.Checked)
                    Program.setting.Setting_IG.ForgotPwdSetting_Front_Mode = 0;
                else if (this.rb_ForgotPwdSetting_Front_Custom_IG.Checked)
                    Program.setting.Setting_IG.ForgotPwdSetting_Front_Mode = 1;
                else Program.setting.Setting_IG.ForgotPwdSetting_Front_Mode = 0;
                Program.setting.Setting_IG.ForgotPwdSetting_Front_Custom_Content =
                    this.txt_ForgotPwdSetting_Front_Custom_Content_IG.Text.Trim();
                Program.setting.Setting_IG.ForgotPwdSetting_After_IsAddDate =
                    this.cb_ForgotPwdSetting_After_IsAddDate_IG.Checked;

                //统计相关
                if (int.TryParse(this.txt_TongJi_WanChengShu_IG.Text.Trim(), out num))
                    Program.setting.Setting_IG.TongJi_False.WanChengShu = num;
                if (int.TryParse(this.txt_TongJi_FengHaoShu_IG.Text.Trim(), out num))
                    Program.setting.Setting_IG.TongJi_False.FengHaoShu = num;
                if (int.TryParse(this.txt_TongJi_WuXiao_IG.Text.Trim(), out num))
                    Program.setting.Setting_IG.TongJi_False.WuXiao = num;
                if (int.TryParse(this.txt_TongJi_YanZhengYouXiang_IG.Text.Trim(), out num))
                    Program.setting.Setting_IG.TongJi_False.YanZhengYouXiang = num;
                if (int.TryParse(this.txt_TongJi_YanZhengSheBei_IG.Text.Trim(), out num))
                    Program.setting.Setting_IG.TongJi_False.YanZhengSheBei = num;
                if (int.TryParse(this.txt_TongJi_QiTaCuoWu_IG.Text.Trim(), out num))
                    Program.setting.Setting_IG.TongJi_False.QiTaCuoWu = num;

                Program.setting.Setting_IG.ForgotPwdSetting_LogMeOut = this.cb_ForgotPwdSetting_LogMeOut_IG.Checked;

                #endregion

                #region IN部分

                if (int.TryParse(this.txt_ThreadCountMax_IN.Text.Trim(), out num))
                    Program.setting.Setting_IN.ThreadCountMax = num;
                Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_IsUse = this.cb_Global_WebProxyInfo_IN.Checked;
                Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Url =
                    this.txt_Global_WebProxyInfo_IN_IPAddress.Text.Trim();
                Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_UserName =
                    this.txt_Global_WebProxyInfo_IN_UserName.Text.Trim();
                Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Pwd =
                    this.txt_Global_WebProxyInfo_IN_Pwd.Text.Trim();
                Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Type_922 =
                    this.cb_Global_922_proxy_IN.Checked;
                Program.setting.Setting_IN.Protocol = this.checkBox_protocol_IN.Checked;
                Program.setting.Setting_IN.Selenium = this.checkBox_selenium_IN.Checked;
                Program.setting.Setting_IN.ADSPower = this.checkBox_ADSPower_IN.Checked;

                //忘记密码相关
                if (this.rb_ForgotPwdSetting_Front_Random_IN.Checked)
                    Program.setting.Setting_IN.ForgotPwdSetting_Front_Mode = 0;
                else if (this.rb_ForgotPwdSetting_Front_Custom_IN.Checked)
                    Program.setting.Setting_IN.ForgotPwdSetting_Front_Mode = 1;
                else Program.setting.Setting_IN.ForgotPwdSetting_Front_Mode = 0;
                Program.setting.Setting_IN.ForgotPwdSetting_Front_Custom_Content =
                    this.txt_ForgotPwdSetting_Front_Custom_Content_IN.Text.Trim();
                Program.setting.Setting_IN.ForgotPwdSetting_After_IsAddDate =
                    this.cb_ForgotPwdSetting_After_IsAddDate_IN.Checked;

                //统计相关
                if (int.TryParse(this.txt_TongJi_WanChengShu_IN.Text.Trim(), out num))
                    Program.setting.Setting_IN.TongJi_False.WanChengShu = num;
                if (int.TryParse(this.txt_TongJi_FengHaoShu_IN.Text.Trim(), out num))
                    Program.setting.Setting_IN.TongJi_False.FengHaoShu = num;
                if (int.TryParse(this.txt_TongJi_WuXiao_IN.Text.Trim(), out num))
                    Program.setting.Setting_IN.TongJi_False.WuXiao = num;
                if (int.TryParse(this.txt_TongJi_YanZhengYouXiang_IN.Text.Trim(), out num))
                    Program.setting.Setting_IN.TongJi_False.YanZhengYouXiang = num;
                if (int.TryParse(this.txt_TongJi_YanZhengSheBei_IN.Text.Trim(), out num))
                    Program.setting.Setting_IN.TongJi_False.YanZhengSheBei = num;
                if (int.TryParse(this.txt_TongJi_QiTaCuoWu_IN.Text.Trim(), out num))
                    Program.setting.Setting_IN.TongJi_False.QiTaCuoWu = num;

                Program.setting.Setting_IN.ForgotPwdSetting_LogMeOut = this.cb_ForgotPwdSetting_LogMeOut_IN.Checked;

                #endregion

                #region MS部分

                if (int.TryParse(this.txt_ThreadCountMax_EM.Text.Trim(), out num))
                    Program.setting.Setting_EM.ThreadCountMax = num;
                Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_IsUse = this.cb_Global_WebProxyInfo_EM.Checked;
                Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Url =
                    this.txt_Global_WebProxyInfo_EM_IPAddress.Text.Trim();
                Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_UserName =
                    this.txt_Global_WebProxyInfo_EM_UserName.Text.Trim();
                Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Pwd =
                    this.txt_Global_WebProxyInfo_EM_Pwd.Text.Trim();

                Program.setting.Setting_EM.Protocol = this.checkBox_protocol_EM.Checked;
                Program.setting.Setting_EM.Selenium = this.checkBox_selenium_EM.Checked;
                Program.setting.Setting_EM.ADSPower = this.checkBox_ADSPower_EM.Checked;
                Program.setting.Setting_EM.BitBrowser = this.checkBox_Bit_EM.Checked;
                Program.setting.Setting_EM.SmsActivate = this.checkBox_sms_activate_EM.Checked;
                Program.setting.Setting_EM.FiveSim = this.checkBox_five_sim_EM.Checked;
                Program.setting.Setting_EM.RecoveryEmail = this.checkBox_RecoveryEmail_EM.Checked;
                try
                {
                    Program.setting.Setting_EM.Country = this.comboBox_country_EM.SelectedValue.ToString();
                }
                catch (Exception e)
                {
                }

                Program.setting.Setting_EM.TokenSms = this.textBox_token_sms_EM.Text.Trim();
                Program.setting.Setting_EM.TokenSis = this.textBox_token_sim_EM.Text.Trim();
                Program.setting.Setting_EM.SmsBalance = this.label_sms_Balance_EM.Text.Trim();
                Program.setting.Setting_EM.SisBalance = this.label_five_sim_Balance_EM.Text.Trim();

                //忘记密码相关
                if (this.rb_ForgotPwdSetting_Front_Random_EM.Checked)
                    Program.setting.Setting_EM.ForgotPwdSetting_Front_Mode = 0;
                else if (this.rb_ForgotPwdSetting_Front_Custom_EM.Checked)
                    Program.setting.Setting_EM.ForgotPwdSetting_Front_Mode = 1;
                else Program.setting.Setting_EM.ForgotPwdSetting_Front_Mode = 0;
                Program.setting.Setting_EM.ForgotPwdSetting_Front_Custom_Content =
                    this.txt_ForgotPwdSetting_Front_Custom_Content_EM.Text.Trim();
                Program.setting.Setting_EM.ForgotPwdSetting_After_IsAddDate =
                    this.cb_ForgotPwdSetting_After_IsAddDate_EM.Checked;

                #endregion

                #region IN RE部分

                if (int.TryParse(this.txt_ThreadCountMax_IN_RE.Text.Trim(), out num))
                    Program.setting.Setting_IN_RE.ThreadCountMax = num;
                Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_IsUse =
                    this.cb_Global_WebProxyInfo_IN_RE.Checked;
                Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Url =
                    this.txt_Global_WebProxyInfo_IN_RE_IPAddress.Text.Trim();
                Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_UserName =
                    this.txt_Global_WebProxyInfo_IN_RE_UserName.Text.Trim();
                Program.setting.Setting_IN_RE.Global_WebProxyInfo.Proxy_Pwd =
                    this.txt_Global_WebProxyInfo_IN_RE_Pwd.Text.Trim();

                Program.setting.Setting_IN_RE.Protocol = this.radioButton_protocol_IN_RE.Checked;
                Program.setting.Setting_IN_RE.Selenium = this.radioButton_selenium_IN_RE.Checked;
                Program.setting.Setting_IN_RE.ADSPower = this.radioButton_ADSPower_IN_RE.Checked;
                Program.setting.Setting_IN_RE.BitBrowser = this.radioButton_bit_IN_RE.Checked;
                Program.setting.Setting_IN_RE.SmsActivate = this.checkBox_sms_activate_IN_RE.Checked;
                Program.setting.Setting_IN_RE.FiveSim = this.checkBox_five_sim_IN_RE.Checked;

                // Program.setting.Setting_IN_RE.Country = this.comboBox_country_IN_RE.SelectedValue.ToString();
                Program.setting.Setting_IN_RE.TokenSms = this.textBox_token_sms_IN_RE.Text.Trim();
                Program.setting.Setting_IN_RE.TokenSis = this.textBox_token_sim_IN_RE.Text.Trim();
                Program.setting.Setting_IN_RE.SmsBalance = this.label_sms_Balance_IN_RE.Text.Trim();
                Program.setting.Setting_IN_RE.SisBalance = this.label_five_sim_Balance_IN_RE.Text.Trim();
                //忘记密码相关
                if (this.rb_ForgotPwdSetting_Front_Random_IN_RE.Checked)
                    Program.setting.Setting_IN_RE.ForgotPwdSetting_Front_Mode = 0;
                else if (this.rb_ForgotPwdSetting_Front_Custom_IN_RE.Checked)
                    Program.setting.Setting_IN_RE.ForgotPwdSetting_Front_Mode = 1;
                else Program.setting.Setting_IN_RE.ForgotPwdSetting_Front_Mode = 0;
                Program.setting.Setting_IN_RE.ForgotPwdSetting_Front_Custom_Content =
                    this.txt_ForgotPwdSetting_Front_Custom_Content_IN_RE.Text.Trim();
                Program.setting.Setting_IN_RE.ForgotPwdSetting_After_IsAddDate =
                    this.cb_ForgotPwdSetting_After_IsAddDate_IN_RE.Checked;

                #endregion

                #region ToolGetCk部分

                //域名匹配信息
                Program.setting.DomainInfos = new List<DomainInfo>();
                string CheckingMethod;
                for (int i = 0; i < this.dgv_DomainList.RowCount; i++)
                {
                    DataGridViewRow row = this.dgv_DomainList.Rows[i];
                    if (row == null || row.Cells == null || row.Cells.Count < 2
                        || row.Cells["DomainName"] == null || row.Cells["DomainListStr"] == null
                        || row.Cells["DomainName"].Value == null || row.Cells["DomainListStr"].Value == null
                        || string.IsNullOrEmpty(row.Cells["DomainName"].Value.ToString()) ||
                        string.IsNullOrEmpty(row.Cells["DomainListStr"].Value.ToString())
                       ) continue;

                    CheckingMethod = string.Empty;
                    if (row.Cells["CheckingMethod"].Value != null &&
                        !string.IsNullOrEmpty(row.Cells["CheckingMethod"].Value.ToString()))
                        CheckingMethod = row.Cells["CheckingMethod"].Value.ToString().Trim();
                    if (string.IsNullOrEmpty(CheckingMethod)) CheckingMethod = "None";

                    ICheckingMethod iCheckingMethod = null;
                    if (CheckingMethod != "None") iCheckingMethod = this.CreateICheckingMethod(CheckingMethod);

                    Program.setting.DomainInfos.Add(new DomainInfo()
                    {
                        DomainName = row.Cells["DomainName"].Value.ToString(),
                        DomainListStr = row.Cells["DomainListStr"].Value.ToString(),
                        CheckingMethod = CheckingMethod,
                        ICheckingMethod = iCheckingMethod,
                    });
                }

                if (int.TryParse(this.txt_ThreadCount_ck_tool.Text.Trim(), out num))
                    Program.setting.ThreadCountMax = num;
                Program.setting.IsGetPassword =
                    this.checkBox_password_ck_tool.Checked;
                // //域名匹配信息
                // Program.setting.Setting_CK.DomainInfos = new List<DomainInfo>();
                // string CheckingMethod;
                // for (int i = 0; i < this.dgv_DomainList.RowCount; i++)
                // {
                //     DataGridViewRow row = this.dgv_DomainList.Rows[i];
                //     if (row == null || row.Cells == null || row.Cells.Count < 2
                //         || row.Cells["DomainName"] == null || row.Cells["DomainListStr"] == null
                //         || row.Cells["DomainName"].Value == null || row.Cells["DomainListStr"].Value == null
                //         || string.IsNullOrEmpty(row.Cells["DomainName"].Value.ToString()) ||
                //         string.IsNullOrEmpty(row.Cells["DomainListStr"].Value.ToString())
                //        ) continue;
                //
                //     CheckingMethod = string.Empty;
                //     if (row.Cells["CheckingMethod"].Value != null &&
                //         !string.IsNullOrEmpty(row.Cells["CheckingMethod"].Value.ToString()))
                //         CheckingMethod = row.Cells["CheckingMethod"].Value.ToString().Trim();
                //     if (string.IsNullOrEmpty(CheckingMethod)) CheckingMethod = "None";
                //
                //     ICheckingMethod iCheckingMethod = null;
                //     if (CheckingMethod != "None") iCheckingMethod = this.CreateICheckingMethod(CheckingMethod);
                //
                //     Program.setting.Setting_CK.DomainInfos.Add(new DomainInfo()
                //     {
                //         DomainName = row.Cells["DomainName"].Value.ToString(),
                //         DomainListStr = row.Cells["DomainListStr"].Value.ToString(),
                //         CheckingMethod = CheckingMethod,
                //         ICheckingMethod = iCheckingMethod,
                //     });
                // }
                //
                // if (int.TryParse(this.txt_ThreadCount_ck_tool.Text.Trim(), out num))
                //     Program.setting.Setting_CK.ThreadCountMax = num;
                // Program.setting.Setting_CK.IsGetPassword =
                //     this.checkBox_password_ck_tool.Checked;

                #endregion
            }));
        }

        private void SaveSetting_ToDisk()
        {
            string dir = $@"{Application.StartupPath}\UserInfo";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string fileName = $@"{dir}\Setting.db";
            string fileContent = JsonConvert.SerializeObject(Program.setting);
            SerializeObjectToString.WriteTxt(fileName, fileContent);
        }

        #endregion

        #region 辅助方法

        //切换标签时刷新表格
        private void tc_Main_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabPage tab = this.tc_Main.TabPages[this.tc_Main.SelectedIndex];
            List<Control> dgvs = tab.Controls.Cast<Control>().Where(c => c is DataGridView).ToList();
            foreach (Control dgv in dgvs)
            {
                dgv.Refresh();
            }
        }

        //获取选中的索引列表
        private List<int> GetSelectedIndexList(DataGridView dgv)
        {
            List<int> iList = new List<int>();
            if (dgv.SelectedCells == null || dgv.SelectedCells.Count == 0) return iList;
            foreach (DataGridViewCell cell in dgv.SelectedCells)
            {
                if (!iList.Contains(cell.RowIndex)) iList.Add(cell.RowIndex);
            }

            iList = iList.OrderBy(i => i).ToList();

            return iList;
        }

        private void SetProperty(object obj, string propertyName, object value)
        {
            // 获取属性信息
            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName);

            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                // 设置属性值
                propertyInfo.SetValue(obj, Convert.ChangeType(value, propertyInfo.PropertyType), null);
            }
        }

        #endregion

        #region 刷新UI线程

        private Thread thread_UpdateDGV = null;

        private void ThreadMethod_UpdateDGV()
        {
            int timeSpan = 1000;
            int timeCount = 0;
            while (timeSpan > 0)
            {
                Thread.Sleep(timeSpan);
                Application.DoEvents();
                timeCount += timeSpan;

                //更新账号列表
                this.Invoke(new Action(() =>
                {
                    if (this.tc_Main.TabPages[this.tc_Main.SelectedIndex].Text == "FB | 操作")
                        this.dgv_FB.Refresh();
                    else if (this.tc_Main.TabPages[this.tc_Main.SelectedIndex].Text == "IG | 操作")
                        this.dgv_IG.Refresh();
                    else if (this.tc_Main.TabPages[this.tc_Main.SelectedIndex].Text == "IN | 操作")
                        this.dgv_IN.Refresh();
                    else if (this.tc_Main.TabPages[this.tc_Main.SelectedIndex].Text == "EM | 操作")
                        this.dgv_EM.Refresh();
                    else if (this.tc_Main.TabPages[this.tc_Main.SelectedIndex].Text == "IN_RE | 操作")
                        this.dgv_IN_RE.Refresh();
                    else if (this.tc_Main.TabPages[this.tc_Main.SelectedIndex].Text == "GETCK | 工具")
                        this.dgv_Main.Refresh();
                }));

                Application.DoEvents();
            }
        }

        #endregion

        /*以下代码为FB部分*/

        #region IN数据统计

        private void ShowTongJiInfo_IN(int useType = -1)
        {
            if (useType > -1) Program.setting.Setting_IN.TongJi_UseType = useType;

            TongJi_FBOrIns tongJi = Program.setting.Setting_IN.TongJi_UseType == 0
                ? Program.setting.Setting_IN.TongJi_Real
                : Program.setting.Setting_IN.TongJi_False;

            this.Invoke(new Action(() =>
            {
                this.lbl_TongJi_WanChengShu_IN.Text = tongJi.WanChengShu.ToString("D5");
                this.lbl_TongJi_FengHaoShu_IN.Text = tongJi.FengHaoShu.ToString("D5");
                this.lbl_TongJi_WuXiao_IN.Text = tongJi.WuXiao.ToString("D5");
                this.lbl_TongJi_YanZhengYouXiang_IN.Text = tongJi.YanZhengYouXiang.ToString("D5");
                this.lbl_TongJi_YanZhengSheBei_IN.Text = tongJi.YanZhengSheBei.ToString("D5");
                this.lbl_TongJi_QiTaCuoWu_IN.Text = tongJi.QiTaCuoWu.ToString("D5");
            }));
        }

        //显示新数据到设置页
        private void ShowTongJiInfo_False_IN(int useType = -1)
        {
            if (useType < 0) useType = 0;
            TongJi_FBOrIns tongJi = useType == 0
                ? Program.setting.Setting_IN.TongJi_Real
                : Program.setting.Setting_IN.TongJi_False;
            this.Invoke(new Action(() =>
            {
                this.txt_TongJi_WanChengShu_IN.Text = tongJi.WanChengShu.ToString();
                this.txt_TongJi_FengHaoShu_IN.Text = tongJi.FengHaoShu.ToString();
                this.txt_TongJi_WuXiao_IN.Text = tongJi.WuXiao.ToString();
                this.txt_TongJi_YanZhengYouXiang_IN.Text = tongJi.YanZhengYouXiang.ToString();
                this.txt_TongJi_YanZhengSheBei_IN.Text = tongJi.YanZhengSheBei.ToString();
                this.txt_TongJi_QiTaCuoWu_IN.Text = tongJi.QiTaCuoWu.ToString();
            }));
        }

        #endregion

        #region IN执行流程控制

        private LinkedinService linkedinService = new LinkedinService();
        private EmailRegisterService emailRegisterService = new EmailRegisterService();
        private Thread thread_Main_IN_RE = null;
        private Thread thread_Main_IN = null;
        private SmartThreadPool stp_IN_RE = null;
        private SmartThreadPool stp_IN = null;

        private void btn_Start_IN_Click(object sender, EventArgs e)
        {
            this.thread_Main_IN = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_IN_Main));
            this.thread_Main_IN.IsBackground = true;
            this.thread_Main_IN.Start(0);
        }

        //初始化
        private void TaskList_IN_Init()
        {
            List<TaskInfo> tasks = new List<TaskInfo>();
            tasks.Add(new TaskInfo("LoginByCookie", "1 : 检测Email_CK", true));
            tasks.Add(new TaskInfo("ForgotPassword", "2 : 忘记密码", true));
            tasks.Add(new TaskInfo("BindNewEmail", "3 : 添加邮箱", true));
            tasks.Add(new TaskInfo("ChangeEmail", "4 : 切换邮箱", true));
            tasks.Add(new TaskInfo("GetInfo", "5 : 获取信息", true));
            tasks.Add(new TaskInfo("VerifyPassword", "6 : CK验证密码", true));
            tasks.Add(new TaskInfo("ChangePassword", "7 : CK修改密码", true));

            if (Program.setting.Setting_IN.TaskInfoList == null)
                Program.setting.Setting_IN.TaskInfoList = new List<TaskInfo>();

            for (int i = 0; i < tasks.Count; i++)
            {
                TaskInfo tFind = Program.setting.Setting_IN.TaskInfoList.Where(t => t.TaskName == tasks[i].TaskName)
                    .FirstOrDefault();
                if (tFind != null) tasks[i].IsSelected = tFind.IsSelected;
            }

            Program.setting.Setting_IN.TaskInfoList = tasks;

            this.Invoke(new Action(() =>
            {
                this.dgv_TaskList_IN.DataSource = null;
                if (Program.setting.Setting_IN.TaskInfoList != null &&
                    Program.setting.Setting_IN.TaskInfoList.Count > 0)
                    this.dgv_TaskList_IN.DataSource = Program.setting.Setting_IN.TaskInfoList;
            }));
        }

        //右键弹出菜单
        private void dgv_TaskList_IN_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.Button == MouseButtons.Right)
            {
                DataGridView dgv = this.dgv_TaskList_IN;
                //弹出操作菜单
                this.cms_dgv_TaskList_IN.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //编辑即时响应
        private void dgv_TaskList_IN_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_IN;
            if (dgv.IsCurrentCellDirty) dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        //初始化
        private void TaskList_IN_RE_Init()
        {
            List<TaskInfo> tasks = new List<TaskInfo>();
            tasks.Add(new TaskInfo("LoginByCookie", "1 : 注册账号", true));
            tasks.Add(new TaskInfo("ForgotPassword", "2 : 是否打码", true));
            tasks.Add(new TaskInfo("BindNewEmail", "3 : 是否使用SMS", true));

            if (Program.setting.Setting_IN_RE.TaskInfoList == null)
                Program.setting.Setting_IN_RE.TaskInfoList = new List<TaskInfo>();

            for (int i = 0; i < tasks.Count; i++)
            {
                TaskInfo tFind = Program.setting.Setting_IN_RE.TaskInfoList.Where(t => t.TaskName == tasks[i].TaskName)
                    .FirstOrDefault();
                if (tFind != null) tasks[i].IsSelected = tFind.IsSelected;
            }

            Program.setting.Setting_IN_RE.TaskInfoList = tasks;

            this.Invoke(new Action(() =>
            {
                this.dgv_TaskList_IN_RE.DataSource = null;
                if (Program.setting.Setting_IN_RE.TaskInfoList != null &&
                    Program.setting.Setting_IN_RE.TaskInfoList.Count > 0)
                    this.dgv_TaskList_IN_RE.DataSource = Program.setting.Setting_IN_RE.TaskInfoList;
            }));
        }

        //清空账号列表
        private void btn_ClearData_IN_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空列表吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;
            this.Invoke(new Action(() => { this.dgv_IN.DataSource = null; }));
            Program.setting.Setting_IN.Account_List = null;
        }

        //清空账号列表
        private void btn_ClearData_IN_RE_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空列表吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;
            this.Invoke(new Action(() => { this.dgv_IN_RE.DataSource = null; }));
            Program.setting.Setting_IN_RE.Account_List = null;
        }

        //获取账号模板
        private void btn_GetExcelModelFile_IN_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            // 设置保存对话框的标题
            saveFileDialog.Title = "保存导入账号模版";

            // 设置默认的文件名
            saveFileDialog.FileName = "导入账号模版_IN.xlsx";

            // 设置默认的文件类型筛选
            saveFileDialog.Filter = "Excel文档 (*.xlsx)|*.xlsx";

            // 设置默认的文件类型索引
            saveFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            saveFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 将文本保存到文件
                    File.WriteAllBytes(saveFileDialog.FileName, Properties.Resources.导入账号模版_IN);

                    StringHelper.OpenFolderAndSelectFiles(new FileInfo(saveFileDialog.FileName).Directory.FullName,
                        saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //获取账号模板
        private void btn_GetExcelModelFile_IN_RE_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            // 设置保存对话框的标题
            saveFileDialog.Title = "保存导入账号模版";

            // 设置默认的文件名
            saveFileDialog.FileName = "导入账号模版_IN_RE.xlsx";

            // 设置默认的文件类型筛选
            saveFileDialog.Filter = "Excel文档 (*.xlsx)|*.xlsx";

            // 设置默认的文件类型索引
            saveFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            saveFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 将文本保存到文件
                    File.WriteAllBytes(saveFileDialog.FileName, Properties.Resources.导入账号模版_IN);

                    StringHelper.OpenFolderAndSelectFiles(new FileInfo(saveFileDialog.FileName).Directory.FullName,
                        saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //导入账号
        private void btn_ImportAccount_IN_RE_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开Excel文档";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "Excel文档 (*.xls;*.xlsx)|*.xls;*.xlsx";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 获取用户选择的文件名
                    string filePath = openFileDialog.FileName;
                    this.ImportAccount_IN_RE(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //拖动导入事件
        private void dgv_IN_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }


        //拖动导入事件
        private void dgv_IN_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            List<string> fList = files.Where(s => s.ToLower().EndsWith(".xls") || s.ToLower().EndsWith(".xlsx"))
                .ToList();

            if (fList.Count == 0)
            {
                MessageBox.Show($"导入账号时发生错误：只能导入Excel文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (fList.Count > 1)
            {
                MessageBox.Show($"导入账号时发生错误：每次只能导入1个Excel文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ImportAccount_IN(fList[0]);
        }

        private void btn_ImportAccount_ForBind_IN_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开TXT文档";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "TXT文档 (*.txt)|*.txt";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 获取用户选择的文件名
                    string filePath = openFileDialog.FileName;
                    this.ImportAccount_ForBind_IN(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btn_ImportAccount_ForBind_IN_RE_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开TXT文档";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "TXT文档 (*.txt)|*.txt";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 获取用户选择的文件名
                    string filePath = openFileDialog.FileName;
                    this.ImportAccount_ForBind_IN_RE(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btn_ClearData_ForBind_IN_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空列表吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;

            this.Invoke(new Action(() => { this.dgv_GMList_ForBind_IN.DataSource = null; }));
            Program.setting.Setting_IN.Mail_ForBind_List = null;
        }

        private void btn_ClearData_ForBind_IN_RE_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空列表吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;

            this.Invoke(new Action(() => { this.dgv_GMList_ForBind_IN_RE.DataSource = null; }));
            Program.setting.Setting_IN_RE.Mail_ForBind_List = null;
        }

        //导出TXT文档_具体方法
        private void ExportTXT_GM_IN()
        {
            if (Program.setting.Setting_IN.Mail_ForBind_List == null ||
                Program.setting.Setting_IN.Mail_ForBind_List.Count == 0)
            {
                MessageBox.Show("No Data!");
                return;
            }

            DateTime now = DateTime.Now;
            string sheetName = $"IN_MailList_{now.ToString("yyyyMMdd_HHmmss")}";

            FileInfo fileInfo = null;
            this.Invoke(new Action(() =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "TXT文档（*.txt）|*.txt";
                sfd.FilterIndex = 1;
                sfd.InitialDirectory = Application.StartupPath;
                sfd.RestoreDirectory = true;
                sfd.FileName = $"{sheetName}.txt";
                if (sfd.ShowDialog() != DialogResult.OK) return;
                fileInfo = new FileInfo(sfd.FileName);
            }));
            if (fileInfo == null) return;

            //禁用按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_GM_IN.Enabled = false;
                Application.DoEvents();
            }));

            List<MailInfo> mails_NotUsed = Program.setting.Setting_IN.Mail_ForBind_List.Where(m => !m.Is_Used).ToList();
            List<MailInfo> mails_Used = Program.setting.Setting_IN.Mail_ForBind_List.Where(m => m.Is_Used).ToList();
            List<MailInfo> mails_All = mails_Used.Union(mails_NotUsed).ToList();

            string fileContent = string.Join("\r\n",
                mails_All.Select(m =>
                    $"{m.Mail_Name}----{m.Mail_Pwd}----{m.VerifyMail_Name}{(m.Is_Used ? $"----{m.Is_Used_Des}" : string.Empty)}"));

            //写出文档
            File.WriteAllText(fileInfo.FullName, fileContent);

            //显示Excel文档
            this.Invoke(new Action(() =>
            {
                StringHelper.OpenFolderAndSelectFiles(fileInfo.Directory.FullName,
                    new string[] { fileInfo.FullName });
            }));

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_GM_IN.Enabled = true;
                Application.DoEvents();
            }));
        }

        //导出TXT文档_具体方法
        private void ExportTXT_GM_IN_RE()
        {
            if (Program.setting.Setting_IN_RE.Mail_ForBind_List == null ||
                Program.setting.Setting_IN_RE.Mail_ForBind_List.Count == 0)
            {
                MessageBox.Show("No Data!");
                return;
            }

            DateTime now = DateTime.Now;
            string sheetName = $"IN_RE_MailList_{now.ToString("yyyyMMdd_HHmmss")}";

            FileInfo fileInfo = null;
            this.Invoke(new Action(() =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "TXT文档（*.txt）|*.txt";
                sfd.FilterIndex = 1;
                sfd.InitialDirectory = Application.StartupPath;
                sfd.RestoreDirectory = true;
                sfd.FileName = $"{sheetName}.txt";
                if (sfd.ShowDialog() != DialogResult.OK) return;
                fileInfo = new FileInfo(sfd.FileName);
            }));
            if (fileInfo == null) return;

            //禁用按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_GM_IN_RE.Enabled = false;
                Application.DoEvents();
            }));

            List<MailInfo> mails_NotUsed = Program.setting.Setting_IN.Mail_ForBind_List.Where(m => !m.Is_Used).ToList();
            List<MailInfo> mails_Used = Program.setting.Setting_IN.Mail_ForBind_List.Where(m => m.Is_Used).ToList();
            List<MailInfo> mails_All = mails_Used.Union(mails_NotUsed).ToList();

            string fileContent = string.Join("\r\n",
                mails_All.Select(m =>
                    $"{m.Mail_Name}----{m.Mail_Pwd}----{m.VerifyMail_Name}{(m.Is_Used ? $"----{m.Is_Used_Des}" : string.Empty)}"));

            //写出文档
            File.WriteAllText(fileInfo.FullName, fileContent);

            //显示Excel文档
            this.Invoke(new Action(() =>
            {
                StringHelper.OpenFolderAndSelectFiles(fileInfo.Directory.FullName,
                    new string[] { fileInfo.FullName });
            }));

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_GM_IN_RE.Enabled = true;
                Application.DoEvents();
            }));
        }

        private void btn_Export_List_GM_IN_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(this.ExportTXT_GM_IN);
        }

        private void btn_Export_List_GM_IN_RE_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(this.ExportTXT_GM_IN_RE);
        }

        //标记未使用
        private void tsmi_SetNotUsed_GM_IN_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IN;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<MailInfo> mails_Selected = iList.Select(i => tasks[i]).ToList();
            if (mails_Selected.Count == 0) return;

            for (int i = 0; i < mails_Selected.Count; i++)
            {
                mails_Selected[i].Is_Used = false;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //标记未使用
        private void tsmi_SetNotUsed_GM_IN_RE_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IN_RE;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<MailInfo> mails_Selected = iList.Select(i => tasks[i]).ToList();
            if (mails_Selected.Count == 0) return;

            for (int i = 0; i < mails_Selected.Count; i++)
            {
                mails_Selected[i].Is_Used = false;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //标记已使用
        private void tsmi_SetUsed_GM_IN_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IN;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<MailInfo> mails_Selected = iList.Select(i => tasks[i]).ToList();
            if (mails_Selected.Count == 0) return;

            for (int i = 0; i < mails_Selected.Count; i++)
            {
                mails_Selected[i].Is_Used = true;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //标记已使用
        private void tsmi_SetUsed_GM_IN_RE_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IN_RE;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<MailInfo> mails_Selected = iList.Select(i => tasks[i]).ToList();
            if (mails_Selected.Count == 0) return;

            for (int i = 0; i < mails_Selected.Count; i++)
            {
                mails_Selected[i].Is_Used = true;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //删除
        private void tsmi_Delete_One_GM_IN_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IN;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_IN.Mail_ForBind_List.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_IN.Mail_ForBind_List != null ||
                Program.setting.Setting_IN.Mail_ForBind_List.Count > 0)
                this.Invoke(new Action(() => { dgv.DataSource = Program.setting.Setting_IN.Mail_ForBind_List; }));
        }
        //删除

        private void tsmi_Delete_One_GM_IN_RE_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IN_RE;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_IN_RE.Mail_ForBind_List.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_IN_RE.Mail_ForBind_List != null ||
                Program.setting.Setting_IN_RE.Mail_ForBind_List.Count > 0)
                this.Invoke(new Action(() => { dgv.DataSource = Program.setting.Setting_IN_RE.Mail_ForBind_List; }));
        }


        //删除_全部
        private void tsmi_Delete_All_GM_IN_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IN;
            if (Program.setting.Setting_IN.Mail_ForBind_List == null ||
                Program.setting.Setting_IN.Mail_ForBind_List.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            Program.setting.Setting_IN.Mail_ForBind_List.Clear();
        }

        //删除_全部
        private void tsmi_Delete_All_GM_IN_RE_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IN;
            if (Program.setting.Setting_IN_RE.Mail_ForBind_List == null ||
                Program.setting.Setting_IN_RE.Mail_ForBind_List.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            Program.setting.Setting_IN_RE.Mail_ForBind_List.Clear();
        }

        //DGV画行号
        private void dgv_IN_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == -1)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                using (Brush brush = new SolidBrush(e.CellStyle.ForeColor))
                {
                    e.Graphics.DrawString((e.RowIndex + 1).ToString(), e.CellStyle.Font, brush,
                        e.CellBounds.Location.X + 10, e.CellBounds.Location.Y + 4);
                }

                e.Handled = true;
            }
        }


        //弹出右键菜单
        private void dgv_IN_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;

            if (e.Button == MouseButtons.Right)
            {
                //this.dgv_FB.CurrentCell = this.dgv_FB.Rows[e.RowIndex].Cells[e.ColumnIndex];
                DataGridViewCell cell = this.dgv_IN.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell != null && !cell.Selected) cell.Selected = true;

                this.cms_dgv_IN.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //编辑即时响应
        private void dgv_IN_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (this.dgv_IN.IsCurrentCellDirty) this.dgv_IN.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        //开始操作
        private void tsmi_Start_One_IN_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_IN_Main));
            thread.IsBackground = true;
            thread.Start(1);
        }

        //开始操作
        private void tsmi_Start_One_IN_RE_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_IN_RE_Main));
            thread.IsBackground = true;
            thread.Start(1);
        }

        //开始操作_全部
        private void tsmi_Start_All_IN_Click(object sender, EventArgs e)
        {
            this.thread_Main_IN = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_IN_Main));
            this.thread_Main_IN.IsBackground = true;
            this.thread_Main_IN.Start(0);
        }

        //开始操作_全部
        private void tsmi_Start_All_IN_RE_Click(object sender, EventArgs e)
        {
            this.thread_Main_IN_RE = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_IN_RE_Main));
            this.thread_Main_IN_RE.IsBackground = true;
            this.thread_Main_IN_RE.Start(0);
        }

        //停止操作
        private void tsmi_Stop_One_IN_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_IN(1);
        }
        //停止操作

        private void tsmi_Stop_One_IN_RE_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_IN_RE(1);
        }

        //停止操作_全部
        private void tsmi_Stop_All_IN_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_IN(0);
        }
        //停止操作_全部

        private void tsmi_Stop_All_IN_RE_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_IN_RE(0);
        }

        //删除
        private void tsmi_Delete_One_IN_Click(object sender, EventArgs e)
        {
            if (Program.setting.Setting_IN.Account_List == null ||
                Program.setting.Setting_IN.Account_List.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(this.dgv_IN);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_IN.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_IN.Account_List.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_IN.Account_List != null || Program.setting.Setting_IN.Account_List.Count > 0)
                this.Invoke(new Action(() => { this.dgv_IN.DataSource = Program.setting.Setting_IN.Account_List; }));
        }

        //删除
        private void tsmi_Delete_One_IN_RE_Click(object sender, EventArgs e)
        {
            if (Program.setting.Setting_IN_RE.Account_List == null ||
                Program.setting.Setting_IN_RE.Account_List.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(this.dgv_IN_RE);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_IN_RE.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_IN_RE.Account_List.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_IN_RE.Account_List != null ||
                Program.setting.Setting_IN_RE.Account_List.Count > 0)
                this.Invoke(
                    new Action(() => { this.dgv_IN_RE.DataSource = Program.setting.Setting_IN_RE.Account_List; }));
        }

        //删除_全部
        private void tsmi_Delete_All_IN_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要删除全部吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;

            if (Program.setting.Setting_IN.Account_List == null ||
                Program.setting.Setting_IN.Account_List.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_IN.DataSource = null; }));

            Program.setting.Setting_IN.Account_List.Clear();
        }
        //删除_全部

        private void tsmi_Delete_All_IN_RE_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要删除全部吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;

            if (Program.setting.Setting_IN_RE.Account_List == null ||
                Program.setting.Setting_IN_RE.Account_List.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_IN_RE.DataSource = null; }));

            Program.setting.Setting_IN_RE.Account_List.Clear();
        }

        //导出Excel_具体方法
        private void ExportExcel_IN()
        {
            if (Program.setting.Setting_IN.Account_List == null || Program.setting.Setting_IN.Account_List.Count == 0)
            {
                MessageBox.Show("No Data!");
                return;
            }

            DateTime now = DateTime.Now;
            string sheetName = $"IN_{now.ToString("yyyyMMdd_HHmmss")}";
            Regex regex = new Regex(@"^\d+$");

            FileInfo fileInfo = null;
            this.Invoke(new Action(() =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Excel文档（*.xlsx）|*.xlsx";
                sfd.FilterIndex = 1;
                sfd.InitialDirectory = Application.StartupPath;
                sfd.RestoreDirectory = true;
                sfd.FileName = $"{sheetName}.xlsx";
                if (sfd.ShowDialog() != DialogResult.OK) return;
                fileInfo = new FileInfo(sfd.FileName);
            }));
            if (fileInfo == null) return;

            //禁用按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_IN.Enabled = false;
                Application.DoEvents();
            }));

            //整理表头信息
            List<ExcelColumnInfo> excelCols = new List<ExcelColumnInfo>()
            {
                new ExcelColumnInfo("操作日志", "Running_Log"),
                new ExcelColumnInfo("账号分类", "Account_Type_Des"),
                new ExcelColumnInfo("原邮箱CK", "Old_Mail_CK"),
                new ExcelColumnInfo("原邮箱账号", "Old_Mail_Name"),
                new ExcelColumnInfo("原邮箱密码", "Old_Mail_Pwd"),
                new ExcelColumnInfo("新邮箱账号", "New_Mail_Name"),
                new ExcelColumnInfo("新邮箱密码", "New_Mail_Pwd"),
                new ExcelColumnInfo("IN_密码", "Facebook_Pwd"),
                new ExcelColumnInfo("COOKIE", "Facebook_CK"),
                new ExcelColumnInfo("UA", "UserAgent"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("手机号码", "Phone_Num"),
                new ExcelColumnInfo("接码地址", "Phone_Num_Url"),
                new ExcelColumnInfo("国家", "GuoJia"),
                new ExcelColumnInfo("注册日期", "ZhuCeRiQi"),
                new ExcelColumnInfo("账户链接", "AccountName"),
                new ExcelColumnInfo("好友", "HaoYouCount"),
                new ExcelColumnInfo("认证", "Certification"),
                new ExcelColumnInfo("加好友参数", "FsdProfile"),
            };
            for (int i = 0; i < excelCols.Count; i++)
            {
                excelCols[i].HeaderIndex = i;
            }

            Type type = typeof(Account_FBOrIns);
            string headers = string.Join("\t", excelCols.Select(c => c.HeaderName));
            string content = string.Join("\r\n", Program.setting.Setting_IN.Account_List.Select(c =>
            {
                string lineStr = string.Join("\t", excelCols.Select(ec =>
                {
                    string cellValue;
                    PropertyInfo propInfo = type.GetProperty(ec.PropertyName);
                    cellValue = propInfo == null || propInfo.GetValue(c, null) == null
                        ? string.Empty
                        : propInfo.GetValue(c, null).ToString();
                    if (regex.IsMatch(cellValue) && cellValue.Length > 10) cellValue = $"'{cellValue}";
                    return cellValue;
                }));
                return lineStr;
            }));

            //创建一个新的Excel文件
            using (var package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(sheetName);

                //调整为文本格式
                for (int i = 0; i < excelCols.Count; i++)
                {
                    worksheet.Columns[i + 1].Style.Numberformat.Format = "@";
                }

                ExcelTextFormat excelTextFormat = new ExcelTextFormat();
                excelTextFormat.Delimiter = '\t';
                //表头处理
                worksheet.Cells["A1"].LoadFromText(headers, excelTextFormat);

                //在A2单元格粘贴内容
                worksheet.Cells["A2"].LoadFromText(content, excelTextFormat);

                //首行加粗
                worksheet.Rows[1].Style.Font.Bold = true;

                //单元格内容重新赋值
                foreach (var cell in worksheet.Cells[2, 1, Program.setting.Setting_IN.Account_List.Count + 1,
                             excelCols.Count])
                {
                    if (cell.Value != null) cell.Value = cell.Value.ToString().Replace("'", "");
                }

                //自动列宽，居中[CK那一列不居中]
                for (int i = 0; i < excelCols.Count; i++)
                {
                    if (worksheet.Cells[1, i + 1].Text == "COOKIE")
                    {
                        worksheet.Cells[1, i + 1].Style.HorizontalAlignment =
                            OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Columns[i + 1].Width = 20;
                        continue;
                    }

                    worksheet.Columns[i + 1].AutoFit();
                    worksheet.Columns[i + 1].Style.HorizontalAlignment =
                        OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                // 保存Excel文件
                package.SaveAs(fileInfo);
            }

            //显示Excel文档
            this.Invoke(new Action(() =>
            {
                StringHelper.OpenFolderAndSelectFiles(fileInfo.Directory.FullName,
                    new string[] { fileInfo.FullName });
            }));

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_IN.Enabled = true;
                Application.DoEvents();
            }));
        }

        //导出Excel_具体方法
        private void ExportExcel_IN_RE()
        {
            if (Program.setting.Setting_IN_RE.Account_List == null ||
                Program.setting.Setting_IN_RE.Account_List.Count == 0)
            {
                MessageBox.Show("No Data!");
                return;
            }

            DateTime now = DateTime.Now;
            string sheetName = $"IN_RE_{now.ToString("yyyyMMdd_HHmmss")}";
            Regex regex = new Regex(@"^\d+$");

            FileInfo fileInfo = null;
            this.Invoke(new Action(() =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Excel文档（*.xlsx）|*.xlsx";
                sfd.FilterIndex = 1;
                sfd.InitialDirectory = Application.StartupPath;
                sfd.RestoreDirectory = true;
                sfd.FileName = $"{sheetName}.xlsx";
                if (sfd.ShowDialog() != DialogResult.OK) return;
                fileInfo = new FileInfo(sfd.FileName);
            }));
            if (fileInfo == null) return;

            //禁用按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_IN_RE.Enabled = false;
                Application.DoEvents();
            }));

            //整理表头信息
            List<ExcelColumnInfo> excelCols = new List<ExcelColumnInfo>()
            {
                new ExcelColumnInfo("操作日志", "Running_Log"),
                new ExcelColumnInfo("账号分类", "Account_Type_Des"),
                new ExcelColumnInfo("原邮箱CK", "Old_Mail_CK"),
                new ExcelColumnInfo("原邮箱账号", "Old_Mail_Name"),
                new ExcelColumnInfo("原邮箱密码", "Old_Mail_Pwd"),
                new ExcelColumnInfo("新邮箱账号", "New_Mail_Name"),
                new ExcelColumnInfo("新邮箱密码", "New_Mail_Pwd"),
                new ExcelColumnInfo("IN_密码", "Facebook_Pwd"),
                new ExcelColumnInfo("COOKIE", "Facebook_CK"),
                new ExcelColumnInfo("UA", "UserAgent"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("手机号码", "Phone_Num"),
                new ExcelColumnInfo("接码地址", "Phone_Num_Url"),
                new ExcelColumnInfo("国家", "GuoJia"),
                new ExcelColumnInfo("注册日期", "ZhuCeRiQi"),
                new ExcelColumnInfo("账户链接", "AccountName"),
                new ExcelColumnInfo("好友", "HaoYouCount"),
                new ExcelColumnInfo("认证", "Certification"),
                new ExcelColumnInfo("加好友参数", "FsdProfile"),
            };
            for (int i = 0; i < excelCols.Count; i++)
            {
                excelCols[i].HeaderIndex = i;
            }

            Type type = typeof(Account_FBOrIns);
            string headers = string.Join("\t", excelCols.Select(c => c.HeaderName));
            string content = string.Join("\r\n", Program.setting.Setting_IN_RE.Account_List.Select(c =>
            {
                string lineStr = string.Join("\t", excelCols.Select(ec =>
                {
                    string cellValue;
                    PropertyInfo propInfo = type.GetProperty(ec.PropertyName);
                    cellValue = propInfo == null || propInfo.GetValue(c, null) == null
                        ? string.Empty
                        : propInfo.GetValue(c, null).ToString();
                    if (regex.IsMatch(cellValue) && cellValue.Length > 10) cellValue = $"'{cellValue}";
                    return cellValue;
                }));
                return lineStr;
            }));

            //创建一个新的Excel文件
            using (var package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(sheetName);

                //调整为文本格式
                for (int i = 0; i < excelCols.Count; i++)
                {
                    worksheet.Columns[i + 1].Style.Numberformat.Format = "@";
                }

                ExcelTextFormat excelTextFormat = new ExcelTextFormat();
                excelTextFormat.Delimiter = '\t';
                //表头处理
                worksheet.Cells["A1"].LoadFromText(headers, excelTextFormat);

                //在A2单元格粘贴内容
                worksheet.Cells["A2"].LoadFromText(content, excelTextFormat);

                //首行加粗
                worksheet.Rows[1].Style.Font.Bold = true;

                //单元格内容重新赋值
                foreach (var cell in worksheet.Cells[2, 1, Program.setting.Setting_IN.Account_List.Count + 1,
                             excelCols.Count])
                {
                    if (cell.Value != null) cell.Value = cell.Value.ToString().Replace("'", "");
                }

                //自动列宽，居中[CK那一列不居中]
                for (int i = 0; i < excelCols.Count; i++)
                {
                    if (worksheet.Cells[1, i + 1].Text == "Facebook_CK" || worksheet.Cells[1, i + 1].Text == "UA")
                    {
                        worksheet.Cells[1, i + 1].Style.HorizontalAlignment =
                            OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Columns[i + 1].Width = 20;
                        continue;
                    }

                    worksheet.Columns[i + 1].AutoFit();
                    worksheet.Columns[i + 1].Style.HorizontalAlignment =
                        OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                // 保存Excel文件
                package.SaveAs(fileInfo);
            }

            //显示Excel文档
            this.Invoke(new Action(() =>
            {
                StringHelper.OpenFolderAndSelectFiles(fileInfo.Directory.FullName,
                    new string[] { fileInfo.FullName });
            }));

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_IN_RE.Enabled = true;
                Application.DoEvents();
            }));
        }

        //导出账号
        private void btn_Export_List_IN_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(this.ExportExcel_IN);
        }

        //导出账号
        private void btn_Export_List_IN_RE_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(this.ExportExcel_IN_RE);
        }

        private string Setting_Check_IN()
        {
            string errorMsg = string.Empty;

            if (Program.setting.Setting_IN.Account_List == null || Program.setting.Setting_IN.Account_List.Count == 0)
            {
                errorMsg = $"请先导入账号";
                return errorMsg;
            }

            int num = 0;
            this.Invoke(new Action(() =>
            {
                if (!int.TryParse(this.txt_ThreadCountMax_IN.Text.Trim(), out num) || num <= 0)
                {
                    errorMsg = $"线程数设置不正确,应填写正整数";
                    return;
                }

                if (this.cb_Global_WebProxyInfo_IN.Checked)
                {
                    if (this.txt_Global_WebProxyInfo_IN_IPAddress.Text.Trim().Length == 0)
                    {
                        errorMsg = $"要开启全局代理，必须填写代理地址";
                        return;
                    }
                }

                if (!this.rb_ForgotPwdSetting_Front_Custom_IN.Checked &&
                    !this.rb_ForgotPwdSetting_Front_Random_IN.Checked)
                {
                    errorMsg = $"请先设置忘记密码时设定密码的模式";
                    return;
                }

                if (this.rb_ForgotPwdSetting_Front_Custom_IN.Checked &&
                    string.IsNullOrEmpty(this.txt_ForgotPwdSetting_Front_Custom_Content_IN.Text.Trim()))
                {
                    errorMsg = $"密码前缀为自定义时需要设置自定义内容";
                    return;
                }

                if (Program.setting.Setting_IN.TaskInfoList == null ||
                    Program.setting.Setting_IN.TaskInfoList.Where(t => t.IsSelected).Count() == 0)
                {
                    errorMsg = $"至少设置1项执行任务";
                    return;
                }
            }));

            return errorMsg;
        }

        //启动任务方法
        private void ThreadMethod_StartTasks_IN_Main(object obj_SelectType)
        {
            int selectType = Convert.ToInt32(obj_SelectType);
            string errorMsg = string.Empty;
            //判断是否有现成在运行
            bool isRunning = false;
            if (Program.setting.Setting_IN.Account_List != null)
                isRunning = Program.setting.Setting_IN.Account_List.Where(a => a.Running_IsWorking).Count() > 0;
            if (!isRunning)
            {
                //数据验证
                errorMsg = this.Setting_Check_IN();
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    MessageBox.Show(errorMsg);
                    return;
                }
            }

            List<Account_FBOrIns> account_Selected = null;
            if (selectType == 0) account_Selected = Program.setting.Setting_IN.Account_List;
            else
            {
                List<int> iList = null;
                this.Invoke(new Action(() => { iList = this.GetSelectedIndexList(this.dgv_IN); }));
                if (iList.Count == 0)
                {
                    errorMsg = "请先选择需要操作的账号";
                    MessageBox.Show(errorMsg);
                    return;
                }

                account_Selected = iList.Select(i => Program.setting.Setting_IN.Account_List[i]).ToList();
            }

            if (!isRunning)
            {
                //禁用按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_IN.Enabled = false;
                    this.btn_ImportAccount_IN.Enabled = false;
                    this.btn_Start_IN.Enabled = false;
                    this.btn_Stop_IN.Enabled = true;

                    this.tsmi_Start_All_IN.Enabled = false;
                    this.tsmi_Stop_All_IN.Enabled = true;

                    this.tsmi_Delete_One_IN.Enabled = false;
                    this.tsmi_Delete_All_IN.Enabled = false;

                    this.dgv_GMList_ForBind_IN.Enabled = false;

                    this.btn_List_Order_IN.Enabled = false;
                }));

                this.SaveSetting_FromUser();
                this.SaveSetting_ToDisk();

                //线程设置
                if (this.stp_IN == null)
                {
                    this.stp_IN = new SmartThreadPool();
                }

                this.stp_IN.Concurrency = Program.setting.Setting_IN.ThreadCountMax;

                //统计显示
                this.ShowTongJiInfo_IN(0);
                //派发任务
                for (int i = 0; i < account_Selected.Count; i++)
                {
                    account_Selected[i].WorkItemResult =
                        this.stp_IN.QueueWorkItem(this.ThreadMethod_StartTasks_IN_Child, account_Selected[i]);
                    Thread.Sleep(50);
                    Application.DoEvents();
                }

                //等待任务结束，恢复按钮
                if (!isRunning)
                {
                    while (!this.stp_IN.IsIdle)
                    {
                        Thread.Sleep(500);
                        Application.DoEvents();
                    }

                    //恢复按钮
                    this.Invoke(new Action(() =>
                    {
                        this.btn_ClearData_IN.Enabled = true;
                        this.btn_ImportAccount_IN.Enabled = true;
                        this.btn_Start_IN.Enabled = true;
                        this.btn_Stop_IN.Enabled = false;

                        this.tsmi_Start_All_IN.Enabled = true;
                        this.tsmi_Stop_All_IN.Enabled = false;

                        this.tsmi_Delete_One_IN.Enabled = true;
                        this.tsmi_Delete_All_IN.Enabled = true;

                        this.dgv_GMList_ForBind_IN.Enabled = true;

                        this.btn_List_Order_IN.Enabled = true;
                    }));
                }
            }
        }

        //启动任务方法
        private void ThreadMethod_StartTasks_IN_RE_Main(object obj_SelectType)
        {
            string errorMsg = string.Empty;
            //判断是否有现成在运行
            bool isRunning = false;
            if (Program.setting.Setting_IN_RE.Account_List != null)
                isRunning = Program.setting.Setting_IN_RE.Account_List.Where(a => a.Running_IsWorking).Count() > 0;

            if (!isRunning)
            {
                //数据验证
                errorMsg = this.Setting_Check_IN_RE();
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    MessageBox.Show(errorMsg);
                    return;
                }
            }

            if (!isRunning)
            {
                //禁用按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_IN_RE.Enabled = false;
                    this.btn_Start_IN_RE.Enabled = false;
                    this.btn_Stop_IN_RE.Enabled = true;

                    // this.tsmi_Start_All_IN_RE.Enabled = false;
                    // this.tsmi_Stop_All_IN_RE.Enabled = true;
                    //
                    // this.tsmi_Delete_One_IN_RE.Enabled = false;
                    // this.tsmi_Delete_All_IN_RE.Enabled = false;

                    this.btn_List_Order_IN_RE.Enabled = false;
                }));

                this.SaveSetting_FromUser();
                this.SaveSetting_ToDisk();

                //线程设置
                if (this.stp_IN_RE == null)
                {
                    this.stp_IN_RE = new SmartThreadPool();
                }

                this.stp_IN_RE.Concurrency = Program.setting.Setting_IN_RE.ThreadCountMax;
                //
                // //统计显示
                // this.ShowTongJiInfo_IG(0);
            }

            //派发任务
            for (int i = 0; i < Int32.Parse(this.textBox_email_num_EM.Text); i++)
            {
                var accountFbOrIns = new Account_FBOrIns();

                accountFbOrIns.WorkItemResult =
                    this.stp_IN_RE.QueueWorkItem(this.ThreadMethod_StartTasks_IN_RE_Child, accountFbOrIns);
                Thread.Sleep(50);
                Application.DoEvents();
            }

            //等待任务结束，恢复按钮
            if (!isRunning)
            {
                while (!this.stp_IN_RE.IsIdle)
                {
                    Thread.Sleep(500);
                    Application.DoEvents();
                }

                //恢复按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_IN_RE.Enabled = true;
                    this.btn_Start_IN_RE.Enabled = true;
                    this.btn_Stop_IN_RE.Enabled = false;

                    // this.tsmi_Start_All_IN_RE.Enabled = true;
                    // this.tsmi_Stop_All_IN_RE.Enabled = false;
                    //
                    // this.tsmi_Delete_One_IN_RE.Enabled = true;
                    // this.tsmi_Delete_All_IN_RE.Enabled = true;

                    this.btn_List_Order_IN_RE.Enabled = true;
                }));
            }
        }

        //获取一个未使用的邮箱
        private MailInfo GetNotUsedMailInfo_IN()
        {
            MailInfo mail = null;

            lock (Program.setting.Setting_IN.Lock_Mail_ForBind_List)
            {
                if (Program.setting.Setting_IN.Mail_ForBind_List == null) mail = null;
                else
                    mail = Program.setting.Setting_IN.Mail_ForBind_List.Where(m => !m.Is_Used && !m.IsLocked)
                        .FirstOrDefault();

                if (mail != null) mail.IsLocked = true;
            }

            return mail;
        }

        //获取一个未使用的邮箱
        private MailInfo GetNotUsedMailInfo_IN_RE()
        {
            MailInfo mail = null;

            lock (Program.setting.Setting_IN_RE.Lock_Mail_ForBind_List)
            {
                if (Program.setting.Setting_IN_RE.Mail_ForBind_List == null) mail = null;
                else
                    mail = Program.setting.Setting_IN_RE.Mail_ForBind_List.Where(m => !m.Is_Used && !m.IsLocked)
                        .FirstOrDefault();

                if (mail != null) mail.IsLocked = true;
            }

            return mail;
        }

        //获取一个未使用的邮箱
        private MailInfo GetNotUsedMailInfo_EM()
        {
            MailInfo mail = null;

            lock (Program.setting.Setting_EM.Lock_Mail_ForBind_List)
            {
                if (Program.setting.Setting_EM.Mail_ForBind_List == null) mail = null;
                else
                    mail = Program.setting.Setting_EM.Mail_ForBind_List.Where(m =>
                            !m.Is_Used && !m.IsLocked && m.Mail_Name.Contains("@gmail.com"))
                        .FirstOrDefault();

                if (mail != null) mail.IsLocked = true;
            }

            return mail;
        }

        //获取一个未使用的邮箱
        private MailInfo GetNotUsedMailInfoBindEmail_EM()
        {
            MailInfo mail = null;

            lock (Program.setting.Setting_EM.Lock_Mail_ForBind_List)
            {
                if (Program.setting.Setting_EM.Mail_ForBind_List == null) mail = null;
                else
                    mail = Program.setting.Setting_EM.Mail_ForBind_List.Where(m =>
                            !m.Is_Used && !m.IsLocked && m.Mail_Name.Contains("@rambler.ru"))
                        .FirstOrDefault();

                if (mail != null) mail.IsLocked = true;
            }

            return mail;
        }

        //生成一个新的密码
        private string GetNewPassword_IN()
        {
            string newPwd = string.Empty;
            if (Program.setting.Setting_IN.ForgotPwdSetting_Front_Mode == 0)
                newPwd = StringHelper.GetRandomString(true, true, true, true, 10, 10);
            else newPwd = Program.setting.Setting_IN.ForgotPwdSetting_Front_Custom_Content.Trim();

            if (Program.setting.Setting_IN.ForgotPwdSetting_After_IsAddDate)
                newPwd += $"{DateTime.Now.ToString("MMdd")}";

            return newPwd;
        }
        //生成一个新的密码

        private string GetNewPassword_IN_RE()
        {
            string newPwd = string.Empty;
            if (Program.setting.Setting_IN_RE.ForgotPwdSetting_Front_Mode == 0)
                newPwd = StringHelper.GetRandomString(true, true, true, true, 10, 10);
            else newPwd = Program.setting.Setting_IN_RE.ForgotPwdSetting_Front_Custom_Content.Trim();

            if (Program.setting.Setting_IN_RE.ForgotPwdSetting_After_IsAddDate)
                newPwd += $"{DateTime.Now.ToString("MMdd")}";

            return newPwd;
        }

        //核心子线程
        private void ThreadMethod_StartTasks_IN_RE_Child(Account_FBOrIns account)
        {
            #region 实例化ADS API

            AdsPowerService adsPowerService = new AdsPowerService();

            var adsUserCreate = adsPowerService.ADS_UserCreate("IN_RE", null, StringHelper.CreateRandomUserAgent());
            var user_id = adsUserCreate["data"]["id"].ToString();
            var adsStartBrowser = adsPowerService.ADS_StartBrowser(user_id);
            var selenium = adsStartBrowser["data"]["ws"]["selenium"].ToString();
            var webdriver = adsStartBrowser["data"]["webdriver"].ToString();

            #endregion

            ChromeDriverSetting chromeDriverSetting = new ChromeDriverSetting();
            ChromeDriver driverSet = null;
            try
            {
                DateTime sendCodeTime = DateTime.Parse("1970-01-01");
                account.Running_Log = "开始操作";
                Thread.Sleep(1000);
                var strSnowId = UUID.StrSnowId;
                string proxy = string.Empty;
                if (Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_IsUse &&
                    !string.IsNullOrEmpty(Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Url))
                {
                    proxy = Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Url + ":" +
                            Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_UserName + ":" +
                            Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Pwd;
                }

                //1：配置浏览器环境信息
                driverSet = chromeDriverSetting.GetDriverSetting("IN_RE", strSnowId, selenium, webdriver);

                driverSet.Navigate().GoToUrl(
                    "https://www.linkedin.com/signup?trk=guest_homepage-basic_nav-header-join");
                Thread.Sleep(3000);

                MailInfo mail = this.GetNotUsedMailInfo_IN_RE();
                if (mail == null)
                {
                    account.Running_Log = "无可用邮箱";
                    return;
                }

                Thread.Sleep(3000);
                //输入邮箱
                if (CheckIsExists(driverSet,
                        By.Id("email-address")))
                {
                    driverSet.FindElement(By.Id("email-address"))
                        .SendKeys(mail.Mail_Name);
                }

                Thread.Sleep(3000);
                var newPassword = this.GetNewPassword_IN_RE();
                //输入密码
                if (CheckIsExists(driverSet,
                        By.Id("password")))
                {
                    driverSet.FindElement(By.Id("password"))
                        .SendKeys(newPassword);
                }

                Thread.Sleep(3000);
                //点击提交
                if (CheckIsExists(driverSet,
                        By.Id("join-form-submit")))
                {
                    driverSet.FindElement(By.Id("join-form-submit"))
                        .Click();
                }

                Thread.Sleep(3000);

                var generateSurname = StringHelper.GenerateSurname();
                //First Name
                if (CheckIsExists(driverSet,
                        By.Id("first-name")))
                {
                    driverSet.FindElement(By.Id("first-name"))
                        .SendKeys("Mc");
                }

                //Last Name
                if (CheckIsExists(driverSet,
                        By.Id("last-name")))
                {
                    driverSet.FindElement(By.Id("last-name"))
                        .SendKeys(generateSurname);
                }

                Thread.Sleep(3000);
                //注册提交
                if (CheckIsExists(driverSet,
                        By.Id("join-form-submit")))
                {
                    driverSet.FindElement(By.Id("join-form-submit"))
                        .Click();
                }

                Thread.Sleep(10000);
                //等待代码 或者sms 接码
                driverSet.SwitchTo().DefaultContent();

                var webElement = driverSet.FindElement(By.CssSelector("[class='challenge-dialog__iframe']"));
                driverSet.SwitchTo().Frame(webElement);


                if (CheckIsExists(driverSet,
                        By.Id("select-register-phone-country")))
                {
                    var findElement = driverSet.FindElement(By.Id("select-register-phone-country"));
                    SelectElement selectObj = new SelectElement(findElement);
                    selectObj.SelectByValue("hk");

                    BindPhone :
                    SmsActivateService smsActivateService = new SmsActivateService();
                    var smsGetPhoneNum = smsActivateService.SMS_GetPhoneNum("tn", "14");
                    if (smsGetPhoneNum["ErrorMsg"] != null &&
                        !string.IsNullOrEmpty(smsGetPhoneNum["ErrorMsg"].ToString()))
                    {
                        return;
                    }

                    //输入收集号码
                    if (CheckIsExists(driverSet,
                            By.Id("register-verification-phone-number")))
                    {
                        driverSet.FindElement(
                            By.Id("register-verification-phone-number")).SendKeys(smsGetPhoneNum["Number"].ToString());
                    }

                    Thread.Sleep(3000);
                    //提交验证码
                    if (CheckIsExists(driverSet,
                            By.Id("register-phone-submit-button")))
                    {
                        driverSet.FindElement(By.Id("register-phone-submit-button"))
                            .Click();
                    }

                    Thread.Sleep(3000);
                    if (driverSet.PageSource.Contains("You can’t use this phone number. Please try a different one") ||
                        driverSet.PageSource.Contains("Something unexpected happened. Please try again."))
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

                    if (string.IsNullOrEmpty(sms_code)) return;
                    //验证码
                    if (CheckIsExists(driverSet,
                            By.Id("input__phone_verification_pin")))
                    {
                        driverSet.FindElement(By.Id("input__phone_verification_pin"))
                            .SendKeys(sms_code);
                    }

                    Thread.Sleep(3000);
                }
                else
                {
                    Thread.Sleep(20000);
                    if (driverSet.PageSource.Contains("Your noCAPTCHA user response code is missing or invalid."))
                    {
                        return;
                    }

                    driverSet.SwitchTo().DefaultContent();
                    if (driverSet.PageSource.Contains("Someone’s already using that email."))
                    {
                        lock (Program.setting.Setting_IN.Lock_Mail_ForBind_List)
                        {
                            mail.IsLocked = true;
                            mail.Is_Used = true;
                        }

                        return;
                    }
                }


                Thread.Sleep(3000);
                //提交验证码
                if (CheckIsExists(driverSet,
                        By.Id("register-phone-submit-button")))
                {
                    driverSet.FindElement(By.Id("register-phone-submit-button"))
                        .Click();
                }

                Thread.Sleep(3000);
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
                Thread.Sleep(3000);
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/mypreferences/d/manage-email-addresses");
                Thread.Sleep(3000);

                driverSet.SwitchTo().DefaultContent();
                Thread.Sleep(5000);
                var settings = driverSet.FindElement(By.CssSelector("[class='settings-iframe--frame']"));
                driverSet.SwitchTo().Frame(settings);
                //发送邮箱链接
                if (CheckIsExists(driverSet,
                        By.CssSelector("[class='send-verification tertiary-btn']")))
                {
                    driverSet.FindElement(By.CssSelector("[class='send-verification tertiary-btn']"))
                        .Click();
                }

                Thread.Sleep(3000);

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
                    return;
                }

                if (pop3MailMessage == null)
                {
                    return;
                }

                var confirmCode = StringHelper.GetMidStr(pop3MailMessage.Html,
                    "https://www.linkedin.com/comm/psettings/email/confirm", "\n").Trim();
                if (string.IsNullOrEmpty(confirmCode))
                {
                    return;
                }

                confirmCode = "https://www.linkedin.com/comm/psettings/email/confirm" +
                              confirmCode;

                #endregion

                driverSet.Navigate().GoToUrl(confirmCode);
                Thread.Sleep(5000);
                driverSet.Navigate().GoToUrl("https://www.linkedin.com/");
                Thread.Sleep(5000);
                if (driverSet.Url.Contains("https://www.linkedin.com/"))
                {
                    var cookieJar = driverSet.Manage().Cookies.AllCookies;
                    if (cookieJar.Count > 0)
                    {
                        string strJson = JsonConvert.SerializeObject(cookieJar);
                        account.Facebook_CK = strJson;
                    }

                    account.Facebook_Pwd = newPassword;
                    account.New_Mail_Name = mail.Mail_Name;
                    account.New_Mail_Pwd = mail.Mail_Pwd;
                    //处理邮箱的绑定问题
                    lock (Program.setting.Setting_IN.Lock_Mail_ForBind_List)
                    {
                        mail.IsLocked = true;
                        mail.Is_Used = true;
                    }

                    if (Program.setting.Setting_IN_RE.Account_List == null)
                        Program.setting.Setting_IN_RE.Account_List = new List<Account_FBOrIns>();
                    Program.setting.Setting_IN_RE.Account_List.Add(account);
                    if (Program.setting.Setting_IN_RE.Account_List != null &&
                        Program.setting.Setting_IN_RE.Account_List.Count > 0)
                        this.Invoke(new Action(() =>
                        {
                            this.dgv_IN_RE.DataSource = Program.setting.Setting_IN_RE.Account_List;
                            this.dgv_IN_RE.Refresh();
                        }));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                adsPowerService.ADS_UserDelete(user_id);
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
        }

        //核心子线程
        private void ThreadMethod_StartTasks_IN_Child(Account_FBOrIns account)
        {
            JObject jo_Result = null;
            bool isSuccess = false;
            bool isMailUsed = false;
            int trySpan = 0;
            int tryTimes = 0;
            int tryTimesMax = 3;
            MailInfo mail = null;
            string taskName = string.Empty;
            TaskInfo task = null;
            string newPassword = string.Empty;
            ChromeDriver driverSet = null;
            account.Running_Log = "开始操作";
            Thread.Sleep(1000);

            if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
            account.LoginInfo.CookieCollection =
                StringHelper.GetCookieCollectionByCookieJsonStr(account.Facebook_CK);

            #region 初始化代理/UA

            account.Running_Log = "初始化代理/UA";
            if (string.IsNullOrEmpty(account.UserAgent) || account.WebProxy == null)
            {
                if (string.IsNullOrEmpty(account.UserAgent)) account.UserAgent = StringHelper.CreateRandomUserAgent();
                if (account.WebProxy == null)
                {
                    if (Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_IsUse &&
                        !string.IsNullOrEmpty(Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Url))
                    {
                        account.WebProxy =
                            new System.Net.WebProxy(Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Url);
                        if (!string.IsNullOrEmpty(Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_UserName) &&
                            !string.IsNullOrEmpty(Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Pwd))
                            account.WebProxy.Credentials = new NetworkCredential(
                                Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_UserName,
                                Program.setting.Setting_IN.Global_WebProxyInfo.Proxy_Pwd);
                    }
                }

                Thread.Sleep(1000);
            }

            #endregion

            #region 检测Cookie

            taskName = "LoginByCookie";
            task = Program.setting.Setting_IN.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                isSuccess = false;
                isMailUsed = true;
                trySpan = 1000;
                tryTimes = 0;
                tryTimesMax = 2;
                while (!isSuccess && tryTimes < tryTimesMax)
                {
                    if (tryTimes > 0) Thread.Sleep(trySpan);
                    tryTimes += 1;

                    account.Running_Log = "检测Cookie是否有效";
                    jo_Result = this.linkedinService.IN_EmailByCookie(account);
                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                    isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());

                    if (!isSuccess && jo_Result["isNeedLoop"].ToString().ToLower() == "false") break;
                }

                //状态记录
                //lock (Program.setting.Setting_Facebook.LockObj_TongJi_Real) { Program.setting.Setting_Facebook.TongJi_Real.WuXiao += 1; this.ShowTongJiInfo_FB(0); }
                if (!isSuccess)
                {
                    lock (Program.setting.Setting_IN.LockObj_TongJi_Real)
                    {
                        if (account.Running_Log == "Cookie无效")
                        {
                            account.Account_Type_Des = "Cookie无效";
                            Program.setting.Setting_FB.TongJi_Real.WuXiao += 1;
                        }
                        else if (account.Running_Log.Contains("账户被锁定"))
                        {
                            account.Account_Type_Des = "账户被锁定";
                            Program.setting.Setting_FB.TongJi_Real.FengHaoShu += 1;
                        }
                        else
                        {
                            account.Account_Type_Des = "其它错误";
                            Program.setting.Setting_FB.TongJi_Real.QiTaCuoWu += 1;
                        }

                        this.ShowTongJiInfo_IN(0);
                    }
                }

                if (!isSuccess) return;
            }

            #endregion

            #region 进行忘记密码操作

            newPassword = this.GetNewPassword_IN();
            taskName = "ForgotPassword";
            task = Program.setting.Setting_IN.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "进行忘记密码操作";
                jo_Result = this.linkedinService.IN_ForgotPassword_choose(account, newPassword, driverSet);

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());

                if (isSuccess)
                {
                    account.Facebook_Pwd = newPassword;
                }
                else
                {
                    lock (Program.setting.Setting_IN.LockObj_TongJi_Real)
                    {
                        account.Account_Type_Des = "其它错误";
                        Program.setting.Setting_IN.TongJi_Real.QiTaCuoWu += 1;

                        this.ShowTongJiInfo_IN(0);
                    }

                    return;
                }
            }

            #endregion

            #region 进行邮箱绑定

            taskName = "BindNewEmail";
            task = Program.setting.Setting_IN.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                isSuccess = false;
                isMailUsed = true;
                trySpan = 1000;
                tryTimes = 0;
                tryTimesMax = 10;
                while (!isSuccess && isMailUsed && tryTimes < tryTimesMax)
                {
                    Thread.Sleep(trySpan);
                    tryTimes += 1;

                    account.Running_Log = "进行邮箱绑定";
                    mail = this.GetNotUsedMailInfo_IN();
                    if (mail == null)
                    {
                        account.Running_Log = "无可用邮箱";
                        break;
                    }

                    jo_Result = this.linkedinService.IN_BindNewEmail_choose(account, mail, driverSet);
                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                    isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                    isMailUsed = Convert.ToBoolean(jo_Result["IsMailUsed"].ToString());
                    //处理邮箱的绑定问题
                    lock (Program.setting.Setting_IN.Lock_Mail_ForBind_List)
                    {
                        mail.IsLocked = isMailUsed;
                        mail.Is_Used = isMailUsed;
                    }
                }

                if (isSuccess)
                {
                    account.New_Mail_Name = mail.Mail_Name;
                    account.New_Mail_Pwd = mail.Mail_Pwd;
                }

                Thread.Sleep(1000);
            }

            #endregion

            #region 切换邮箱

            taskName = "ChangeEmail";
            task = Program.setting.Setting_IN.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.New_Mail_Name) || !string.IsNullOrEmpty(account.New_Mail_Pwd))
                {
                    isSuccess = false;
                    isMailUsed = true;
                    trySpan = 1000;
                    tryTimes = 0;
                    tryTimesMax = 10;
                    while (!isSuccess && isMailUsed && tryTimes < tryTimesMax)
                    {
                        Thread.Sleep(trySpan);
                        tryTimes += 1;

                        account.Running_Log = "切换邮箱";
                        jo_Result = this.linkedinService.IN_ChangeEmail_choose(account, driverSet);
                        account.Running_Log = jo_Result["ErrorMsg"].ToString();
                        isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                    }

                    Thread.Sleep(1000);
                }
            }

            #endregion

            #region 获取信息

            taskName = "GetInfo";
            task = Program.setting.Setting_IN.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "获取信息";
                jo_Result = this.linkedinService.IN_GetInfo_choose(account, driverSet);
                account.Running_Log = jo_Result["ErrorMsg"].ToString();
                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                Thread.Sleep(1000);
            }

            #endregion

            #region 验证密码

            taskName = "VerifyPassword";
            task = Program.setting.Setting_IN.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "验证密码";
                jo_Result = this.linkedinService.VerifyPassword(account, null);
                account.Running_Log = jo_Result["ErrorMsg"].ToString();
                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                Thread.Sleep(1000);
            }

            #endregion

            #region 修改密码

            taskName = "ChangePassword";
            task = Program.setting.Setting_IN.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "修改密码";
                string password = this.GetNewPassword_IN();
                jo_Result = this.linkedinService.VerifyPassword(account, password);
                account.Running_Log = jo_Result["ErrorMsg"].ToString();
                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                Thread.Sleep(1000);
            }

            #endregion
        }

        private void Method_StopTasks_IN_RE(int selectType = 0)
        {
            List<Account_FBOrIns> account_Selected = null;
            if (selectType == 0) account_Selected = Program.setting.Setting_IN_RE.Account_List;
            else
            {
                List<int> iList = null;
                this.Invoke(new Action(() => { iList = this.GetSelectedIndexList(this.dgv_IN_RE); }));
                if (iList.Count == 0)
                {
                    MessageBox.Show("请先选择需要操作的账号");
                    return;
                }

                account_Selected = iList.Select(i => Program.setting.Setting_IN_RE.Account_List[i]).ToList();
            }

            //停止子线程
            for (int i = 0; i < account_Selected.Count; i++)
            {
                if (account_Selected[i].WorkItemsGroup != null) account_Selected[i].WorkItemsGroup.Cancel(true);
                if (account_Selected[i].WorkItemResult != null) account_Selected[i].WorkItemResult.Cancel(true);
                //account_Selected[i].Running_Log = "操作中止";
            }

            if (selectType == 0)
            {
                //停止主线程
                if (this.stp_IN_RE != null) this.stp_IN_RE.Cancel(true);
                if (this.thread_Main_IN_RE != null) this.thread_Main_IN_RE.Abort();

                this.SaveSetting_FromUser();
                this.SaveSetting_ToDisk();

                //恢复按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_IN_RE.Enabled = true;
                    this.btn_ImportAccount_IN_RE.Enabled = true;
                    this.btn_Start_IN_RE.Enabled = true;
                    this.btn_Stop_IN_RE.Enabled = false;

                    this.tsmi_Start_All_IN_RE.Enabled = true;
                    this.tsmi_Stop_All_IN_RE.Enabled = false;

                    this.tsmi_Delete_One_IN_RE.Enabled = true;
                    this.tsmi_Delete_All_IN_RE.Enabled = true;

                    this.dgv_GMList_ForBind_IN_RE.Enabled = true;

                    this.btn_List_Order_IN_RE.Enabled = true;
                }));
            }
        }

        private void Method_StopTasks_IN(int selectType = 0)
        {
            List<Account_FBOrIns> account_Selected = null;
            if (selectType == 0) account_Selected = Program.setting.Setting_IN.Account_List;
            else
            {
                List<int> iList = null;
                this.Invoke(new Action(() => { iList = this.GetSelectedIndexList(this.dgv_IN); }));
                if (iList.Count == 0)
                {
                    MessageBox.Show("请先选择需要操作的账号");
                    return;
                }

                account_Selected = iList.Select(i => Program.setting.Setting_IN.Account_List[i]).ToList();
            }

            //停止子线程
            for (int i = 0; i < account_Selected.Count; i++)
            {
                if (account_Selected[i].WorkItemsGroup != null) account_Selected[i].WorkItemsGroup.Cancel(true);
                if (account_Selected[i].WorkItemResult != null) account_Selected[i].WorkItemResult.Cancel(true);
                //account_Selected[i].Running_Log = "操作中止";
            }

            if (selectType == 0)
            {
                //停止主线程
                if (this.stp_IN != null) this.stp_IN.Cancel(true);
                if (this.thread_Main_IN != null) this.thread_Main_IN.Abort();

                this.SaveSetting_FromUser();
                this.SaveSetting_ToDisk();

                //恢复按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_IN.Enabled = true;
                    this.btn_ImportAccount_IN.Enabled = true;
                    this.btn_Start_IN.Enabled = true;
                    this.btn_Stop_IN.Enabled = false;

                    this.tsmi_Start_All_IN.Enabled = true;
                    this.tsmi_Stop_All_IN.Enabled = false;

                    this.tsmi_Delete_One_IN.Enabled = true;
                    this.tsmi_Delete_All_IN.Enabled = true;

                    this.dgv_GMList_ForBind_IN.Enabled = true;

                    this.btn_List_Order_IN.Enabled = true;
                }));
            }
        }

        private void btn_Stop_IN_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_IN(0);
        }

        private void btn_Start_IN_RE_Click(object sender, EventArgs e)
        {
            this.thread_Main_IN_RE = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_IN_RE_Main));
            this.thread_Main_IN_RE.IsBackground = true;
            this.thread_Main_IN_RE.Start(0);
        }

        //真实数据清零
        private void btn_TongJi_Real_Clear_IN_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("重新统计，将清除原来的数据，确定操作吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;
            Program.setting.Setting_IN.TongJi_Real = new TongJi_FBOrIns();
            this.ShowTongJiInfo_IN(0);
        }

        //导入账号的具体方法
        private void ImportAccount_IN(string fileName)
        {
            string errorMsg = string.Empty;

            FileInfo fi = new FileInfo(fileName);
            ExcelPackage excelPackage = null;
            try
            {
                excelPackage = new ExcelPackage(fi);
            }
            catch (Exception ex)
            {
                errorMsg = $"打开文件失败({ex.Message})";
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                MessageBox.Show(errorMsg);
                return;
            }

            if (excelPackage.Workbook.Worksheets.Count == 0)
            {
                errorMsg = $"表格内容不存在";
                MessageBox.Show(errorMsg);
                return;
            }

            ExcelWorksheet sheet = excelPackage.Workbook.Worksheets[0];
            int rowCount = sheet.Dimension.End.Row;
            int colCount = sheet.Dimension.End.Column;

            if (rowCount < 2)
            {
                errorMsg = $"表格行数至少为2行，第一行未表头，第二行开始为内容";
                MessageBox.Show(errorMsg);
                return;
            }

            List<ExcelColumnInfo> excelCols = new List<ExcelColumnInfo>()
            {
                new ExcelColumnInfo("操作日志", "Running_Log"),
                new ExcelColumnInfo("账号分类", "Account_Type_Des"),
                new ExcelColumnInfo("原邮箱CK", "Old_Mail_CK"),
                new ExcelColumnInfo("原邮箱账号", "Old_Mail_Name"),
                new ExcelColumnInfo("原邮箱密码", "Old_Mail_Pwd"),
                new ExcelColumnInfo("新邮箱账号", "New_Mail_Name"),
                new ExcelColumnInfo("新邮箱密码", "New_Mail_Pwd"),
                new ExcelColumnInfo("IN_密码", "Facebook_Pwd"),
                new ExcelColumnInfo("COOKIE", "Facebook_CK"),
                new ExcelColumnInfo("UA", "UserAgent"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("2FA密钥", "TwoFA_Dynamic_SecretKey"),
                new ExcelColumnInfo("手机号码", "Phone_Num"),
                new ExcelColumnInfo("接码地址", "Phone_Num_Url"),
                new ExcelColumnInfo("国家", "GuoJia"),
                new ExcelColumnInfo("注册日期", "ZhuCeRiQi"),
                new ExcelColumnInfo("账户链接", "AccountName"),
                new ExcelColumnInfo("好友", "HaoYouCount"),
                new ExcelColumnInfo("认证", "Certification"),
                new ExcelColumnInfo("加好友参数", "FsdProfile"),
            };

            List<int> rList = Enumerable.Range(2, rowCount - 1).ToList();
            List<int> cList = Enumerable.Range(1, colCount).ToList();

            //表头定位
            List<string> sList = cList.Select(c => sheet.Cells[1, c].Text).ToList();
            foreach (var eCol in excelCols)
            {
                eCol.HeaderIndex = sList.FindIndex(s => s.Trim() == eCol.HeaderName);
            }

            //一次读取所有行内容
            rList = Enumerable.Range(2, rowCount - 1).ToList();
            cList = Enumerable.Range(1, colCount).ToList();
            sList = rList.Select(r => string.Join("\t", cList.Select(c => sheet.Cells[r, c].Text))).ToList();

            //每一行内容，创建实例添加到列表中
            List<Account_FBOrIns> accounts = sList.Select(s =>
            {
                string[] cellArr = s.Split('\t');

                Account_FBOrIns account = new Account_FBOrIns();

                int cellValue;
                for (int i = 0; i < excelCols.Count; i++)
                {
                    if (excelCols[i].HeaderIndex < 0 || excelCols[i].HeaderIndex >= cellArr.Length) continue;

                    if (excelCols[i].PropertyName == "TwoFA_Dynamic_StatusDes")
                    {
                        if (cellArr[excelCols[i].HeaderIndex].Trim() == "开") cellValue = 1;
                        else if (cellArr[excelCols[i].HeaderIndex].Trim() == "关") cellValue = 0;
                        else cellValue = -1;
                        this.SetProperty(account, "TwoFA_Dynamic_Status", cellValue);
                    }
                    else if (excelCols[i].PropertyName == "C_User")
                    {
                        if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
                        account.LoginInfo.LoginData_Account_Id = cellArr[excelCols[i].HeaderIndex].Trim();
                    }
                    else if (excelCols[i].PropertyName == "UserName")
                    {
                        if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
                        account.LoginInfo.LoginData_UserName = cellArr[excelCols[i].HeaderIndex].Trim();
                    }
                    else this.SetProperty(account, excelCols[i].PropertyName, cellArr[excelCols[i].HeaderIndex].Trim());
                }

                if (string.IsNullOrEmpty(account.Facebook_CK) && string.IsNullOrEmpty(account.Old_Mail_CK))
                    account = null;

                return account;
            }).Where(a => a != null).GroupBy(a =>
            {
                //"name":"c_user","value":"100070943890270"
                string key = StringHelper.GetMidStr(a.Facebook_CK, "\"name\":\"c_user\",\"value\":\"", "\"").Trim();
                return key;
            }).SelectMany(ga =>
            {
                if (!string.IsNullOrEmpty(ga.Key)) return ga.ToList().GetRange(0, 1);
                else return ga.ToList();
            }).ToList();

            //合并更新到表格中
            this.Invoke(new Action(() => { this.dgv_IN.DataSource = null; }));

            Program.setting.Setting_IN.Account_List = accounts;

            if (Program.setting.Setting_IN.Account_List != null && Program.setting.Setting_IN.Account_List.Count > 0)
                this.Invoke(new Action(() => { this.dgv_IN.DataSource = Program.setting.Setting_IN.Account_List; }));
        }

        //导入账号的具体方法
        private void ImportAccount_IN_RE(string fileName)
        {
            string errorMsg = string.Empty;

            FileInfo fi = new FileInfo(fileName);
            ExcelPackage excelPackage = null;
            try
            {
                excelPackage = new ExcelPackage(fi);
            }
            catch (Exception ex)
            {
                errorMsg = $"打开文件失败({ex.Message})";
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                MessageBox.Show(errorMsg);
                return;
            }

            if (excelPackage.Workbook.Worksheets.Count == 0)
            {
                errorMsg = $"表格内容不存在";
                MessageBox.Show(errorMsg);
                return;
            }

            ExcelWorksheet sheet = excelPackage.Workbook.Worksheets[0];
            int rowCount = sheet.Dimension.End.Row;
            int colCount = sheet.Dimension.End.Column;

            if (rowCount < 2)
            {
                errorMsg = $"表格行数至少为2行，第一行未表头，第二行开始为内容";
                MessageBox.Show(errorMsg);
                return;
            }

            List<ExcelColumnInfo> excelCols = new List<ExcelColumnInfo>()
            {
                new ExcelColumnInfo("操作日志", "Running_Log"),
                new ExcelColumnInfo("账号分类", "Account_Type_Des"),
                new ExcelColumnInfo("原邮箱CK", "Old_Mail_CK"),
                new ExcelColumnInfo("原邮箱账号", "Old_Mail_Name"),
                new ExcelColumnInfo("原邮箱密码", "Old_Mail_Pwd"),
                new ExcelColumnInfo("新邮箱账号", "New_Mail_Name"),
                new ExcelColumnInfo("新邮箱密码", "New_Mail_Pwd"),
                new ExcelColumnInfo("IN_密码", "Facebook_Pwd"),
                new ExcelColumnInfo("COOKIE", "Facebook_CK"),
                new ExcelColumnInfo("UA", "UserAgent"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("2FA密钥", "TwoFA_Dynamic_SecretKey"),
                new ExcelColumnInfo("手机号码", "Phone_Num"),
                new ExcelColumnInfo("接码地址", "Phone_Num_Url"),
                new ExcelColumnInfo("国家", "GuoJia"),
                new ExcelColumnInfo("注册日期", "ZhuCeRiQi"),
                new ExcelColumnInfo("账户链接", "AccountName"),
                new ExcelColumnInfo("好友", "HaoYouCount"),
                new ExcelColumnInfo("认证", "Certification"),
                new ExcelColumnInfo("加好友参数", "FsdProfile"),
            };

            List<int> rList = Enumerable.Range(2, rowCount - 1).ToList();
            List<int> cList = Enumerable.Range(1, colCount).ToList();

            //表头定位
            List<string> sList = cList.Select(c => sheet.Cells[1, c].Text).ToList();
            foreach (var eCol in excelCols)
            {
                eCol.HeaderIndex = sList.FindIndex(s => s.Trim() == eCol.HeaderName);
            }

            //一次读取所有行内容
            rList = Enumerable.Range(2, rowCount - 1).ToList();
            cList = Enumerable.Range(1, colCount).ToList();
            sList = rList.Select(r => string.Join("\t", cList.Select(c => sheet.Cells[r, c].Text))).ToList();

            //每一行内容，创建实例添加到列表中
            List<Account_FBOrIns> accounts = sList.Select(s =>
            {
                string[] cellArr = s.Split('\t');

                Account_FBOrIns account = new Account_FBOrIns();

                int cellValue;
                for (int i = 0; i < excelCols.Count; i++)
                {
                    if (excelCols[i].HeaderIndex < 0 || excelCols[i].HeaderIndex >= cellArr.Length) continue;

                    if (excelCols[i].PropertyName == "TwoFA_Dynamic_StatusDes")
                    {
                        if (cellArr[excelCols[i].HeaderIndex].Trim() == "开") cellValue = 1;
                        else if (cellArr[excelCols[i].HeaderIndex].Trim() == "关") cellValue = 0;
                        else cellValue = -1;
                        this.SetProperty(account, "TwoFA_Dynamic_Status", cellValue);
                    }
                    else if (excelCols[i].PropertyName == "C_User")
                    {
                        if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
                        account.LoginInfo.LoginData_Account_Id = cellArr[excelCols[i].HeaderIndex].Trim();
                    }
                    else if (excelCols[i].PropertyName == "UserName")
                    {
                        if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
                        account.LoginInfo.LoginData_UserName = cellArr[excelCols[i].HeaderIndex].Trim();
                    }
                    else this.SetProperty(account, excelCols[i].PropertyName, cellArr[excelCols[i].HeaderIndex].Trim());
                }

                if (string.IsNullOrEmpty(account.Facebook_CK) && string.IsNullOrEmpty(account.Old_Mail_CK))
                    account = null;

                return account;
            }).Where(a => a != null).GroupBy(a =>
            {
                //"name":"c_user","value":"100070943890270"
                string key = StringHelper.GetMidStr(a.Facebook_CK, "\"name\":\"c_user\",\"value\":\"", "\"").Trim();
                return key;
            }).SelectMany(ga =>
            {
                if (!string.IsNullOrEmpty(ga.Key)) return ga.ToList().GetRange(0, 1);
                else return ga.ToList();
            }).ToList();

            //合并更新到表格中
            this.Invoke(new Action(() => { this.dgv_IN_RE.DataSource = null; }));

            Program.setting.Setting_IN_RE.Account_List = accounts;

            if (Program.setting.Setting_IN_RE.Account_List != null &&
                Program.setting.Setting_IN_RE.Account_List.Count > 0)
                this.Invoke(
                    new Action(() => { this.dgv_IN_RE.DataSource = Program.setting.Setting_IN_RE.Account_List; }));
        }

        //导入账号
        private void btn_ImportAccount_IN_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开Excel文档";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "Excel文档 (*.xls;*.xlsx)|*.xls;*.xlsx";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 获取用户选择的文件名
                    string filePath = openFileDialog.FileName;
                    this.ImportAccount_IN(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //导入账号的具体方法
        private void ImportAccount_ForBind_IN(string fileName)
        {
            string fileContent = File.ReadAllText(fileName);
            List<string> sList = Regex.Split(fileContent, "(\r\n)|\n").Where(s => s.Trim().Length > 0)
                .Select(s => s.Trim()).ToList();

            List<MailInfo> mails = sList.Select(s =>
            {
                string[] arr = Regex.Split(s, @"----|\||,|，").ToArray();
                if (arr.Length < 2) return null;

                MailInfo m = new MailInfo();
                m.Mail_Name = arr[0].Trim();
                m.Mail_Pwd = arr[1].Trim();
                if (arr.Length > 2) m.VerifyMail_Name = arr[2].Trim();
                if (arr.Length > 3) m.Is_Used = arr[3].Trim() == "已使用";
                return m;
            }).Where(m => m != null).GroupBy(m => m.Mail_Name).Select(gm => gm.FirstOrDefault()).ToList();

            if (mails.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_GMList_ForBind_IN.DataSource = null; }));

            if (Program.setting.Setting_IN.Mail_ForBind_List == null)
                Program.setting.Setting_IN.Mail_ForBind_List = new List<MailInfo>();
            foreach (MailInfo mail in mails)
            {
                int mIndex =
                    Program.setting.Setting_IN.Mail_ForBind_List.FindIndex(m => m.Mail_Name == mail.Mail_Name);
                if (mIndex == -1) Program.setting.Setting_IN.Mail_ForBind_List.Add(mail);
                else
                {
                    Program.setting.Setting_IN.Mail_ForBind_List[mIndex].Mail_Pwd = mail.Mail_Pwd;
                    Program.setting.Setting_IN.Mail_ForBind_List[mIndex].VerifyMail_Name = mail.VerifyMail_Name;
                }
            }

            if (Program.setting.Setting_IN.Mail_ForBind_List != null &&
                Program.setting.Setting_IN.Mail_ForBind_List.Count > 0)
                this.Invoke(new Action(() =>
                {
                    this.dgv_GMList_ForBind_IN.DataSource = Program.setting.Setting_IN.Mail_ForBind_List;
                }));
        }

        //导入账号的具体方法
        private void ImportAccount_ForBind_IN_RE(string fileName)
        {
            string fileContent = File.ReadAllText(fileName);
            List<string> sList = Regex.Split(fileContent, "(\r\n)|\n").Where(s => s.Trim().Length > 0)
                .Select(s => s.Trim()).ToList();

            List<MailInfo> mails = sList.Select(s =>
            {
                string[] arr = Regex.Split(s, @"----|\||,|，").ToArray();
                if (arr.Length < 2) return null;

                MailInfo m = new MailInfo();
                m.Mail_Name = arr[0].Trim();
                m.Mail_Pwd = arr[1].Trim();
                if (arr.Length > 2) m.VerifyMail_Name = arr[2].Trim();
                if (arr.Length > 3) m.Is_Used = arr[3].Trim() == "已使用";
                return m;
            }).Where(m => m != null).GroupBy(m => m.Mail_Name).Select(gm => gm.FirstOrDefault()).ToList();

            if (mails.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_GMList_ForBind_IN.DataSource = null; }));

            if (Program.setting.Setting_IN_RE.Mail_ForBind_List == null)
                Program.setting.Setting_IN_RE.Mail_ForBind_List = new List<MailInfo>();
            foreach (MailInfo mail in mails)
            {
                int mIndex =
                    Program.setting.Setting_IN_RE.Mail_ForBind_List.FindIndex(m => m.Mail_Name == mail.Mail_Name);
                if (mIndex == -1) Program.setting.Setting_IN_RE.Mail_ForBind_List.Add(mail);
                else
                {
                    Program.setting.Setting_IN_RE.Mail_ForBind_List[mIndex].Mail_Pwd = mail.Mail_Pwd;
                    Program.setting.Setting_IN_RE.Mail_ForBind_List[mIndex].VerifyMail_Name = mail.VerifyMail_Name;
                }
            }

            if (Program.setting.Setting_IN_RE.Mail_ForBind_List != null &&
                Program.setting.Setting_IN_RE.Mail_ForBind_List.Count > 0)
                this.Invoke(new Action(() =>
                {
                    this.dgv_GMList_ForBind_IN_RE.DataSource = Program.setting.Setting_IN_RE.Mail_ForBind_List;
                }));
        }

        private void dgv_GMList_ForBind_IN_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            List<string> fList = files.Where(s => s.ToLower().EndsWith(".txt")).ToList();

            if (fList.Count == 0)
            {
                MessageBox.Show($"导入账号时发生错误：只能导入TXT文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (fList.Count > 1)
            {
                MessageBox.Show($"导入账号时发生错误：每次只能导入1个TXT文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ImportAccount_ForBind_IN(fList[0]);
        }

        private void dgv_GMList_ForBind_IN_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }


        private void dgv_GMList_ForBind_IN_RE_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            List<string> fList = files.Where(s => s.ToLower().EndsWith(".txt")).ToList();

            if (fList.Count == 0)
            {
                MessageBox.Show($"导入账号时发生错误：只能导入TXT文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (fList.Count > 1)
            {
                MessageBox.Show($"导入账号时发生错误：每次只能导入1个TXT文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ImportAccount_ForBind_IN_RE(fList[0]);
        }

        private void dgv_GMList_ForBind_IN_RE_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        //右键弹出菜单
        private void dgv_GMList_ForBind_IN_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.Button == MouseButtons.Right)
            {
                DataGridView dgv = this.dgv_GMList_ForBind_IN;
                //弹出操作菜单
                this.cms_dgv_GM_IN.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //右键弹出菜单
        private void dgv_GMList_ForBind_IN_RE_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.Button == MouseButtons.Right)
            {
                DataGridView dgv = this.dgv_GMList_ForBind_IN_RE;
                //弹出操作菜单
                this.cms_dgv_GM_IN_RE.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //分类排序
        private void ThreadMethod_List_Order_IN()
        {
            this.Invoke(new Action(() => { this.btn_List_Order_IN.Enabled = false; }));

            DataGridView dgv = null;
            this.Invoke(new Action(() =>
            {
                dgv = this.dgv_IN;
                dgv.DataSource = null;
            }));

            Program.setting.Setting_IN.Account_List.ForEach(a =>
            {
                if (!string.IsNullOrEmpty(a.TwoFA_Dynamic_SecretKey) && string.IsNullOrEmpty(a.Account_Type_Des))
                    a.Account_Type_Des = "已完成";
            });

            var orderbyList = Program.setting.Setting_IN.Account_List.OrderByDescending(a =>
            {
                int oNum = 0;
                if (a.Account_Type_Des == "已完成") oNum = 20;
                else if (a.Account_Type_Des == "账户被锁定") oNum = 19;
                else if (a.Account_Type_Des == "Cookie无效") oNum = 18;
                else if (a.Account_Type_Des == "验证其它邮箱") oNum = 17;
                else if (a.Account_Type_Des == "需要设备验证") oNum = 16;
                else if (a.Account_Type_Des == "其它错误") oNum = 15;
                return oNum;
            });

            Program.setting.Setting_IN.Account_List = orderbyList.ToList();

            this.Invoke(new Action(() => { dgv.DataSource = Program.setting.Setting_IN.Account_List; }));

            this.Invoke(new Action(() => { this.btn_List_Order_IN.Enabled = true; }));
        }

        //分类排序
        private void btn_List_Order_IN_Click(object sender, EventArgs e)
        {
            if (Program.setting.Setting_IN.Account_List == null ||
                Program.setting.Setting_IN.Account_List.Count == 0) return;

            Thread thread = new Thread(this.ThreadMethod_List_Order_IN);
            thread.IsBackground = true;
            thread.Start();
        }


        private string Setting_Check_IN_RE()
        {
            string errorMsg = string.Empty;

            int num = 0;
            this.Invoke(new Action(() =>
            {
                if (!int.TryParse(this.txt_ThreadCountMax_IN_RE.Text.Trim(), out num) || num <= 0)
                {
                    errorMsg = $"线程数设置不正确,应填写正整数";
                    return;
                }
            }));

            return errorMsg;
        }

        private void btn_Stop_IN_RE_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_IN_RE(0);
        }

        #endregion

        #region EM执行流程控制

        private Thread thread_Main_EM = null;

        private SmartThreadPool stp_EM = null;

        private void btn_ImportAccount_ForBind_EM_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开TXT文档";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "TXT文档 (*.txt)|*.txt";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 获取用户选择的文件名
                    string filePath = openFileDialog.FileName;
                    this.ImportAccount_ForBind_EM(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btn_Export_List_GM_EM_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(this.ExportTXT_GM_EM);
        }

        //导出TXT文档_具体方法
        private void ExportTXT_GM_EM()
        {
            if (Program.setting.Setting_EM.Mail_ForBind_List == null ||
                Program.setting.Setting_EM.Mail_ForBind_List.Count == 0)
            {
                MessageBox.Show("No Data!");
                return;
            }

            DateTime now = DateTime.Now;
            string sheetName = $"IN_RE_MailList_{now.ToString("yyyyMMdd_HHmmss")}";

            FileInfo fileInfo = null;
            this.Invoke(new Action(() =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "TXT文档（*.txt）|*.txt";
                sfd.FilterIndex = 1;
                sfd.InitialDirectory = Application.StartupPath;
                sfd.RestoreDirectory = true;
                sfd.FileName = $"{sheetName}.txt";
                if (sfd.ShowDialog() != DialogResult.OK) return;
                fileInfo = new FileInfo(sfd.FileName);
            }));
            if (fileInfo == null) return;

            //禁用按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_GM_EM.Enabled = false;
                Application.DoEvents();
            }));

            List<MailInfo> mails_NotUsed = Program.setting.Setting_EM.Mail_ForBind_List.Where(m => !m.Is_Used).ToList();
            List<MailInfo> mails_Used = Program.setting.Setting_EM.Mail_ForBind_List.Where(m => m.Is_Used).ToList();
            List<MailInfo> mails_All = mails_Used.Union(mails_NotUsed).ToList();

            string fileContent = string.Join("\r\n",
                mails_All.Select(m =>
                    $"{m.Mail_Name}----{m.Mail_Pwd}----{m.VerifyMail_Name}----{m.VerifyMail_Pwd}----{(m.Is_Used ? $"----{m.Is_Used_Des}" : string.Empty)}"));

            //写出文档
            File.WriteAllText(fileInfo.FullName, fileContent);

            //显示Excel文档
            this.Invoke(new Action(() =>
            {
                StringHelper.OpenFolderAndSelectFiles(fileInfo.Directory.FullName,
                    new string[] { fileInfo.FullName });
            }));

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_GM_EM.Enabled = true;
                Application.DoEvents();
            }));
        }

        //导出账号
        private void btn_Export_List_EM_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(this.ExportExcel_EM);
        }

        //导出Excel_具体方法
        private void ExportExcel_EM()
        {
            if (Program.setting.Setting_EM.Account_List == null || Program.setting.Setting_EM.Account_List.Count == 0)
            {
                MessageBox.Show("No Data!");
                return;
            }

            DateTime now = DateTime.Now;
            string sheetName = $"EM_{now.ToString("yyyyMMdd_HHmmss")}";
            Regex regex = new Regex(@"^\d+$");

            FileInfo fileInfo = null;
            this.Invoke(new Action(() =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Excel文档（*.xlsx）|*.xlsx";
                sfd.FilterIndex = 1;
                sfd.InitialDirectory = Application.StartupPath;
                sfd.RestoreDirectory = true;
                sfd.FileName = $"{sheetName}.xlsx";
                if (sfd.ShowDialog() != DialogResult.OK) return;
                fileInfo = new FileInfo(sfd.FileName);
            }));
            if (fileInfo == null) return;

            //禁用按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_EM.Enabled = false;
                Application.DoEvents();
            }));

            //整理表头信息
            List<ExcelColumnInfo> excelCols = new List<ExcelColumnInfo>()
            {
                new ExcelColumnInfo("操作日志", "Running_Log"),
                new ExcelColumnInfo("账号分类", "Account_Type_Des"),
                new ExcelColumnInfo("新Mail_号", "New_Mail_Name"),
                new ExcelColumnInfo("新Mail_密", "New_Mail_Pwd"),
                new ExcelColumnInfo("辅助邮箱", "Recovery_Email"),
                new ExcelColumnInfo("辅助邮箱密码", "Recovery_Email_Password"),
                new ExcelColumnInfo("RU_Mail_号", "RU_Mail_Name"),
                new ExcelColumnInfo("RU_Mail_密", "RU_Mail_Pwd"),
                new ExcelColumnInfo("Facebook_Pwd", "Facebook_Pwd"),
                new ExcelColumnInfo("Facebook_CK", "Facebook_CK"),
                new ExcelColumnInfo("UA", "UserAgent"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("2FA密钥", "TwoFA_Dynamic_SecretKey"),
                new ExcelColumnInfo("国家", "GuoJia"),
                new ExcelColumnInfo("注册日期", "ZhuCeRiQi"),
                new ExcelColumnInfo("FSD", "FsdProfile"),
            };
            for (int i = 0; i < excelCols.Count; i++)
            {
                excelCols[i].HeaderIndex = i;
            }

            Type type = typeof(Account_FBOrIns);
            string headers = string.Join("\t", excelCols.Select(c => c.HeaderName));
            string content = string.Empty;
            MemberInfo[] members = new MemberInfo[]
            {
                type.GetProperty("Running_Log"),
                type.GetProperty("Account_Type_Des"),
                type.GetProperty("New_Mail_Name"),
                type.GetProperty("New_Mail_Pwd"),
                type.GetProperty("Recovery_Email"),
                type.GetProperty("Recovery_Email_Password"),
                type.GetProperty("RU_Mail_Name"),
                type.GetProperty("RU_Mail_Pwd"),
                type.GetProperty("Facebook_Pwd"),
                type.GetProperty("Facebook_CK"),
                type.GetProperty("UserAgent"),
                type.GetProperty("TwoFA_Dynamic_StatusDes"),
                type.GetProperty("TwoFA_Dynamic_SecretKey"),
                type.GetProperty("GuoJia"),
                type.GetProperty("FsdProfile"),
            };

            //创建一个新的Excel文件
            using (var package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(sheetName);

                //调整为文本格式
                for (int i = 0; i < excelCols.Count; i++)
                {
                    worksheet.Columns[i + 1].Style.Numberformat.Format = "@";
                }

                ExcelTextFormat excelTextFormat = new ExcelTextFormat();
                excelTextFormat.Delimiter = '\t';
                //表头处理
                worksheet.Cells["A1"].LoadFromText(headers, excelTextFormat);

                #region 粘贴内容

                now = DateTime.Now;
                int ccIndex = 0;
                int realCount = 0;
                int stepCount = 500;
                int stepTimes = Program.setting.Setting_EM.Account_List.Count / stepCount;
                if (Program.setting.Setting_EM.Account_List.Count % stepCount > 0) stepTimes += 1;
                for (int i = 0; i < stepTimes; i++)
                {
                    realCount = i < stepTimes - 1
                        ? stepCount
                        : Program.setting.Setting_EM.Account_List.Count - i * stepCount;

                    content = string.Join("\r\n", Program.setting.Setting_EM.Account_List
                        .GetRange(i * stepCount, realCount).Select(c =>
                        {
                            string lineStr = string.Join("\t", excelCols.Select(ec =>
                            {
                                string cellValue = string.Empty;

                                PropertyInfo propInfo = type.GetProperty(ec.PropertyName);
                                cellValue = propInfo == null || propInfo.GetValue(c, null) == null
                                    ? string.Empty
                                    : propInfo.GetValue(c, null).ToString();
                                if (regex.IsMatch(cellValue) && cellValue.Length > 10) cellValue = $"'{cellValue}";

                                if (ec.PropertyName == "Running_Log" && cellValue.Length > 100)
                                    cellValue = cellValue.Substring(0, 100);

                                return cellValue;
                            }));
                            return lineStr;
                        }));

                    ccIndex = i * stepCount + 2;
                    //在A2单元格粘贴内容
                    worksheet.Cells[$"A{ccIndex}"].LoadFromText(content, excelTextFormat);
                }

                Console.WriteLine($"粘贴内容耗时 > {(DateTime.Now - now).TotalMilliseconds.ToString("N2")} ms");

                #endregion

                //首行加粗
                worksheet.Rows[1].Style.Font.Bold = true;

                //单元格内容重新赋值
                foreach (var cell in worksheet.Cells[2, 1, Program.setting.Setting_EM.Account_List.Count + 1,
                             excelCols.Count])
                {
                    if (cell.Value != null) cell.Value = cell.Value.ToString().Replace("'", "");
                }

                //自动列宽，居中[CK那一列不居中]
                for (int i = 0; i < excelCols.Count; i++)
                {
                    if (worksheet.Cells[1, i + 1].Text == "操作日志")
                    {
                        worksheet.Cells[1, i + 1].Style.HorizontalAlignment =
                            OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Columns[i + 1].Width = 80;
                        continue;
                    }
                    else if (worksheet.Cells[1, i + 1].Text == "Facebook_CK" || worksheet.Cells[1, i + 1].Text == "UA")
                    {
                        worksheet.Cells[1, i + 1].Style.HorizontalAlignment =
                            OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Columns[i + 1].Width = 20;
                        continue;
                    }

                    worksheet.Columns[i + 1].AutoFit();
                    worksheet.Columns[i + 1].Style.HorizontalAlignment =
                        OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                // 保存Excel文件
                package.SaveAs(fileInfo);
            }

            //显示Excel文档
            this.Invoke(new Action(() =>
            {
                StringHelper.OpenFolderAndSelectFiles(fileInfo.Directory.FullName,
                    new string[] { fileInfo.FullName });
            }));

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_EM.Enabled = true;
                Application.DoEvents();
            }));
        }

        //清空账号列表
        private void btn_ClearData_ForBind_EM_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空列表吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;

            this.Invoke(new Action(() => { this.dgv_GMList_ForBind_EM.DataSource = null; }));
            Program.setting.Setting_EM.Mail_ForBind_List = null;
        }

        //标记未使用
        private void tsmi_SetNotUsed_GM_EM_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_EM;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<MailInfo> mails_Selected = iList.Select(i => tasks[i]).ToList();
            if (mails_Selected.Count == 0) return;

            for (int i = 0; i < mails_Selected.Count; i++)
            {
                mails_Selected[i].Is_Used = false;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //标记已使用
        private void tsmi_SetUsed_GM_EM_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_EM;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<MailInfo> mails_Selected = iList.Select(i => tasks[i]).ToList();
            if (mails_Selected.Count == 0) return;

            for (int i = 0; i < mails_Selected.Count; i++)
            {
                mails_Selected[i].Is_Used = true;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //删除
        private void tsmi_Delete_One_GM_EM_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_EM;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_EM.Mail_ForBind_List.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_EM.Mail_ForBind_List != null ||
                Program.setting.Setting_EM.Mail_ForBind_List.Count > 0)
                this.Invoke(new Action(() => { dgv.DataSource = Program.setting.Setting_EM.Mail_ForBind_List; }));
        }

        //删除_全部
        private void tsmi_Delete_All_GM_EM_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_EM;
            if (Program.setting.Setting_EM.Mail_ForBind_List == null ||
                Program.setting.Setting_EM.Mail_ForBind_List.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            Program.setting.Setting_EM.Mail_ForBind_List.Clear();
        }

        //右键弹出菜单
        private void dgv_GMList_ForBind_EM_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.Button == MouseButtons.Right)
            {
                DataGridView dgv = this.dgv_GMList_ForBind_EM;
                //弹出操作菜单
                this.cms_dgv_GM_EM.Show(MousePosition.X, MousePosition.Y);
            }
        }

        private void dgv_GMList_ForBind_EM_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            List<string> fList = files.Where(s => s.ToLower().EndsWith(".txt")).ToList();

            if (fList.Count == 0)
            {
                MessageBox.Show($"导入账号时发生错误：只能导入TXT文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (fList.Count > 1)
            {
                MessageBox.Show($"导入账号时发生错误：每次只能导入1个TXT文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ImportAccount_ForBind_EM(fList[0]);
        }

        //导入账号的具体方法
        private void ImportAccount_ForBind_EM(string fileName)
        {
            string fileContent = File.ReadAllText(fileName);
            List<string> sList = Regex.Split(fileContent, "(\r\n)|\n").Where(s => s.Trim().Length > 0)
                .Select(s => s.Trim()).ToList();

            List<MailInfo> mails = sList.Select(s =>
            {
                // m.RecoveryEmail = arr[2].Trim();
                string[] arr = Regex.Split(s, @"----|\||,|，").ToArray();
                if (arr.Length < 2) return null;

                MailInfo m = new MailInfo();
                m.Mail_Name = arr[0].Trim();
                m.Mail_Pwd = arr[1].Trim();
                if (arr.Length > 2) m.VerifyMail_Name = arr[2].Trim();
                if (arr.Length > 3) m.VerifyMail_Pwd = arr[3].Trim();
                if (arr.Length > 4) m.Is_Used = arr[3].Trim() == "已使用";
                return m;
            }).Where(m => m != null).GroupBy(m => m.Mail_Name).Select(gm => gm.FirstOrDefault()).ToList();

            if (mails.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_GMList_ForBind_IN.DataSource = null; }));

            if (Program.setting.Setting_EM.Mail_ForBind_List == null)
                Program.setting.Setting_EM.Mail_ForBind_List = new List<MailInfo>();
            foreach (MailInfo mail in mails)
            {
                int mIndex =
                    Program.setting.Setting_EM.Mail_ForBind_List.FindIndex(m => m.Mail_Name == mail.Mail_Name);
                if (mIndex == -1) Program.setting.Setting_EM.Mail_ForBind_List.Add(mail);
                else
                {
                    Program.setting.Setting_EM.Mail_ForBind_List[mIndex].Mail_Pwd = mail.Mail_Pwd;
                    Program.setting.Setting_EM.Mail_ForBind_List[mIndex].VerifyMail_Name = mail.VerifyMail_Name;
                    Program.setting.Setting_EM.Mail_ForBind_List[mIndex].VerifyMail_Pwd = mail.VerifyMail_Pwd;
                }
            }

            if (Program.setting.Setting_EM.Mail_ForBind_List != null &&
                Program.setting.Setting_EM.Mail_ForBind_List.Count > 0)
                this.Invoke(new Action(() =>
                {
                    this.dgv_GMList_ForBind_EM.DataSource = Program.setting.Setting_EM.Mail_ForBind_List;
                }));
        }

        private void dgv_GMList_ForBind_EM_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        //清空账号列表
        private void btn_ClearData_EM_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空列表吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;
            this.Invoke(new Action(() => { this.dgv_EM.DataSource = null; }));
            Program.setting.Setting_EM.Account_List = null;
        }

        //拖动导入事件
        private void dgv_MS_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        //拖动导入事件
        private void dgv_MS_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            List<string> fList = files.Where(s => s.ToLower().EndsWith(".xls") || s.ToLower().EndsWith(".xlsx"))
                .ToList();

            if (fList.Count == 0)
            {
                MessageBox.Show($"导入账号时发生错误：只能导入Excel文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (fList.Count > 1)
            {
                MessageBox.Show($"导入账号时发生错误：每次只能导入1个Excel文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ImportAccount_EM(fList[0]);
        }

//导入账号的具体方法
        private void ImportAccount_EM(string fileName)
        {
            string errorMsg = string.Empty;

            FileInfo fi = new FileInfo(fileName);
            ExcelPackage excelPackage = null;
            try
            {
                excelPackage = new ExcelPackage(fi, true);
            }
            catch (Exception ex)
            {
                errorMsg = $"打开文件失败({ex.Message})";
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                MessageBox.Show(errorMsg);
                return;
            }

            if (excelPackage.Workbook.Worksheets.Count == 0)
            {
                errorMsg = $"表格内容不存在";
                MessageBox.Show(errorMsg);
                return;
            }

            ExcelWorksheet sheet = excelPackage.Workbook.Worksheets[0];
            int rowCount = sheet.Dimension.End.Row;
            int colCount = sheet.Dimension.End.Column;

            if (rowCount < 2)
            {
                errorMsg = $"表格行数至少为2行，第一行未表头，第二行开始为内容";
                MessageBox.Show(errorMsg);
                return;
            }

            List<ExcelColumnInfo> excelCols = new List<ExcelColumnInfo>()
            {
                new ExcelColumnInfo("操作日志", "Running_Log"),
                new ExcelColumnInfo("账号分类", "Account_Type_Des"),
                new ExcelColumnInfo("新Mail_号", "New_Mail_Name"),
                new ExcelColumnInfo("新Mail_密", "New_Mail_Pwd"),
                new ExcelColumnInfo("辅助邮箱", "Recovery_Email"),
                new ExcelColumnInfo("RU_Mail_号", "RU_Mail_Name"),
                new ExcelColumnInfo("RU_Mail_密", "RU_Mail_Pwd"),
                new ExcelColumnInfo("Facebook_Pwd", "Facebook_Pwd"),
                new ExcelColumnInfo("Facebook_CK", "Facebook_CK"),
                new ExcelColumnInfo("UA", "UserAgent"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("2FA密钥", "TwoFA_Dynamic_SecretKey"),
                new ExcelColumnInfo("国家", "GuoJia"),
                new ExcelColumnInfo("注册日期", "ZhuCeRiQi"),
                new ExcelColumnInfo("FSD", "FsdProfile"),
            };

            List<int> rList = Enumerable.Range(2, rowCount - 1).ToList();
            List<int> cList = Enumerable.Range(1, colCount).ToList();

            //表头定位
            List<string> sList = cList.Select(c => sheet.Cells[1, c].Text).ToList();
            foreach (var eCol in excelCols)
            {
                eCol.HeaderIndex = sList.FindIndex(s => s.Trim() == eCol.HeaderName);
            }

            //一次读取所有行内容
            rList = Enumerable.Range(2, rowCount - 1).ToList();
            cList = Enumerable.Range(1, colCount).ToList();
            sList = rList.Select(r => string.Join("\t", cList.Select(c => sheet.Cells[r, c].Text))).ToList();

            //每一行内容，创建实例添加到列表中
            List<Account_FBOrIns> accounts = sList.Select(s =>
            {
                string[] cellArr = s.Split('\t');

                Account_FBOrIns account = new Account_FBOrIns();

                int cellValue;
                for (int i = 0; i < excelCols.Count; i++)
                {
                    if (excelCols[i].HeaderIndex < 0 || excelCols[i].HeaderIndex >= cellArr.Length) continue;

                    if (excelCols[i].PropertyName == "TwoFA_Dynamic_StatusDes")
                    {
                        if (cellArr[excelCols[i].HeaderIndex].Trim() == "开") cellValue = 1;
                        else if (cellArr[excelCols[i].HeaderIndex].Trim() == "关") cellValue = 0;
                        else cellValue = -1;
                        this.SetProperty(account, "TwoFA_Dynamic_Status", cellValue);
                    }
                    else this.SetProperty(account, excelCols[i].PropertyName, cellArr[excelCols[i].HeaderIndex].Trim());
                }

                if (string.IsNullOrEmpty(account.Facebook_CK) && string.IsNullOrEmpty(account.Old_Mail_CK))
                    account = null;

                return account;
            }).Where(a => a != null).GroupBy(a =>
            {
                //"name":"c_user","value":"100070943890270"
                string key = StringHelper.GetMidStr(a.Facebook_CK, "\"name\":\"c_user\",\"value\":\"", "\"").Trim();
                return key;
            }).SelectMany(ga =>
            {
                if (!string.IsNullOrEmpty(ga.Key)) return ga.ToList().GetRange(0, 1);
                else return ga.ToList();
            }).ToList();

            //合并更新到表格中
            this.Invoke(new Action(() => { this.dgv_EM.DataSource = null; }));

            Program.setting.Setting_EM.Account_List = accounts;

            if (Program.setting.Setting_EM.Account_List != null && Program.setting.Setting_EM.Account_List.Count > 0)
                this.Invoke(new Action(() => { this.dgv_EM.DataSource = Program.setting.Setting_EM.Account_List; }));
        }

        //DGV画行号
        private void dgv_MS_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == -1)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                using (Brush brush = new SolidBrush(e.CellStyle.ForeColor))
                {
                    e.Graphics.DrawString((e.RowIndex + 1).ToString(), e.CellStyle.Font, brush,
                        e.CellBounds.Location.X + 10, e.CellBounds.Location.Y + 4);
                }

                e.Handled = true;
            }
        }

        //弹出右键菜单
        private void dgv_MS_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;

            if (e.Button == MouseButtons.Right)
            {
                //this.dgv_FB.CurrentCell = this.dgv_FB.Rows[e.RowIndex].Cells[e.ColumnIndex];
                DataGridViewCell cell = this.dgv_EM.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell != null && !cell.Selected) cell.Selected = true;

                this.cms_dgv_EM.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //编辑即时响应
        private void dgv_MS_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (this.dgv_EM.IsCurrentCellDirty) this.dgv_EM.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        //开始操作
        private void tsmi_Start_One_MS_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_MS_Main));
            thread.IsBackground = true;
            thread.Start(1);
        }

        //开始操作_全部
        private void tsmi_Start_All_MS_Click(object sender, EventArgs e)
        {
            this.thread_Main_EM = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_MS_Main));
            this.thread_Main_EM.IsBackground = true;
            this.thread_Main_EM.Start(0);
        }

        //停止操作
        private void tsmi_Stop_One_MS_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_MS(1);
        }

        //停止操作_全部
        private void tsmi_Stop_All_MS_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_MS(0);
        }

        //删除
        private void tsmi_Delete_One_MS_Click(object sender, EventArgs e)
        {
            if (Program.setting.Setting_EM.Account_List == null ||
                Program.setting.Setting_EM.Account_List.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(this.dgv_EM);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_EM.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_EM.Account_List.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_EM.Account_List != null || Program.setting.Setting_EM.Account_List.Count > 0)
                this.Invoke(new Action(() => { this.dgv_EM.DataSource = Program.setting.Setting_EM.Account_List; }));
        }

        //删除_全部
        private void tsmi_Delete_All_MS_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要删除全部吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;

            if (Program.setting.Setting_EM.Account_List == null ||
                Program.setting.Setting_EM.Account_List.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_EM.DataSource = null; }));

            Program.setting.Setting_EM.Account_List.Clear();
        }

        //核心子线程
        private void ThreadMethod_StartTasks_MS_Child(Account_FBOrIns account)
        {
            JObject jo_Result = null;
            bool isSuccess = false;
            string taskName = string.Empty;
            TaskInfo task = null;
            account.Running_Log = "开始操作";
            Thread.Sleep(1000);

            #region 初始化代理/UA

            account.Running_Log = "初始化代理/UA";
            if (string.IsNullOrEmpty(account.UserAgent) || account.WebProxy == null)
            {
                if (string.IsNullOrEmpty(account.UserAgent)) account.UserAgent = StringHelper.CreateRandomUserAgent();
                if (account.WebProxy == null)
                {
                    if (Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_IsUse &&
                        !string.IsNullOrEmpty(Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Url))
                    {
                        account.WebProxy =
                            new System.Net.WebProxy(Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Url);
                        if (!string.IsNullOrEmpty(Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_UserName) &&
                            !string.IsNullOrEmpty(Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Pwd))
                            account.WebProxy.Credentials = new NetworkCredential(
                                Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_UserName,
                                Program.setting.Setting_EM.Global_WebProxyInfo.Proxy_Pwd);
                    }
                }

                Thread.Sleep(1000);
            }

            #endregion

            #region 注册雅虎

            taskName = "RegisterYA";
            task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "注册雅虎";
                jo_Result = this.emailRegisterService.EM_RegisterYaSelenium(account);

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                if (isSuccess)
                {
                    this.Invoke(new Action(() =>
                    {
                        if (Program.setting.Setting_EM.Account_List != null &&
                            Program.setting.Setting_EM.Account_List.Count > 0)
                            this.dgv_EM.DataSource = Program.setting.Setting_EM.Account_List;
                    }));
                }
            }

            #endregion

            #region 注册微软

            taskName = "RegisterMS";
            task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "注册微软";
                jo_Result = this.emailRegisterService.EM_RegisterMSSelenium(account);

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                if (isSuccess)
                {
                    this.Invoke(new Action(() =>
                    {
                        if (Program.setting.Setting_EM.Account_List != null &&
                            Program.setting.Setting_EM.Account_List.Count > 0)
                            this.dgv_EM.DataSource = Program.setting.Setting_EM.Account_List;
                    }));
                }
            }

            #endregion

            #region 注册谷歌

            taskName = "RegisterGoogle";
            task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                MailInfo mailRu = null;

                if (string.IsNullOrEmpty(account.RU_Mail_Name) && string.IsNullOrEmpty(account.RU_Mail_Pwd))
                {
                    mailRu = this.GetNotUsedMailInfoBindEmail_EM();
                    if (mailRu == null)
                    {
                        mailRu = new MailInfo();
                        Random randomEmail = new Random();
                        var firstName = StringHelper.GenerateSurname();
                        var lastName = StringHelper.GenerateSurname();
                        var emailAddress = firstName + lastName;
                        emailAddress += randomEmail.Next(1000, 9999);
                        mailRu.Mail_Name = emailAddress + "@gmailcx.com";
                        mailRu.Mail_Pwd = account.RU_Mail_Pwd;
                    }
                }
                else
                {
                    mailRu = new MailInfo();
                    mailRu.Mail_Name = account.RU_Mail_Name;
                    mailRu.Mail_Pwd = account.RU_Mail_Pwd;
                }

                account.Running_Log = "注册谷歌";
                jo_Result = this.emailRegisterService.EM_RegisterGoogleSelenium(account, mailRu);

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                if (isSuccess)
                {
                    this.Invoke(new Action(() =>
                    {
                        if (Program.setting.Setting_EM.Account_List != null &&
                            Program.setting.Setting_EM.Account_List.Count > 0)
                            this.dgv_EM.DataSource = Program.setting.Setting_EM.Account_List;
                    }));
                }
            }

            #endregion

            #region 注册领英

            taskName = "RegisterIN";
            task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (task.DisplayName.Equals("4 : 注册领英(授权)"))
                {
                    account.IsAuthorization = true;
                }

                account.Running_Log = "注册领英";
                MailInfo mail = null;
                if (string.IsNullOrEmpty(account.New_Mail_Name) && string.IsNullOrEmpty(account.New_Mail_Pwd))
                {
                    mail = this.GetNotUsedMailInfo_EM();
                    if (mail == null)
                    {
                        account.Running_Log = "无可用邮箱";
                        return;
                    }
                }
                else
                {
                    mail.Mail_Name = account.New_Mail_Name;
                    mail.Mail_Pwd = account.New_Mail_Pwd;
                    mail.VerifyMail_Name = account.Recovery_Email;
                }

                jo_Result = this.emailRegisterService.EM_RegisterLinkedinSelenium(account, mail);
                if (!string.IsNullOrEmpty(jo_Result["ErrorMsg"].ToString()))
                {
                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                }

                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                if (isSuccess)
                {
                    //处理邮箱的绑定问题
                    lock (Program.setting.Setting_EM.Lock_Mail_ForBind_List)
                    {
                        mail.IsLocked = true;
                        mail.Is_Used = true;
                    }
                }
            }

            taskName = "RegisterAUIN";
            task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.IsAuthorization = true;
                account.Running_Log = "注册领英";

                MailInfo mail = null;
                if (string.IsNullOrEmpty(account.New_Mail_Name) && string.IsNullOrEmpty(account.New_Mail_Pwd))
                {
                    mail = this.GetNotUsedMailInfo_EM();
                    if (mail == null)
                    {
                        account.Running_Log = "无可用邮箱";
                        return;
                    }
                }
                else
                {
                    mail = new MailInfo();
                    mail.Mail_Name = account.New_Mail_Name;
                    mail.Mail_Pwd = account.New_Mail_Pwd;
                    mail.VerifyMail_Name = account.Recovery_Email;
                }

                jo_Result = this.emailRegisterService.EM_RegisterLinkedinSelenium(account, mail);

                if (!string.IsNullOrEmpty(jo_Result["ErrorMsg"].ToString()))
                {
                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                }

                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                if (isSuccess)
                {
                    //处理邮箱的绑定问题
                    lock (Program.setting.Setting_EM.Lock_Mail_ForBind_List)
                    {
                        mail.IsLocked = true;
                        mail.Is_Used = true;
                    }
                }
            }

            taskName = "RegisterINGoogle";
            task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.IsAuthorization = true;
                account.Running_Log = "注册领英";

                MailInfo mailGoogle = null;
                if (string.IsNullOrEmpty(account.New_Mail_Name) && string.IsNullOrEmpty(account.New_Mail_Pwd))
                {
                    mailGoogle = this.GetNotUsedMailInfo_EM();
                    if (mailGoogle == null)
                    {
                        account.Running_Log = "无可用邮箱";
                        return;
                    }
                }
                else
                {
                    mailGoogle = new MailInfo();
                    mailGoogle.Mail_Name = account.New_Mail_Name;
                    mailGoogle.Mail_Pwd = account.New_Mail_Pwd;
                    mailGoogle.VerifyMail_Name = account.Recovery_Email;
                }

                MailInfo mailRu = null;
                taskName = "RegisterINGoogleBindEmail";
                task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
                if (task != null && task.IsSelected)
                {
                    if (string.IsNullOrEmpty(account.RU_Mail_Name) && string.IsNullOrEmpty(account.RU_Mail_Pwd))
                    {
                        mailRu = this.GetNotUsedMailInfoBindEmail_EM();
                        if (mailRu == null)
                        {
                            account.Running_Log = "无可用邮箱";
                            return;
                        }
                    }
                    else
                    {
                        mailRu = new MailInfo();
                        mailRu.Mail_Name = account.RU_Mail_Name;
                        mailRu.Mail_Pwd = account.RU_Mail_Pwd;
                    }
                }


                jo_Result = this.emailRegisterService.EM_RegisterLinkedinSeleniumByLoginGoolge(account, mailGoogle,
                    mailRu);

                if (!string.IsNullOrEmpty(jo_Result["ErrorMsg"].ToString()))
                {
                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                }

                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                if (isSuccess)
                {
                    //处理邮箱的绑定问题
                    lock (Program.setting.Setting_EM.Lock_Mail_ForBind_List)
                    {
                        mailGoogle.IsLocked = true;
                        mailGoogle.Is_Used = true;
                    }
                }
            }

            taskName = "AddFans";
            task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.IsAuthorization = true;
                account.Running_Log = "增加粉丝";

                jo_Result = this.emailRegisterService.EM_AddFans(account);

                if (!string.IsNullOrEmpty(jo_Result["ErrorMsg"].ToString()))
                {
                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                }
            }

            taskName = "ConfirmFans";
            task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.IsAuthorization = true;
                account.Running_Log = "确认粉丝";

                jo_Result = this.emailRegisterService.EM_ConfirmFans(account);

                if (!string.IsNullOrEmpty(jo_Result["ErrorMsg"].ToString()))
                {
                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                }
            }

            taskName = "BindEmail";
            task = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.IsAuthorization = true;
                account.Running_Log = "绑定邮箱";
                MailInfo mail = null;
                if (string.IsNullOrEmpty(account.RU_Mail_Name) && string.IsNullOrEmpty(account.RU_Mail_Pwd))
                {
                    mail = this.GetNotUsedMailInfoBindEmail_EM();
                    if (mail == null)
                    {
                        account.Running_Log = "无可用邮箱";
                        return;
                    }
                }
                else
                {
                    mail = new MailInfo();
                    mail.Mail_Name = account.RU_Mail_Name;
                    mail.Mail_Pwd = account.RU_Mail_Pwd;
                }

                jo_Result = this.emailRegisterService.EM_BindEmail(account, mail);

                if (!string.IsNullOrEmpty(jo_Result["ErrorMsg"].ToString()))
                {
                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                }
            }

            #endregion
        }

        private void Method_StopTasks_MS(int selectType = 0)
        {
            List<Account_FBOrIns> account_Selected = null;
            if (selectType == 0) account_Selected = Program.setting.Setting_EM.Account_List;
            else
            {
                List<int> iList = null;
                this.Invoke(new Action(() => { iList = this.GetSelectedIndexList(this.dgv_EM); }));
                if (iList.Count == 0)
                {
                    MessageBox.Show("请先选择需要操作的账号");
                    return;
                }

                account_Selected = iList.Select(i => Program.setting.Setting_EM.Account_List[i]).ToList();
            }

            //停止子线程
            for (int i = 0; i < account_Selected.Count; i++)
            {
                if (account_Selected[i].WorkItemsGroup != null) account_Selected[i].WorkItemsGroup.Cancel(true);
                if (account_Selected[i].WorkItemResult != null) account_Selected[i].WorkItemResult.Cancel(true);
                account_Selected[i].Running_Log = "操作中止";
            }

            if (selectType == 0)
            {
                //停止主线程
                if (this.stp_EM != null) this.stp_EM.Cancel(true);
                if (this.thread_Main_EM != null) this.thread_Main_EM.Abort();

                this.SaveSetting_FromUser();
                this.SaveSetting_ToDisk();

                //恢复按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_EM.Enabled = true;
                    this.btn_Start_EM.Enabled = true;
                    this.btn_Stop_EM.Enabled = false;

                    this.tsmi_Start_All_EM.Enabled = true;
                    this.tsmi_Stop_All_EM.Enabled = false;

                    this.tsmi_Delete_One_EM.Enabled = true;
                    this.tsmi_Delete_All_EM.Enabled = true;

                    this.btn_List_Order_EM.Enabled = true;
                }));
            }
        }

        private string Setting_Check_MS()
        {
            string errorMsg = string.Empty;

            int num = 0;
            this.Invoke(new Action(() =>
            {
                if (!int.TryParse(this.txt_ThreadCountMax_EM.Text.Trim(), out num) || num <= 0)
                {
                    errorMsg = $"线程数设置不正确,应填写正整数";
                    return;
                }
            }));

            return errorMsg;
        }

        //启动任务方法
        private void ThreadMethod_StartTasks_MS_Main(object obj_SelectType)
        {
            int selectType = Convert.ToInt32(obj_SelectType);

            string errorMsg = string.Empty;
            //判断是否有现成在运行
            bool isRunning = false;
            if (Program.setting.Setting_EM.Account_List != null)
                isRunning = Program.setting.Setting_EM.Account_List.Where(a => a.Running_IsWorking).Count() > 0;

            if (!isRunning)
            {
                //数据验证
                errorMsg = this.Setting_Check_MS();
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    MessageBox.Show(errorMsg);
                    return;
                }
            }

            List<Account_FBOrIns> account_Selected = null;
            if (selectType == 0)
            {
                List<Account_FBOrIns> list = new List<Account_FBOrIns>();
                for (int i = 0; i < Int32.Parse(this.textBox_email_num_EM.Text); i++)
                {
                    var accountFbOrIns = new Account_FBOrIns();
                    list.Add(accountFbOrIns);
                    if (Program.setting.Setting_EM.Account_List == null)
                        Program.setting.Setting_EM.Account_List = new List<Account_FBOrIns>();
                    Program.setting.Setting_EM.Account_List.Add(accountFbOrIns);
                }

                account_Selected = list;
            }
            else
            {
                List<int> iList = null;
                this.Invoke(new Action(() => { iList = this.GetSelectedIndexList(this.dgv_EM); }));
                if (iList.Count == 0)
                {
                    errorMsg = "请先选择需要操作的账号";
                    MessageBox.Show(errorMsg);
                    return;
                }

                account_Selected = iList.Select(i => Program.setting.Setting_EM.Account_List[i]).ToList();
            }

            if (!isRunning)
            {
                //禁用按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_EM.Enabled = false;
                    this.btn_Start_EM.Enabled = false;
                    this.btn_Stop_EM.Enabled = true;

                    this.tsmi_Start_All_EM.Enabled = false;
                    this.tsmi_Stop_All_EM.Enabled = true;

                    this.tsmi_Delete_One_EM.Enabled = false;
                    this.tsmi_Delete_All_EM.Enabled = false;

                    this.btn_List_Order_EM.Enabled = false;
                }));

                this.SaveSetting_FromUser();
                this.SaveSetting_ToDisk();

                //线程设置
                if (this.stp_EM == null)
                {
                    this.stp_EM = new SmartThreadPool();
                }

                this.stp_EM.Concurrency = Program.setting.Setting_EM.ThreadCountMax;
                //
                // //统计显示
                // this.ShowTongJiInfo_IG(0);
            }

            this.Invoke(new Action(() =>
            {
                if (Program.setting.Setting_EM.Account_List != null &&
                    Program.setting.Setting_EM.Account_List.Count > 0)
                {
                    this.dgv_EM.DataSource = null;
                    this.dgv_EM.DataSource = Program.setting.Setting_EM.Account_List;
                }
            }));
            //派发任务
            for (int i = 0; i < account_Selected.Count; i++)
            {
                account_Selected[i].WorkItemResult =
                    this.stp_EM.QueueWorkItem(this.ThreadMethod_StartTasks_MS_Child, account_Selected[i]);
                Thread.Sleep(50);
                Application.DoEvents();
            }

            //等待任务结束，恢复按钮
            if (!isRunning)
            {
                while (!this.stp_EM.IsIdle)
                {
                    Thread.Sleep(500);
                    Application.DoEvents();
                }

                //恢复按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_EM.Enabled = true;
                    this.btn_Start_EM.Enabled = true;
                    this.btn_Stop_EM.Enabled = false;

                    this.tsmi_Start_All_EM.Enabled = true;
                    this.tsmi_Stop_All_EM.Enabled = false;

                    this.tsmi_Delete_One_EM.Enabled = true;
                    this.tsmi_Delete_All_EM.Enabled = true;

                    this.btn_List_Order_EM.Enabled = true;
                }));
            }
        }

        private void btn_Stop_EM_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_MS(0);
        }

        private void btn_Start_MS_Click(object sender, EventArgs e)
        {
            this.thread_Main_EM = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_MS_Main));
            this.thread_Main_EM.IsBackground = true;
            this.thread_Main_EM.Start(0);
        }

        //查询余额
        private void button_query_sms_EM_Click(object sender, EventArgs e)
        {
        }

        //查询余额
        private void button_ezcaptcha_query_EM_Click(object sender, EventArgs e)
        {
        }

        //查询余额
        private void button_yescaptcha_query_EM_Click(object sender, EventArgs e)
        {
        }

        //查询余额
        private void button_capsolver_query_EM_Click(object sender, EventArgs e)
        {
        }

        //查询余额
        private void button_query_sim_EM_Click(object sender, EventArgs e)
        {
        }

        #endregion

        /*以下代码为FB部分*/

        #region FB数据统计

        //显示新数据到主页
        private void ShowTongJiInfo_FB(int useType = -1)
        {
            if (useType > -1) Program.setting.Setting_FB.TongJi_UseType = useType;

            TongJi_FBOrIns tongJi = Program.setting.Setting_FB.TongJi_UseType == 0
                ? Program.setting.Setting_FB.TongJi_Real
                : Program.setting.Setting_FB.TongJi_False;

            this.Invoke(new Action(() =>
            {
                this.lbl_TongJi_WanChengShu_FB.Text = tongJi.WanChengShu.ToString("D5");
                this.lbl_TongJi_FengHaoShu_FB.Text = tongJi.FengHaoShu.ToString("D5");
                this.lbl_TongJi_WuXiao_FB.Text = tongJi.WuXiao.ToString("D5");
                this.lbl_TongJi_YanZhengYouXiang_FB.Text = tongJi.YanZhengYouXiang.ToString("D5");
                this.lbl_TongJi_YanZhengSheBei_FB.Text = tongJi.YanZhengSheBei.ToString("D5");
                this.lbl_TongJi_QiTaCuoWu_FB.Text = tongJi.QiTaCuoWu.ToString("D5");
            }));
        }

        //真实数据清零
        private void btn_TongJi_Real_Clear_FB_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("重新统计，将清除原来的数据，确定操作吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;
            Program.setting.Setting_FB.TongJi_Real = new TongJi_FBOrIns();
            this.ShowTongJiInfo_FB(0);
        }

        //显示新数据到设置页
        private void ShowTongJiInfo_False_FB(int useType = -1)
        {
            if (useType < 0) useType = 0;
            TongJi_FBOrIns tongJi = useType == 0
                ? Program.setting.Setting_FB.TongJi_Real
                : Program.setting.Setting_FB.TongJi_False;
            this.Invoke(new Action(() =>
            {
                this.txt_TongJi_WanChengShu_FB.Text = tongJi.WanChengShu.ToString();
                this.txt_TongJi_FengHaoShu_FB.Text = tongJi.FengHaoShu.ToString();
                this.txt_TongJi_WuXiao_FB.Text = tongJi.WuXiao.ToString();
                this.txt_TongJi_YanZhengYouXiang_FB.Text = tongJi.YanZhengYouXiang.ToString();
                this.txt_TongJi_YanZhengSheBei_FB.Text = tongJi.YanZhengSheBei.ToString();
                this.txt_TongJi_QiTaCuoWu_FB.Text = tongJi.QiTaCuoWu.ToString();
            }));
        }

        //显示真实数据
        private void btn_TongJi_ShowRealInfo_FB_Click(object sender, EventArgs e)
        {
            lock (Program.setting.Setting_FB.LockObj_TongJi_Real)
            {
                this.ShowTongJiInfo_False_FB(0);
            }
        }

        //显示新数据
        private void btn_TongJi_ShowFalseInfo_FB_Click(object sender, EventArgs e)
        {
            this.ShowTongJiInfo_False_FB(1);
        }

        //清除新数据
        private void btn_TongJi_ClearFalseInfo_FB_Click(object sender, EventArgs e)
        {
            Program.setting.Setting_FB.TongJi_False = new TongJi_FBOrIns();
            this.ShowTongJiInfo_False_FB(1);
        }

        //修改并显示新数据
        private void btn_TongJi_EditAndShowFalseInfo_FB_Click(object sender, EventArgs e)
        {
            string errorMsg = this.TongJiInfo_Check_FB();
            if (!string.IsNullOrEmpty(errorMsg))
            {
                MessageBox.Show(errorMsg);
                return;
            }

            Program.setting.Setting_FB.TongJi_False.WanChengShu = int.Parse(this.txt_TongJi_WanChengShu_FB.Text.Trim());
            Program.setting.Setting_FB.TongJi_False.FengHaoShu = int.Parse(this.txt_TongJi_FengHaoShu_FB.Text.Trim());
            Program.setting.Setting_FB.TongJi_False.WuXiao = int.Parse(this.txt_TongJi_WuXiao_FB.Text.Trim());
            Program.setting.Setting_FB.TongJi_False.YanZhengYouXiang =
                int.Parse(this.txt_TongJi_YanZhengYouXiang_FB.Text.Trim());
            Program.setting.Setting_FB.TongJi_False.YanZhengSheBei =
                int.Parse(this.txt_TongJi_YanZhengSheBei_FB.Text.Trim());
            Program.setting.Setting_FB.TongJi_False.QiTaCuoWu = int.Parse(this.txt_TongJi_QiTaCuoWu_FB.Text.Trim());

            this.ShowTongJiInfo_FB(1);
        }

        //数据检测
        private string TongJiInfo_Check_FB()
        {
            string errorMsg = string.Empty;

            int num;
            this.Invoke(new Action(() =>
            {
                if (!int.TryParse(this.txt_TongJi_WanChengShu_FB.Text.Trim(), out num))
                {
                    errorMsg = "完成数格式不正确";
                    return;
                }

                if (!int.TryParse(this.txt_TongJi_FengHaoShu_FB.Text.Trim(), out num))
                {
                    errorMsg = "封号数格式不正确";
                    return;
                }

                if (!int.TryParse(this.txt_TongJi_WuXiao_FB.Text.Trim(), out num))
                {
                    errorMsg = "Cookie无效数量格式不正确";
                    return;
                }

                if (!int.TryParse(this.txt_TongJi_YanZhengYouXiang_FB.Text.Trim(), out num))
                {
                    errorMsg = "验证其它邮箱数量格式不正确";
                    return;
                }

                if (!int.TryParse(this.txt_TongJi_YanZhengSheBei_FB.Text.Trim(), out num))
                {
                    errorMsg = "验证设备数量格式不正确";
                    return;
                }

                if (!int.TryParse(this.txt_TongJi_QiTaCuoWu_FB.Text.Trim(), out num))
                {
                    errorMsg = "其它错误数量格式不正确";
                    return;
                }
            }));

            return errorMsg;
        }

        #endregion

        #region FB执行流程控制

        //初始化
        private void TaskList_FB_Init()
        {
            List<TaskInfo> tasks = new List<TaskInfo>();
            tasks.Add(new TaskInfo("LoginByCookie", "1 : 检测Cookie", true));
            tasks.Add(new TaskInfo("BindNewEmail", "2 : 绑定新邮箱", true));
            tasks.Add(new TaskInfo("ForgotPassword", "3 : 忘记密码", true));
            tasks.Add(new TaskInfo("OpenTwoFA_Dynamic", "4 : 打开2FA(动态)", true));
            tasks.Add(new TaskInfo("RemoveInsAccount", "5 : 删除Ins关联", true));
            tasks.Add(new TaskInfo("LogOutOfOtherSession", "5 : 删除其它登录会话", true));
            tasks.Add(new TaskInfo("TwoFactorRemoveTrustedDevice", "5 : 删除信任设备", true));
            tasks.Add(new TaskInfo("DeleteOtherContacts", "5 : 删除联系方式", true));
            tasks.Add(new TaskInfo("Query_RecoveryCodes", "6 : 查辅助词", true));
            tasks.Add(new TaskInfo("Query_Country_RegTime", "6 : 查国家|注册日期", true));
            tasks.Add(new TaskInfo("Query_Apps_Websites", "6 : APP第三方网站授权", true));
            tasks.Add(new TaskInfo("Query_Birthday_Gender_Friends", "6 : 查生日|性别|好友", true));
            tasks.Add(new TaskInfo("Query_Posts", "6 : 查帖子", true));
            tasks.Add(new TaskInfo("Query_AdAccount_Pages", "6 : 查商城,专页", true));
            tasks.Add(new TaskInfo("Query_AdStatus_ZhangDan_YuE", "6 : 查权限,账单,余额", true));

            if (Program.setting.Setting_FB.TaskInfoList == null)
                Program.setting.Setting_FB.TaskInfoList = new List<TaskInfo>();

            for (int i = 0; i < tasks.Count; i++)
            {
                TaskInfo tFind = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == tasks[i].TaskName)
                    .FirstOrDefault();
                if (tFind != null) tasks[i].IsSelected = tFind.IsSelected;
            }

            Program.setting.Setting_FB.TaskInfoList = tasks;

            this.Invoke(new Action(() =>
            {
                this.dgv_TaskList_FB.DataSource = null;
                if (Program.setting.Setting_FB.TaskInfoList != null &&
                    Program.setting.Setting_FB.TaskInfoList.Count > 0)
                    this.dgv_TaskList_FB.DataSource = Program.setting.Setting_FB.TaskInfoList;
            }));
        }

        //右键弹出菜单
        private void dgv_TaskList_FB_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.Button == MouseButtons.Right)
            {
                DataGridView dgv = this.dgv_TaskList_FB;
                //弹出操作菜单
                this.cms_dgv_TaskList_FB.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //选中
        private void tsmi_Select_TaskList_FB_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_FB;
            List<TaskInfo> tasks = (List<TaskInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<TaskInfo> tasks_Selected = iList.Select(i => tasks[i]).ToList();
            if (tasks_Selected.Count == 0) return;

            for (int i = 0; i < tasks_Selected.Count; i++)
            {
                tasks_Selected[i].IsSelected = true;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //选中_全部
        private void tsmi_Select_All_TaskList_FB_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_FB;
            List<TaskInfo> tasks_Selected = Program.setting.Setting_FB.TaskInfoList;
            if (tasks_Selected.Count == 0) return;

            for (int i = 0; i < tasks_Selected.Count; i++)
            {
                tasks_Selected[i].IsSelected = true;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //取消选中
        private void tsmi_NotSelect_TaskList_FB_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_FB;
            List<TaskInfo> tasks = (List<TaskInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<TaskInfo> tasks_Selected = iList.Select(i => tasks[i]).ToList();
            if (tasks_Selected.Count == 0) return;

            for (int i = 0; i < tasks_Selected.Count; i++)
            {
                tasks_Selected[i].IsSelected = false;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //取消选中_全部
        private void tsmi_NotSelect_All_TaskList_FB_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_FB;
            List<TaskInfo> tasks_Selected = Program.setting.Setting_FB.TaskInfoList;
            if (tasks_Selected.Count == 0) return;

            for (int i = 0; i < tasks_Selected.Count; i++)
            {
                tasks_Selected[i].IsSelected = false;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //删除
        private void tsmi_Delete_TaskList_FB_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_FB;
            List<TaskInfo> tasks = (List<TaskInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_FB.TaskInfoList.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_FB.TaskInfoList != null || Program.setting.Setting_FB.TaskInfoList.Count > 0)
                this.Invoke(new Action(() => { dgv.DataSource = Program.setting.Setting_FB.TaskInfoList; }));
        }

        //删除_全部
        private void tsmi_Delete_All_TaskList_FB_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_FB;
            if (Program.setting.Setting_FB.TaskInfoList == null ||
                Program.setting.Setting_FB.TaskInfoList.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            Program.setting.Setting_FB.TaskInfoList.Clear();
        }

        //编辑即时响应
        private void dgv_TaskList_FB_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_FB;
            if (dgv.IsCurrentCellDirty) dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        #endregion

        #region FB账号导入

        //清空账号列表
        private void btn_ClearData_FB_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空列表吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;
            this.Invoke(new Action(() => { this.dgv_FB.DataSource = null; }));
            Program.setting.Setting_FB.Account_List = null;
        }

        //获取账号模板
        private void btn_GetExcelModelFile_FB_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            // 设置保存对话框的标题
            saveFileDialog.Title = "保存导入账号模版";

            // 设置默认的文件名
            saveFileDialog.FileName = "导入账号模版_FB.xlsx";

            // 设置默认的文件类型筛选
            saveFileDialog.Filter = "Excel文档 (*.xlsx)|*.xlsx";

            // 设置默认的文件类型索引
            saveFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            saveFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 将文本保存到文件
                    File.WriteAllBytes(saveFileDialog.FileName, Properties.Resources.导入账号模版_FB);

                    StringHelper.OpenFolderAndSelectFiles(new FileInfo(saveFileDialog.FileName).Directory.FullName,
                        saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //导入账号的具体方法
        private void ImportAccount_FB(string fileName)
        {
            string errorMsg = string.Empty;

            FileInfo fi = new FileInfo(fileName);
            ExcelPackage excelPackage = null;
            try
            {
                excelPackage = new ExcelPackage(fi, true);
            }
            catch (Exception ex)
            {
                errorMsg = $"打开文件失败({ex.Message})";
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                MessageBox.Show(errorMsg);
                return;
            }

            if (excelPackage.Workbook.Worksheets.Count == 0)
            {
                errorMsg = $"表格内容不存在";
                MessageBox.Show(errorMsg);
                return;
            }

            ExcelWorksheet sheet = excelPackage.Workbook.Worksheets[0];
            int rowCount = sheet.Dimension.End.Row;
            int colCount = sheet.Dimension.End.Column;

            if (rowCount < 2)
            {
                errorMsg = $"表格行数至少为2行，第一行未表头，第二行开始为内容";
                MessageBox.Show(errorMsg);
                return;
            }

            List<ExcelColumnInfo> excelCols = new List<ExcelColumnInfo>()
            {
                new ExcelColumnInfo("操作日志", "Running_Log"),
                new ExcelColumnInfo("账号分类", "Account_Type_Des"),
                new ExcelColumnInfo("原Mail_CK", "Old_Mail_CK"),
                new ExcelColumnInfo("原Mail_号", "Old_Mail_Name"),
                new ExcelColumnInfo("新Mail_号", "New_Mail_Name"),
                new ExcelColumnInfo("新Mail_密", "New_Mail_Pwd"),
                new ExcelColumnInfo("Facebook_Pwd", "Facebook_Pwd"),
                new ExcelColumnInfo("Facebook_CK", "Facebook_CK"),
                new ExcelColumnInfo("UA", "UserAgent"),
                new ExcelColumnInfo("C_User", "C_User"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("2FA密钥", "TwoFA_Dynamic_SecretKey"),
                new ExcelColumnInfo("删除Ins关联", "Log_RemoveInsAccount"),
                new ExcelColumnInfo("删除其它登录会话", "Log_LogOutOfOtherSession"),
                new ExcelColumnInfo("删除信任设备", "Log_TwoFactorRemoveTrustedDevice"),
                new ExcelColumnInfo("删除联系方式", "Log_DeleteOtherContacts"),
                new ExcelColumnInfo("辅助词", "FuZhuCi"),
                new ExcelColumnInfo("国家", "GuoJia"),
                new ExcelColumnInfo("注册日期", "ZhuCeRiQi"),
                new ExcelColumnInfo("生日", "ShengRi"),
                new ExcelColumnInfo("性别", "XingBie"),
                new ExcelColumnInfo("好友", "HaoYouCount"),
                new ExcelColumnInfo("帖子", "TieZiCount"),
                new ExcelColumnInfo("商城", "ShangCheng"),
                new ExcelColumnInfo("专页", "ZhuanYe"),
                new ExcelColumnInfo("权限", "AdQuanXian"),
                new ExcelColumnInfo("账单", "ZhangDan"),
                new ExcelColumnInfo("余额", "YuE"),
            };

            List<int> rList = Enumerable.Range(2, rowCount - 1).ToList();
            List<int> cList = Enumerable.Range(1, colCount).ToList();

            //表头定位
            List<string> sList = cList.Select(c => sheet.Cells[1, c].Text).ToList();
            foreach (var eCol in excelCols)
            {
                eCol.HeaderIndex = sList.FindIndex(s => s.Trim() == eCol.HeaderName);
            }

            //一次读取所有行内容
            rList = Enumerable.Range(2, rowCount - 1).ToList();
            cList = Enumerable.Range(1, colCount).ToList();
            sList = rList.Select(r => string.Join("\t", cList.Select(c => sheet.Cells[r, c].Text))).ToList();

            //每一行内容，创建实例添加到列表中
            List<Account_FBOrIns> accounts = sList.Select(s =>
            {
                string[] cellArr = s.Split('\t');

                Account_FBOrIns account = new Account_FBOrIns();

                int cellValue;
                for (int i = 0; i < excelCols.Count; i++)
                {
                    if (excelCols[i].HeaderIndex < 0 || excelCols[i].HeaderIndex >= cellArr.Length) continue;

                    if (excelCols[i].PropertyName == "TwoFA_Dynamic_StatusDes")
                    {
                        if (cellArr[excelCols[i].HeaderIndex].Trim() == "开") cellValue = 1;
                        else if (cellArr[excelCols[i].HeaderIndex].Trim() == "关") cellValue = 0;
                        else cellValue = -1;
                        this.SetProperty(account, "TwoFA_Dynamic_Status", cellValue);
                    }
                    else if (excelCols[i].PropertyName == "C_User")
                    {
                        if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
                        account.LoginInfo.LoginData_Account_Id = cellArr[excelCols[i].HeaderIndex].Trim();
                    }
                    else this.SetProperty(account, excelCols[i].PropertyName, cellArr[excelCols[i].HeaderIndex].Trim());
                }

                if (string.IsNullOrEmpty(account.Facebook_CK) && string.IsNullOrEmpty(account.Old_Mail_CK))
                    account = null;

                return account;
            }).Where(a => a != null).GroupBy(a =>
            {
                //"name":"c_user","value":"100070943890270"
                string key = StringHelper.GetMidStr(a.Facebook_CK, "\"name\":\"c_user\",\"value\":\"", "\"").Trim();
                return key;
            }).SelectMany(ga =>
            {
                if (!string.IsNullOrEmpty(ga.Key)) return ga.ToList().GetRange(0, 1);
                else return ga.ToList();
            }).ToList();

            //合并更新到表格中
            this.Invoke(new Action(() => { this.dgv_FB.DataSource = null; }));

            Program.setting.Setting_FB.Account_List = accounts;

            if (Program.setting.Setting_FB.Account_List != null && Program.setting.Setting_FB.Account_List.Count > 0)
                this.Invoke(new Action(() => { this.dgv_FB.DataSource = Program.setting.Setting_FB.Account_List; }));
        }

        //导入账号
        private void btn_ImportAccount_FB_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开Excel文档";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "Excel文档 (*.xls;*.xlsx)|*.xls;*.xlsx";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 获取用户选择的文件名
                    string filePath = openFileDialog.FileName;
                    this.ImportAccount_FB(filePath);
                    //this.ImportAccount_FB_SP(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //拖动导入事件
        private void dgv_FB_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        //拖动导入事件
        private void dgv_FB_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            List<string> fList = files.Where(s => s.ToLower().EndsWith(".xls") || s.ToLower().EndsWith(".xlsx"))
                .ToList();

            if (fList.Count == 0)
            {
                MessageBox.Show($"导入账号时发生错误：只能导入Excel文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (fList.Count > 1)
            {
                MessageBox.Show($"导入账号时发生错误：每次只能导入1个Excel文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ImportAccount_FB(fList[0]);
            //this.ImportAccount_FB_SP(fList[0]);
        }

        #endregion

        #region 用于FB绑定的邮箱账号导入和导出

        //导入账号的具体方法
        private void ImportAccount_ForBind_FB(string fileName)
        {
            string fileContent = File.ReadAllText(fileName);
            List<string> sList = Regex.Split(fileContent, "(\r\n)|\n").Where(s => s.Trim().Length > 0)
                .Select(s => s.Trim()).ToList();

            List<MailInfo> mails = sList.Select(s =>
            {
                string[] arr = Regex.Split(s, @"----|\||,|，").ToArray();
                if (arr.Length < 2) return null;

                MailInfo m = new MailInfo();
                m.Mail_Name = arr[0].Trim();
                m.Mail_Pwd = arr[1].Trim();
                if (arr.Length > 2) m.VerifyMail_Name = arr[2].Trim();
                if (arr.Length > 3) m.Is_Used = arr[3].Trim() == "已使用";
                return m;
            }).Where(m => m != null).GroupBy(m => m.Mail_Name).Select(gm => gm.FirstOrDefault()).ToList();

            if (mails.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_GMList_ForBind_FB.DataSource = null; }));

            if (Program.setting.Setting_FB.Mail_ForBind_List == null)
                Program.setting.Setting_FB.Mail_ForBind_List = new List<MailInfo>();
            foreach (MailInfo mail in mails)
            {
                int mIndex = Program.setting.Setting_FB.Mail_ForBind_List.FindIndex(m => m.Mail_Name == mail.Mail_Name);
                if (mIndex == -1) Program.setting.Setting_FB.Mail_ForBind_List.Add(mail);
                else
                {
                    Program.setting.Setting_FB.Mail_ForBind_List[mIndex].Mail_Pwd = mail.Mail_Pwd;
                    Program.setting.Setting_FB.Mail_ForBind_List[mIndex].VerifyMail_Name = mail.VerifyMail_Name;
                }
            }

            if (Program.setting.Setting_FB.Mail_ForBind_List != null &&
                Program.setting.Setting_FB.Mail_ForBind_List.Count > 0)
                this.Invoke(new Action(() =>
                {
                    this.dgv_GMList_ForBind_FB.DataSource = Program.setting.Setting_FB.Mail_ForBind_List;
                }));
        }

        private void btn_ImportAccount_ForBind_FB_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开TXT文档";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "TXT文档 (*.txt)|*.txt";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 获取用户选择的文件名
                    string filePath = openFileDialog.FileName;
                    this.ImportAccount_ForBind_FB(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btn_ClearData_ForBind_FB_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空列表吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;

            this.Invoke(new Action(() => { this.dgv_GMList_ForBind_FB.DataSource = null; }));
            Program.setting.Setting_FB.Mail_ForBind_List = null;
        }

        //导出TXT文档_具体方法
        private void ExportTXT_GM_FB()
        {
            if (Program.setting.Setting_FB.Mail_ForBind_List == null ||
                Program.setting.Setting_FB.Mail_ForBind_List.Count == 0)
            {
                MessageBox.Show("No Data!");
                return;
            }

            DateTime now = DateTime.Now;
            string sheetName = $"Facebook_MailList_{now.ToString("yyyyMMdd_HHmmss")}";

            FileInfo fileInfo = null;
            this.Invoke(new Action(() =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "TXT文档（*.txt）|*.txt";
                sfd.FilterIndex = 1;
                sfd.InitialDirectory = Application.StartupPath;
                sfd.RestoreDirectory = true;
                sfd.FileName = $"{sheetName}.txt";
                if (sfd.ShowDialog() != DialogResult.OK) return;
                fileInfo = new FileInfo(sfd.FileName);
            }));
            if (fileInfo == null) return;

            //禁用按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_GM_FB.Enabled = false;
                Application.DoEvents();
            }));

            List<MailInfo> mails_NotUsed = Program.setting.Setting_FB.Mail_ForBind_List.Where(m => !m.Is_Used).ToList();
            List<MailInfo> mails_Used = Program.setting.Setting_FB.Mail_ForBind_List.Where(m => m.Is_Used).ToList();
            List<MailInfo> mails_All = mails_Used.Union(mails_NotUsed).ToList();

            string fileContent = string.Join("\r\n",
                mails_All.Select(m =>
                    $"{m.Mail_Name}----{m.Mail_Pwd}----{m.VerifyMail_Name}{(m.Is_Used ? $"----{m.Is_Used_Des}" : string.Empty)}"));

            //写出文档
            File.WriteAllText(fileInfo.FullName, fileContent);

            //显示Excel文档
            this.Invoke(new Action(() =>
            {
                StringHelper.OpenFolderAndSelectFiles(fileInfo.Directory.FullName,
                    new string[] { fileInfo.FullName });
            }));

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_GM_FB.Enabled = true;
                Application.DoEvents();
            }));
        }

        private void btn_Export_List_GM_FB_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(this.ExportTXT_GM_FB);
        }

        private void dgv_GMList_ForBind_FB_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            List<string> fList = files.Where(s => s.ToLower().EndsWith(".txt")).ToList();

            if (fList.Count == 0)
            {
                MessageBox.Show($"导入账号时发生错误：只能导入TXT文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (fList.Count > 1)
            {
                MessageBox.Show($"导入账号时发生错误：每次只能导入1个TXT文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ImportAccount_ForBind_FB(fList[0]);
        }

        private void dgv_GMList_ForBind_FB_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        #endregion

        #region 用于FB绑定新邮箱的右键菜单

        //右键弹出菜单
        private void dgv_GMList_ForBind_FB_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.Button == MouseButtons.Right)
            {
                DataGridView dgv = this.dgv_GMList_ForBind_FB;
                //弹出操作菜单
                this.cms_dgv_GM_FB.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //标记未使用
        private void tsmi_SetNotUsed_GM_FB_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_FB;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<MailInfo> mails_Selected = iList.Select(i => tasks[i]).ToList();
            if (mails_Selected.Count == 0) return;

            for (int i = 0; i < mails_Selected.Count; i++)
            {
                mails_Selected[i].Is_Used = false;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //标记已使用
        private void tsmi_SetUsed_GM_FB_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_FB;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<MailInfo> mails_Selected = iList.Select(i => tasks[i]).ToList();
            if (mails_Selected.Count == 0) return;

            for (int i = 0; i < mails_Selected.Count; i++)
            {
                mails_Selected[i].Is_Used = true;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //删除
        private void tsmi_Delete_One_GM_FB_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_FB;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_FB.Mail_ForBind_List.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_FB.Mail_ForBind_List != null ||
                Program.setting.Setting_FB.Mail_ForBind_List.Count > 0)
                this.Invoke(new Action(() => { dgv.DataSource = Program.setting.Setting_FB.Mail_ForBind_List; }));
        }

        //删除_全部
        private void tsmi_Delete_All_GM_FB_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_FB;
            if (Program.setting.Setting_FB.Mail_ForBind_List == null ||
                Program.setting.Setting_FB.Mail_ForBind_List.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            Program.setting.Setting_FB.Mail_ForBind_List.Clear();
        }

        #endregion

        #region dgv_FB事件和右键菜单（导出和分类）

        //DGV画行号
        private void dgv_FB_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == -1)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                using (Brush brush = new SolidBrush(e.CellStyle.ForeColor))
                {
                    e.Graphics.DrawString((e.RowIndex + 1).ToString(), e.CellStyle.Font, brush,
                        e.CellBounds.Location.X + 10, e.CellBounds.Location.Y + 4);
                }

                e.Handled = true;
            }
        }

        //弹出右键菜单
        private void dgv_FB_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;

            if (e.Button == MouseButtons.Right)
            {
                //this.dgv_FB.CurrentCell = this.dgv_FB.Rows[e.RowIndex].Cells[e.ColumnIndex];
                DataGridViewCell cell = this.dgv_FB.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell != null && !cell.Selected) cell.Selected = true;

                this.cms_dgv_FB.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //编辑即时响应
        private void dgv_FB_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (this.dgv_FB.IsCurrentCellDirty) this.dgv_FB.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        //开始操作
        private void tsmi_Start_One_FB_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_FB_Main));
            thread.IsBackground = true;
            thread.Start(1);
        }

        //开始操作_全部
        private void tsmi_Start_All_FB_Click(object sender, EventArgs e)
        {
            this.thread_Main_FB = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_FB_Main));
            this.thread_Main_FB.IsBackground = true;
            this.thread_Main_FB.Start(0);
        }

        //停止操作
        private void tsmi_Stop_One_FB_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_FB(1);
        }

        //停止操作_全部
        private void tsmi_Stop_All_FB_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_FB(0);
        }

        //删除
        private void tsmi_Delete_One_FB_Click(object sender, EventArgs e)
        {
            if (Program.setting.Setting_FB.Account_List == null ||
                Program.setting.Setting_FB.Account_List.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(this.dgv_FB);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_FB.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_FB.Account_List.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_FB.Account_List != null || Program.setting.Setting_FB.Account_List.Count > 0)
                this.Invoke(new Action(() => { this.dgv_FB.DataSource = Program.setting.Setting_FB.Account_List; }));
        }

        //删除_全部
        private void tsmi_Delete_All_FB_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要删除全部吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;

            if (Program.setting.Setting_FB.Account_List == null ||
                Program.setting.Setting_FB.Account_List.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_FB.DataSource = null; }));

            Program.setting.Setting_FB.Account_List.Clear();
        }

        //导出Excel_具体方法
        private void ExportExcel_FB()
        {
            if (Program.setting.Setting_FB.Account_List == null || Program.setting.Setting_FB.Account_List.Count == 0)
            {
                MessageBox.Show("No Data!");
                return;
            }

            DateTime now = DateTime.Now;
            string sheetName = $"Facebook_{now.ToString("yyyyMMdd_HHmmss")}";
            Regex regex = new Regex(@"^\d+$");

            FileInfo fileInfo = null;
            this.Invoke(new Action(() =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Excel文档（*.xlsx）|*.xlsx";
                sfd.FilterIndex = 1;
                sfd.InitialDirectory = Application.StartupPath;
                sfd.RestoreDirectory = true;
                sfd.FileName = $"{sheetName}.xlsx";
                if (sfd.ShowDialog() != DialogResult.OK) return;
                fileInfo = new FileInfo(sfd.FileName);
            }));
            if (fileInfo == null) return;

            //禁用按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_FB.Enabled = false;
                Application.DoEvents();
            }));

            //整理表头信息
            List<ExcelColumnInfo> excelCols = new List<ExcelColumnInfo>()
            {
                new ExcelColumnInfo("操作日志", "Running_Log"),
                new ExcelColumnInfo("账号分类", "Account_Type_Des"),
                new ExcelColumnInfo("原Mail_CK", "Old_Mail_CK"),
                new ExcelColumnInfo("原Mail_号", "Old_Mail_Name"),
                new ExcelColumnInfo("新Mail_号", "New_Mail_Name"),
                new ExcelColumnInfo("新Mail_密", "New_Mail_Pwd"),
                new ExcelColumnInfo("Facebook_Pwd", "Facebook_Pwd"),
                new ExcelColumnInfo("Facebook_CK", "Facebook_CK"),
                new ExcelColumnInfo("UA", "UserAgent"),
                new ExcelColumnInfo("C_User", "C_User"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("2FA密钥", "TwoFA_Dynamic_SecretKey"),
                new ExcelColumnInfo("删除Ins关联", "Log_RemoveInsAccount"),
                new ExcelColumnInfo("删除其它登录会话", "Log_LogOutOfOtherSession"),
                new ExcelColumnInfo("删除信任设备", "Log_TwoFactorRemoveTrustedDevice"),
                new ExcelColumnInfo("删除联系方式", "Log_DeleteOtherContacts"),
                new ExcelColumnInfo("辅助词", "FuZhuCi"),
                new ExcelColumnInfo("国家", "GuoJia"),
                new ExcelColumnInfo("注册日期", "ZhuCeRiQi"),
                new ExcelColumnInfo("生日", "ShengRi"),
                new ExcelColumnInfo("性别", "XingBie"),
                new ExcelColumnInfo("好友", "HaoYouCount"),
                new ExcelColumnInfo("帖子", "TieZiCount"),
                new ExcelColumnInfo("商城", "ShangCheng"),
                new ExcelColumnInfo("专页", "ZhuanYe"),
                new ExcelColumnInfo("权限", "AdQuanXian"),
                new ExcelColumnInfo("账单", "ZhangDan"),
                new ExcelColumnInfo("余额", "YuE"),
            };
            for (int i = 0; i < excelCols.Count; i++)
            {
                excelCols[i].HeaderIndex = i;
            }

            Type type = typeof(Account_FBOrIns);
            string headers = string.Join("\t", excelCols.Select(c => c.HeaderName));
            string content = string.Empty;
            MemberInfo[] members = new MemberInfo[]
            {
                type.GetProperty("Running_Log"),
                type.GetProperty("Account_Type_Des"),
                type.GetProperty("Old_Mail_CK"),
                type.GetProperty("Old_Mail_Name"),
                type.GetProperty("New_Mail_Name"),
                type.GetProperty("New_Mail_Pwd"),
                type.GetProperty("Facebook_Pwd"),
                type.GetProperty("Facebook_CK"),
                type.GetProperty("UserAgent"),
                type.GetProperty("C_User"),
                type.GetProperty("TwoFA_Dynamic_StatusDes"),
                type.GetProperty("TwoFA_Dynamic_SecretKey"),
                type.GetProperty("Log_RemoveInsAccount"),
                type.GetProperty("Log_LogOutOfOtherSession"),
                type.GetProperty("Log_TwoFactorRemoveTrustedDevice"),
                type.GetProperty("Log_DeleteOtherContacts"),
                type.GetProperty("FuZhuCi"),
                type.GetProperty("GuoJia"),
                type.GetProperty("ZhuCeRiQi"),
                type.GetProperty("ShengRi"),
                type.GetProperty("XingBie"),
                type.GetProperty("HaoYouCount"),
                type.GetProperty("TieZiCount"),
                type.GetProperty("ShangCheng"),
                type.GetProperty("ZhuanYe"),
                type.GetProperty("AdQuanXian"),
                type.GetProperty("ZhangDan"),
                type.GetProperty("YuE"),
            };

            //创建一个新的Excel文件
            using (var package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(sheetName);

                //调整为文本格式
                for (int i = 0; i < excelCols.Count; i++)
                {
                    worksheet.Columns[i + 1].Style.Numberformat.Format = "@";
                }

                ExcelTextFormat excelTextFormat = new ExcelTextFormat();
                excelTextFormat.Delimiter = '\t';
                //表头处理
                worksheet.Cells["A1"].LoadFromText(headers, excelTextFormat);

                #region 粘贴内容

                now = DateTime.Now;
                int ccIndex = 0;
                int realCount = 0;
                int stepCount = 500;
                int stepTimes = Program.setting.Setting_FB.Account_List.Count / stepCount;
                if (Program.setting.Setting_FB.Account_List.Count % stepCount > 0) stepTimes += 1;
                for (int i = 0; i < stepTimes; i++)
                {
                    realCount = i < stepTimes - 1
                        ? stepCount
                        : Program.setting.Setting_FB.Account_List.Count - i * stepCount;

                    content = string.Join("\r\n", Program.setting.Setting_FB.Account_List
                        .GetRange(i * stepCount, realCount).Select(c =>
                        {
                            string lineStr = string.Join("\t", excelCols.Select(ec =>
                            {
                                string cellValue = string.Empty;

                                PropertyInfo propInfo = type.GetProperty(ec.PropertyName);
                                cellValue = propInfo == null || propInfo.GetValue(c, null) == null
                                    ? string.Empty
                                    : propInfo.GetValue(c, null).ToString();
                                if (regex.IsMatch(cellValue) && cellValue.Length > 10) cellValue = $"'{cellValue}";

                                if (ec.PropertyName == "Running_Log" && cellValue.Length > 100)
                                    cellValue = cellValue.Substring(0, 100);

                                return cellValue;
                            }));
                            return lineStr;
                        }));

                    ccIndex = i * stepCount + 2;
                    //在A2单元格粘贴内容
                    worksheet.Cells[$"A{ccIndex}"].LoadFromText(content, excelTextFormat);
                }

                Console.WriteLine($"粘贴内容耗时 > {(DateTime.Now - now).TotalMilliseconds.ToString("N2")} ms");

                #endregion

                //首行加粗
                worksheet.Rows[1].Style.Font.Bold = true;

                //单元格内容重新赋值
                foreach (var cell in worksheet.Cells[2, 1, Program.setting.Setting_FB.Account_List.Count + 1,
                             excelCols.Count])
                {
                    if (cell.Value != null) cell.Value = cell.Value.ToString().Replace("'", "");
                }

                //自动列宽，居中[CK那一列不居中]
                for (int i = 0; i < excelCols.Count; i++)
                {
                    if (worksheet.Cells[1, i + 1].Text == "操作日志")
                    {
                        worksheet.Cells[1, i + 1].Style.HorizontalAlignment =
                            OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Columns[i + 1].Width = 80;
                        continue;
                    }
                    else if (worksheet.Cells[1, i + 1].Text == "Facebook_CK" || worksheet.Cells[1, i + 1].Text == "UA")
                    {
                        worksheet.Cells[1, i + 1].Style.HorizontalAlignment =
                            OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Columns[i + 1].Width = 20;
                        continue;
                    }

                    worksheet.Columns[i + 1].AutoFit();
                    worksheet.Columns[i + 1].Style.HorizontalAlignment =
                        OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                // 保存Excel文件
                package.SaveAs(fileInfo);
            }

            //显示Excel文档
            this.Invoke(new Action(() =>
            {
                StringHelper.OpenFolderAndSelectFiles(fileInfo.Directory.FullName,
                    new string[] { fileInfo.FullName });
            }));

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_FB.Enabled = true;
                Application.DoEvents();
            }));
        }

        //导出账号
        private void btn_Export_List_FB_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(this.ExportExcel_FB);
        }


        //分类排序
        private void ThreadMethod_List_Order_FB()
        {
            this.Invoke(new Action(() => { this.btn_List_Order_FB.Enabled = false; }));

            DataGridView dgv = null;
            this.Invoke(new Action(() =>
            {
                dgv = this.dgv_FB;
                dgv.DataSource = null;
            }));

            Program.setting.Setting_FB.Account_List.ForEach(a =>
            {
                if (!string.IsNullOrEmpty(a.TwoFA_Dynamic_SecretKey) && string.IsNullOrEmpty(a.Account_Type_Des))
                    a.Account_Type_Des = "已完成";
            });

            var orderbyList = Program.setting.Setting_FB.Account_List.OrderByDescending(a =>
            {
                int oNum = 0;
                if (a.Account_Type_Des == "已完成") oNum = 20;
                else if (a.Account_Type_Des == "账户被锁定") oNum = 19;
                else if (a.Account_Type_Des == "Cookie无效") oNum = 18;
                else if (a.Account_Type_Des == "验证其它邮箱") oNum = 17;
                else if (a.Account_Type_Des == "需要设备验证") oNum = 16;
                else if (a.Account_Type_Des == "其它错误") oNum = 15;
                return oNum;
            });

            Program.setting.Setting_FB.Account_List = orderbyList.ToList();

            this.Invoke(new Action(() => { dgv.DataSource = Program.setting.Setting_FB.Account_List; }));

            this.Invoke(new Action(() => { this.btn_List_Order_FB.Enabled = true; }));
        }

        //分类排序
        private void btn_List_Order_FB_Click(object sender, EventArgs e)
        {
            if (Program.setting.Setting_FB.Account_List == null ||
                Program.setting.Setting_FB.Account_List.Count == 0) return;

            Thread thread = new Thread(this.ThreadMethod_List_Order_FB);
            thread.IsBackground = true;
            thread.Start();
        }

        #endregion

        #region FB核心功能

        private FacebookService facebookService = new FacebookService();
        private Thread thread_Main_FB = null;
        private SmartThreadPool stp_FB = null;

        private string Setting_Check_FB()
        {
            string errorMsg = string.Empty;

            if (Program.setting.Setting_FB.Account_List == null || Program.setting.Setting_FB.Account_List.Count == 0)
            {
                errorMsg = $"请先导入账号";
                return errorMsg;
            }

            int num = 0;
            this.Invoke(new Action(() =>
            {
                if (!int.TryParse(this.txt_ThreadCountMax_FB.Text.Trim(), out num) || num <= 0)
                {
                    errorMsg = $"线程数设置不正确,应填写正整数";
                    return;
                }

                if (this.cb_Global_WebProxyInfo_FB.Checked)
                {
                    if (this.txt_Global_WebProxyInfo_FB_IPAddress.Text.Trim().Length == 0)
                    {
                        errorMsg = $"要开启全局代理，必须填写代理地址";
                        return;
                    }
                }

                if (!this.rb_ForgotPwdSetting_Front_Custom_FB.Checked &&
                    !this.rb_ForgotPwdSetting_Front_Random_FB.Checked)
                {
                    errorMsg = $"请先设置忘记密码时设定密码的模式";
                    return;
                }

                if (this.rb_ForgotPwdSetting_Front_Custom_FB.Checked &&
                    string.IsNullOrEmpty(this.txt_ForgotPwdSetting_Front_Custom_Content_FB.Text.Trim()))
                {
                    errorMsg = $"密码前缀为自定义时需要设置自定义内容";
                    return;
                }

                if (Program.setting.Setting_FB.TaskInfoList == null ||
                    Program.setting.Setting_FB.TaskInfoList.Where(t => t.IsSelected).Count() == 0)
                {
                    errorMsg = $"至少设置1项执行任务";
                    return;
                }
            }));

            return errorMsg;
        }

        //启动任务方法
        private void ThreadMethod_StartTasks_FB_Main(object obj_SelectType)
        {
            int selectType = Convert.ToInt32(obj_SelectType);

            string errorMsg = string.Empty;
            //判断是否有现成在运行
            bool isRunning = false;
            if (Program.setting.Setting_FB.Account_List != null)
                isRunning = Program.setting.Setting_FB.Account_List.Where(a => a.Running_IsWorking).Count() > 0;

            if (!isRunning)
            {
                //数据验证
                errorMsg = this.Setting_Check_FB();
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    MessageBox.Show(errorMsg);
                    return;
                }
            }

            List<Account_FBOrIns> account_Selected = null;
            if (selectType == 0) account_Selected = Program.setting.Setting_FB.Account_List;
            else
            {
                List<int> iList = null;
                this.Invoke(new Action(() => { iList = this.GetSelectedIndexList(this.dgv_FB); }));
                if (iList.Count == 0)
                {
                    errorMsg = "请先选择需要操作的账号";
                    MessageBox.Show(errorMsg);
                    return;
                }

                account_Selected = iList.Select(i => Program.setting.Setting_FB.Account_List[i]).ToList();
            }

            if (!isRunning)
            {
                //禁用按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_FB.Enabled = false;
                    this.btn_ImportAccount_FB.Enabled = false;
                    this.btn_Start_FB.Enabled = false;
                    this.btn_Stop_FB.Enabled = true;

                    this.tsmi_Start_All_FB.Enabled = false;
                    this.tsmi_Stop_All_FB.Enabled = true;

                    this.tsmi_Delete_One_FB.Enabled = false;
                    this.tsmi_Delete_All_FB.Enabled = false;

                    this.dgv_GMList_ForBind_FB.Enabled = false;

                    this.btn_List_Order_FB.Enabled = false;
                }));

                this.SaveSetting_FromUser();
                this.SaveSetting_ToDisk();

                //线程设置
                if (this.stp_FB == null)
                {
                    this.stp_FB = new SmartThreadPool();
                }

                this.stp_FB.Concurrency = Program.setting.Setting_FB.ThreadCountMax;

                //统计显示
                this.ShowTongJiInfo_FB(0);
            }

            //派发任务
            for (int i = 0; i < account_Selected.Count; i++)
            {
                if (this.stp_FB.InUseThreads >= this.stp_FB.Concurrency)
                {
                    Thread.Sleep(300);
                    Application.DoEvents();
                    i--;
                    continue;
                }

                account_Selected[i].WorkItemResult =
                    this.stp_FB.QueueWorkItem(this.ThreadMethod_StartTasks_FB_Child, account_Selected[i]);
                //Console.WriteLine($"派发了第[{i + 1}]个任务");
                Thread.Sleep(50);
                Application.DoEvents();
            }

            //等待任务结束，恢复按钮
            if (!isRunning)
            {
                while (!this.stp_FB.IsIdle)
                {
                    Thread.Sleep(500);
                    Application.DoEvents();
                }

                //恢复按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_FB.Enabled = true;
                    this.btn_ImportAccount_FB.Enabled = true;
                    this.btn_Start_FB.Enabled = true;
                    this.btn_Stop_FB.Enabled = false;

                    this.tsmi_Start_All_FB.Enabled = true;
                    this.tsmi_Stop_All_FB.Enabled = false;

                    this.tsmi_Delete_One_FB.Enabled = true;
                    this.tsmi_Delete_All_FB.Enabled = true;

                    this.dgv_GMList_ForBind_FB.Enabled = true;

                    this.btn_List_Order_FB.Enabled = true;
                }));
            }
        }

        //获取一个未使用的邮箱
        private MailInfo GetNotUsedMailInfo_FB()
        {
            MailInfo mail = null;

            lock (Program.setting.Setting_FB.Lock_Mail_ForBind_List)
            {
                if (Program.setting.Setting_FB.Mail_ForBind_List == null) mail = null;
                else
                    mail = Program.setting.Setting_FB.Mail_ForBind_List.Where(m => !m.Is_Used && !m.IsLocked)
                        .FirstOrDefault();

                if (mail != null) mail.IsLocked = true;
            }

            return mail;
        }

        //生成一个新的密码
        private string GetNewPassword_FB()
        {
            string newPwd = string.Empty;
            if (Program.setting.Setting_FB.ForgotPwdSetting_Front_Mode == 0)
                newPwd = StringHelper.GetRandomString(true, true, true, true, 10, 10);
            else newPwd = Program.setting.Setting_FB.ForgotPwdSetting_Front_Custom_Content.Trim();

            if (Program.setting.Setting_FB.ForgotPwdSetting_After_IsAddDate)
                newPwd += $"{DateTime.Now.ToString("MMdd")}";

            return newPwd;
        }

        //核心子线程
        private void ThreadMethod_StartTasks_FB_Child(Account_FBOrIns account)
        {
            JObject jo_Result = null;
            bool isSuccess = false;
            bool isMailUsed = false;
            int trySpan = 0;
            int tryTimes = 0;
            int tryTimesMax = 3;
            MailInfo mail = null;
            string taskName = string.Empty;
            TaskInfo task = null;
            string newPassword = string.Empty;
            bool isNeedLoop = false;

            account.Running_Log = "开始操作";
            Thread.Sleep(1000);

            #region 初始化代理/UA

            account.Running_Log = "初始化代理/UA";
            if (string.IsNullOrEmpty(account.UserAgent) || account.WebProxy == null)
            {
                if (string.IsNullOrEmpty(account.UserAgent)) account.UserAgent = StringHelper.CreateRandomUserAgent();
                if (account.WebProxy == null)
                {
                    if (Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_IsUse &&
                        !string.IsNullOrEmpty(Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_Url))
                    {
                        account.WebProxy =
                            new System.Net.WebProxy(Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_Url);
                        if (!string.IsNullOrEmpty(Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_UserName) &&
                            !string.IsNullOrEmpty(Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_Pwd))
                            account.WebProxy.Credentials = new NetworkCredential(
                                Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_UserName,
                                Program.setting.Setting_FB.Global_WebProxyInfo.Proxy_Pwd);
                    }
                }

                Thread.Sleep(1000);
            }

            #endregion

            #region 检测Cookie

            isSuccess = false;
            isMailUsed = true;
            trySpan = 1000;
            tryTimes = 0;
            tryTimesMax = 2;
            while (!isSuccess && tryTimes < tryTimesMax)
            {
                if (tryTimes > 0) Thread.Sleep(trySpan);
                tryTimes += 1;

                account.Running_Log = "检测Cookie是否有效";
                jo_Result = this.facebookService.FB_LoginByCookie(account);
                account.Running_Log = jo_Result["ErrorMsg"].ToString();
                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());

                if (!isSuccess && jo_Result["isNeedLoop"].ToString().ToLower() == "false") break;
            }

            //状态记录
            //lock (Program.setting.Setting_Facebook.LockObj_TongJi_Real) { Program.setting.Setting_Facebook.TongJi_Real.WuXiao += 1; this.ShowTongJiInfo_FB(0); }
            if (!isSuccess)
            {
                lock (Program.setting.Setting_FB.LockObj_TongJi_Real)
                {
                    if (account.Running_Log.StartsWith("Cookie无效"))
                    {
                        account.Account_Type_Des = "Cookie无效";
                        Program.setting.Setting_FB.TongJi_Real.WuXiao += 1;
                    }
                    else if (account.Running_Log.Contains("账户被锁定"))
                    {
                        account.Account_Type_Des = "账户被锁定";
                        Program.setting.Setting_FB.TongJi_Real.FengHaoShu += 1;
                    }
                    else
                    {
                        account.Account_Type_Des = "其它错误";
                        Program.setting.Setting_FB.TongJi_Real.QiTaCuoWu += 1;
                    }

                    this.ShowTongJiInfo_FB(0);
                }
            }

            if (!isSuccess) return;

            #endregion

            #region 检测并设置语言为英文

            isSuccess = false;
            isMailUsed = true;
            trySpan = 1000;
            tryTimes = 0;
            tryTimesMax = 2;
            while (!isSuccess && tryTimes < tryTimesMax)
            {
                if (tryTimes > 0) Thread.Sleep(trySpan);
                tryTimes += 1;

                account.Running_Log = "检测并设置语言为英文";
                jo_Result = this.facebookService.FB_UpdateLanguageLocale(account, "en_US");

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());

                if (!isSuccess) continue;
            }

            //状态记录

            if (!isSuccess)
            {
                account.Account_Type_Des = "其它错误";

                lock (Program.setting.Setting_FB.LockObj_TongJi_Real)
                {
                    Program.setting.Setting_FB.TongJi_Real.WuXiao += 1;
                    this.ShowTongJiInfo_FB(0);
                }
            }


            if (!isSuccess) return;

            #endregion

            #region 进行邮箱绑定

            taskName = "BindNewEmail";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (string.IsNullOrEmpty(account.New_Mail_Name) || string.IsNullOrEmpty(account.New_Mail_Pwd))
                {
                    isSuccess = false;
                    isMailUsed = true;
                    trySpan = 1000;
                    tryTimes = 0;
                    tryTimesMax = 10;
                    while (!isSuccess && isMailUsed && tryTimes < tryTimesMax)
                    {
                        Thread.Sleep(trySpan);
                        tryTimes += 1;

                        account.Running_Log = "进行邮箱绑定";
                        mail = this.GetNotUsedMailInfo_FB();
                        if (mail == null)
                        {
                            account.Running_Log = "无可用邮箱";
                            break;
                        }

                        jo_Result = this.facebookService.FB_BindNewEmail(account, mail);
                        account.Running_Log = jo_Result["ErrorMsg"].ToString();
                        isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                        isMailUsed = Convert.ToBoolean(jo_Result["IsMailUsed"].ToString());
                        //处理邮箱的绑定问题
                        lock (Program.setting.Setting_FB.Lock_Mail_ForBind_List)
                        {
                            mail.IsLocked = isMailUsed;
                            mail.Is_Used = isMailUsed;
                        }
                    }

                    if (isSuccess)
                    {
                        account.New_Mail_Name = mail.Mail_Name;
                        account.New_Mail_Pwd = mail.Mail_Pwd;
                    }
                    else
                    {
                        lock (Program.setting.Setting_FB.LockObj_TongJi_Real)
                        {
                            if (account.Running_Log == "需要验证设备")
                            {
                                account.Account_Type_Des = "需要验证设备";
                                Program.setting.Setting_FB.TongJi_Real.WuXiao += 1;
                            }
                            else if (account.Running_Log.Contains("需要验证其它方式"))
                            {
                                account.Account_Type_Des = "验证其它邮箱";
                                Program.setting.Setting_FB.TongJi_Real.FengHaoShu += 1;
                            }
                            else
                            {
                                account.Account_Type_Des = "其它错误";
                                Program.setting.Setting_FB.TongJi_Real.QiTaCuoWu += 1;
                            }

                            this.ShowTongJiInfo_FB(0);
                        }

                        return;
                    }

                    Thread.Sleep(1000);
                }
            }

            #endregion

            #region 进行忘记密码操作

            newPassword = this.GetNewPassword_FB();

            taskName = "ForgotPassword";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.New_Mail_Name) && !string.IsNullOrEmpty(account.New_Mail_Pwd) &&
                    string.IsNullOrEmpty(account.Facebook_Pwd))
                {
                    trySpan = 500;
                    tryTimes = 0;
                    tryTimesMax = 5;
                    isNeedLoop = true;
                    while (isNeedLoop && tryTimes < tryTimesMax)
                    {
                        tryTimes += 1;
                        if (tryTimes > 1)
                        {
                            Thread.Sleep(trySpan);
                            Application.DoEvents();
                        }

                        account.Running_Log = "进行忘记密码操作";
                        jo_Result = this.facebookService.FB_ForgotPassword(account, newPassword,
                            Program.setting.Setting_FB.ForgotPwdSetting_LogMeOut);

                        account.Running_Log = jo_Result["ErrorMsg"].ToString();

                        isNeedLoop = jo_Result["isNeedLoop"].Value<bool>();
                    }

                    isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());

                    if (isSuccess)
                    {
                        account.Facebook_Pwd = newPassword;
                    }
                    else
                    {
                        lock (Program.setting.Setting_FB.LockObj_TongJi_Real)
                        {
                            account.Account_Type_Des = "其它错误";
                            Program.setting.Setting_FB.TongJi_Real.QiTaCuoWu += 1;

                            this.ShowTongJiInfo_FB(0);
                        }

                        return;
                    }
                }
            }

            #endregion

            #region 进行打开2FA操作

            taskName = "OpenTwoFA_Dynamic";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.New_Mail_Name) && !string.IsNullOrEmpty(account.New_Mail_Pwd) &&
                    !string.IsNullOrEmpty(account.Facebook_Pwd))
                {
                    account.Running_Log = "进行打开2FA操作";
                    jo_Result = this.facebookService.FB_OpenTwoFA_Dynamic(account);

                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                    isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());

                    if (isSuccess) account.Account_Type_Des = "已完成";
                    else
                    {
                        lock (Program.setting.Setting_FB.LockObj_TongJi_Real)
                        {
                            if (account.Running_Log == "需要验证设备")
                            {
                                account.Account_Type_Des = "需要验证设备";
                                Program.setting.Setting_FB.TongJi_Real.WuXiao += 1;
                            }
                            else if (account.Running_Log.Contains("需要验证其它方式"))
                            {
                                account.Account_Type_Des = "验证其它邮箱";
                                Program.setting.Setting_FB.TongJi_Real.FengHaoShu += 1;
                            }
                            else
                            {
                                account.Account_Type_Des = "其它错误";
                                Program.setting.Setting_FB.TongJi_Real.QiTaCuoWu += 1;
                            }

                            this.ShowTongJiInfo_FB(0);
                        }
                    }
                }
            }

            #endregion

            #region 删除Ins关联

            taskName = "RemoveInsAccount";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.TwoFA_Dynamic_SecretKey))
                {
                    account.Running_Log = "进行删除Ins关联操作";
                    jo_Result = this.facebookService.FB_RemoveInsAccount(account);

                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                    account.Log_RemoveInsAccount = jo_Result["ErrorMsg"].ToString();
                }
            }

            #endregion

            #region 删除其它登录会话

            taskName = "LogOutOfOtherSession";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.TwoFA_Dynamic_SecretKey))
                {
                    account.Running_Log = "进行删除其它登录会话操作";
                    jo_Result = this.facebookService.FB_LogOutOfOtherSession(account);

                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                    account.Log_RemoveInsAccount = jo_Result["ErrorMsg"].ToString();
                }
            }

            #endregion

            #region 删除信任设备

            taskName = "TwoFactorRemoveTrustedDevice";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.TwoFA_Dynamic_SecretKey))
                {
                    account.Running_Log = "删除信任设备";
                    jo_Result = this.facebookService.FB_TwoFactorRemoveTrustedDevice(account);

                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                    account.Log_RemoveInsAccount = jo_Result["ErrorMsg"].ToString();
                }
            }

            #endregion

            #region 删除联系方式

            taskName = "DeleteOtherContacts";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.TwoFA_Dynamic_SecretKey))
                {
                    account.Running_Log = "进行删除联系方式操作";
                    jo_Result = this.facebookService.FB_DeleteOtherContacts(account);

                    //查询国家
                    if (jo_Result["GuoJia"].ToString().Trim().Length > 0)
                        account.GuoJia = jo_Result["GuoJia"].ToString().Trim();

                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                    account.Log_RemoveInsAccount = jo_Result["ErrorMsg"].ToString();
                }
            }

            #endregion

            #region 查辅助词

            taskName = "Query_RecoveryCodes";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.TwoFA_Dynamic_SecretKey) && string.IsNullOrEmpty(account.FuZhuCi))
                {
                    account.Running_Log = "进行查辅助词操作";
                    jo_Result = this.facebookService.FB_Query_RecoveryCodes(account);

                    if (!string.IsNullOrEmpty(jo_Result["FuZhuCi"].ToString()))
                        account.FuZhuCi = jo_Result["FuZhuCi"].ToString();

                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                }
            }

            #endregion

            #region 查国家|注册日期

            taskName = "Query_Country_RegTime";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "进行查国家|注册日期操作";
                jo_Result = this.facebookService.FB_Query_Country_RegTime(account);

                if (!string.IsNullOrEmpty(jo_Result["GuoJia"].ToString()) && string.IsNullOrEmpty(account.GuoJia))
                    account.GuoJia = jo_Result["GuoJia"].ToString();
                if (!string.IsNullOrEmpty(jo_Result["ZhuCeRiQi"].ToString()))
                    account.ZhuCeRiQi = jo_Result["ZhuCeRiQi"].ToString();

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
            }

            #endregion

            #region 查生日|性别|好友

            taskName = "Query_Birthday_Gender_Friends";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "进行查生日|性别|好友操作";
                jo_Result = this.facebookService.FB_Query_Birthday_Gender_Friends(account);

                if (!string.IsNullOrEmpty(jo_Result["ShengRi"].ToString()))
                    account.ShengRi = jo_Result["ShengRi"].ToString();
                if (!string.IsNullOrEmpty(jo_Result["XingBie"].ToString()))
                    account.XingBie = jo_Result["XingBie"].ToString();
                if (!string.IsNullOrEmpty(jo_Result["HaoYouCount"].ToString()))
                    account.HaoYouCount = jo_Result["HaoYouCount"].ToString();

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
            }

            #endregion

            #region 查帖子

            taskName = "Query_Posts";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "进行查帖子操作";
                jo_Result = this.facebookService.FB_Query_Posts(account);

                if (!string.IsNullOrEmpty(jo_Result["TieZiCount"].ToString()))
                    account.TieZiCount = jo_Result["TieZiCount"].ToString();

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
            }

            #endregion

            #region 查商城,专页

            taskName = "Query_AdAccount_Pages";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "进行查商城,专页操作";
                jo_Result = this.facebookService.FB_Query_AdAccount_Pages(account);

                if (!string.IsNullOrEmpty(jo_Result["ShangCheng"].ToString()))
                    account.ShangCheng = jo_Result["ShangCheng"].ToString();
                if (!string.IsNullOrEmpty(jo_Result["ZhuanYe"].ToString()))
                    account.ZhuanYe = jo_Result["ZhuanYe"].ToString();

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
            }

            #endregion

            #region 查权限,账单,余额

            taskName = "Query_AdStatus_ZhangDan_YuE";
            task = Program.setting.Setting_FB.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "进行查权限,账单,余额操作";
                jo_Result = this.facebookService.FB_Query_AdStatus_ZhangDan_YuE(account);

                if (!string.IsNullOrEmpty(jo_Result["AdQuanXian"].ToString()))
                    account.AdQuanXian = jo_Result["AdQuanXian"].ToString();
                if (!string.IsNullOrEmpty(jo_Result["ZhangDan"].ToString()))
                    account.ZhangDan = jo_Result["ZhangDan"].ToString();
                if (!string.IsNullOrEmpty(jo_Result["YuE"].ToString())) account.YuE = jo_Result["YuE"].ToString();

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
            }

            #endregion
        }

        private void Method_StopTasks_FB(int selectType = 0)
        {
            List<Account_FBOrIns> account_Selected = null;
            if (selectType == 0) account_Selected = Program.setting.Setting_FB.Account_List;
            else
            {
                List<int> iList = null;
                this.Invoke(new Action(() => { iList = this.GetSelectedIndexList(this.dgv_FB); }));
                if (iList.Count == 0)
                {
                    MessageBox.Show("请先选择需要操作的账号");
                    return;
                }

                account_Selected = iList.Select(i => Program.setting.Setting_FB.Account_List[i]).ToList();
            }

            if (selectType == 0)
            {
                //停止主线程
                if (this.thread_Main_FB != null) this.thread_Main_FB.Abort();
                if (this.stp_FB != null) this.stp_FB.Cancel(true);
            }

            //停止子线程
            for (int i = 0; i < account_Selected.Count; i++)
            {
                if (account_Selected[i].WorkItemsGroup != null) account_Selected[i].WorkItemsGroup.Cancel(true);
                if (account_Selected[i].WorkItemResult != null) account_Selected[i].WorkItemResult.Cancel(true);
                //account_Selected[i].Running_Log = "操作中止";
            }

            if (selectType == 0)
            {
                this.SaveSetting_FromUser();
                this.SaveSetting_ToDisk();

                //恢复按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_FB.Enabled = true;
                    this.btn_ImportAccount_FB.Enabled = true;
                    this.btn_Start_FB.Enabled = true;
                    this.btn_Stop_FB.Enabled = false;

                    this.tsmi_Start_All_FB.Enabled = true;
                    this.tsmi_Stop_All_FB.Enabled = false;

                    this.tsmi_Delete_One_FB.Enabled = true;
                    this.tsmi_Delete_All_FB.Enabled = true;

                    this.dgv_GMList_ForBind_FB.Enabled = true;

                    this.btn_List_Order_FB.Enabled = true;
                }));
            }
        }

        private void btn_Start_FB_Click(object sender, EventArgs e)
        {
            this.thread_Main_FB = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_FB_Main));
            this.thread_Main_FB.IsBackground = true;
            this.thread_Main_FB.Start(0);
        }

        private void btn_Stop_FB_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_FB(0);
        }

        #endregion

        /*以上代码为FB部分*/


        /*以下代码为Ins部分*/

        #region Ins数据统计

        //显示新数据到主页
        private void ShowTongJiInfo_IG(int useType = -1)
        {
            if (useType > -1) Program.setting.Setting_IG.TongJi_UseType = useType;

            TongJi_FBOrIns tongJi = Program.setting.Setting_IG.TongJi_UseType == 0
                ? Program.setting.Setting_IG.TongJi_Real
                : Program.setting.Setting_IG.TongJi_False;

            this.Invoke(new Action(() =>
            {
                this.lbl_TongJi_WanChengShu_IG.Text = tongJi.WanChengShu.ToString("D5");
                this.lbl_TongJi_FengHaoShu_IG.Text = tongJi.FengHaoShu.ToString("D5");
                this.lbl_TongJi_WuXiao_IG.Text = tongJi.WuXiao.ToString("D5");
                this.lbl_TongJi_YanZhengYouXiang_IG.Text = tongJi.YanZhengYouXiang.ToString("D5");
                this.lbl_TongJi_YanZhengSheBei_IG.Text = tongJi.YanZhengSheBei.ToString("D5");
                this.lbl_TongJi_QiTaCuoWu_IG.Text = tongJi.QiTaCuoWu.ToString("D5");
            }));
        }

        //真实数据清零
        private void btn_TongJi_Real_Clear_IG_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("重新统计，将清除原来的数据，确定操作吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;
            Program.setting.Setting_IG.TongJi_Real = new TongJi_FBOrIns();
            this.ShowTongJiInfo_IG(0);
        }


        //显示新数据到设置页
        private void ShowTongJiInfo_False_IG(int useType = -1)
        {
            if (useType < 0) useType = 0;
            TongJi_FBOrIns tongJi = useType == 0
                ? Program.setting.Setting_IG.TongJi_Real
                : Program.setting.Setting_IG.TongJi_False;
            this.Invoke(new Action(() =>
            {
                this.txt_TongJi_WanChengShu_IG.Text = tongJi.WanChengShu.ToString();
                this.txt_TongJi_FengHaoShu_IG.Text = tongJi.FengHaoShu.ToString();
                this.txt_TongJi_WuXiao_IG.Text = tongJi.WuXiao.ToString();
                this.txt_TongJi_YanZhengYouXiang_IG.Text = tongJi.YanZhengYouXiang.ToString();
                this.txt_TongJi_YanZhengSheBei_IG.Text = tongJi.YanZhengSheBei.ToString();
                this.txt_TongJi_QiTaCuoWu_IG.Text = tongJi.QiTaCuoWu.ToString();
            }));
        }

        //显示真实数据
        private void btn_TongJi_ShowRealInfo_IG_Click(object sender, EventArgs e)
        {
            lock (Program.setting.Setting_IG.LockObj_TongJi_Real)
            {
                this.ShowTongJiInfo_False_IG(0);
            }
        }

        //显示新数据
        private void btn_TongJi_ShowFalseInfo_IG_Click(object sender, EventArgs e)
        {
            this.ShowTongJiInfo_False_IG(1);
        }

        //清除新数据
        private void btn_TongJi_ClearFalseInfo_IG_Click(object sender, EventArgs e)
        {
            Program.setting.Setting_IG.TongJi_False = new TongJi_FBOrIns();
            this.ShowTongJiInfo_False_IG(1);
        }

        //修改并显示新数据
        private void btn_TongJi_EditAndShowFalseInfo_IG_Click(object sender, EventArgs e)
        {
            string errorMsg = this.TongJiInfo_Check_IG();
            if (!string.IsNullOrEmpty(errorMsg))
            {
                MessageBox.Show(errorMsg);
                return;
            }

            Program.setting.Setting_IG.TongJi_False.WanChengShu =
                int.Parse(this.txt_TongJi_WanChengShu_IG.Text.Trim());
            Program.setting.Setting_IG.TongJi_False.FengHaoShu = int.Parse(this.txt_TongJi_FengHaoShu_IG.Text.Trim());
            Program.setting.Setting_IG.TongJi_False.WuXiao = int.Parse(this.txt_TongJi_WuXiao_IG.Text.Trim());
            Program.setting.Setting_IG.TongJi_False.YanZhengYouXiang =
                int.Parse(this.txt_TongJi_YanZhengYouXiang_IG.Text.Trim());
            Program.setting.Setting_IG.TongJi_False.YanZhengSheBei =
                int.Parse(this.txt_TongJi_YanZhengSheBei_IG.Text.Trim());
            Program.setting.Setting_IG.TongJi_False.QiTaCuoWu = int.Parse(this.txt_TongJi_QiTaCuoWu_IG.Text.Trim());

            this.ShowTongJiInfo_IG(1);
        }

        //数据检测
        private string TongJiInfo_Check_IG()
        {
            string errorMsg = string.Empty;

            int num;
            this.Invoke(new Action(() =>
            {
                if (!int.TryParse(this.txt_TongJi_WanChengShu_IG.Text.Trim(), out num))
                {
                    errorMsg = "完成数格式不正确";
                    return;
                }

                if (!int.TryParse(this.txt_TongJi_FengHaoShu_IG.Text.Trim(), out num))
                {
                    errorMsg = "封号数格式不正确";
                    return;
                }

                if (!int.TryParse(this.txt_TongJi_WuXiao_IG.Text.Trim(), out num))
                {
                    errorMsg = "Cookie无效数量格式不正确";
                    return;
                }

                if (!int.TryParse(this.txt_TongJi_YanZhengYouXiang_IG.Text.Trim(), out num))
                {
                    errorMsg = "验证其它邮箱数量格式不正确";
                    return;
                }

                if (!int.TryParse(this.txt_TongJi_YanZhengSheBei_IG.Text.Trim(), out num))
                {
                    errorMsg = "验证设备数量格式不正确";
                    return;
                }

                if (!int.TryParse(this.txt_TongJi_QiTaCuoWu_IG.Text.Trim(), out num))
                {
                    errorMsg = "其它错误数量格式不正确";
                    return;
                }
            }));

            return errorMsg;
        }

        #endregion

        #region Ins执行流程控制

        //初始化
        private void TaskList_IG_Init()
        {
            List<TaskInfo> tasks = new List<TaskInfo>();
            tasks.Add(new TaskInfo("LoginByCookie", "1 : 检测Cookie", true));
            tasks.Add(new TaskInfo("BindNewEmail", "2 : 绑定新邮箱", true));
            tasks.Add(new TaskInfo("ForgotPassword", "3 : 忘记密码", true));
            tasks.Add(new TaskInfo("OpenTwoFA_Dynamic", "4 : 打开2FA(动态)", true));
            tasks.Add(new TaskInfo("RemoveFBAccount", "5 : 删除FB关联", true));
            tasks.Add(new TaskInfo("LogOutOfOtherSession", "5 : 删除其它登录会话", true));
            tasks.Add(new TaskInfo("DeleteOtherContacts_QueryCountry", "5 : 删除联系方式|查国家", true));
            tasks.Add(new TaskInfo("Query_Country", "6 : 查国家", true));
            tasks.Add(new TaskInfo("Query_Birthday", "6 : 查生日", true));
            tasks.Add(new TaskInfo("Query_ZhuCeRiQi", "6 : 查注册日期", true));
            tasks.Add(new TaskInfo("Query_Posts_Followers_Following", "6 : 查帖子|好友|关注", true));

            if (Program.setting.Setting_IG.TaskInfoList == null)
                Program.setting.Setting_IG.TaskInfoList = new List<TaskInfo>();

            for (int i = 0; i < tasks.Count; i++)
            {
                TaskInfo tFind = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == tasks[i].TaskName)
                    .FirstOrDefault();
                if (tFind != null) tasks[i].IsSelected = tFind.IsSelected;
            }

            Program.setting.Setting_IG.TaskInfoList = tasks;

            this.Invoke(new Action(() =>
            {
                this.dgv_TaskList_IG.DataSource = null;
                if (Program.setting.Setting_IG.TaskInfoList != null &&
                    Program.setting.Setting_IG.TaskInfoList.Count > 0)
                    this.dgv_TaskList_IG.DataSource = Program.setting.Setting_IG.TaskInfoList;
            }));
        }

        //初始化
        private void TaskList_EM_Init()
        {
            List<TaskInfo> tasks = new List<TaskInfo>();
            tasks.Add(new TaskInfo("RegisterYA", "1 : 注册雅虎", true));
            tasks.Add(new TaskInfo("RegisterGoogle", "2 : 注册谷歌", true));
            tasks.Add(new TaskInfo("RegisterGoogleAndLinkedin", "2 : 注册谷歌和领英", true));
            tasks.Add(new TaskInfo("RegisterMS", "3 : 注册微软", true));
            tasks.Add(new TaskInfo("RegisterAUIN", "4 : 注册领英(授权)", true));
            tasks.Add(new TaskInfo("RegisterINGoogle", "4 : 注册领英(先登录谷歌)", true));
            tasks.Add(new TaskInfo("RegisterINGoogleBindEmail", "4 : 注册绑定邮箱", true));
            tasks.Add(new TaskInfo("RegisterIN", "4 : 注册领英", true));
            tasks.Add(new TaskInfo("AddFans", "5 : 增加粉丝", true));
            tasks.Add(new TaskInfo("ConfirmFans", "6 : 确认粉丝", true));
            tasks.Add(new TaskInfo("BindEmail", "7 : 绑定邮箱", true));
            tasks.Add(new TaskInfo("OpenTwoFA", "7 : 开启2Fa", true));

            if (Program.setting.Setting_EM.TaskInfoList == null)
                Program.setting.Setting_EM.TaskInfoList = new List<TaskInfo>();

            for (int i = 0; i < tasks.Count; i++)
            {
                TaskInfo tFind = Program.setting.Setting_EM.TaskInfoList.Where(t => t.TaskName == tasks[i].TaskName)
                    .FirstOrDefault();
                if (tFind != null) tasks[i].IsSelected = tFind.IsSelected;
            }

            Program.setting.Setting_EM.TaskInfoList = tasks;
            this.Invoke(new Action(() =>
            {
                this.dgv_TaskList_EM.DataSource = null;
                if (Program.setting.Setting_EM.TaskInfoList != null &&
                    Program.setting.Setting_EM.TaskInfoList.Count > 0)
                    this.dgv_TaskList_EM.DataSource = Program.setting.Setting_EM.TaskInfoList;
            }));
        }

        //右键弹出菜单
        private void dgv_TaskList_IG_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.Button == MouseButtons.Right)
            {
                DataGridView dgv = this.dgv_TaskList_IG;
                //弹出操作菜单
                this.cms_dgv_TaskList_IG.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //选中
        private void tsmi_Select_TaskList_IG_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_IG;
            List<TaskInfo> tasks = (List<TaskInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<TaskInfo> tasks_Selected = iList.Select(i => tasks[i]).ToList();
            if (tasks_Selected.Count == 0) return;

            for (int i = 0; i < tasks_Selected.Count; i++)
            {
                tasks_Selected[i].IsSelected = true;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //选中_全部
        private void tsmi_Select_All_TaskList_IG_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_IG;
            List<TaskInfo> tasks_Selected = Program.setting.Setting_IG.TaskInfoList;
            if (tasks_Selected.Count == 0) return;

            for (int i = 0; i < tasks_Selected.Count; i++)
            {
                tasks_Selected[i].IsSelected = true;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //取消选中
        private void tsmi_NotSelect_TaskList_IG_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_IG;
            List<TaskInfo> tasks = (List<TaskInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<TaskInfo> tasks_Selected = iList.Select(i => tasks[i]).ToList();
            if (tasks_Selected.Count == 0) return;

            for (int i = 0; i < tasks_Selected.Count; i++)
            {
                tasks_Selected[i].IsSelected = false;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //取消选中_全部
        private void tsmi_NotSelect_All_TaskList_IG_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_IG;
            List<TaskInfo> tasks_Selected = Program.setting.Setting_IG.TaskInfoList;
            if (tasks_Selected.Count == 0) return;

            for (int i = 0; i < tasks_Selected.Count; i++)
            {
                tasks_Selected[i].IsSelected = false;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //删除
        private void tsmi_Delete_TaskList_IG_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_IG;
            List<TaskInfo> tasks = (List<TaskInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_IG.TaskInfoList.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_IG.TaskInfoList != null || Program.setting.Setting_IG.TaskInfoList.Count > 0)
                this.Invoke(new Action(() => { dgv.DataSource = Program.setting.Setting_IG.TaskInfoList; }));
        }

        //删除_全部
        private void tsmi_Delete_All_TaskList_IG_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_IG;
            if (Program.setting.Setting_IG.TaskInfoList == null ||
                Program.setting.Setting_IG.TaskInfoList.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            Program.setting.Setting_IG.TaskInfoList.Clear();
        }

        //编辑即时响应
        private void dgv_TaskList_IG_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_TaskList_IG;
            if (dgv.IsCurrentCellDirty) dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        #endregion

        #region Ins账号导入

        //清空账号列表
        private void btn_ClearData_IG_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空列表吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;
            this.Invoke(new Action(() => { this.dgv_IG.DataSource = null; }));
            Program.setting.Setting_IG.Account_List = null;
        }

        //获取账号模板
        private void btn_GetExcelModelFile_IG_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            // 设置保存对话框的标题
            saveFileDialog.Title = "保存导入账号模版";

            // 设置默认的文件名
            saveFileDialog.FileName = "导入账号模版_IG.xlsx";

            // 设置默认的文件类型筛选
            saveFileDialog.Filter = "Excel文档 (*.xlsx)|*.xlsx";

            // 设置默认的文件类型索引
            saveFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            saveFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 将文本保存到文件
                    File.WriteAllBytes(saveFileDialog.FileName, Properties.Resources.导入账号模版_IG);

                    StringHelper.OpenFolderAndSelectFiles(new FileInfo(saveFileDialog.FileName).Directory.FullName,
                        saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //导入账号的具体方法
        private void ImportAccount_IG(string fileName)
        {
            string errorMsg = string.Empty;

            FileInfo fi = new FileInfo(fileName);
            ExcelPackage excelPackage = null;
            try
            {
                excelPackage = new ExcelPackage(fi);
            }
            catch (Exception ex)
            {
                errorMsg = $"打开文件失败({ex.Message})";
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                MessageBox.Show(errorMsg);
                return;
            }

            if (excelPackage.Workbook.Worksheets.Count == 0)
            {
                errorMsg = $"表格内容不存在";
                MessageBox.Show(errorMsg);
                return;
            }

            ExcelWorksheet sheet = excelPackage.Workbook.Worksheets[0];
            int rowCount = sheet.Dimension.End.Row;
            int colCount = sheet.Dimension.End.Column;

            if (rowCount < 2)
            {
                errorMsg = $"表格行数至少为2行，第一行未表头，第二行开始为内容";
                MessageBox.Show(errorMsg);
                return;
            }

            List<ExcelColumnInfo> excelCols = new List<ExcelColumnInfo>()
            {
                new ExcelColumnInfo("操作日志", "Running_Log"),
                new ExcelColumnInfo("账号分类", "Account_Type_Des"),
                new ExcelColumnInfo("原Mail_CK", "Old_Mail_CK"),
                new ExcelColumnInfo("原Mail_号", "Old_Mail_Name"),
                new ExcelColumnInfo("新Mail_号", "New_Mail_Name"),
                new ExcelColumnInfo("新Mail_密", "New_Mail_Pwd"),
                new ExcelColumnInfo("Ins_Pwd", "Facebook_Pwd"),
                new ExcelColumnInfo("Ins_CK", "Facebook_CK"),
                new ExcelColumnInfo("Ins_User", "C_User"),
                new ExcelColumnInfo("昵称", "UserName"),
                new ExcelColumnInfo("UA", "UserAgent"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("2FA密钥", "TwoFA_Dynamic_SecretKey"),
                new ExcelColumnInfo("删除Ins关联", "Log_RemoveInsAccount"),
                new ExcelColumnInfo("删除其它登录会话", "Log_LogOutOfOtherSession"),
                //new ExcelColumnInfo("删除信任设备","Log_TwoFactorRemoveTrustedDevice"),
                new ExcelColumnInfo("删除联系方式", "Log_DeleteOtherContacts"),
                new ExcelColumnInfo("国家", "GuoJia"),
                new ExcelColumnInfo("生日", "ShengRi"),
                new ExcelColumnInfo("注册日期", "ZhuCeRiQi"),
                new ExcelColumnInfo("帖子", "TieZiCount"),
                new ExcelColumnInfo("好友", "HaoYouCount"),
                new ExcelColumnInfo("关注", "GuanZhuCount"),
            };

            List<int> rList = Enumerable.Range(2, rowCount - 1).ToList();
            List<int> cList = Enumerable.Range(1, colCount).ToList();

            //表头定位
            List<string> sList = cList.Select(c => sheet.Cells[1, c].Text).ToList();
            foreach (var eCol in excelCols)
            {
                eCol.HeaderIndex = sList.FindIndex(s => s.Trim() == eCol.HeaderName);
            }

            //一次读取所有行内容
            rList = Enumerable.Range(2, rowCount - 1).ToList();
            cList = Enumerable.Range(1, colCount).ToList();
            sList = rList.Select(r => string.Join("\t", cList.Select(c => sheet.Cells[r, c].Text))).ToList();

            //每一行内容，创建实例添加到列表中
            List<Account_FBOrIns> accounts = sList.Select(s =>
            {
                string[] cellArr = s.Split('\t');

                Account_FBOrIns account = new Account_FBOrIns();

                int cellValue;
                for (int i = 0; i < excelCols.Count; i++)
                {
                    if (excelCols[i].HeaderIndex < 0 || excelCols[i].HeaderIndex >= cellArr.Length) continue;

                    if (excelCols[i].PropertyName == "TwoFA_Dynamic_StatusDes")
                    {
                        if (cellArr[excelCols[i].HeaderIndex].Trim() == "开") cellValue = 1;
                        else if (cellArr[excelCols[i].HeaderIndex].Trim() == "关") cellValue = 0;
                        else cellValue = -1;
                        this.SetProperty(account, "TwoFA_Dynamic_Status", cellValue);
                    }
                    else if (excelCols[i].PropertyName == "C_User")
                    {
                        if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
                        account.LoginInfo.LoginData_Account_Id = cellArr[excelCols[i].HeaderIndex].Trim();
                    }
                    else if (excelCols[i].PropertyName == "UserName")
                    {
                        if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
                        account.LoginInfo.LoginData_UserName = cellArr[excelCols[i].HeaderIndex].Trim();
                    }
                    else this.SetProperty(account, excelCols[i].PropertyName, cellArr[excelCols[i].HeaderIndex].Trim());
                }

                if (string.IsNullOrEmpty(account.Facebook_CK) && string.IsNullOrEmpty(account.Old_Mail_CK))
                    account = null;

                return account;
            }).Where(a => a != null).GroupBy(a =>
            {
                //"name":"c_user","value":"100070943890270"
                string key = StringHelper.GetMidStr(a.Facebook_CK, "\"name\":\"c_user\",\"value\":\"", "\"").Trim();
                return key;
            }).SelectMany(ga =>
            {
                if (!string.IsNullOrEmpty(ga.Key)) return ga.ToList().GetRange(0, 1);
                else return ga.ToList();
            }).ToList();

            //合并更新到表格中
            this.Invoke(new Action(() => { this.dgv_IG.DataSource = null; }));

            Program.setting.Setting_IG.Account_List = accounts;

            if (Program.setting.Setting_IG.Account_List != null && Program.setting.Setting_IG.Account_List.Count > 0)
                this.Invoke(new Action(() => { this.dgv_IG.DataSource = Program.setting.Setting_IG.Account_List; }));
        }

        //导入账号
        private void btn_ImportAccount_IG_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开Excel文档";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "Excel文档 (*.xls;*.xlsx)|*.xls;*.xlsx";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 获取用户选择的文件名
                    string filePath = openFileDialog.FileName;
                    this.ImportAccount_IG(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        //拖动导入事件
        private void dgv_IG_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        //拖动导入事件
        private void dgv_IG_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            List<string> fList = files.Where(s => s.ToLower().EndsWith(".xls") || s.ToLower().EndsWith(".xlsx"))
                .ToList();

            if (fList.Count == 0)
            {
                MessageBox.Show($"导入账号时发生错误：只能导入Excel文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (fList.Count > 1)
            {
                MessageBox.Show($"导入账号时发生错误：每次只能导入1个Excel文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ImportAccount_IG(fList[0]);
        }

        #endregion

        #region 用于Ins绑定的邮箱账号导入和导出

        //导入账号的具体方法
        private void ImportAccount_ForBind_IG(string fileName)
        {
            string fileContent = File.ReadAllText(fileName);
            List<string> sList = Regex.Split(fileContent, "(\r\n)|\n").Where(s => s.Trim().Length > 0)
                .Select(s => s.Trim()).ToList();

            List<MailInfo> mails = sList.Select(s =>
            {
                string[] arr = Regex.Split(s, @"----|\||,|，").ToArray();
                if (arr.Length < 2) return null;

                MailInfo m = new MailInfo();
                m.Mail_Name = arr[0].Trim();
                m.Mail_Pwd = arr[1].Trim();
                if (arr.Length > 2) m.VerifyMail_Name = arr[2].Trim();
                if (arr.Length > 3) m.Is_Used = arr[3].Trim() == "已使用";
                return m;
            }).Where(m => m != null).GroupBy(m => m.Mail_Name).Select(gm => gm.FirstOrDefault()).ToList();

            if (mails.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_GMList_ForBind_IG.DataSource = null; }));

            if (Program.setting.Setting_IG.Mail_ForBind_List == null)
                Program.setting.Setting_IG.Mail_ForBind_List = new List<MailInfo>();
            foreach (MailInfo mail in mails)
            {
                int mIndex =
                    Program.setting.Setting_IG.Mail_ForBind_List.FindIndex(m => m.Mail_Name == mail.Mail_Name);
                if (mIndex == -1) Program.setting.Setting_IG.Mail_ForBind_List.Add(mail);
                else
                {
                    Program.setting.Setting_IG.Mail_ForBind_List[mIndex].Mail_Pwd = mail.Mail_Pwd;
                    Program.setting.Setting_IG.Mail_ForBind_List[mIndex].VerifyMail_Name = mail.VerifyMail_Name;
                }
            }

            if (Program.setting.Setting_IG.Mail_ForBind_List != null &&
                Program.setting.Setting_IG.Mail_ForBind_List.Count > 0)
                this.Invoke(new Action(() =>
                {
                    this.dgv_GMList_ForBind_IG.DataSource = Program.setting.Setting_IG.Mail_ForBind_List;
                }));
        }

        private void btn_ImportAccount_ForBind_IG_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开TXT文档";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "TXT文档 (*.txt)|*.txt";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 获取用户选择的文件名
                    string filePath = openFileDialog.FileName;
                    this.ImportAccount_ForBind_IG(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btn_ClearData_ForBind_IG_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空列表吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;

            this.Invoke(new Action(() => { this.dgv_GMList_ForBind_IG.DataSource = null; }));
            Program.setting.Setting_IG.Mail_ForBind_List = null;
        }

        //导出TXT文档_具体方法
        private void ExportTXT_GM_IG()
        {
            if (Program.setting.Setting_IG.Mail_ForBind_List == null ||
                Program.setting.Setting_IG.Mail_ForBind_List.Count == 0)
            {
                MessageBox.Show("No Data!");
                return;
            }

            DateTime now = DateTime.Now;
            string sheetName = $"Ins_MailList_{now.ToString("yyyyMMdd_HHmmss")}";

            FileInfo fileInfo = null;
            this.Invoke(new Action(() =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "TXT文档（*.txt）|*.txt";
                sfd.FilterIndex = 1;
                sfd.InitialDirectory = Application.StartupPath;
                sfd.RestoreDirectory = true;
                sfd.FileName = $"{sheetName}.txt";
                if (sfd.ShowDialog() != DialogResult.OK) return;
                fileInfo = new FileInfo(sfd.FileName);
            }));
            if (fileInfo == null) return;

            //禁用按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_GM_IG.Enabled = false;
                Application.DoEvents();
            }));

            List<MailInfo> mails_NotUsed =
                Program.setting.Setting_IG.Mail_ForBind_List.Where(m => !m.Is_Used).ToList();
            List<MailInfo> mails_Used = Program.setting.Setting_IG.Mail_ForBind_List.Where(m => m.Is_Used).ToList();
            List<MailInfo> mails_All = mails_Used.Union(mails_NotUsed).ToList();

            string fileContent = string.Join("\r\n",
                mails_All.Select(m =>
                    $"{m.Mail_Name}----{m.Mail_Pwd}----{m.VerifyMail_Name}{(m.Is_Used ? $"----{m.Is_Used_Des}" : string.Empty)}"));

            //写出文档
            File.WriteAllText(fileInfo.FullName, fileContent);

            //显示Excel文档
            this.Invoke(new Action(() =>
            {
                StringHelper.OpenFolderAndSelectFiles(fileInfo.Directory.FullName,
                    new string[] { fileInfo.FullName });
            }));

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_GM_IG.Enabled = true;
                Application.DoEvents();
            }));
        }

        private void btn_Export_List_GM_IG_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(this.ExportTXT_GM_IG);
        }

        private void dgv_GMList_ForBind_IG_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            List<string> fList = files.Where(s => s.ToLower().EndsWith(".txt")).ToList();

            if (fList.Count == 0)
            {
                MessageBox.Show($"导入账号时发生错误：只能导入TXT文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (fList.Count > 1)
            {
                MessageBox.Show($"导入账号时发生错误：每次只能导入1个TXT文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ImportAccount_ForBind_IG(fList[0]);
        }

        private void dgv_GMList_ForBind_IG_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        #endregion

        #region 用于Ins绑定新邮箱的右键菜单

        //右键弹出菜单
        private void dgv_GMList_ForBind_IG_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.Button == MouseButtons.Right)
            {
                DataGridView dgv = this.dgv_GMList_ForBind_IG;
                //弹出操作菜单
                this.cms_dgv_GM_IG.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //标记未使用
        private void tsmi_SetNotUsed_GM_IG_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IG;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<MailInfo> mails_Selected = iList.Select(i => tasks[i]).ToList();
            if (mails_Selected.Count == 0) return;

            for (int i = 0; i < mails_Selected.Count; i++)
            {
                mails_Selected[i].Is_Used = false;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //标记已使用
        private void tsmi_SetUsed_GM_IG_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IG;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            List<MailInfo> mails_Selected = iList.Select(i => tasks[i]).ToList();
            if (mails_Selected.Count == 0) return;

            for (int i = 0; i < mails_Selected.Count; i++)
            {
                mails_Selected[i].Is_Used = true;
            }

            this.Invoke(new Action(() => { dgv.Refresh(); }));
        }

        //删除
        private void tsmi_Delete_One_GM_IG_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IG;
            List<MailInfo> tasks = (List<MailInfo>)dgv.DataSource;
            if (tasks == null || tasks.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(dgv);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_IG.Mail_ForBind_List.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_IG.Mail_ForBind_List != null ||
                Program.setting.Setting_IG.Mail_ForBind_List.Count > 0)
                this.Invoke(new Action(() => { dgv.DataSource = Program.setting.Setting_IG.Mail_ForBind_List; }));
        }

        //删除_全部
        private void tsmi_Delete_All_GM_IG_Click(object sender, EventArgs e)
        {
            DataGridView dgv = this.dgv_GMList_ForBind_IG;
            if (Program.setting.Setting_IG.Mail_ForBind_List == null ||
                Program.setting.Setting_IG.Mail_ForBind_List.Count == 0) return;

            this.Invoke(new Action(() => { dgv.DataSource = null; }));

            Program.setting.Setting_IG.Mail_ForBind_List.Clear();
        }

        #endregion

        #region dgv_IG事件和右键菜单（导出和分类）

        //DGV画行号
        private void dgv_IG_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == -1)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                using (Brush brush = new SolidBrush(e.CellStyle.ForeColor))
                {
                    e.Graphics.DrawString((e.RowIndex + 1).ToString(), e.CellStyle.Font, brush,
                        e.CellBounds.Location.X + 10, e.CellBounds.Location.Y + 4);
                }

                e.Handled = true;
            }
        }

        //弹出右键菜单
        private void dgv_IG_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;

            if (e.Button == MouseButtons.Right)
            {
                //this.dgv_IG.CurrentCell = this.dgv_IG.Rows[e.RowIndex].Cells[e.ColumnIndex];
                DataGridViewCell cell = this.dgv_IG.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell != null && !cell.Selected) cell.Selected = true;

                this.cms_dgv_IG.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //编辑即时响应
        private void dgv_IG_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (this.dgv_IG.IsCurrentCellDirty) this.dgv_IG.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        //开始操作
        private void tsmi_Start_One_IG_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_IG_Main));
            thread.IsBackground = true;
            thread.Start(1);
        }

        //开始操作_全部
        private void tsmi_Start_All_IG_Click(object sender, EventArgs e)
        {
            this.thread_Main_IG = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_IG_Main));
            this.thread_Main_IG.IsBackground = true;
            this.thread_Main_IG.Start(0);
        }

        //停止操作
        private void tsmi_Stop_One_IG_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_IG(1);
        }

        //停止操作_全部
        private void tsmi_Stop_All_IG_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_IG(0);
        }

        //删除
        private void tsmi_Delete_One_IG_Click(object sender, EventArgs e)
        {
            if (Program.setting.Setting_IG.Account_List == null ||
                Program.setting.Setting_IG.Account_List.Count == 0) return;

            List<int> iList = this.GetSelectedIndexList(this.dgv_IG);
            if (iList.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_IG.DataSource = null; }));

            int deleteCount = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                Program.setting.Setting_IG.Account_List.RemoveAt(iList[i] - deleteCount);
                deleteCount++;
            }

            if (Program.setting.Setting_IG.Account_List != null || Program.setting.Setting_IG.Account_List.Count > 0)
                this.Invoke(new Action(() => { this.dgv_IG.DataSource = Program.setting.Setting_IG.Account_List; }));
        }

        //删除_全部
        private void tsmi_Delete_All_IG_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要删除全部吗？", "温馨提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) !=
                DialogResult.OK) return;

            if (Program.setting.Setting_IG.Account_List == null ||
                Program.setting.Setting_IG.Account_List.Count == 0) return;

            this.Invoke(new Action(() => { this.dgv_IG.DataSource = null; }));

            Program.setting.Setting_IG.Account_List.Clear();
        }

        //导出Excel_具体方法
        private void ExportExcel_IG()
        {
            if (Program.setting.Setting_IG.Account_List == null || Program.setting.Setting_IG.Account_List.Count == 0)
            {
                MessageBox.Show("No Data!");
                return;
            }

            DateTime now = DateTime.Now;
            string sheetName = $"Ins_{now.ToString("yyyyMMdd_HHmmss")}";
            Regex regex = new Regex(@"^\d+$");

            FileInfo fileInfo = null;
            this.Invoke(new Action(() =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Excel文档（*.xlsx）|*.xlsx";
                sfd.FilterIndex = 1;
                sfd.InitialDirectory = Application.StartupPath;
                sfd.RestoreDirectory = true;
                sfd.FileName = $"{sheetName}.xlsx";
                if (sfd.ShowDialog() != DialogResult.OK) return;
                fileInfo = new FileInfo(sfd.FileName);
            }));
            if (fileInfo == null) return;

            //禁用按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_IG.Enabled = false;
                Application.DoEvents();
            }));

            //整理表头信息
            List<ExcelColumnInfo> excelCols = new List<ExcelColumnInfo>()
            {
                new ExcelColumnInfo("操作日志", "Running_Log"),
                new ExcelColumnInfo("账号分类", "Account_Type_Des"),
                new ExcelColumnInfo("原Mail_CK", "Old_Mail_CK"),
                new ExcelColumnInfo("原Mail_号", "Old_Mail_Name"),
                new ExcelColumnInfo("新Mail_号", "New_Mail_Name"),
                new ExcelColumnInfo("新Mail_密", "New_Mail_Pwd"),
                new ExcelColumnInfo("Ins_Pwd", "Facebook_Pwd"),
                new ExcelColumnInfo("Ins_CK", "Facebook_CK"),
                new ExcelColumnInfo("Ins_User", "C_User"),
                new ExcelColumnInfo("昵称", "UserName"),
                new ExcelColumnInfo("UA", "UserAgent"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("2FA密钥", "TwoFA_Dynamic_SecretKey"),
                new ExcelColumnInfo("删除Ins关联", "Log_RemoveInsAccount"),
                new ExcelColumnInfo("删除其它登录会话", "Log_LogOutOfOtherSession"),
                //new ExcelColumnInfo("删除信任设备","Log_TwoFactorRemoveTrustedDevice"),
                new ExcelColumnInfo("删除联系方式", "Log_DeleteOtherContacts"),
                new ExcelColumnInfo("国家", "GuoJia"),
                new ExcelColumnInfo("生日", "ShengRi"),
                new ExcelColumnInfo("注册日期", "ZhuCeRiQi"),
                new ExcelColumnInfo("帖子", "TieZiCount"),
                new ExcelColumnInfo("好友", "HaoYouCount"),
                new ExcelColumnInfo("关注", "GuanZhuCount"),
            };
            for (int i = 0; i < excelCols.Count; i++)
            {
                excelCols[i].HeaderIndex = i;
            }

            Type type = typeof(Account_FBOrIns);
            string headers = string.Join("\t", excelCols.Select(c => c.HeaderName));
            string content = string.Join("\r\n", Program.setting.Setting_IG.Account_List.Select(c =>
            {
                string lineStr = string.Join("\t", excelCols.Select(ec =>
                {
                    string cellValue;
                    PropertyInfo propInfo = type.GetProperty(ec.PropertyName);
                    cellValue = propInfo == null || propInfo.GetValue(c, null) == null
                        ? string.Empty
                        : propInfo.GetValue(c, null).ToString();
                    if (regex.IsMatch(cellValue) && cellValue.Length > 10) cellValue = $"'{cellValue}";
                    return cellValue;
                }));
                return lineStr;
            }));

            //创建一个新的Excel文件
            using (var package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(sheetName);

                //调整为文本格式
                for (int i = 0; i < excelCols.Count; i++)
                {
                    worksheet.Columns[i + 1].Style.Numberformat.Format = "@";
                }

                ExcelTextFormat excelTextFormat = new ExcelTextFormat();
                excelTextFormat.Delimiter = '\t';
                //表头处理
                worksheet.Cells["A1"].LoadFromText(headers, excelTextFormat);

                //在A2单元格粘贴内容
                worksheet.Cells["A2"].LoadFromText(content, excelTextFormat);

                //首行加粗
                worksheet.Rows[1].Style.Font.Bold = true;

                //单元格内容重新赋值
                foreach (var cell in worksheet.Cells[2, 1, Program.setting.Setting_IG.Account_List.Count + 1,
                             excelCols.Count])
                {
                    if (cell.Value != null) cell.Value = cell.Value.ToString().Replace("'", "");
                }

                //自动列宽，居中[CK那一列不居中]
                for (int i = 0; i < excelCols.Count; i++)
                {
                    if (worksheet.Cells[1, i + 1].Text == "Facebook_CK" || worksheet.Cells[1, i + 1].Text == "UA")
                    {
                        worksheet.Cells[1, i + 1].Style.HorizontalAlignment =
                            OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        worksheet.Columns[i + 1].Width = 20;
                        continue;
                    }

                    worksheet.Columns[i + 1].AutoFit();
                    worksheet.Columns[i + 1].Style.HorizontalAlignment =
                        OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                // 保存Excel文件
                package.SaveAs(fileInfo);
            }

            //显示Excel文档
            this.Invoke(new Action(() =>
            {
                StringHelper.OpenFolderAndSelectFiles(fileInfo.Directory.FullName,
                    new string[] { fileInfo.FullName });
            }));

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.btn_Export_List_IG.Enabled = true;
                Application.DoEvents();
            }));
        }

        //导出账号
        private void btn_Export_List_IG_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(this.ExportExcel_IG);
        }

        //分类排序
        private void ThreadMethod_List_Order_IG()
        {
            this.Invoke(new Action(() => { this.btn_List_Order_IG.Enabled = false; }));

            DataGridView dgv = null;
            this.Invoke(new Action(() =>
            {
                dgv = this.dgv_IG;
                dgv.DataSource = null;
            }));

            Program.setting.Setting_IG.Account_List.ForEach(a =>
            {
                if (!string.IsNullOrEmpty(a.TwoFA_Dynamic_SecretKey) && string.IsNullOrEmpty(a.Account_Type_Des))
                    a.Account_Type_Des = "已完成";
            });

            var orderbyList = Program.setting.Setting_IG.Account_List.OrderByDescending(a =>
            {
                int oNum = 0;
                if (a.Account_Type_Des == "已完成") oNum = 20;
                else if (a.Account_Type_Des == "账户被锁定") oNum = 19;
                else if (a.Account_Type_Des == "Cookie无效") oNum = 18;
                else if (a.Account_Type_Des == "验证其它邮箱") oNum = 17;
                else if (a.Account_Type_Des == "需要设备验证") oNum = 16;
                else if (a.Account_Type_Des == "其它错误") oNum = 15;
                return oNum;
            });

            Program.setting.Setting_IG.Account_List = orderbyList.ToList();

            this.Invoke(new Action(() => { dgv.DataSource = Program.setting.Setting_IG.Account_List; }));

            this.Invoke(new Action(() => { this.btn_List_Order_IG.Enabled = true; }));
        }


        //分类排序
        private void btn_List_Order_IG_Click(object sender, EventArgs e)
        {
            if (Program.setting.Setting_IG.Account_List == null ||
                Program.setting.Setting_IG.Account_List.Count == 0) return;

            Thread thread = new Thread(this.ThreadMethod_List_Order_IG);
            thread.IsBackground = true;
            thread.Start();
        }

        #endregion

        #region Ins核心功能

        private InstagramService instagramService = new InstagramService();
        private Thread thread_Main_IG = null;
        private SmartThreadPool stp_IG = null;

        private string Setting_Check_IG()
        {
            string errorMsg = string.Empty;

            if (Program.setting.Setting_IG.Account_List == null || Program.setting.Setting_IG.Account_List.Count == 0)
            {
                errorMsg = $"请先导入账号";
                return errorMsg;
            }

            int num = 0;
            this.Invoke(new Action(() =>
            {
                if (!int.TryParse(this.txt_ThreadCountMax_IG.Text.Trim(), out num) || num <= 0)
                {
                    errorMsg = $"线程数设置不正确,应填写正整数";
                    return;
                }

                if (this.cb_Global_WebProxyInfo_IG.Checked)
                {
                    if (this.txt_Global_WebProxyInfo_IG_IPAddress.Text.Trim().Length == 0)
                    {
                        errorMsg = $"要开启全局代理，必须填写代理地址";
                        return;
                    }
                }

                if (!this.rb_ForgotPwdSetting_Front_Custom_IG.Checked &&
                    !this.rb_ForgotPwdSetting_Front_Random_IG.Checked)
                {
                    errorMsg = $"请先设置忘记密码时设定密码的模式";
                    return;
                }

                if (this.rb_ForgotPwdSetting_Front_Custom_IG.Checked &&
                    string.IsNullOrEmpty(this.txt_ForgotPwdSetting_Front_Custom_Content_IG.Text.Trim()))
                {
                    errorMsg = $"密码前缀为自定义时需要设置自定义内容";
                    return;
                }

                if (Program.setting.Setting_IG.TaskInfoList == null ||
                    Program.setting.Setting_IG.TaskInfoList.Where(t => t.IsSelected).Count() == 0)
                {
                    errorMsg = $"至少设置1项执行任务";
                    return;
                }
            }));

            return errorMsg;
        }

        //启动任务方法
        private void ThreadMethod_StartTasks_IG_Main(object obj_SelectType)
        {
            int selectType = Convert.ToInt32(obj_SelectType);

            string errorMsg = string.Empty;
            //判断是否有现成在运行
            bool isRunning = false;
            if (Program.setting.Setting_IG.Account_List != null)
                isRunning = Program.setting.Setting_IG.Account_List.Where(a => a.Running_IsWorking).Count() > 0;

            if (!isRunning)
            {
                //数据验证
                errorMsg = this.Setting_Check_IG();
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    MessageBox.Show(errorMsg);
                    return;
                }
            }

            List<Account_FBOrIns> account_Selected = null;
            if (selectType == 0) account_Selected = Program.setting.Setting_IG.Account_List;
            else
            {
                List<int> iList = null;
                this.Invoke(new Action(() => { iList = this.GetSelectedIndexList(this.dgv_IG); }));
                if (iList.Count == 0)
                {
                    errorMsg = "请先选择需要操作的账号";
                    MessageBox.Show(errorMsg);
                    return;
                }

                account_Selected = iList.Select(i => Program.setting.Setting_IG.Account_List[i]).ToList();
            }

            if (!isRunning)
            {
                //禁用按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_IG.Enabled = false;
                    this.btn_ImportAccount_IG.Enabled = false;
                    this.btn_Start_IG.Enabled = false;
                    this.btn_Stop_IG.Enabled = true;

                    this.tsmi_Start_All_IG.Enabled = false;
                    this.tsmi_Stop_All_IG.Enabled = true;

                    this.tsmi_Delete_One_IG.Enabled = false;
                    this.tsmi_Delete_All_IG.Enabled = false;

                    this.dgv_GMList_ForBind_IG.Enabled = false;

                    this.btn_List_Order_IG.Enabled = false;
                }));

                this.SaveSetting_FromUser();
                this.SaveSetting_ToDisk();

                //线程设置
                if (this.stp_IG == null)
                {
                    this.stp_IG = new SmartThreadPool();
                }

                this.stp_IG.Concurrency = Program.setting.Setting_IG.ThreadCountMax;

                //统计显示
                this.ShowTongJiInfo_IG(0);
            }

            //派发任务
            for (int i = 0; i < account_Selected.Count; i++)
            {
                account_Selected[i].WorkItemResult =
                    this.stp_IG.QueueWorkItem(this.ThreadMethod_StartTasks_IG_Child, account_Selected[i]);
                Thread.Sleep(50);
                Application.DoEvents();
            }

            //等待任务结束，恢复按钮
            if (!isRunning)
            {
                while (!this.stp_IG.IsIdle)
                {
                    Thread.Sleep(500);
                    Application.DoEvents();
                }

                //恢复按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_IG.Enabled = true;
                    this.btn_ImportAccount_IG.Enabled = true;
                    this.btn_Start_IG.Enabled = true;
                    this.btn_Stop_IG.Enabled = false;

                    this.tsmi_Start_All_IG.Enabled = true;
                    this.tsmi_Stop_All_IG.Enabled = false;

                    this.tsmi_Delete_One_IG.Enabled = true;
                    this.tsmi_Delete_All_IG.Enabled = true;

                    this.dgv_GMList_ForBind_IG.Enabled = true;

                    this.btn_List_Order_IG.Enabled = true;
                }));
            }
        }

        //获取一个未使用的邮箱
        private MailInfo GetNotUsedMailInfo_IG()
        {
            MailInfo mail = null;

            lock (Program.setting.Setting_IG.Lock_Mail_ForBind_List)
            {
                if (Program.setting.Setting_IG.Mail_ForBind_List == null) mail = null;
                else
                    mail = Program.setting.Setting_IG.Mail_ForBind_List.Where(m => !m.Is_Used && !m.IsLocked)
                        .FirstOrDefault();

                if (mail != null) mail.IsLocked = true;
            }

            return mail;
        }

        //生成一个新的密码
        private string GetNewPassword_IG()
        {
            string newPwd = string.Empty;
            if (Program.setting.Setting_IG.ForgotPwdSetting_Front_Mode == 0)
                newPwd = StringHelper.GetRandomString(true, true, true, true, 10, 10);
            else newPwd = Program.setting.Setting_IG.ForgotPwdSetting_Front_Custom_Content.Trim();

            if (Program.setting.Setting_IG.ForgotPwdSetting_After_IsAddDate)
                newPwd += $"{DateTime.Now.ToString("MMdd")}";

            return newPwd;
        }

        //核心子线程
        private void ThreadMethod_StartTasks_IG_Child(Account_FBOrIns account)
        {
            JObject jo_Result = null;
            bool isSuccess = false;
            bool isMailUsed = false;
            int trySpan = 0;
            int tryTimes = 0;
            int tryTimesMax = 3;
            MailInfo mail = null;
            string taskName = string.Empty;
            TaskInfo task = null;
            string newPassword = string.Empty;

            account.Running_Log = "开始操作";
            Thread.Sleep(1000);

            #region 初始化代理/UA

            account.Running_Log = "初始化代理/UA";
            if (string.IsNullOrEmpty(account.UserAgent) || account.WebProxy == null)
            {
                if (string.IsNullOrEmpty(account.UserAgent)) account.UserAgent = StringHelper.CreateRandomUserAgent();
                if (account.WebProxy == null)
                {
                    if (Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_IsUse &&
                        !string.IsNullOrEmpty(Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_Url))
                    {
                        account.WebProxy =
                            new System.Net.WebProxy(Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_Url);
                        if (!string.IsNullOrEmpty(Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_UserName) &&
                            !string.IsNullOrEmpty(Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_Pwd))
                            account.WebProxy.Credentials = new NetworkCredential(
                                Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_UserName,
                                Program.setting.Setting_IG.Global_WebProxyInfo.Proxy_Pwd);
                    }
                }

                Thread.Sleep(1000);
            }

            #endregion

            #region 检测Cookie

            isSuccess = false;
            isMailUsed = true;
            trySpan = 1000;
            tryTimes = 0;
            tryTimesMax = 2;
            while (!isSuccess && tryTimes < tryTimesMax)
            {
                if (tryTimes > 0) Thread.Sleep(trySpan);
                tryTimes += 1;

                account.Running_Log = "检测Cookie是否有效";
                jo_Result = this.instagramService.Ins_LoginByCookie(account);
                account.Running_Log = jo_Result["ErrorMsg"].ToString();
                isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());

                if (!isSuccess && jo_Result["isNeedLoop"].ToString().ToLower() == "false") break;
            }

            //状态记录
            if (!isSuccess)
            {
                lock (Program.setting.Setting_IG.LockObj_TongJi_Real)
                {
                    if (account.Running_Log == "Cookie无效")
                    {
                        account.Account_Type_Des = "Cookie无效";
                        Program.setting.Setting_IG.TongJi_Real.WuXiao += 1;
                    }
                    else if (account.Running_Log.Contains("账户被锁定"))
                    {
                        account.Account_Type_Des = "账户被锁定";
                        Program.setting.Setting_IG.TongJi_Real.FengHaoShu += 1;
                    }
                    else
                    {
                        account.Account_Type_Des = "其它错误";
                        Program.setting.Setting_IG.TongJi_Real.QiTaCuoWu += 1;
                    }

                    this.ShowTongJiInfo_IG(0);
                }
            }

            if (!isSuccess) return;

            #endregion

            #region 检测并设置语言为英文

            //isSuccess = false;
            //isMailUsed = true;
            //trySpan = 1000;
            //tryTimes = 0;
            //tryTimesMax = 2;
            //while (!isSuccess && tryTimes < tryTimesMax)
            //{
            //    if (tryTimes > 0) Thread.Sleep(trySpan);
            //    tryTimes += 1;

            //    account.Running_Log = "检测并设置语言为英文";
            //    jo_Result = this.instagramService.Ins_UpdateLanguageLocale(account, "en_US");

            //    account.Running_Log = jo_Result["ErrorMsg"].ToString();
            //    isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());

            //    if (!isSuccess) continue;
            //}

            ////状态记录

            //if (!isSuccess)
            //{
            //    account.Account_Type_Des = "其它错误";

            //    lock (Program.setting.Setting_IG.LockObj_TongJi_Real) { Program.setting.Setting_IG.TongJi_Real.WuXiao += 1; this.ShowTongJiInfo_IG(0); }
            //}


            //if (!isSuccess) return;

            #endregion

            #region 进行邮箱绑定

            taskName = "BindNewEmail";
            task = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (string.IsNullOrEmpty(account.New_Mail_Name) || string.IsNullOrEmpty(account.New_Mail_Pwd))
                {
                    isSuccess = false;
                    isMailUsed = true;
                    trySpan = 1000;
                    tryTimes = 0;
                    tryTimesMax = 10;
                    while (!isSuccess && isMailUsed && tryTimes < tryTimesMax)
                    {
                        Thread.Sleep(trySpan);
                        tryTimes += 1;

                        account.Running_Log = "进行邮箱绑定";
                        mail = this.GetNotUsedMailInfo_IG();
                        if (mail == null)
                        {
                            account.Running_Log = "无可用邮箱";
                            break;
                        }

                        jo_Result = this.instagramService.Ins_BindNewEmail(account, mail);
                        account.Running_Log = jo_Result["ErrorMsg"].ToString();
                        isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());
                        isMailUsed = Convert.ToBoolean(jo_Result["IsMailUsed"].ToString());
                        //处理邮箱的绑定问题
                        lock (Program.setting.Setting_IG.Lock_Mail_ForBind_List)
                        {
                            mail.IsLocked = isMailUsed;
                            mail.Is_Used = isMailUsed;
                        }
                    }

                    if (isSuccess)
                    {
                        account.New_Mail_Name = mail.Mail_Name;
                        account.New_Mail_Pwd = mail.Mail_Pwd;
                    }
                    else
                    {
                        lock (Program.setting.Setting_IG.LockObj_TongJi_Real)
                        {
                            if (account.Running_Log == "需要验证设备")
                            {
                                account.Account_Type_Des = "需要验证设备";
                                Program.setting.Setting_IG.TongJi_Real.WuXiao += 1;
                            }
                            else if (account.Running_Log.Contains("需要验证其它方式"))
                            {
                                account.Account_Type_Des = "验证其它邮箱";
                                Program.setting.Setting_IG.TongJi_Real.FengHaoShu += 1;
                            }
                            else
                            {
                                account.Account_Type_Des = "其它错误";
                                Program.setting.Setting_IG.TongJi_Real.QiTaCuoWu += 1;
                            }

                            this.ShowTongJiInfo_IG(0);
                        }

                        return;
                    }

                    Thread.Sleep(1000);
                }
            }

            #endregion

            #region 进行忘记密码操作

            //暂时先固定密码
            //newPassword = $"niubi.fb";
            newPassword = this.GetNewPassword_IG();

            taskName = "ForgotPassword";
            task = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.New_Mail_Name) && !string.IsNullOrEmpty(account.New_Mail_Pwd) &&
                    string.IsNullOrEmpty(account.Facebook_Pwd))
                {
                    account.Running_Log = "进行忘记密码操作";
                    jo_Result = this.instagramService.Ins_ForgotPassword(account, newPassword,
                        Program.setting.Setting_IG.ForgotPwdSetting_LogMeOut);

                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                    isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());

                    if (isSuccess)
                    {
                        account.Facebook_Pwd = newPassword;
                    }
                    else
                    {
                        lock (Program.setting.Setting_IG.LockObj_TongJi_Real)
                        {
                            account.Account_Type_Des = "其它错误";
                            Program.setting.Setting_IG.TongJi_Real.QiTaCuoWu += 1;

                            this.ShowTongJiInfo_IG(0);
                        }

                        return;
                    }
                }
            }

            #endregion

            #region 进行打开2FA操作

            taskName = "OpenTwoFA_Dynamic";
            task = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.New_Mail_Name) && !string.IsNullOrEmpty(account.New_Mail_Pwd) &&
                    !string.IsNullOrEmpty(account.Facebook_Pwd))
                {
                    if (string.IsNullOrEmpty(account.TwoFA_Dynamic_SecretKey))
                    {
                        account.Running_Log = "进行打开2FA操作";
                        jo_Result = this.instagramService.Ins_OpenTwoFA_Dynamic(account);

                        account.Running_Log = jo_Result["ErrorMsg"].ToString();
                        isSuccess = Convert.ToBoolean(jo_Result["Success"].ToString());

                        if (isSuccess) account.Account_Type_Des = "已完成";
                        else
                        {
                            lock (Program.setting.Setting_IG.LockObj_TongJi_Real)
                            {
                                if (account.Running_Log == "需要验证设备")
                                {
                                    account.Account_Type_Des = "需要验证设备";
                                    Program.setting.Setting_IG.TongJi_Real.WuXiao += 1;
                                }
                                else if (account.Running_Log.Contains("需要验证其它方式"))
                                {
                                    account.Account_Type_Des = "验证其它邮箱";
                                    Program.setting.Setting_IG.TongJi_Real.FengHaoShu += 1;
                                }
                                else
                                {
                                    account.Account_Type_Des = "其它错误";
                                    Program.setting.Setting_IG.TongJi_Real.QiTaCuoWu += 1;
                                }

                                this.ShowTongJiInfo_IG(0);
                            }
                        }
                    }
                }
            }

            #endregion

            #region 删除FB关联

            taskName = "RemoveFBAccount";
            task = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.TwoFA_Dynamic_SecretKey))
                {
                    account.Running_Log = "进行删除FB关联操作";
                    jo_Result = this.instagramService.Ins_RemoveFBAccount(account);

                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                    account.Log_RemoveInsAccount = jo_Result["ErrorMsg"].ToString();
                }
            }

            #endregion

            #region 删除其它登录会话

            taskName = "LogOutOfOtherSession";
            task = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.TwoFA_Dynamic_SecretKey))
                {
                    account.Running_Log = "进行删除其它登录会话操作";
                    jo_Result = this.instagramService.Ins_LogOutOfOtherSession(account);

                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                    account.Log_RemoveInsAccount = jo_Result["ErrorMsg"].ToString();
                }
            }

            #endregion

            #region 删除信任设备(Ins没有这个操作)

            //taskName = "TwoFactorRemoveTrustedDevice";
            //task = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            //if (task != null && task.IsSelected)
            //{
            //    if (!string.IsNullOrEmpty(account.TwoFA_Dynamic_SecretKey))
            //    {
            //        account.Running_Log = "删除信任设备";
            //        jo_Result = this.instagramService.Ins_TwoFactorRemoveTrustedDevice(account);

            //        account.Running_Log = jo_Result["ErrorMsg"].ToString();
            //        account.Log_RemoveInsAccount = jo_Result["ErrorMsg"].ToString();
            //    }
            //}

            #endregion

            #region 删除联系方式|查国家)

            taskName = "DeleteOtherContacts";
            task = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                if (!string.IsNullOrEmpty(account.TwoFA_Dynamic_SecretKey))
                {
                    account.Running_Log = "进行删除联系方式操作";
                    jo_Result = this.instagramService.Ins_DeleteOtherContacts_QueryCountry(account);

                    //查询国家
                    if (string.IsNullOrEmpty(account.GuoJia) && jo_Result["GuoJia"].ToString().Trim().Length > 0)
                        account.GuoJia = jo_Result["GuoJia"].ToString().Trim();

                    account.Running_Log = jo_Result["ErrorMsg"].ToString();
                    account.Log_RemoveInsAccount = jo_Result["ErrorMsg"].ToString();
                }
            }

            #endregion

            #region 查国家(先查联系方式，再查城市)

            taskName = "Query_Country";
            task = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "进行查国家操作";
                jo_Result = this.instagramService.Ins_Query_Country(account);

                if (!string.IsNullOrEmpty(jo_Result["GuoJia"].ToString()))
                    account.GuoJia = jo_Result["GuoJia"].ToString();

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
            }

            #endregion

            #region 查生日

            taskName = "Query_Birthday";
            task = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "进行查生日操作";
                jo_Result = this.instagramService.Ins_Query_Birthday(account);

                if (!string.IsNullOrEmpty(jo_Result["ShengRi"].ToString()))
                    account.ShengRi = jo_Result["ShengRi"].ToString();

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
            }

            #endregion

            #region 查注册日期

            taskName = "Query_ZhuCeRiQi";
            task = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "进行查注册日期操作";
                jo_Result = this.instagramService.Ins_Query_ZhuCeRiQi(account);

                if (!string.IsNullOrEmpty(jo_Result["ZhuCeRiQi"].ToString()))
                    account.ZhuCeRiQi = jo_Result["ZhuCeRiQi"].ToString();

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
            }

            #endregion

            #region 查帖子|好友|关注

            taskName = "Query_Posts_Followers_Following";
            task = Program.setting.Setting_IG.TaskInfoList.Where(t => t.TaskName == taskName).FirstOrDefault();
            if (task != null && task.IsSelected)
            {
                account.Running_Log = "进行查帖子|好友|关注操作";
                jo_Result = this.instagramService.Ins_Query_Posts_Followers_Following(account);

                if (!string.IsNullOrEmpty(jo_Result["TieZiCount"].ToString()))
                    account.TieZiCount = jo_Result["TieZiCount"].ToString();
                if (!string.IsNullOrEmpty(jo_Result["HaoYouCount"].ToString()))
                    account.HaoYouCount = jo_Result["HaoYouCount"].ToString();
                if (!string.IsNullOrEmpty(jo_Result["GuanZhuCount"].ToString()))
                    account.GuanZhuCount = jo_Result["GuanZhuCount"].ToString();

                account.Running_Log = jo_Result["ErrorMsg"].ToString();
            }

            #endregion
        }

        private void Method_StopTasks_IG(int selectType = 0)
        {
            List<Account_FBOrIns> account_Selected = null;
            if (selectType == 0) account_Selected = Program.setting.Setting_IG.Account_List;
            else
            {
                List<int> iList = null;
                this.Invoke(new Action(() => { iList = this.GetSelectedIndexList(this.dgv_IG); }));
                if (iList.Count == 0)
                {
                    MessageBox.Show("请先选择需要操作的账号");
                    return;
                }

                account_Selected = iList.Select(i => Program.setting.Setting_IG.Account_List[i]).ToList();
            }

            //停止子线程
            for (int i = 0; i < account_Selected.Count; i++)
            {
                if (account_Selected[i].WorkItemsGroup != null) account_Selected[i].WorkItemsGroup.Cancel(true);
                if (account_Selected[i].WorkItemResult != null) account_Selected[i].WorkItemResult.Cancel(true);
                //account_Selected[i].Running_Log = "操作中止";
            }

            if (selectType == 0)
            {
                //停止主线程
                if (this.stp_IG != null) this.stp_IG.Cancel(true);
                if (this.thread_Main_IG != null) this.thread_Main_IG.Abort();

                this.SaveSetting_FromUser();
                this.SaveSetting_ToDisk();

                //恢复按钮
                this.Invoke(new Action(() =>
                {
                    this.btn_ClearData_IG.Enabled = true;
                    this.btn_ImportAccount_IG.Enabled = true;
                    this.btn_Start_IG.Enabled = true;
                    this.btn_Stop_IG.Enabled = false;

                    this.tsmi_Start_All_IG.Enabled = true;
                    this.tsmi_Stop_All_IG.Enabled = false;

                    this.tsmi_Delete_One_IG.Enabled = true;
                    this.tsmi_Delete_All_IG.Enabled = true;

                    this.dgv_GMList_ForBind_IG.Enabled = true;

                    this.btn_List_Order_IG.Enabled = true;
                }));
            }
        }

        private void btn_Start_IG_Click(object sender, EventArgs e)
        {
            this.thread_Main_IG = new Thread(new ParameterizedThreadStart(this.ThreadMethod_StartTasks_IG_Main));
            this.thread_Main_IG.IsBackground = true;
            this.thread_Main_IG.Start(0);
        }

        private void btn_Stop_IG_Click(object sender, EventArgs e)
        {
            this.Method_StopTasks_IG(0);
        }

        #endregion

        /*以上代码为Ins部分*/

        #region 工具相关

        //导入文件
        private void btn_Import_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开文件";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "Zip压缩文件(*.zip)|*.zip";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            //多选模式
            openFileDialog.Multiselect = true;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.ImportFiles(openFileDialog.FileNames);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private ICheckingMethod CreateICheckingMethod(string CheckingMethod)
        {
            string currentAssemblyName = $"{Assembly.GetExecutingAssembly().FullName.Split(',')[0]}";
            string className = $"{currentAssemblyName}.DAL.CheckingMethod_{CheckingMethod}";
            //使用反射获取类型信息
            Type type = Type.GetType(className);

            if (type == null)
            {
                Console.WriteLine($"类型 {className} 未找到。");
                return null;
            }

            // 使用Activator.CreateInstance创建类的实例
            return (ICheckingMethod)Activator.CreateInstance(type);
        }

        #endregion

        #region DGV右键菜单和行号显示

        //添加行号
        private void dgv_Main_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int i = 0; i < e.RowCount; i++)
            {
                this.dgv_Main.Rows[e.RowIndex + i].HeaderCell.Style.Alignment =
                    DataGridViewContentAlignment.MiddleRight;
                this.dgv_Main.Rows[e.RowIndex + i].HeaderCell.Value = (e.RowIndex + i + 1).ToString();
            }
        }

        //移除行号
        private void dgv_Main_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            for (int i = e.RowIndex + e.RowCount; i < this.dgv_Main.Rows.Count; i++)
            {
                this.dgv_Main.Rows[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                this.dgv_Main.Rows[i].HeaderCell.Value = (i + 1).ToString();
            }
        }

        //DGV右键菜单
        private void dgv_Main_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;

            if (e.Button == MouseButtons.Right)
            {
                this.dgv_Main.CurrentCell = this.dgv_Main.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (this.dgv_Main.CurrentCell != null && !this.dgv_Main.CurrentCell.Selected)
                    this.dgv_Main.CurrentCell.Selected = true;

                this.cms_dgv_Main.Show(MousePosition.X, MousePosition.Y);
            }
        }

        //获取选中的索引列表
        private List<int> GetSelectedIndexList()
        {
            List<int> iList = new List<int>();
            if (this.dgv_Main.SelectedCells == null || this.dgv_Main.SelectedCells.Count == 0) return iList;
            foreach (DataGridViewCell cell in this.dgv_Main.SelectedCells)
            {
                if (!iList.Contains(cell.RowIndex)) iList.Add(cell.RowIndex);
            }

            iList = iList.OrderBy(i => i).ToList();

            return iList;
        }

        //删除单个记录
        private void tsmi_Delete_One_ck_tool_Click(object sender, EventArgs e)
        {
            List<int> iList = this.GetSelectedIndexList();
            if (iList.Count == 0) return;

            this.dgv_Main.DataSource = null;

            //删除
            int deleteCount = 0;
            int rIndex = 0;
            for (int i = 0; i < iList.Count; i++)
            {
                rIndex = iList[i] - deleteCount;

                //停止
                if (Program.setting.TaskInfos[rIndex].WorkStatusDes == "运行中")
                {
                    Program.setting.TaskInfos[rIndex].WorkItemsGroup.Cancel(true);
                    Program.setting.TaskInfos[rIndex].WorkItemResult.Cancel(true);
                }

                Program.setting.TaskInfos.RemoveAt(rIndex);
                deleteCount = deleteCount + 1;
            }

            if (Program.setting.TaskInfos != null && Program.setting.TaskInfos.Count > 0)
                this.dgv_Main.DataSource = Program.setting.TaskInfos;

            //判断是否有运行中的线程
            if (Program.setting.TaskInfos.Where(t => t.WorkStatusDes == "运行中").Count() == 0)
                this.ThreadMethod_StopWork_ck_tool();
        }

        //清空全部记录
        private void tsmi_Delete_All_ck_tool_Click(object sender, EventArgs e)
        {
            this.dgv_Main.DataSource = null;

            this.ThreadMethod_StopWork_ck_tool();

            Program.setting.TaskInfos.Clear();
        }

        #endregion

        #region DGV显示下拉框

        //定义下拉列表框
        private ComboBox cmb_Temp = new ComboBox();

        //定义下拉框键值对
        private Dictionary<string, string> dic_CheckingMethodList = new Dictionary<string, string>()
        {
            //前面是值，后面是自定义的显示的内容
            { "None", "无验证" },
            { "Outlook", "Outlook" },
        };

        //定义下拉框键值对
        private Dictionary<string, string> dic_CountryList = new Dictionary<string, string>()
        {
            //前面是值，后面是自定义的显示的内容
            { "14", "香港" },
            { "12", "美国（虚拟）" },
            { "33", "哥伦比亚" },
            { "16", "英格兰" },
            { "2", "哈萨克斯坦" },
            { "22", "印度" },
            { "0", "俄罗斯" },
            { "6", "印度尼西亚" },
            { "11", "吉尔吉斯斯坦" },
            { "7", "马来西亚" },
            { "10", "越南" },
        };

        //Dgv所在的列明
        private string cbbColumnName = $"CheckingMethod";

        //初始化方法，程序开始时运行这个方法
        private void DgvCbbInit()
        {
            //绑定下拉列表框
            BindCheckingMethod();

            //设置下拉列表框不可见
            cmb_Temp.Visible = false;

            //下拉框风格
            cmb_Temp.BackColor = Color.White;

            //添加下拉列表框事件
            cmb_Temp.SelectedIndexChanged += new EventHandler(cmb_Temp_SelectedIndexChanged);

            //将下拉列表框加入到DataGridView控件中
            this.dgv_DomainList.Controls.Add(cmb_Temp);

            this.dgv_DomainList.CurrentCellChanged += this.dgv_DomainList_CurrentCellChanged;
            this.dgv_DomainList.Scroll += this.dgv_DomainList_Scroll;
            this.dgv_DomainList.ColumnWidthChanged += this.dgv_DomainList_ColumnWidthChanged;
        }

        //绑定下拉列表框
        private void BindCheckingMethod()
        {
            DataTable dtCheckingMethod = new DataTable();
            dtCheckingMethod.Columns.Add("Value");
            dtCheckingMethod.Columns.Add("Name");
            DataRow drCheckingMethod;
            foreach (var kvp in this.dic_CheckingMethodList)
            {
                drCheckingMethod = dtCheckingMethod.NewRow();
                drCheckingMethod[0] = kvp.Key;
                drCheckingMethod[1] = kvp.Value;
                dtCheckingMethod.Rows.Add(drCheckingMethod);
            }

            cmb_Temp.ValueMember = "Value";
            cmb_Temp.DisplayMember = "Name";
            cmb_Temp.DataSource = dtCheckingMethod;
            cmb_Temp.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        //绑定下拉列表框
        private void SetCountry_EM()
        {
            DataTable dtCheckingMethod = new DataTable();
            dtCheckingMethod.Columns.Add("Value");
            dtCheckingMethod.Columns.Add("Name");
            DataRow drCheckingMethod;
            foreach (var kvp in this.dic_CountryList)
            {
                drCheckingMethod = dtCheckingMethod.NewRow();
                drCheckingMethod[0] = kvp.Key;
                drCheckingMethod[1] = kvp.Value;
                dtCheckingMethod.Rows.Add(drCheckingMethod);
            }

            comboBox_country_EM.ValueMember = "Value";
            comboBox_country_EM.DisplayMember = "Name";
            comboBox_country_EM.DataSource = dtCheckingMethod;
        }

        //绑定下拉列表框
        private void SetCountry_IN_RE()
        {
            DataTable dtCheckingMethod = new DataTable();
            dtCheckingMethod.Columns.Add("Value");
            dtCheckingMethod.Columns.Add("Name");
            DataRow drCheckingMethod;
            foreach (var kvp in this.dic_CountryList)
            {
                drCheckingMethod = dtCheckingMethod.NewRow();
                drCheckingMethod[0] = kvp.Key;
                drCheckingMethod[1] = kvp.Value;
                dtCheckingMethod.Rows.Add(drCheckingMethod);
            }

            comboBox_country_IN_RE.ValueMember = "Value";
            comboBox_country_IN_RE.DisplayMember = "Name";
            comboBox_country_IN_RE.DataSource = dtCheckingMethod;
        }

        //当用户移动到这一列时单元格显示下拉列表框
        private void dgv_DomainList_CurrentCellChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.dgv_DomainList.CurrentCell == null) return;

                DataGridViewColumn col = this.dgv_DomainList.Columns[this.dgv_DomainList.CurrentCell.ColumnIndex];
                if (col.Name == cbbColumnName)
                {
                    Rectangle rect = dgv_DomainList.GetCellDisplayRectangle(dgv_DomainList.CurrentCell.ColumnIndex,
                        dgv_DomainList.CurrentCell.RowIndex, false);
                    string cValue = dgv_DomainList.CurrentCell.Value == null
                        ? string.Empty
                        : dgv_DomainList.CurrentCell.Value.ToString().Trim();

                    if (this.dic_CheckingMethodList.ContainsKey(cValue))
                        cmb_Temp.Text = this.dic_CheckingMethodList[cValue];

                    cmb_Temp.Left = rect.Left;
                    cmb_Temp.Top = rect.Top;
                    cmb_Temp.Width = rect.Width;
                    cmb_Temp.Height = rect.Height;
                    cmb_Temp.Visible = true;
                }
                else cmb_Temp.Visible = false;
            }
            catch
            {
            }
        }

        //当用户选择下拉列表框时改变DataGridView单元格的内容
        private void cmb_Temp_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cbb = ((ComboBox)sender);

            if (this.dic_CheckingMethodList.Where(k => k.Value == cbb.Text.Trim()).Count() > 0)
            {
                var kvp = this.dic_CheckingMethodList.Where(k => k.Value == cbb.Text.Trim()).FirstOrDefault();
                this.dgv_DomainList.CurrentCell.Value = kvp.Key;
                this.dgv_DomainList.CurrentCell.Tag = kvp.Value;
            }
        }

        //滚动DataGridView时将下拉列表框设为不可见
        private void dgv_DomainList_Scroll(object sender, ScrollEventArgs e)
        {
            this.cmb_Temp.Visible = false;
        }

        //改变DataGridView列宽时将下拉列表框设为不可见
        private void dgv_DomainList_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            this.cmb_Temp.Visible = false;
        }

        #endregion

        #region 导出导入

        //导入的具体方法
        private void ImportFiles(string[] files)
        {
            if (files == null || files.Length == 0) return;

            if (Program.setting.TaskInfos == null) Program.setting.TaskInfos = new List<ToolTaskInfo>();

            bool isUpdate = false;
            for (int i = 0; i < files.Length; i++)
            {
                ToolTaskInfo t = Program.setting.TaskInfos.Where(ta => ta.ZipFullName == files[i]).FirstOrDefault();
                if (t == null)
                {
                    t = new ToolTaskInfo();
                    t.ZipFullName = files[i];

                    if (!isUpdate)
                    {
                        isUpdate = true;
                        this.Invoke(new Action(() => { this.dgv_Main.DataSource = null; }));
                    }

                    Program.setting.TaskInfos.Add(t);
                }
            }

            if (isUpdate)
            {
                this.Invoke(new Action(() => { this.dgv_Main.DataSource = Program.setting.TaskInfos; }));
            }

            // if (files == null || files.Length == 0) return;
            //
            // if (Program.setting.Setting_CK.TaskInfos == null) Program.setting.Setting_CK.TaskInfos = new List<ToolTaskInfo>();
            //
            // bool isUpdate = false;
            // for (int i = 0; i < files.Length; i++)
            // {
            //     ToolTaskInfo t = Program.setting.Setting_CK.TaskInfos.Where(ta => ta.ZipFullName == files[i]).FirstOrDefault();
            //     if (t == null)
            //     {
            //         t = new ToolTaskInfo();
            //         t.ZipFullName = files[i];
            //
            //         if (!isUpdate)
            //         {
            //             isUpdate = true;
            //             this.Invoke(new Action(() => { this.dgv_Main.DataSource = null; }));
            //         }
            //
            //         Program.setting.Setting_CK.TaskInfos.Add(t);
            //     }
            // }
            //
            // if (isUpdate)
            // {
            //     this.Invoke(new Action(() => { this.dgv_Main.DataSource = Program.setting.Setting_CK.TaskInfos; }));
            // }
        }

        //拖动导入事件
        private void dgv_Main_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        //拖动导入事件
        private void dgv_Main_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            string[] fList = files.Where(s => s.ToLower().LastIndexOf(".zip") == s.Length - 4).ToArray();

            if (fList.Length == 0)
            {
                MessageBox.Show($"导入账号时发生错误：只能导入Zip格式文档", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ImportFiles(fList);
        }

        //导入文件
        private void btn_GetCk_Import_ck_tool_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开文件";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "Zip压缩文件(*.zip)|*.zip";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            //多选模式
            openFileDialog.Multiselect = true;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.ImportFiles(openFileDialog.FileNames);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //导出结果
        private void btn_Export_ck_tool_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(this.ThreadMethod_Export_ck_tool));
            thread.IsBackground = true;
            thread.Start();
        }

        //导出结果线程方法
        private void ThreadMethod_Export_ck_tool()
        {
            if (Program.setting.TaskInfos == null || Program.setting.TaskInfos.Count == 0) return;

            List<CookieInfo> cksAll = Program.setting.TaskInfos.Where(t => t.SuccessList != null)
                .SelectMany(t => t.SuccessList).ToList();

            if (cksAll.Count == 0) return;
            List<CookieInfo> passwordAll = Program.setting.TaskInfos.Where(t => t.SuccessPasswordList != null)
                .SelectMany(t => t.SuccessPasswordList).ToList();

            if (cksAll.Count == 0) return;

            //根据域名分组
            var group = cksAll.GroupBy(c => c.MatchDomainInfo.DomainName);
            var groupPassword = passwordAll.GroupBy(c => c.MatchDomainInfo.DomainName);

            #region 设置保存目录

            string saveDir = $@"{Application.StartupPath}\Results";
            if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

            this.Invoke(new Action(() =>
            {
                this.btn_Export_ck_tool.Enabled = false;

                // 创建FolderBrowserDialog对象
                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

                // 设置对话框的描述信息
                folderBrowserDialog.Description = $"请选择保存的目录(共有 {group.FirstOrDefault().Key} 等 {group.Count()} 个文件)";

                // 设置初始目录
                folderBrowserDialog.SelectedPath = saveDir;
                saveDir = string.Empty;

                // 显示FolderBrowserDialog窗口，并等待用户选择文件夹或取消
                DialogResult result = folderBrowserDialog.ShowDialog();

                // 判断用户是否点击了确定按钮
                if (result == DialogResult.OK) saveDir = folderBrowserDialog.SelectedPath;
            }));

            if (string.IsNullOrEmpty(saveDir)) return;

            #endregion

            //按照分组保存Excel文件
            foreach (var kvp in group)
            {
                // 创建一个新的Excel包（即Excel文件）
                using (var package = new ExcelPackage())
                {
                    // 添加一个名为“Sheet1”的工作表
                    var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                    // 向工作表添加一些数据
                    // 从A1单元格开始写入数据
                    worksheet.Cells["A1"].LoadFromCollection<string>(kvp.Select(k => k.CookieJsonStr));

                    // 设置文件保存路径
                    var fileInfo = new FileInfo($@"{saveDir}\{kvp.Key.Replace(".", "_")}.xlsx");

                    // 将Excel包保存到文件
                    package.SaveAs(fileInfo);
                }
            }

            //按照分组保存Excel文件
            foreach (var kvp in groupPassword)
            {
                // 创建一个新的Excel包（即Excel文件）
                using (var package = new ExcelPackage())
                {
                    // 添加一个名为“Sheet1”的工作表
                    var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                    // 向工作表添加一些数据
                    // 从A1单元格开始写入数据
                    worksheet.Cells["A1"].LoadFromCollection<string>(kvp.Select(k => k.CookieJsonStr));

                    // 设置文件保存路径
                    var fileInfo = new FileInfo($@"{saveDir}\{kvp.Key.Replace(".", "_")}_password.xlsx");

                    // 将Excel包保存到文件
                    package.SaveAs(fileInfo);
                }
            }

            //打开保存的目录
            Process.Start("explorer.exe", saveDir);

            this.Invoke(new Action(() => { this.btn_Export_ck_tool.Enabled = true; }));
        }

        #endregion

        #region 核心方法

        private SmartThreadPool stp = null;
        private Thread thread_Main = null;

        private void ThreadMethod_StopWork_ck_tool()
        {
            //先停止线程组的子线程，在停止线程组线程，再停止主线程
            if (Program.setting.TaskInfos != null || Program.setting.TaskInfos.Count > 0)
            {
                for (int i = 0; i < Program.setting.TaskInfos.Count; i++)
                {
                    ToolTaskInfo toolTaskInfo = Program.setting.TaskInfos[i];
                    if (toolTaskInfo != null)
                    {
                        if (toolTaskInfo.WorkItemsGroup != null) toolTaskInfo.WorkItemsGroup.Cancel(true);
                        if (toolTaskInfo.WorkItemResult != null) toolTaskInfo.WorkItemResult.Cancel(true);
                    }
                }
            }

            if (this.stp != null) this.stp.Cancel(true);

            if (this.thread_Main != null) this.thread_Main.Abort();

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.dgv_Main.AllowDrop = true;
                this.cms_dgv_Main.Enabled = true;
                this.dgv_DomainList.Enabled = true;
                this.btn_Import_ck_tool.Enabled = true;
                this.btn_Export_ck_tool.Enabled = true;
                this.btn_Start_ck_tool.Enabled = true;
                this.btn_Stop_ck_tool.Enabled = false;
            }));
        }

        //停止操作
        private void btn_Stop_ck_tool_Click(object sender, EventArgs e)
        {
            this.ThreadMethod_StopWork_ck_tool();
        }

        //启动操作
        private void btn_Start_ck_tool_Click(object sender, EventArgs e)
        {
            this.thread_Main = new Thread(new ThreadStart(this.ThreadMethod_MainWork));
            this.thread_Main.IsBackground = true;
            this.thread_Main.Start();
        }

        private string Setting_Check()
        {
            string errorMsg = string.Empty;

            int num;
            List<string> sList = new List<string>();
            this.Invoke(new Action(() =>
            {
                if (this.dgv_DomainList.RowCount == 0)
                {
                    errorMsg = $"请输入域名匹配规则";
                    return;
                }

                for (int i = 0; i < this.dgv_DomainList.RowCount; i++)
                {
                    DataGridViewRow row = this.dgv_DomainList.Rows[i];
                    if (row == null || row.Cells == null || row.Cells.Count < 2
                        || row.Cells["DomainName"] == null || row.Cells["DomainListStr"] == null
                        || row.Cells["DomainName"].Value == null || row.Cells["DomainListStr"].Value == null
                        || string.IsNullOrEmpty(row.Cells["DomainName"].Value.ToString()) ||
                        string.IsNullOrEmpty(row.Cells["DomainListStr"].Value.ToString())
                       ) continue;

                    if (sList.Contains(row.Cells["DomainName"].Value.ToString()))
                    {
                        errorMsg = $"域名称 {row.Cells["DomainName"].Value.ToString()} 不能重复";
                        return;
                    }

                    sList.Add(row.Cells["DomainName"].Value.ToString());
                }

                if (sList.Count == 0)
                {
                    errorMsg = $"无有效域名匹配规则，请检查名称和匹配规则都是否输入完整";
                    return;
                }

                if (this.txt_ThreadCount_ck_tool.Text.Trim().Length == 0)
                {
                    errorMsg = $"请输入工作线程数";
                    this.txt_ThreadCount_ck_tool.Focus();
                    return;
                }

                if (!int.TryParse(this.txt_ThreadCount_ck_tool.Text.Trim(), out num) || num <= 0)
                {
                    errorMsg = $"工作线程数必须为正整数";
                    this.txt_ThreadCount_ck_tool.Focus();
                    return;
                }
            }));

            if (Program.setting.TaskInfos == null || Program.setting.TaskInfos.Count == 0)
            {
                errorMsg = $"请先导入需要处理的压缩包（zip）";
                return errorMsg;
            }

            return errorMsg;
        }

        //主线程
        private void ThreadMethod_MainWork()
        {
            //验证用户配置
            string errorMsg = this.Setting_Check();
            if (!string.IsNullOrEmpty(errorMsg))
            {
                MessageBox.Show(errorMsg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //保存用户配置
            this.SaveSetting_FromUser();
            this.SaveSetting_ToDisk();

            //禁用按钮
            this.Invoke(new Action(() =>
            {
                this.dgv_Main.AllowDrop = false;
                this.cms_dgv_Main.Enabled = false;
                this.dgv_DomainList.Enabled = false;
                this.btn_Import_ck_tool.Enabled = false;
                this.btn_Export_ck_tool.Enabled = false;
                this.btn_Start_ck_tool.Enabled = false;
                this.btn_Stop_ck_tool.Enabled = true;
            }));

            //保存用户配置
            this.SaveSetting_FromUser();
            //写出用户配置
            this.SaveSetting_ToDisk();

            //线程池设置
            if (this.stp == null)
            {
                this.stp = new SmartThreadPool();
                this.stp.Concurrency = Program.setting.ThreadCountMax;
            }

            //开启任务(创建线程组任务)
            for (int i = 0; i < Program.setting.TaskInfos.Count; i++)
            {
                ToolTaskInfo toolTaskInfo = Program.setting.TaskInfos[i];

                toolTaskInfo.WorkItemResult = this.stp.QueueWorkItem(this.ThreadMethod_GroupWork, toolTaskInfo);

                Thread.Sleep(50);
                Application.DoEvents();
            }

            //等待结束
            int timeSpan = 500;
            while (!this.stp.IsIdle)
            {
                Thread.Sleep(timeSpan);
                Application.DoEvents();
            }

            //恢复按钮
            this.Invoke(new Action(() =>
            {
                this.dgv_Main.AllowDrop = true;
                this.cms_dgv_Main.Enabled = true;
                this.dgv_DomainList.Enabled = true;
                this.btn_Import_ck_tool.Enabled = true;
                this.btn_Export_ck_tool.Enabled = true;
                this.btn_Start_ck_tool.Enabled = true;
                this.btn_Stop_ck_tool.Enabled = false;
            }));
        }

        //线程组方法
        private void ThreadMethod_GroupWork(ToolTaskInfo toolTaskInfo)
        {
            toolTaskInfo.WorkItemsGroup = this.stp.CreateWorkItemsGroup(Program.setting.ThreadCountMax);

            if (!File.Exists(toolTaskInfo.ZipFullName)) toolTaskInfo.WorkLog = $"文件不存在";

            toolTaskInfo.TxtFileList = new List<string>();
            toolTaskInfo.CompleteCount = 0;
            toolTaskInfo.MatchCount = 0;
            toolTaskInfo.SuccessList = new List<CookieInfo>();

            //一边解压，一边读取(等待所有子线程结束后，再释放ZipFile对象)
            toolTaskInfo.WorkLog = $"解压并读取...";
            // ZipFile zip = ZipFile.Read(toolTaskInfo.ZipFullName);

            // foreach (ZipEntry e in zip)
            // {
            //     if (!Path.GetExtension(e.FileName).Equals(".txt", StringComparison.OrdinalIgnoreCase)) continue;
            //
            //     //读取文档内容
            //     string textContent;
            //     using (var stream = e.OpenReader())
            //     {
            //         using (var reader = new StreamReader(stream))
            //         {
            //             textContent = reader.ReadToEnd();
            //         }
            //     }
            //
            //     if (string.IsNullOrEmpty(textContent)) continue;
            //
            //     //加入文档列表
            //     toolTaskInfo.TxtFileList.Add(e.FileName);
            //
            //     //投递到子线程进行匹配
            //     toolTaskInfo.WorkItemsGroup.QueueWorkItem(this.ThreadMethod_ChildWork, toolTaskInfo, textContent,
            //         e.FileName);
            //     Thread.Sleep(10);
            // }

            toolTaskInfo.WorkLog = $"解压完成，正在读取...";

            //等待结束
            while (!toolTaskInfo.WorkItemsGroup.IsIdle)
            {
                Thread.Sleep(300);
                Application.DoEvents();
            }

            //读取完成
            toolTaskInfo.WorkLog = $"已完成";
        }

        //子线程方法
        private void ThreadMethod_ChildWork(ToolTaskInfo toolTaskInfo, string textContent, string fileName)
        {
            if (fileName.Contains("passwords"))
            {
                if (Program.setting.IsGetPassword)
                {
                    var passwordJsonStr = this.GetPasswordJsonStr(textContent);
                    toolTaskInfo.SuccessPasswordList.Add(passwordJsonStr);
                }
            }

            CookieInfo cookieInfo = null;

            //匹配Cookie
            if (!string.IsNullOrEmpty(textContent)) cookieInfo = this.GetCookieJsonStr(textContent);

            //记录完成
            lock (toolTaskInfo.LockObject.Lock_CompleteCount)
            {
                toolTaskInfo.CompleteCount += 1;
            }

            //记录匹配成功
            if (cookieInfo != null)
            {
                //验证是否有效
                bool isMatch = cookieInfo.MatchDomainInfo.ICheckingMethod == null;
                if (!isMatch)
                    isMatch = cookieInfo.MatchDomainInfo.ICheckingMethod.CookieChecking(cookieInfo.CookieJsonStr);
                if (isMatch)
                    lock (toolTaskInfo.LockObject.Lock_MatchCount)
                    {
                        toolTaskInfo.MatchCount += 1;
                    }

                if (isMatch) toolTaskInfo.SuccessList.Add(cookieInfo);
            }
        }

        //提取Cookie的方法
        private CookieInfo GetCookieJsonStr(string textContent)
        {
            CookieInfo cookieInfo = null;
            bool isMatch = false;

            for (int i = 0; i < Program.setting.DomainInfos.Count; i++)
            {
                DomainInfo domainInfo = Program.setting.DomainInfos[i];

                isMatch = false;

                for (int j = 0; j < domainInfo.DomainPatternList.Count; j++)
                {
                    isMatch = Regex.IsMatch(textContent, domainInfo.DomainPatternList[j]);
                    if (isMatch)
                    {
                        MatchCollection matchCollection = Regex.Matches(textContent, domainInfo.DomainPatternList[j]);

                        if (matchCollection == null || matchCollection.Count == 0) continue;

                        JArray ja_cookies = new JArray();
                        foreach (Match m in matchCollection.Cast<Match>())
                        {
                            JObject jo = new JObject();

                            jo.Add("name", m.Groups[6].Value.Trim());
                            jo.Add("value", m.Groups[7].Value.Trim());
                            jo.Add("domain", m.Groups[1].Value.Trim());
                            jo.Add("path", m.Groups[3].Value.Trim());
                            jo.Add("secure", m.Groups[4].Value.Trim().ToLower() == "true");
                            jo.Add("httpOnly", m.Groups[2].Value.Trim().ToLower() == "true");
                            jo.Add("sameSite", "unspecified");
                            jo.Add("expiry", m.Groups[5].Value.Trim());

                            ja_cookies.Add(jo);
                        }

                        cookieInfo = new CookieInfo();
                        cookieInfo.MatchDomainInfo = Program.setting.DomainInfos[i];
                        cookieInfo.CookieJsonStr = JsonConvert.SerializeObject(ja_cookies);

                        break;
                    }
                }

                if (isMatch) break;
            }

            return cookieInfo;
        }

        //提取Password的方法
        private CookieInfo GetPasswordJsonStr(string textContent)
        {
            string patternUrl = @"url:\s*(.*)";
            string patternLogin = @"login:\s*(.*)";
            string patternPassword = @"password:\s*(.*)";

            // 创建正则表达式对象
            Regex regexUrl = new Regex(patternUrl);
            Regex regexLogin = new Regex(patternLogin);
            Regex regexPassword = new Regex(patternPassword);

            // 查找匹配项
            MatchCollection matchesUrl = regexUrl.Matches(textContent);
            MatchCollection matchesLogin = regexLogin.Matches(textContent);
            MatchCollection matchesPassword = regexPassword.Matches(textContent);
            CookieInfo cookieInfo = null;
            bool isMatch = false;

            for (int i = 0; i < Program.setting.DomainInfos.Count; i++)
            {
                DomainInfo domainInfo = Program.setting.DomainInfos[i];

                // 输出提取到的信息
                JArray ja_cookies = new JArray();
                for (int h = 0; h < matchesUrl.Count; h++)
                {
                    string url = matchesUrl[h].Groups[1].Value.Trim();
                    if (url.Contains(domainInfo.DomainName))
                    {
                        JObject jo = new JObject();
                        string login = matchesLogin[h].Groups[1].Value.Trim();
                        string password = matchesPassword[h].Groups[1].Value.Trim();
                        jo.Add("url", url);
                        jo.Add("login", login);
                        jo.Add("password", password);
                        ja_cookies.Add(jo);
                    }
                }

                if (ja_cookies.Count > 0)
                {
                    cookieInfo = new CookieInfo();
                    cookieInfo.MatchDomainInfo = Program.setting.DomainInfos[i];
                    cookieInfo.CookieJsonStr = JsonConvert.SerializeObject(ja_cookies);
                }

                break;
            }


            return cookieInfo;
        }

        #endregion

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

        private void ImplrtButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置打开对话框的标题
            openFileDialog.Title = "打开Excel文档";

            // 设置默认的文件类型筛选
            openFileDialog.Filter = "Excel文档 (*.xls;*.xlsx)|*.xls;*.xlsx";

            // 设置默认的文件类型索引
            openFileDialog.FilterIndex = 1;

            // 是否在对话框中包含“另存为”框
            openFileDialog.RestoreDirectory = true;

            // 如果用户点击了“OK”按钮
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 获取用户选择的文件名
                    string filePath = openFileDialog.FileName;
                    this.ImportAccount_Password(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //导入账号的具体方法
        private void ImportAccount_Password(string fileName)
        {
            string errorMsg = string.Empty;

            FileInfo fi = new FileInfo(fileName);
            ExcelPackage excelPackage = null;
            try
            {
                excelPackage = new ExcelPackage(fi);
            }
            catch (Exception ex)
            {
                errorMsg = $"打开文件失败({ex.Message})";
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                MessageBox.Show(errorMsg);
                return;
            }

            if (excelPackage.Workbook.Worksheets.Count == 0)
            {
                errorMsg = $"表格内容不存在";
                MessageBox.Show(errorMsg);
                return;
            }

            ExcelWorksheet sheet = excelPackage.Workbook.Worksheets[0];
            int rowCount = sheet.Dimension.End.Row;
            int colCount = sheet.Dimension.End.Column;

            if (rowCount < 1)
            {
                errorMsg = $"表格行数至少为2行，第一行未表头，第二行开始为内容";
                MessageBox.Show(errorMsg);
                return;
            }

            List<ExcelColumnInfo> excelCols = new List<ExcelColumnInfo>()
            {
                new ExcelColumnInfo("操作日志", "Running_Log"),
                new ExcelColumnInfo("账号分类", "Account_Type_Des"),
                new ExcelColumnInfo("原Mail_CK", "Old_Mail_CK"),
                new ExcelColumnInfo("原Mail_号", "Old_Mail_Name"),
                new ExcelColumnInfo("新Mail_号", "New_Mail_Name"),
                new ExcelColumnInfo("新Mail_密", "New_Mail_Pwd"),
                new ExcelColumnInfo("Ins_Pwd", "Facebook_Pwd"),
                new ExcelColumnInfo("Ins_CK", "Facebook_CK"),
                new ExcelColumnInfo("Ins_User", "C_User"),
                new ExcelColumnInfo("昵称", "UserName"),
                new ExcelColumnInfo("UA", "UserAgent"),
                new ExcelColumnInfo("2FA状态", "TwoFA_Dynamic_StatusDes"),
                new ExcelColumnInfo("2FA密钥", "TwoFA_Dynamic_SecretKey"),
                new ExcelColumnInfo("删除Ins关联", "Log_RemoveInsAccount"),
                new ExcelColumnInfo("删除其它登录会话", "Log_LogOutOfOtherSession"),
                //new ExcelColumnInfo("删除信任设备","Log_TwoFactorRemoveTrustedDevice"),
                new ExcelColumnInfo("删除联系方式", "Log_DeleteOtherContacts"),
                new ExcelColumnInfo("国家", "GuoJia"),
                new ExcelColumnInfo("生日", "ShengRi"),
                new ExcelColumnInfo("注册日期", "ZhuCeRiQi"),
                new ExcelColumnInfo("帖子", "TieZiCount"),
                new ExcelColumnInfo("好友", "HaoYouCount"),
                new ExcelColumnInfo("关注", "GuanZhuCount"),
            };

            List<int> rList = Enumerable.Range(2, rowCount - 1).ToList();
            List<int> cList = Enumerable.Range(1, colCount).ToList();

            //表头定位
            List<string> sList = cList.Select(c => sheet.Cells[1, c].Text).ToList();
            foreach (var eCol in excelCols)
            {
                eCol.HeaderIndex = sList.FindIndex(s => s.Trim() == eCol.HeaderName);
            }

            //一次读取所有行内容
            rList = Enumerable.Range(2, rowCount - 1).ToList();
            cList = Enumerable.Range(1, colCount).ToList();
            sList = rList.Select(r => string.Join("\t", cList.Select(c => sheet.Cells[r, c].Text))).ToList();

            //每一行内容，创建实例添加到列表中
            List<Account_FBOrIns> accounts = sList.Select(s =>
            {
                string[] cellArr = s.Split('\t');

                Account_FBOrIns account = new Account_FBOrIns();

                int cellValue;
                for (int i = 0; i < excelCols.Count; i++)
                {
                    if (excelCols[i].HeaderIndex < 0 || excelCols[i].HeaderIndex >= cellArr.Length) continue;

                    if (excelCols[i].PropertyName == "TwoFA_Dynamic_StatusDes")
                    {
                        if (cellArr[excelCols[i].HeaderIndex].Trim() == "开") cellValue = 1;
                        else if (cellArr[excelCols[i].HeaderIndex].Trim() == "关") cellValue = 0;
                        else cellValue = -1;
                        this.SetProperty(account, "TwoFA_Dynamic_Status", cellValue);
                    }
                    else if (excelCols[i].PropertyName == "C_User")
                    {
                        if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
                        account.LoginInfo.LoginData_Account_Id = cellArr[excelCols[i].HeaderIndex].Trim();
                    }
                    else if (excelCols[i].PropertyName == "UserName")
                    {
                        if (account.LoginInfo == null) account.LoginInfo = new LoginInfo_FBOrIns();
                        account.LoginInfo.LoginData_UserName = cellArr[excelCols[i].HeaderIndex].Trim();
                    }
                    else this.SetProperty(account, excelCols[i].PropertyName, cellArr[excelCols[i].HeaderIndex].Trim());
                }

                if (string.IsNullOrEmpty(account.Facebook_CK) && string.IsNullOrEmpty(account.Old_Mail_CK))
                    account = null;

                return account;
            }).Where(a => a != null).GroupBy(a =>
            {
                //"name":"c_user","value":"100070943890270"
                string key = StringHelper.GetMidStr(a.Facebook_CK, "\"name\":\"c_user\",\"value\":\"", "\"").Trim();
                return key;
            }).SelectMany(ga =>
            {
                if (!string.IsNullOrEmpty(ga.Key)) return ga.ToList().GetRange(0, 1);
                else return ga.ToList();
            }).ToList();

            //合并更新到表格中
            this.Invoke(new Action(() => { this.dgv_IG.DataSource = null; }));

            Program.setting.Setting_IG.Account_List = accounts;

            if (Program.setting.Setting_IG.Account_List != null && Program.setting.Setting_IG.Account_List.Count > 0)
                this.Invoke(new Action(() => { this.dgv_IG.DataSource = Program.setting.Setting_IG.Account_List; }));
        }
    }

    //DGV开启双缓冲,避免闪烁问题
    public static class DoubleBufferDataGridView
    {
        /// <summary>
        /// 双缓冲，解决闪烁问题
        /// </summary>
        public static void DoubleBufferedDataGirdView(this DataGridView dgv, bool flag)
        {
            Type dgvType = dgv.GetType();
            PropertyInfo pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(dgv, flag, null);
        }
    }
}