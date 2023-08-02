
using System.Windows.Forms;
using System.Drawing;
using System.IO;
//using System.Drawing;
namespace SetupExe
{
    partial class formMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(formMain));
            this.panelMain = new Telerik.WinControls.UI.RadPanel();
            this.lblTimeout = new Telerik.WinControls.UI.RadLabel();
            this.btnFinshed = new Telerik.WinControls.UI.RadButton();
            this.pictureBoxHeaderPic = new System.Windows.Forms.PictureBox();
            this.richTextBoxCurrentTask = new Telerik.WinControls.UI.RadLabel();
            this.lblInstallationType = new Telerik.WinControls.UI.RadLabel();
            this.picRepairQueue = new System.Windows.Forms.PictureBox();
            this.picUninstallQueue = new System.Windows.Forms.PictureBox();
            this.labelQueue = new Telerik.WinControls.UI.RadLabel();
            this.picInstallQueue = new System.Windows.Forms.PictureBox();
            this.pictureBoxAnimatedGif = new System.Windows.Forms.PictureBox();
            this.panelForRichTextBoxMetaData = new Telerik.WinControls.UI.RadPanel();
            this.lblSWToInstall = new Telerik.WinControls.UI.RadLabel();
            this.richTextBoxMetaData = new Telerik.WinControls.UI.RadLabel();
            this.lblTitel = new Telerik.WinControls.UI.RadLabel();
            this.lblCurrentTaskName = new Telerik.WinControls.UI.RadLabel();
            this.lblCurrentTask = new Telerik.WinControls.UI.RadLabel();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.panelMessageBox = new Telerik.WinControls.UI.RadPanel();
            this.panelMessageBoxMessage = new Telerik.WinControls.UI.RadPanel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.rtbDialogMessage = new Telerik.WinControls.UI.RadLabel();
            this.pictureBoxTopPic = new System.Windows.Forms.PictureBox();
            this.panelMessageBoxControls = new Telerik.WinControls.UI.RadPanel();
            this.btnPostpone = new Telerik.WinControls.UI.RadButton();
            this.btnStart = new Telerik.WinControls.UI.RadButton();
            this.btnOK = new Telerik.WinControls.UI.RadButton();
            this.cBRunningProcessClose = new System.Windows.Forms.CheckBox();
            this.pictureBoxInfo = new System.Windows.Forms.PictureBox();
            this.pictureBoxWarning = new System.Windows.Forms.PictureBox();
            this.pictureBoxBattery = new System.Windows.Forms.PictureBox();
            this.pictureBoxLoad = new System.Windows.Forms.PictureBox();
            this.pictureBoxError = new System.Windows.Forms.PictureBox();
            this.pictureBoxRunningProcess = new System.Windows.Forms.PictureBox();
            this.txtBoxBtn = new System.Windows.Forms.TextBox();
            this.panelPostpone = new Telerik.WinControls.UI.RadPanel();
            this.txtBoxBtnPostpone = new System.Windows.Forms.TextBox();
            this.labelAt = new Telerik.WinControls.UI.RadLabel();
            this.dateTimePickerTime = new System.Windows.Forms.DateTimePicker();
            this.btnPostponeOK = new Telerik.WinControls.UI.RadButton();
            this.dateTimePickerDate = new System.Windows.Forms.DateTimePicker();
            this.labelMinutes = new Telerik.WinControls.UI.RadLabel();
            this.numericUpDownMinutes = new System.Windows.Forms.NumericUpDown();
            this.radioButtonCancel = new System.Windows.Forms.RadioButton();
            this.radioButtonRestartOn = new System.Windows.Forms.RadioButton();
            this.radioButtonRestartIn = new System.Windows.Forms.RadioButton();
            this.radioButtonStartNow = new System.Windows.Forms.RadioButton();
            this.panelPostponeTitle = new Telerik.WinControls.UI.RadPanel();
            this.pictureBoxPostpone = new System.Windows.Forms.PictureBox();
            this.labelPostponeTitle = new Telerik.WinControls.UI.RadLabel();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.panelReboot = new Telerik.WinControls.UI.RadPanel();
            this.txtBoxBtnReboot = new System.Windows.Forms.TextBox();
            this.btnRebootOK = new Telerik.WinControls.UI.RadButton();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.labelMinutesReboot = new Telerik.WinControls.UI.RadLabel();
            this.labelAtReboot = new Telerik.WinControls.UI.RadLabel();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.dateTimePicker2 = new System.Windows.Forms.DateTimePicker();
            this.radioBtnRebootDontBotherMeAgain = new System.Windows.Forms.RadioButton();
            this.radioBtnRebootRestartOn = new System.Windows.Forms.RadioButton();
            this.radioBtnRebootRemindMeIn = new System.Windows.Forms.RadioButton();
            this.radioBtnRebootRestartNow = new System.Windows.Forms.RadioButton();
            this.panelRebootTitle = new Telerik.WinControls.UI.RadPanel();
            this.pictureBoxReboot = new System.Windows.Forms.PictureBox();
            this.labelRebootTitle = new Telerik.WinControls.UI.RadLabel();
            this.pictureBox4 = new System.Windows.Forms.PictureBox();
            this.telerikMetroTheme1 = new Telerik.WinControls.Themes.TelerikMetroTheme();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.panelMain)).BeginInit();
            this.panelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lblTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnFinshed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxHeaderPic)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.richTextBoxCurrentTask)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lblInstallationType)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRepairQueue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picUninstallQueue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelQueue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picInstallQueue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAnimatedGif)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelForRichTextBoxMetaData)).BeginInit();
            this.panelForRichTextBoxMetaData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lblSWToInstall)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.richTextBoxMetaData)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lblTitel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lblCurrentTaskName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lblCurrentTask)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelMessageBox)).BeginInit();
            this.panelMessageBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.panelMessageBoxMessage)).BeginInit();
            this.panelMessageBoxMessage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rtbDialogMessage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTopPic)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelMessageBoxControls)).BeginInit();
            this.panelMessageBoxControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.btnPostpone)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnOK)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxInfo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxBattery)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLoad)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxError)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRunningProcess)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelPostpone)).BeginInit();
            this.panelPostpone.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.labelAt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnPostponeOK)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelMinutes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMinutes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelPostponeTitle)).BeginInit();
            this.panelPostponeTitle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPostpone)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelPostponeTitle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelReboot)).BeginInit();
            this.panelReboot.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.btnRebootOK)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelMinutesReboot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelAtReboot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelRebootTitle)).BeginInit();
            this.panelRebootTitle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxReboot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelRebootTitle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // panelMain
            // 
            this.panelMain.BackColor = System.Drawing.Color.White;
            this.panelMain.Controls.Add(this.lblTimeout);
            this.panelMain.Controls.Add(this.btnFinshed);
            this.panelMain.Controls.Add(this.pictureBoxHeaderPic);
            this.panelMain.Controls.Add(this.richTextBoxCurrentTask);
            this.panelMain.Controls.Add(this.lblInstallationType);
            this.panelMain.Controls.Add(this.picRepairQueue);
            this.panelMain.Controls.Add(this.picUninstallQueue);
            this.panelMain.Controls.Add(this.labelQueue);
            this.panelMain.Controls.Add(this.picInstallQueue);
            this.panelMain.Controls.Add(this.pictureBoxAnimatedGif);
            this.panelMain.Controls.Add(this.panelForRichTextBoxMetaData);
            this.panelMain.Controls.Add(this.lblTitel);
            this.panelMain.Controls.Add(this.lblCurrentTaskName);
            this.panelMain.Controls.Add(this.lblCurrentTask);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelMain.Location = new System.Drawing.Point(0, 16);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(664, 270);
            this.panelMain.TabIndex = 0;
            this.panelMain.ThemeName = "TelerikMetro";
            // 
            // lblTimeout
            // 
            this.lblTimeout.AutoSize = false;
            this.lblTimeout.BackColor = System.Drawing.Color.White;
            this.lblTimeout.Location = new System.Drawing.Point(99, 232);
            this.lblTimeout.Name = "lblTimeout";
            this.lblTimeout.Size = new System.Drawing.Size(54, 19);
            this.lblTimeout.TabIndex = 20;
            this.lblTimeout.TextAlignment = System.Drawing.ContentAlignment.TopRight;
            this.lblTimeout.ThemeName = "TelerikMetro";
            // 
            // btnFinshed
            // 
            this.btnFinshed.DisplayStyle = Telerik.WinControls.DisplayStyle.Text;
            this.btnFinshed.Location = new System.Drawing.Point(15, 232);
            this.btnFinshed.Name = "btnFinshed";
            this.btnFinshed.Size = new System.Drawing.Size(75, 23);
            this.btnFinshed.TabIndex = 19;
            this.btnFinshed.Text = "Finish";
            this.btnFinshed.ThemeName = "TelerikMetro";
            this.btnFinshed.Click += new System.EventHandler(this.btnFinshed_Click);
            // 
            // pictureBoxHeaderPic
            // 
            this.pictureBoxHeaderPic.Image = global::SetupExe.Properties.Resources.setup;
            this.pictureBoxHeaderPic.Location = new System.Drawing.Point(-1, -1);
            this.pictureBoxHeaderPic.Name = "pictureBoxHeaderPic";
            this.pictureBoxHeaderPic.Size = new System.Drawing.Size(670, 125);
            this.pictureBoxHeaderPic.TabIndex = 18;
            this.pictureBoxHeaderPic.TabStop = false;
            // 
            // richTextBoxCurrentTask
            // 
            this.richTextBoxCurrentTask.BackColor = System.Drawing.Color.White;
            this.richTextBoxCurrentTask.Location = new System.Drawing.Point(173, 237);
            this.richTextBoxCurrentTask.Name = "richTextBoxCurrentTask";
            this.richTextBoxCurrentTask.Size = new System.Drawing.Size(388, 19);
            this.richTextBoxCurrentTask.TabIndex = 17;
            this.richTextBoxCurrentTask.Text = "Task Name Task Name Task Name Task Name Task Name Task Name ";
            this.richTextBoxCurrentTask.ThemeName = "TelerikMetro";
            // 
            // lblInstallationType
            // 
            this.lblInstallationType.Location = new System.Drawing.Point(41, 174);
            this.lblInstallationType.Name = "lblInstallationType";
            this.lblInstallationType.Size = new System.Drawing.Size(35, 19);
            this.lblInstallationType.TabIndex = 16;
            this.lblInstallationType.Text = "Type:";
            this.lblInstallationType.ThemeName = "TelerikMetro";
            // 
            // picRepairQueue
            // 
            this.picRepairQueue.Image = ((System.Drawing.Image)(resources.GetObject("picRepairQueue.Image")));
            this.picRepairQueue.Location = new System.Drawing.Point(15, 169);
            this.picRepairQueue.Name = "picRepairQueue";
            this.picRepairQueue.Size = new System.Drawing.Size(25, 25);
            this.picRepairQueue.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picRepairQueue.TabIndex = 15;
            this.picRepairQueue.TabStop = false;
            // 
            // picUninstallQueue
            // 
            this.picUninstallQueue.Image = ((System.Drawing.Image)(resources.GetObject("picUninstallQueue.Image")));
            this.picUninstallQueue.Location = new System.Drawing.Point(15, 169);
            this.picUninstallQueue.Name = "picUninstallQueue";
            this.picUninstallQueue.Size = new System.Drawing.Size(25, 25);
            this.picUninstallQueue.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picUninstallQueue.TabIndex = 14;
            this.picUninstallQueue.TabStop = false;
            // 
            // labelQueue
            // 
            this.labelQueue.Location = new System.Drawing.Point(74, 174);
            this.labelQueue.Name = "labelQueue";
            this.labelQueue.Size = new System.Drawing.Size(66, 19);
            this.labelQueue.TabIndex = 13;
            this.labelQueue.Text = "Installation";
            this.labelQueue.ThemeName = "TelerikMetro";
            // 
            // picInstallQueue
            // 
            this.picInstallQueue.Image = ((System.Drawing.Image)(resources.GetObject("picInstallQueue.Image")));
            this.picInstallQueue.Location = new System.Drawing.Point(15, 169);
            this.picInstallQueue.Name = "picInstallQueue";
            this.picInstallQueue.Size = new System.Drawing.Size(25, 25);
            this.picInstallQueue.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picInstallQueue.TabIndex = 12;
            this.picInstallQueue.TabStop = false;
            // 
            // pictureBoxAnimatedGif
            // 
            this.pictureBoxAnimatedGif.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxAnimatedGif.Image")));
            this.pictureBoxAnimatedGif.Location = new System.Drawing.Point(253, 216);
            this.pictureBoxAnimatedGif.Name = "pictureBoxAnimatedGif";
            this.pictureBoxAnimatedGif.Size = new System.Drawing.Size(16, 19);
            this.pictureBoxAnimatedGif.TabIndex = 11;
            this.pictureBoxAnimatedGif.TabStop = false;
            this.pictureBoxAnimatedGif.Visible = false;
            // 
            // panelForRichTextBoxMetaData
            // 
            this.panelForRichTextBoxMetaData.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelForRichTextBoxMetaData.Controls.Add(this.lblSWToInstall);
            this.panelForRichTextBoxMetaData.Controls.Add(this.richTextBoxMetaData);
            this.panelForRichTextBoxMetaData.Location = new System.Drawing.Point(166, 140);
            this.panelForRichTextBoxMetaData.Name = "panelForRichTextBoxMetaData";
            this.panelForRichTextBoxMetaData.Size = new System.Drawing.Size(491, 65);
            this.panelForRichTextBoxMetaData.TabIndex = 10;
            this.panelForRichTextBoxMetaData.ThemeName = "TelerikMetro";
            // 
            // lblSWToInstall
            // 
            this.lblSWToInstall.Location = new System.Drawing.Point(3, 2);
            this.lblSWToInstall.Name = "lblSWToInstall";
            this.lblSWToInstall.Size = new System.Drawing.Size(105, 19);
            this.lblSWToInstall.TabIndex = 12;
            this.lblSWToInstall.Text = "Software Package:";
            this.lblSWToInstall.ThemeName = "TelerikMetro";
            // 
            // richTextBoxMetaData
            // 
            this.richTextBoxMetaData.BackColor = System.Drawing.Color.WhiteSmoke;
            this.richTextBoxMetaData.Cursor = System.Windows.Forms.Cursors.Default;
            this.richTextBoxMetaData.Location = new System.Drawing.Point(4, 18);
            this.richTextBoxMetaData.Name = "richTextBoxMetaData";
            this.richTextBoxMetaData.Size = new System.Drawing.Size(95, 19);
            this.richTextBoxMetaData.TabIndex = 0;
            this.richTextBoxMetaData.Text = "[Package Name]";
            this.richTextBoxMetaData.ThemeName = "TelerikMetro";
            // 
            // lblTitel
            // 
            this.lblTitel.Location = new System.Drawing.Point(11, 143);
            this.lblTitel.Name = "lblTitel";
            this.lblTitel.Size = new System.Drawing.Size(89, 19);
            this.lblTitel.TabIndex = 8;
            this.lblTitel.Text = "Software Setup";
            this.lblTitel.ThemeName = "TelerikMetro";
            // 
            // lblCurrentTaskName
            // 
            this.lblCurrentTaskName.Location = new System.Drawing.Point(170, 217);
            this.lblCurrentTaskName.Name = "lblCurrentTaskName";
            this.lblCurrentTaskName.Size = new System.Drawing.Size(75, 19);
            this.lblCurrentTaskName.TabIndex = 4;
            this.lblCurrentTaskName.Text = "Current task:";
            this.lblCurrentTaskName.ThemeName = "TelerikMetro";
            // 
            // lblCurrentTask
            // 
            this.lblCurrentTask.Location = new System.Drawing.Point(289, 215);
            this.lblCurrentTask.Name = "lblCurrentTask";
            this.lblCurrentTask.Size = new System.Drawing.Size(78, 19);
            this.lblCurrentTask.TabIndex = 1;
            this.lblCurrentTask.Text = "[Task  Name]";
            this.lblCurrentTask.ThemeName = "TelerikMetro";
            this.lblCurrentTask.Visible = false;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.Click += new System.EventHandler(this.notifyIcon1_Click);
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // panelMessageBox
            // 
            this.panelMessageBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelMessageBox.Controls.Add(this.panelMessageBoxMessage);
            this.panelMessageBox.Controls.Add(this.pictureBoxTopPic);
            this.panelMessageBox.Controls.Add(this.panelMessageBoxControls);
            this.panelMessageBox.Controls.Add(this.txtBoxBtn);
            this.panelMessageBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelMessageBox.Location = new System.Drawing.Point(0, 0);
            this.panelMessageBox.Name = "panelMessageBox";
            this.panelMessageBox.Size = new System.Drawing.Size(664, 0);
            this.panelMessageBox.TabIndex = 1;
            this.panelMessageBox.ThemeName = "TelerikMetro";
            this.panelMessageBox.Visible = false;
            // 
            // panelMessageBoxMessage
            // 
            this.panelMessageBoxMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelMessageBoxMessage.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelMessageBoxMessage.Controls.Add(this.pictureBox1);
            this.panelMessageBoxMessage.Controls.Add(this.rtbDialogMessage);
            this.panelMessageBoxMessage.Location = new System.Drawing.Point(1, 5);
            this.panelMessageBoxMessage.Name = "panelMessageBoxMessage";
            this.panelMessageBoxMessage.Size = new System.Drawing.Size(658, 50);
            this.panelMessageBoxMessage.TabIndex = 4;
            this.panelMessageBoxMessage.ThemeName = "TelerikMetro";
            ((Telerik.WinControls.Primitives.BorderPrimitive)(this.panelMessageBoxMessage.GetChildAt(0).GetChildAt(1))).Width = 0F;
            ((Telerik.WinControls.Primitives.BorderPrimitive)(this.panelMessageBoxMessage.GetChildAt(0).GetChildAt(1))).BottomWidth = 0F;
            ((Telerik.WinControls.Primitives.BorderPrimitive)(this.panelMessageBoxMessage.GetChildAt(0).GetChildAt(1))).BottomColor = System.Drawing.SystemColors.Control;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBox1.Image = global::SetupExe.Properties.Resources.board;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(658, 5);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // rtbDialogMessage
            // 
            this.rtbDialogMessage.BackColor = System.Drawing.Color.WhiteSmoke;
            this.rtbDialogMessage.Cursor = System.Windows.Forms.Cursors.Default;
            this.rtbDialogMessage.Location = new System.Drawing.Point(20, 16);
            this.rtbDialogMessage.Name = "rtbDialogMessage";
            this.rtbDialogMessage.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.rtbDialogMessage.Size = new System.Drawing.Size(34, 19);
            this.rtbDialogMessage.TabIndex = 100;
            this.rtbDialogMessage.Text = "[text]";
            this.rtbDialogMessage.ThemeName = "TelerikMetro";
            // 
            // pictureBoxTopPic
            // 
            this.pictureBoxTopPic.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxTopPic.Image = global::SetupExe.Properties.Resources.board;
            this.pictureBoxTopPic.Location = new System.Drawing.Point(1, 0);
            this.pictureBoxTopPic.Name = "pictureBoxTopPic";
            this.pictureBoxTopPic.Size = new System.Drawing.Size(656, 5);
            this.pictureBoxTopPic.TabIndex = 3;
            this.pictureBoxTopPic.TabStop = false;
            // 
            // panelMessageBoxControls
            // 
            this.panelMessageBoxControls.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelMessageBoxControls.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelMessageBoxControls.Controls.Add(this.btnPostpone);
            this.panelMessageBoxControls.Controls.Add(this.btnStart);
            this.panelMessageBoxControls.Controls.Add(this.btnOK);
            this.panelMessageBoxControls.Controls.Add(this.cBRunningProcessClose);
            this.panelMessageBoxControls.Controls.Add(this.pictureBoxInfo);
            this.panelMessageBoxControls.Controls.Add(this.pictureBoxWarning);
            this.panelMessageBoxControls.Controls.Add(this.pictureBoxBattery);
            this.panelMessageBoxControls.Controls.Add(this.pictureBoxLoad);
            this.panelMessageBoxControls.Controls.Add(this.pictureBoxError);
            this.panelMessageBoxControls.Controls.Add(this.pictureBoxRunningProcess);
            this.panelMessageBoxControls.Location = new System.Drawing.Point(1, -52);
            this.panelMessageBoxControls.Name = "panelMessageBoxControls";
            this.panelMessageBoxControls.Size = new System.Drawing.Size(656, 50);
            this.panelMessageBoxControls.TabIndex = 2;
            ((Telerik.WinControls.Primitives.BorderPrimitive)(this.panelMessageBoxControls.GetChildAt(0).GetChildAt(1))).Width = 0F;
            ((Telerik.WinControls.Primitives.BorderPrimitive)(this.panelMessageBoxControls.GetChildAt(0).GetChildAt(1))).LeftWidth = 0F;
            ((Telerik.WinControls.Primitives.BorderPrimitive)(this.panelMessageBoxControls.GetChildAt(0).GetChildAt(1))).TopWidth = 0F;
            ((Telerik.WinControls.Primitives.BorderPrimitive)(this.panelMessageBoxControls.GetChildAt(0).GetChildAt(1))).RightWidth = 0F;
            ((Telerik.WinControls.Primitives.BorderPrimitive)(this.panelMessageBoxControls.GetChildAt(0).GetChildAt(1))).BottomWidth = 0F;
            // 
            // btnPostpone
            // 
            this.btnPostpone.Location = new System.Drawing.Point(195, 14);
            this.btnPostpone.Name = "btnPostpone";
            this.btnPostpone.Size = new System.Drawing.Size(73, 23);
            this.btnPostpone.TabIndex = 18;
            this.btnPostpone.Text = "Postpone";
            this.btnPostpone.ThemeName = "TelerikMetro";
            this.btnPostpone.Click += new System.EventHandler(this.btnPostpone_Click);
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(116, 14);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(73, 23);
            this.btnStart.TabIndex = 17;
            this.btnStart.Text = "Start";
            this.btnStart.ThemeName = "TelerikMetro";
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(116, 14);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(73, 23);
            this.btnOK.TabIndex = 18;
            this.btnOK.Text = "OK";
            this.btnOK.ThemeName = "TelerikMetro";
            this.btnOK.MouseCaptureChanged += new System.EventHandler(this.btnOK_Click);
            // 
            // cBRunningProcessClose
            // 
            this.cBRunningProcessClose.AutoSize = true;
            this.cBRunningProcessClose.Location = new System.Drawing.Point(204, 18);
            this.cBRunningProcessClose.Name = "cBRunningProcessClose";
            this.cBRunningProcessClose.Size = new System.Drawing.Size(210, 17);
            this.cBRunningProcessClose.TabIndex = 10;
            this.cBRunningProcessClose.Text = "close running process automatically";
            this.cBRunningProcessClose.UseVisualStyleBackColor = true;
            this.cBRunningProcessClose.Visible = false;
            // 
            // pictureBoxInfo
            // 
            this.pictureBoxInfo.Image = global::SetupExe.Properties.Resources.info_45;
            this.pictureBoxInfo.Location = new System.Drawing.Point(37, 3);
            this.pictureBoxInfo.Name = "pictureBoxInfo";
            this.pictureBoxInfo.Size = new System.Drawing.Size(46, 45);
            this.pictureBoxInfo.TabIndex = 14;
            this.pictureBoxInfo.TabStop = false;
            // 
            // pictureBoxWarning
            // 
            this.pictureBoxWarning.Image = global::SetupExe.Properties.Resources.warning_45;
            this.pictureBoxWarning.Location = new System.Drawing.Point(37, 3);
            this.pictureBoxWarning.Name = "pictureBoxWarning";
            this.pictureBoxWarning.Size = new System.Drawing.Size(46, 45);
            this.pictureBoxWarning.TabIndex = 13;
            this.pictureBoxWarning.TabStop = false;
            // 
            // pictureBoxBattery
            // 
            this.pictureBoxBattery.Image = global::SetupExe.Properties.Resources.battery_45;
            this.pictureBoxBattery.Location = new System.Drawing.Point(37, 3);
            this.pictureBoxBattery.Name = "pictureBoxBattery";
            this.pictureBoxBattery.Size = new System.Drawing.Size(46, 45);
            this.pictureBoxBattery.TabIndex = 12;
            this.pictureBoxBattery.TabStop = false;
            // 
            // pictureBoxLoad
            // 
            this.pictureBoxLoad.Image = global::SetupExe.Properties.Resources.load_45;
            this.pictureBoxLoad.Location = new System.Drawing.Point(37, 3);
            this.pictureBoxLoad.Name = "pictureBoxLoad";
            this.pictureBoxLoad.Size = new System.Drawing.Size(46, 45);
            this.pictureBoxLoad.TabIndex = 11;
            this.pictureBoxLoad.TabStop = false;
            // 
            // pictureBoxError
            // 
            this.pictureBoxError.Image = global::SetupExe.Properties.Resources.error_45;
            this.pictureBoxError.Location = new System.Drawing.Point(37, 4);
            this.pictureBoxError.Name = "pictureBoxError";
            this.pictureBoxError.Size = new System.Drawing.Size(46, 45);
            this.pictureBoxError.TabIndex = 10;
            this.pictureBoxError.TabStop = false;
            // 
            // pictureBoxRunningProcess
            // 
            this.pictureBoxRunningProcess.Image = global::SetupExe.Properties.Resources.wip_45;
            this.pictureBoxRunningProcess.Location = new System.Drawing.Point(37, 3);
            this.pictureBoxRunningProcess.Name = "pictureBoxRunningProcess";
            this.pictureBoxRunningProcess.Size = new System.Drawing.Size(46, 45);
            this.pictureBoxRunningProcess.TabIndex = 9;
            this.pictureBoxRunningProcess.TabStop = false;
            // 
            // txtBoxBtn
            // 
            this.txtBoxBtn.Location = new System.Drawing.Point(552, 25);
            this.txtBoxBtn.Name = "txtBoxBtn";
            this.txtBoxBtn.Size = new System.Drawing.Size(100, 20);
            this.txtBoxBtn.TabIndex = 1;
            this.txtBoxBtn.Visible = false;
            // 
            // panelPostpone
            // 
            this.panelPostpone.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelPostpone.Controls.Add(this.txtBoxBtnPostpone);
            this.panelPostpone.Controls.Add(this.labelAt);
            this.panelPostpone.Controls.Add(this.dateTimePickerTime);
            this.panelPostpone.Controls.Add(this.btnPostponeOK);
            this.panelPostpone.Controls.Add(this.dateTimePickerDate);
            this.panelPostpone.Controls.Add(this.labelMinutes);
            this.panelPostpone.Controls.Add(this.numericUpDownMinutes);
            this.panelPostpone.Controls.Add(this.radioButtonCancel);
            this.panelPostpone.Controls.Add(this.radioButtonRestartOn);
            this.panelPostpone.Controls.Add(this.radioButtonRestartIn);
            this.panelPostpone.Controls.Add(this.radioButtonStartNow);
            this.panelPostpone.Controls.Add(this.panelPostponeTitle);
            this.panelPostpone.Controls.Add(this.pictureBox3);
            this.panelPostpone.Location = new System.Drawing.Point(0, 0);
            this.panelPostpone.Name = "panelPostpone";
            this.panelPostpone.Size = new System.Drawing.Size(670, 0);
            this.panelPostpone.TabIndex = 19;
            this.panelPostpone.ThemeName = "TelerikMetro";
            this.panelPostpone.Visible = false;
            // 
            // txtBoxBtnPostpone
            // 
            this.txtBoxBtnPostpone.Location = new System.Drawing.Point(531, 59);
            this.txtBoxBtnPostpone.Name = "txtBoxBtnPostpone";
            this.txtBoxBtnPostpone.Size = new System.Drawing.Size(100, 20);
            this.txtBoxBtnPostpone.TabIndex = 20;
            this.txtBoxBtnPostpone.Visible = false;
            // 
            // labelAt
            // 
            this.labelAt.Location = new System.Drawing.Point(295, 109);
            this.labelAt.Name = "labelAt";
            this.labelAt.Size = new System.Drawing.Size(16, 18);
            this.labelAt.TabIndex = 19;
            this.labelAt.Text = "at";
            // 
            // dateTimePickerTime
            // 
            this.dateTimePickerTime.Enabled = false;
            this.dateTimePickerTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dateTimePickerTime.Location = new System.Drawing.Point(324, 103);
            this.dateTimePickerTime.Name = "dateTimePickerTime";
            this.dateTimePickerTime.ShowUpDown = true;
            this.dateTimePickerTime.Size = new System.Drawing.Size(75, 20);
            this.dateTimePickerTime.TabIndex = 18;
            // 
            // btnPostponeOK
            // 
            this.btnPostponeOK.BackColor = System.Drawing.Color.Gainsboro;
            this.btnPostponeOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnPostponeOK.Location = new System.Drawing.Point(564, 114);
            this.btnPostponeOK.Margin = new System.Windows.Forms.Padding(2);
            this.btnPostponeOK.Name = "btnPostponeOK";
            this.btnPostponeOK.Size = new System.Drawing.Size(75, 25);
            this.btnPostponeOK.TabIndex = 17;
            this.btnPostponeOK.Text = "OK";
            this.btnPostponeOK.ThemeName = "TelerikMetro";
            this.btnPostponeOK.Click += new System.EventHandler(this.btnPostponeOK_Click);
            // 
            // dateTimePickerDate
            // 
            this.dateTimePickerDate.Enabled = false;
            this.dateTimePickerDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePickerDate.Location = new System.Drawing.Point(203, 103);
            this.dateTimePickerDate.Name = "dateTimePickerDate";
            this.dateTimePickerDate.Size = new System.Drawing.Size(86, 20);
            this.dateTimePickerDate.TabIndex = 16;
            // 
            // labelMinutes
            // 
            this.labelMinutes.Location = new System.Drawing.Point(295, 84);
            this.labelMinutes.Name = "labelMinutes";
            this.labelMinutes.Size = new System.Drawing.Size(46, 18);
            this.labelMinutes.TabIndex = 15;
            this.labelMinutes.Text = "minutes";
            // 
            // numericUpDownMinutes
            // 
            this.numericUpDownMinutes.Enabled = false;
            this.numericUpDownMinutes.Location = new System.Drawing.Point(203, 82);
            this.numericUpDownMinutes.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
            this.numericUpDownMinutes.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownMinutes.Name = "numericUpDownMinutes";
            this.numericUpDownMinutes.Size = new System.Drawing.Size(86, 20);
            this.numericUpDownMinutes.TabIndex = 14;
            this.numericUpDownMinutes.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // radioButtonCancel
            // 
            this.radioButtonCancel.AutoSize = true;
            this.radioButtonCancel.Location = new System.Drawing.Point(37, 128);
            this.radioButtonCancel.Name = "radioButtonCancel";
            this.radioButtonCancel.Size = new System.Drawing.Size(156, 19);
            this.radioButtonCancel.TabIndex = 13;
            this.radioButtonCancel.Text = "Cancel without installing";
            this.radioButtonCancel.UseVisualStyleBackColor = true;
            // 
            // radioButtonRestartOn
            // 
            this.radioButtonRestartOn.AutoSize = true;
            this.radioButtonRestartOn.Location = new System.Drawing.Point(37, 105);
            this.radioButtonRestartOn.Name = "radioButtonRestartOn";
            this.radioButtonRestartOn.Size = new System.Drawing.Size(78, 19);
            this.radioButtonRestartOn.TabIndex = 12;
            this.radioButtonRestartOn.Text = "Restart on";
            this.radioButtonRestartOn.UseVisualStyleBackColor = true;
            // 
            // radioButtonRestartIn
            // 
            this.radioButtonRestartIn.AutoSize = true;
            this.radioButtonRestartIn.Location = new System.Drawing.Point(37, 82);
            this.radioButtonRestartIn.Name = "radioButtonRestartIn";
            this.radioButtonRestartIn.Size = new System.Drawing.Size(74, 19);
            this.radioButtonRestartIn.TabIndex = 11;
            this.radioButtonRestartIn.Text = "Restart in";
            this.radioButtonRestartIn.UseVisualStyleBackColor = true;
            // 
            // radioButtonStartNow
            // 
            this.radioButtonStartNow.AutoSize = true;
            this.radioButtonStartNow.Checked = true;
            this.radioButtonStartNow.Location = new System.Drawing.Point(37, 59);
            this.radioButtonStartNow.Name = "radioButtonStartNow";
            this.radioButtonStartNow.Size = new System.Drawing.Size(75, 19);
            this.radioButtonStartNow.TabIndex = 10;
            this.radioButtonStartNow.TabStop = true;
            this.radioButtonStartNow.Text = "Start now";
            this.radioButtonStartNow.UseVisualStyleBackColor = true;
            // 
            // panelPostponeTitle
            // 
            this.panelPostponeTitle.Controls.Add(this.pictureBoxPostpone);
            this.panelPostponeTitle.Controls.Add(this.labelPostponeTitle);
            this.panelPostponeTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelPostponeTitle.Location = new System.Drawing.Point(0, 5);
            this.panelPostponeTitle.Name = "panelPostponeTitle";
            this.panelPostponeTitle.Size = new System.Drawing.Size(670, 45);
            this.panelPostponeTitle.TabIndex = 4;
            this.panelPostponeTitle.ThemeName = "TelerikMetro";
            // 
            // pictureBoxPostpone
            // 
            this.pictureBoxPostpone.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxPostpone.Image")));
            this.pictureBoxPostpone.Location = new System.Drawing.Point(3, 2);
            this.pictureBoxPostpone.Name = "pictureBoxPostpone";
            this.pictureBoxPostpone.Size = new System.Drawing.Size(48, 38);
            this.pictureBoxPostpone.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxPostpone.TabIndex = 2;
            this.pictureBoxPostpone.TabStop = false;
            // 
            // labelPostponeTitle
            // 
            this.labelPostponeTitle.Location = new System.Drawing.Point(74, 15);
            this.labelPostponeTitle.Name = "labelPostponeTitle";
            this.labelPostponeTitle.Size = new System.Drawing.Size(290, 18);
            this.labelPostponeTitle.TabIndex = 1;
            this.labelPostponeTitle.Text = "You have the possibility to postpone this installation task";
            // 
            // pictureBox3
            // 
            this.pictureBox3.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBox3.Image = global::SetupExe.Properties.Resources.board;
            this.pictureBox3.Location = new System.Drawing.Point(0, 0);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(670, 5);
            this.pictureBox3.TabIndex = 3;
            this.pictureBox3.TabStop = false;
            // 
            // panelReboot
            // 
            this.panelReboot.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelReboot.Controls.Add(this.txtBoxBtnReboot);
            this.panelReboot.Controls.Add(this.btnRebootOK);
            this.panelReboot.Controls.Add(this.dateTimePicker1);
            this.panelReboot.Controls.Add(this.labelMinutesReboot);
            this.panelReboot.Controls.Add(this.labelAtReboot);
            this.panelReboot.Controls.Add(this.numericUpDown1);
            this.panelReboot.Controls.Add(this.dateTimePicker2);
            this.panelReboot.Controls.Add(this.radioBtnRebootDontBotherMeAgain);
            this.panelReboot.Controls.Add(this.radioBtnRebootRestartOn);
            this.panelReboot.Controls.Add(this.radioBtnRebootRemindMeIn);
            this.panelReboot.Controls.Add(this.radioBtnRebootRestartNow);
            this.panelReboot.Controls.Add(this.panelRebootTitle);
            this.panelReboot.Controls.Add(this.pictureBox4);
            this.panelReboot.Location = new System.Drawing.Point(0, 0);
            this.panelReboot.Name = "panelReboot";
            this.panelReboot.Size = new System.Drawing.Size(670, 0);
            this.panelReboot.TabIndex = 20;
            this.panelReboot.ThemeName = "TelerikMetro";
            this.panelReboot.Visible = false;
            // 
            // txtBoxBtnReboot
            // 
            this.txtBoxBtnReboot.Location = new System.Drawing.Point(564, 62);
            this.txtBoxBtnReboot.Name = "txtBoxBtnReboot";
            this.txtBoxBtnReboot.Size = new System.Drawing.Size(100, 20);
            this.txtBoxBtnReboot.TabIndex = 20;
            this.txtBoxBtnReboot.Visible = false;
            // 
            // btnRebootOK
            // 
            this.btnRebootOK.BackColor = System.Drawing.Color.Gainsboro;
            this.btnRebootOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnRebootOK.Location = new System.Drawing.Point(560, 113);
            this.btnRebootOK.Name = "btnRebootOK";
            this.btnRebootOK.Size = new System.Drawing.Size(75, 25);
            this.btnRebootOK.TabIndex = 19;
            this.btnRebootOK.Text = "OK";
            this.btnRebootOK.ThemeName = "TelerikMetro";
            this.btnRebootOK.Click += new System.EventHandler(this.btnRebootOK_Click);
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Enabled = false;
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dateTimePicker1.Location = new System.Drawing.Point(313, 100);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.ShowUpDown = true;
            this.dateTimePicker1.Size = new System.Drawing.Size(77, 20);
            this.dateTimePicker1.TabIndex = 18;
            // 
            // labelMinutesReboot
            // 
            this.labelMinutesReboot.Location = new System.Drawing.Point(291, 82);
            this.labelMinutesReboot.Name = "labelMinutesReboot";
            this.labelMinutesReboot.Size = new System.Drawing.Size(46, 18);
            this.labelMinutesReboot.TabIndex = 17;
            this.labelMinutesReboot.Text = "minutes";
            // 
            // labelAtReboot
            // 
            this.labelAtReboot.Location = new System.Drawing.Point(291, 105);
            this.labelAtReboot.Name = "labelAtReboot";
            this.labelAtReboot.Size = new System.Drawing.Size(16, 18);
            this.labelAtReboot.TabIndex = 16;
            this.labelAtReboot.Text = "at";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Enabled = false;
            this.numericUpDown1.Location = new System.Drawing.Point(199, 78);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(86, 20);
            this.numericUpDown1.TabIndex = 15;
            this.numericUpDown1.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // dateTimePicker2
            // 
            this.dateTimePicker2.Enabled = false;
            this.dateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker2.Location = new System.Drawing.Point(199, 100);
            this.dateTimePicker2.Name = "dateTimePicker2";
            this.dateTimePicker2.Size = new System.Drawing.Size(86, 20);
            this.dateTimePicker2.TabIndex = 14;
            // 
            // radioBtnRebootDontBotherMeAgain
            // 
            this.radioBtnRebootDontBotherMeAgain.AutoSize = true;
            this.radioBtnRebootDontBotherMeAgain.Location = new System.Drawing.Point(33, 128);
            this.radioBtnRebootDontBotherMeAgain.Name = "radioBtnRebootDontBotherMeAgain";
            this.radioBtnRebootDontBotherMeAgain.Size = new System.Drawing.Size(144, 19);
            this.radioBtnRebootDontBotherMeAgain.TabIndex = 13;
            this.radioBtnRebootDontBotherMeAgain.Text = "Don\'t bother me again";
            this.radioBtnRebootDontBotherMeAgain.UseVisualStyleBackColor = true;
            // 
            // radioBtnRebootRestartOn
            // 
            this.radioBtnRebootRestartOn.AutoSize = true;
            this.radioBtnRebootRestartOn.Location = new System.Drawing.Point(33, 103);
            this.radioBtnRebootRestartOn.Name = "radioBtnRebootRestartOn";
            this.radioBtnRebootRestartOn.Size = new System.Drawing.Size(78, 19);
            this.radioBtnRebootRestartOn.TabIndex = 12;
            this.radioBtnRebootRestartOn.Text = "Restart on";
            this.radioBtnRebootRestartOn.UseVisualStyleBackColor = true;
            // 
            // radioBtnRebootRemindMeIn
            // 
            this.radioBtnRebootRemindMeIn.AutoSize = true;
            this.radioBtnRebootRemindMeIn.Location = new System.Drawing.Point(33, 78);
            this.radioBtnRebootRemindMeIn.Name = "radioBtnRebootRemindMeIn";
            this.radioBtnRebootRemindMeIn.Size = new System.Drawing.Size(99, 19);
            this.radioBtnRebootRemindMeIn.TabIndex = 11;
            this.radioBtnRebootRemindMeIn.Text = "Remind me in";
            this.radioBtnRebootRemindMeIn.UseVisualStyleBackColor = true;
            // 
            // radioBtnRebootRestartNow
            // 
            this.radioBtnRebootRestartNow.AutoSize = true;
            this.radioBtnRebootRestartNow.Checked = true;
            this.radioBtnRebootRestartNow.Location = new System.Drawing.Point(33, 53);
            this.radioBtnRebootRestartNow.Name = "radioBtnRebootRestartNow";
            this.radioBtnRebootRestartNow.Size = new System.Drawing.Size(87, 19);
            this.radioBtnRebootRestartNow.TabIndex = 10;
            this.radioBtnRebootRestartNow.TabStop = true;
            this.radioBtnRebootRestartNow.Text = "Restart now";
            this.radioBtnRebootRestartNow.UseVisualStyleBackColor = true;
            // 
            // panelRebootTitle
            // 
            this.panelRebootTitle.Controls.Add(this.pictureBoxReboot);
            this.panelRebootTitle.Controls.Add(this.labelRebootTitle);
            this.panelRebootTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelRebootTitle.Location = new System.Drawing.Point(0, 5);
            this.panelRebootTitle.Name = "panelRebootTitle";
            this.panelRebootTitle.Size = new System.Drawing.Size(670, 45);
            this.panelRebootTitle.TabIndex = 4;
            this.panelRebootTitle.ThemeName = "TelerikMetro";
            // 
            // pictureBoxReboot
            // 
            this.pictureBoxReboot.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxReboot.Image")));
            this.pictureBoxReboot.Location = new System.Drawing.Point(3, 3);
            this.pictureBoxReboot.Name = "pictureBoxReboot";
            this.pictureBoxReboot.Size = new System.Drawing.Size(44, 38);
            this.pictureBoxReboot.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxReboot.TabIndex = 1;
            this.pictureBoxReboot.TabStop = false;
            // 
            // labelRebootTitle
            // 
            this.labelRebootTitle.Location = new System.Drawing.Point(74, 15);
            this.labelRebootTitle.Name = "labelRebootTitle";
            this.labelRebootTitle.Size = new System.Drawing.Size(302, 18);
            this.labelRebootTitle.TabIndex = 0;
            this.labelRebootTitle.Text = "Windows needs to be restarted to complete the installation";
            // 
            // pictureBox4
            // 
            this.pictureBox4.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBox4.Image = global::SetupExe.Properties.Resources.board;
            this.pictureBox4.Location = new System.Drawing.Point(0, 0);
            this.pictureBox4.Name = "pictureBox4";
            this.pictureBox4.Size = new System.Drawing.Size(670, 5);
            this.pictureBox4.TabIndex = 3;
            this.pictureBox4.TabStop = false;
            // 
            // radLabel1
            // 
            this.radLabel1.Location = new System.Drawing.Point(98, 137);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(55, 18);
            this.radLabel1.TabIndex = 20;
            this.radLabel1.Text = "radLabel1";
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // formMain
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(664, 286);
            this.ControlBox = false;
            this.Controls.Add(this.panelReboot);
            this.Controls.Add(this.panelPostpone);
            this.Controls.Add(this.panelMessageBox);
            this.Controls.Add(this.panelMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(2, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "formMain";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.ThemeName = "TelerikMetro";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.formMain_FormClosing);
            this.Load += new System.EventHandler(this.formMain_Load);
            ((System.ComponentModel.ISupportInitialize)(this.panelMain)).EndInit();
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lblTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnFinshed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxHeaderPic)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.richTextBoxCurrentTask)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lblInstallationType)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRepairQueue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picUninstallQueue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelQueue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picInstallQueue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAnimatedGif)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelForRichTextBoxMetaData)).EndInit();
            this.panelForRichTextBoxMetaData.ResumeLayout(false);
            this.panelForRichTextBoxMetaData.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lblSWToInstall)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.richTextBoxMetaData)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lblTitel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lblCurrentTaskName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lblCurrentTask)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelMessageBox)).EndInit();
            this.panelMessageBox.ResumeLayout(false);
            this.panelMessageBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.panelMessageBoxMessage)).EndInit();
            this.panelMessageBoxMessage.ResumeLayout(false);
            this.panelMessageBoxMessage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rtbDialogMessage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTopPic)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelMessageBoxControls)).EndInit();
            this.panelMessageBoxControls.ResumeLayout(false);
            this.panelMessageBoxControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.btnPostpone)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnOK)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxInfo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxBattery)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLoad)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxError)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRunningProcess)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelPostpone)).EndInit();
            this.panelPostpone.ResumeLayout(false);
            this.panelPostpone.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.labelAt)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnPostponeOK)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelMinutes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMinutes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelPostponeTitle)).EndInit();
            this.panelPostponeTitle.ResumeLayout(false);
            this.panelPostponeTitle.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPostpone)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelPostponeTitle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelReboot)).EndInit();
            this.panelReboot.ResumeLayout(false);
            this.panelReboot.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.btnRebootOK)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelMinutesReboot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelAtReboot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelRebootTitle)).EndInit();
            this.panelRebootTitle.ResumeLayout(false);
            this.panelRebootTitle.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxReboot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.labelRebootTitle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Telerik.WinControls.UI.RadPanel panelMain;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private Telerik.WinControls.UI.RadLabel lblCurrentTask;
        private Telerik.WinControls.UI.RadLabel lblCurrentTaskName;
        private Telerik.WinControls.UI.RadLabel lblTitel;
        private Telerik.WinControls.UI.RadPanel panelForRichTextBoxMetaData;
        private Telerik.WinControls.UI.RadLabel richTextBoxMetaData;
        private PictureBox pictureBoxAnimatedGif;
        private Telerik.WinControls.UI.RadLabel lblSWToInstall;
        private PictureBox picInstallQueue;
        private Telerik.WinControls.UI.RadLabel labelQueue;
        private PictureBox picRepairQueue;
        private PictureBox picUninstallQueue;
        private Telerik.WinControls.UI.RadLabel lblInstallationType;
        private Telerik.WinControls.UI.RadLabel richTextBoxCurrentTask;
        private PictureBox pictureBoxHeaderPic;
        private Telerik.WinControls.UI.RadPanel panelMessageBox;
        private TextBox txtBoxBtn;
        private Telerik.WinControls.UI.RadPanel panelMessageBoxControls;
        private PictureBox pictureBoxRunningProcess;
        private PictureBox pictureBoxError;
        private PictureBox pictureBoxLoad;
        private PictureBox pictureBoxBattery;
        private PictureBox pictureBoxWarning;
        private PictureBox pictureBoxInfo;
        private CheckBox cBRunningProcessClose;
        private PictureBox pictureBoxTopPic;
        private Telerik.WinControls.UI.RadPanel panelMessageBoxMessage;
        private PictureBox pictureBox1;
        private Telerik.WinControls.UI.RadLabel rtbDialogMessage;
        private Telerik.WinControls.UI.RadPanel panelPostpone;
        private PictureBox pictureBox3;
        private Telerik.WinControls.UI.RadPanel panelPostponeTitle;
        private PictureBox pictureBoxPostpone;
        private Telerik.WinControls.UI.RadLabel labelPostponeTitle;
        private Telerik.WinControls.UI.RadLabel labelAt;
        private DateTimePicker dateTimePickerTime;
        private Telerik.WinControls.UI.RadButton btnPostponeOK;
        private DateTimePicker dateTimePickerDate;
        private Telerik.WinControls.UI.RadLabel labelMinutes;
        private NumericUpDown numericUpDownMinutes;
        private RadioButton radioButtonCancel;
        private RadioButton radioButtonRestartOn;
        private RadioButton radioButtonRestartIn;
        private RadioButton radioButtonStartNow;
        private TextBox txtBoxBtnPostpone;
        private Telerik.WinControls.UI.RadPanel panelReboot;
        private PictureBox pictureBox4;
        private Telerik.WinControls.UI.RadPanel panelRebootTitle;
        private PictureBox pictureBoxReboot;
        private Telerik.WinControls.UI.RadLabel labelRebootTitle;
        private Telerik.WinControls.UI.RadButton btnRebootOK;
        private DateTimePicker dateTimePicker1;
        private Telerik.WinControls.UI.RadLabel labelMinutesReboot;
        private Telerik.WinControls.UI.RadLabel labelAtReboot;
        private NumericUpDown numericUpDown1;
        private DateTimePicker dateTimePicker2;
        private RadioButton radioBtnRebootDontBotherMeAgain;
        private RadioButton radioBtnRebootRestartOn;
        private RadioButton radioBtnRebootRemindMeIn;
        private RadioButton radioBtnRebootRestartNow;
        private TextBox txtBoxBtnReboot;
        private Telerik.WinControls.Themes.TelerikMetroTheme telerikMetroTheme1;
        private Telerik.WinControls.UI.RadButton btnFinshed;
        private Telerik.WinControls.UI.RadButton btnPostpone;
        private Telerik.WinControls.UI.RadButton btnStart;
        private Telerik.WinControls.UI.RadButton btnOK;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Timer timer1;
        private Telerik.WinControls.UI.RadLabel lblTimeout;
    }
}

