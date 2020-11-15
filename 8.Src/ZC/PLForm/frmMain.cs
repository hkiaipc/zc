﻿using System;
using System.IO;
using System.Windows.Forms;
using Xdgk.Common;
using NUnit.UiKit;
using PL;
using PLC;

namespace PLForm {
    public partial class frmMain : Form {
        /// <summary>
        /// 
        /// </summary>
        private bool _isClose = false;
        private App _app;


        /// <summary>
        /// 
        /// </summary>
        public frmMain() {
            InitializeComponent();
            _app = App.GetApp();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMain_Load(object sender, EventArgs e) {

            //frmOpcValues.Instance.Show();

            this.Text = AppConfigReader.Read<string>("MainText", "---");
            this.Text += " - " + Application.ProductVersion;

            tssAppStatus.Text = "OPC 未连接";

            PLC.MyLogManager.Logs.Add(new TxtLog(this.txtLog));
            //_app.Opc.ConnectedEvent += Opc_ConnectedEvent;
            _app.AppController.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Opc_ConnectedEvent(object sender, EventArgs e) {
            this.tssAppStatus.Text = "OPC 已连接, " + DateTime.Now.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsbClearLogs_Click(object sender, EventArgs e) {
            this.txtLog.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsbExit_Click(object sender, EventArgs e) {
            var appController = _app.AppController;

            if (appController.ControllerStatus.IsWorking()) {
                NUnit.UiKit.UserMessage.DisplayFailure(S.CannotExit);
            } else {
                if (UserMessage.Ask(S.SureExit) == DialogResult.Yes) {
                    _isClose = true;
                    var opcServer = OpcServerManager.Instance.OpcServer;
                    if (opcServer.IsConnected()) {
                        appController.ControllerStatus.Value = ControllerStatusEnum.NotRun;
                    }
                    Close();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e) {
            if (!_isClose) {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsbSaveLog_Click(object sender, EventArgs e) {
            try {
                var dt = DateTime.Now;
                string fileName = string.Format(
                    "{0}\\log\\{1}.txt",
                    Application.StartupPath,
                    dt.ToString("yyyy_MM_dd_HH_mm_ss")
                    );
                File.WriteAllText(fileName, this.txtLog.Text);
                NUnit.UiKit.UserMessage.DisplayInfo("日志保存成功");
            } catch (Exception ex) {
                ExceptionLogger.Log(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsbGunInfo_Click(object sender, EventArgs e) {
            var frmGunLocation = new frmGunInfo();
            frmGunLocation.Dams = _app.Dams;
            frmGunLocation.ShowDialog();
        }
    }
}
