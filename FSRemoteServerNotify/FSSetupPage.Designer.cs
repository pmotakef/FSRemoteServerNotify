using System;
using System.ComponentModel;

namespace FSRemoteServerNotify
{
    partial class FSSetupPage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FSSetupPage));
            this.niServer = new System.Windows.Forms.NotifyIcon(this.components);
            this.cmsServerMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.smiClientConnect = new System.Windows.Forms.ToolStripMenuItem();
            this.smiFSConnect = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.smiSetup = new System.Windows.Forms.ToolStripMenuItem();
            this.smiAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.smiExit = new System.Windows.Forms.ToolStripMenuItem();
            this.btnClose = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tbPortNum = new System.Windows.Forms.MaskedTextBox();
            this.tbIPAddress = new System.Windows.Forms.MaskedTextBox();
            this.btnSetPort = new System.Windows.Forms.Button();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuP3D = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFSX = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsServerMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // niServer
            // 
            this.niServer.ContextMenuStrip = this.cmsServerMenu;
            this.niServer.Icon = ((System.Drawing.Icon)(resources.GetObject("niServer.Icon")));
            this.niServer.Text = "Remote Server";
            // 
            // cmsServerMenu
            // 
            this.cmsServerMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.smiClientConnect,
            this.smiFSConnect,
            this.toolStripSeparator1,
            this.smiSetup,
            this.smiAbout,
            this.toolStripSeparator2,
            this.mnuFSX,
            this.mnuP3D,
            this.toolStripSeparator3,
            this.smiExit});
            this.cmsServerMenu.Name = "cmsServerMenu";
            this.cmsServerMenu.Size = new System.Drawing.Size(227, 198);
            // 
            // smiClientConnect
            // 
            this.smiClientConnect.Image = global::FSRemoteServerNotify.Properties.Resources.disconnected;
            this.smiClientConnect.Name = "smiClientConnect";
            this.smiClientConnect.Size = new System.Drawing.Size(226, 22);
            this.smiClientConnect.Text = "Client: Not Connected";
            // 
            // smiFSConnect
            // 
            this.smiFSConnect.Image = global::FSRemoteServerNotify.Properties.Resources.disconnected;
            this.smiFSConnect.Name = "smiFSConnect";
            this.smiFSConnect.Size = new System.Drawing.Size(226, 22);
            this.smiFSConnect.Text = "SimConnect: Not Connected";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(223, 6);
            // 
            // smiSetup
            // 
            this.smiSetup.Name = "smiSetup";
            this.smiSetup.Size = new System.Drawing.Size(226, 22);
            this.smiSetup.Text = "Setup";
            this.smiSetup.Click += new System.EventHandler(this.smiSetup_Click);
            // 
            // smiAbout
            // 
            this.smiAbout.Name = "smiAbout";
            this.smiAbout.Size = new System.Drawing.Size(226, 22);
            this.smiAbout.Text = "About";
            this.smiAbout.Click += new System.EventHandler(this.smiAbout_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(223, 6);
            // 
            // smiExit
            // 
            this.smiExit.Name = "smiExit";
            this.smiExit.Size = new System.Drawing.Size(226, 22);
            this.smiExit.Text = "Exit";
            this.smiExit.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(144, 83);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Host IP Address: ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(24, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Host Port#: ";
            // 
            // tbPortNum
            // 
            this.tbPortNum.Location = new System.Drawing.Point(119, 48);
            this.tbPortNum.Mask = "00000";
            this.tbPortNum.Name = "tbPortNum";
            this.tbPortNum.Size = new System.Drawing.Size(100, 20);
            this.tbPortNum.TabIndex = 4;
            this.tbPortNum.Text = "3333";
            this.tbPortNum.ValidatingType = typeof(int);
            // 
            // tbIPAddress
            // 
            this.tbIPAddress.Location = new System.Drawing.Point(119, 22);
            this.tbIPAddress.Name = "tbIPAddress";
            this.tbIPAddress.ReadOnly = true;
            this.tbIPAddress.Size = new System.Drawing.Size(100, 20);
            this.tbIPAddress.TabIndex = 5;
            this.tbIPAddress.Text = "192.168.1.7";
            // 
            // btnSetPort
            // 
            this.btnSetPort.Location = new System.Drawing.Point(27, 83);
            this.btnSetPort.Name = "btnSetPort";
            this.btnSetPort.Size = new System.Drawing.Size(75, 23);
            this.btnSetPort.TabIndex = 6;
            this.btnSetPort.Text = "Set";
            this.btnSetPort.UseVisualStyleBackColor = true;
            this.btnSetPort.Click += new System.EventHandler(this.btnSetPort_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(223, 6);
            // 
            // mnuP3D
            // 
            this.mnuP3D.Checked = true;
            this.mnuP3D.CheckState = System.Windows.Forms.CheckState.Checked;
            this.mnuP3D.Name = "mnuP3D";
            this.mnuP3D.Size = new System.Drawing.Size(226, 22);
            this.mnuP3D.Text = "Prepar3D";
            this.mnuP3D.Click += new System.EventHandler(this.mnuP3D_Click);
            // 
            // mnuFSX
            // 
            this.mnuFSX.Name = "mnuFSX";
            this.mnuFSX.Size = new System.Drawing.Size(226, 22);
            this.mnuFSX.Text = "Flight Simulator X";
            this.mnuFSX.Click += new System.EventHandler(this.mnuFSX_Click);
            // 
            // FSSetupPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(243, 164);
            this.ControlBox = false;
            this.Controls.Add(this.btnSetPort);
            this.Controls.Add(this.tbIPAddress);
            this.Controls.Add(this.tbPortNum);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "FSSetupPage";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Remote Server Setup";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.Resize += new System.EventHandler(this.FSSetupPage_Resize);
            this.cmsServerMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NotifyIcon niServer;
        private System.Windows.Forms.ContextMenuStrip cmsServerMenu;
        private System.Windows.Forms.ToolStripMenuItem smiClientConnect;
        private System.Windows.Forms.ToolStripMenuItem smiFSConnect;
        private System.Windows.Forms.ToolStripMenuItem smiSetup;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem smiExit;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.MaskedTextBox tbPortNum;
        private System.Windows.Forms.MaskedTextBox tbIPAddress;
        private System.Windows.Forms.Button btnSetPort;
        private System.Windows.Forms.ToolStripMenuItem smiAbout;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem mnuP3D;
        private System.Windows.Forms.ToolStripMenuItem mnuFSX;
    }
}

