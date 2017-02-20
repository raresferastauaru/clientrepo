using System.Drawing;

namespace ClientApplication
{
    partial class Main
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
			this.tabApplicationMode = new System.Windows.Forms.TabControl();
			this.tabManual = new System.Windows.Forms.TabPage();
			this.btnGetFileHashes = new System.Windows.Forms.Button();
			this.btnNewFolderConfirm = new System.Windows.Forms.Button();
			this.txtNewFolderName = new System.Windows.Forms.TextBox();
			this.label11 = new System.Windows.Forms.Label();
			this.btnDeleteConfirm = new System.Windows.Forms.Button();
			this.txtDeleteFileName = new System.Windows.Forms.TextBox();
			this.label10 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.txtRenameTo = new System.Windows.Forms.TextBox();
			this.btnRenameConfirm = new System.Windows.Forms.Button();
			this.btnRenameFromBrowse = new System.Windows.Forms.Button();
			this.txtRenameFrom = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.txtPort = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.btnDisconnect = new System.Windows.Forms.Button();
			this.btnConnect = new System.Windows.Forms.Button();
			this.txtHost = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.btnPutConfirm = new System.Windows.Forms.Button();
			this.btnGetConfirm = new System.Windows.Forms.Button();
			this.btnPutBrowse = new System.Windows.Forms.Button();
			this.txtPutPath = new System.Windows.Forms.TextBox();
			this.txtGetFileName = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.tabAuto = new System.Windows.Forms.TabPage();
			this.btnBrowseDefFolderAuto = new System.Windows.Forms.Button();
			this.txtDefaultFolderAuto = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.txtPortAuto = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.btnDisconnectAuto = new System.Windows.Forms.Button();
			this.btnConnectAuto = new System.Windows.Forms.Button();
			this.txtHostAuto = new System.Windows.Forms.TextBox();
			this.label8 = new System.Windows.Forms.Label();
			this.lbTrace = new System.Windows.Forms.ListBox();
			this.tabApplicationMode.SuspendLayout();
			this.tabManual.SuspendLayout();
			this.tabAuto.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabApplicationMode
			// 
			this.tabApplicationMode.Controls.Add(this.tabManual);
			this.tabApplicationMode.Controls.Add(this.tabAuto);
			this.tabApplicationMode.Location = new System.Drawing.Point(0, 0);
			this.tabApplicationMode.Name = "tabApplicationMode";
			this.tabApplicationMode.SelectedIndex = 0;
			this.tabApplicationMode.Size = new System.Drawing.Size(694, 360);
			this.tabApplicationMode.TabIndex = 0;
			this.tabApplicationMode.SelectedIndexChanged += new System.EventHandler(this.tabApplicationMode_SelectedIndexChanged);
			// 
			// tabManual
			// 
			this.tabManual.Controls.Add(this.btnGetFileHashes);
			this.tabManual.Controls.Add(this.btnNewFolderConfirm);
			this.tabManual.Controls.Add(this.txtNewFolderName);
			this.tabManual.Controls.Add(this.label11);
			this.tabManual.Controls.Add(this.btnDeleteConfirm);
			this.tabManual.Controls.Add(this.txtDeleteFileName);
			this.tabManual.Controls.Add(this.label10);
			this.tabManual.Controls.Add(this.label9);
			this.tabManual.Controls.Add(this.txtRenameTo);
			this.tabManual.Controls.Add(this.btnRenameConfirm);
			this.tabManual.Controls.Add(this.btnRenameFromBrowse);
			this.tabManual.Controls.Add(this.txtRenameFrom);
			this.tabManual.Controls.Add(this.label5);
			this.tabManual.Controls.Add(this.txtPort);
			this.tabManual.Controls.Add(this.label4);
			this.tabManual.Controls.Add(this.btnDisconnect);
			this.tabManual.Controls.Add(this.btnConnect);
			this.tabManual.Controls.Add(this.txtHost);
			this.tabManual.Controls.Add(this.label3);
			this.tabManual.Controls.Add(this.btnPutConfirm);
			this.tabManual.Controls.Add(this.btnGetConfirm);
			this.tabManual.Controls.Add(this.btnPutBrowse);
			this.tabManual.Controls.Add(this.txtPutPath);
			this.tabManual.Controls.Add(this.txtGetFileName);
			this.tabManual.Controls.Add(this.label2);
			this.tabManual.Controls.Add(this.label1);
			this.tabManual.Location = new System.Drawing.Point(4, 22);
			this.tabManual.Name = "tabManual";
			this.tabManual.Padding = new System.Windows.Forms.Padding(3);
			this.tabManual.Size = new System.Drawing.Size(686, 334);
			this.tabManual.TabIndex = 0;
			this.tabManual.Text = "Manual";
			this.tabManual.UseVisualStyleBackColor = true;
			// 
			// btnGetFileHashes
			// 
			this.btnGetFileHashes.Location = new System.Drawing.Point(71, 174);
			this.btnGetFileHashes.Name = "btnGetFileHashes";
			this.btnGetFileHashes.Size = new System.Drawing.Size(96, 20);
			this.btnGetFileHashes.TabIndex = 29;
			this.btnGetFileHashes.Text = "GetFileHashes";
			this.btnGetFileHashes.UseVisualStyleBackColor = true;
			this.btnGetFileHashes.Click += new System.EventHandler(this.btnGetFileHashes_Click);
			// 
			// btnNewFolderConfirm
			// 
			this.btnNewFolderConfirm.Location = new System.Drawing.Point(574, 148);
			this.btnNewFolderConfirm.Name = "btnNewFolderConfirm";
			this.btnNewFolderConfirm.Size = new System.Drawing.Size(75, 20);
			this.btnNewFolderConfirm.TabIndex = 26;
			this.btnNewFolderConfirm.Text = "Confirm";
			this.btnNewFolderConfirm.UseVisualStyleBackColor = true;
			this.btnNewFolderConfirm.Click += new System.EventHandler(this.btnNewFolderConfirm_Click);
			// 
			// txtNewFolderName
			// 
			this.txtNewFolderName.Location = new System.Drawing.Point(71, 148);
			this.txtNewFolderName.Name = "txtNewFolderName";
			this.txtNewFolderName.Size = new System.Drawing.Size(415, 20);
			this.txtNewFolderName.TabIndex = 25;
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(7, 151);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(58, 13);
			this.label11.TabIndex = 24;
			this.label11.Text = "NewFolder";
			// 
			// btnDeleteConfirm
			// 
			this.btnDeleteConfirm.Location = new System.Drawing.Point(574, 122);
			this.btnDeleteConfirm.Name = "btnDeleteConfirm";
			this.btnDeleteConfirm.Size = new System.Drawing.Size(75, 20);
			this.btnDeleteConfirm.TabIndex = 23;
			this.btnDeleteConfirm.Text = "Confirm";
			this.btnDeleteConfirm.UseVisualStyleBackColor = true;
			this.btnDeleteConfirm.Click += new System.EventHandler(this.btnDeleteConfirm_Click);
			// 
			// txtDeleteFileName
			// 
			this.txtDeleteFileName.Location = new System.Drawing.Point(71, 122);
			this.txtDeleteFileName.Name = "txtDeleteFileName";
			this.txtDeleteFileName.Size = new System.Drawing.Size(415, 20);
			this.txtDeleteFileName.TabIndex = 22;
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(27, 125);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(38, 13);
			this.label10.TabIndex = 21;
			this.label10.Text = "Delete";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(307, 98);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(20, 13);
			this.label9.TabIndex = 20;
			this.label9.Text = "To";
			// 
			// txtRenameTo
			// 
			this.txtRenameTo.Location = new System.Drawing.Point(333, 95);
			this.txtRenameTo.Name = "txtRenameTo";
			this.txtRenameTo.Size = new System.Drawing.Size(153, 20);
			this.txtRenameTo.TabIndex = 19;
			// 
			// btnRenameConfirm
			// 
			this.btnRenameConfirm.Location = new System.Drawing.Point(573, 96);
			this.btnRenameConfirm.Name = "btnRenameConfirm";
			this.btnRenameConfirm.Size = new System.Drawing.Size(75, 20);
			this.btnRenameConfirm.TabIndex = 18;
			this.btnRenameConfirm.Text = "Confirm";
			this.btnRenameConfirm.UseVisualStyleBackColor = true;
			this.btnRenameConfirm.Click += new System.EventHandler(this.btnRenameConfirm_Click);
			// 
			// btnRenameFromBrowse
			// 
			this.btnRenameFromBrowse.Location = new System.Drawing.Point(226, 95);
			this.btnRenameFromBrowse.Name = "btnRenameFromBrowse";
			this.btnRenameFromBrowse.Size = new System.Drawing.Size(75, 20);
			this.btnRenameFromBrowse.TabIndex = 17;
			this.btnRenameFromBrowse.Text = "Browse";
			this.btnRenameFromBrowse.UseVisualStyleBackColor = true;
			this.btnRenameFromBrowse.Click += new System.EventHandler(this.btnRenameFromBrowse_Click);
			// 
			// txtRenameFrom
			// 
			this.txtRenameFrom.Location = new System.Drawing.Point(71, 96);
			this.txtRenameFrom.Name = "txtRenameFrom";
			this.txtRenameFrom.Size = new System.Drawing.Size(149, 20);
			this.txtRenameFrom.TabIndex = 16;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(19, 98);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(47, 13);
			this.label5.TabIndex = 15;
			this.label5.Text = "Rename";
			// 
			// txtPort
			// 
			this.txtPort.Location = new System.Drawing.Point(354, 18);
			this.txtPort.Name = "txtPort";
			this.txtPort.Size = new System.Drawing.Size(131, 20);
			this.txtPort.TabIndex = 14;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(322, 21);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(26, 13);
			this.label4.TabIndex = 13;
			this.label4.Text = "Port";
			// 
			// btnDisconnect
			// 
			this.btnDisconnect.Location = new System.Drawing.Point(572, 17);
			this.btnDisconnect.Name = "btnDisconnect";
			this.btnDisconnect.Size = new System.Drawing.Size(75, 20);
			this.btnDisconnect.TabIndex = 12;
			this.btnDisconnect.Text = "Disconnect";
			this.btnDisconnect.UseVisualStyleBackColor = true;
			this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
			// 
			// btnConnect
			// 
			this.btnConnect.Location = new System.Drawing.Point(491, 17);
			this.btnConnect.Name = "btnConnect";
			this.btnConnect.Size = new System.Drawing.Size(75, 20);
			this.btnConnect.TabIndex = 11;
			this.btnConnect.Text = "Connect";
			this.btnConnect.UseVisualStyleBackColor = true;
			this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
			// 
			// txtHost
			// 
			this.txtHost.Location = new System.Drawing.Point(70, 18);
			this.txtHost.Name = "txtHost";
			this.txtHost.Size = new System.Drawing.Size(246, 20);
			this.txtHost.TabIndex = 9;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(35, 20);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(29, 13);
			this.label3.TabIndex = 8;
			this.label3.Text = "Host";
			// 
			// btnPutConfirm
			// 
			this.btnPutConfirm.Location = new System.Drawing.Point(573, 70);
			this.btnPutConfirm.Name = "btnPutConfirm";
			this.btnPutConfirm.Size = new System.Drawing.Size(75, 20);
			this.btnPutConfirm.TabIndex = 7;
			this.btnPutConfirm.Text = "Confirm";
			this.btnPutConfirm.UseVisualStyleBackColor = true;
			this.btnPutConfirm.Click += new System.EventHandler(this.btnPutConfirm_Click);
			// 
			// btnGetConfirm
			// 
			this.btnGetConfirm.Location = new System.Drawing.Point(573, 44);
			this.btnGetConfirm.Name = "btnGetConfirm";
			this.btnGetConfirm.Size = new System.Drawing.Size(75, 20);
			this.btnGetConfirm.TabIndex = 6;
			this.btnGetConfirm.Text = "Confirm";
			this.btnGetConfirm.UseVisualStyleBackColor = true;
			this.btnGetConfirm.Click += new System.EventHandler(this.btnGetConfirm_Click);
			// 
			// btnPutBrowse
			// 
			this.btnPutBrowse.Location = new System.Drawing.Point(492, 70);
			this.btnPutBrowse.Name = "btnPutBrowse";
			this.btnPutBrowse.Size = new System.Drawing.Size(75, 20);
			this.btnPutBrowse.TabIndex = 5;
			this.btnPutBrowse.Text = "Browse";
			this.btnPutBrowse.UseVisualStyleBackColor = true;
			this.btnPutBrowse.Click += new System.EventHandler(this.btnPutBrowse_Click);
			// 
			// txtPutPath
			// 
			this.txtPutPath.Location = new System.Drawing.Point(71, 70);
			this.txtPutPath.Name = "txtPutPath";
			this.txtPutPath.Size = new System.Drawing.Size(415, 20);
			this.txtPutPath.TabIndex = 3;
			// 
			// txtGetFileName
			// 
			this.txtGetFileName.Location = new System.Drawing.Point(70, 44);
			this.txtGetFileName.Name = "txtGetFileName";
			this.txtGetFileName.Size = new System.Drawing.Size(415, 20);
			this.txtGetFileName.TabIndex = 2;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(42, 72);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(23, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Put";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(41, 46);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(24, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Get";
			// 
			// tabAuto
			// 
			this.tabAuto.Controls.Add(this.btnBrowseDefFolderAuto);
			this.tabAuto.Controls.Add(this.txtDefaultFolderAuto);
			this.tabAuto.Controls.Add(this.label6);
			this.tabAuto.Controls.Add(this.txtPortAuto);
			this.tabAuto.Controls.Add(this.label7);
			this.tabAuto.Controls.Add(this.btnDisconnectAuto);
			this.tabAuto.Controls.Add(this.btnConnectAuto);
			this.tabAuto.Controls.Add(this.txtHostAuto);
			this.tabAuto.Controls.Add(this.label8);
			this.tabAuto.Location = new System.Drawing.Point(4, 22);
			this.tabAuto.Name = "tabAuto";
			this.tabAuto.Padding = new System.Windows.Forms.Padding(3);
			this.tabAuto.Size = new System.Drawing.Size(662, 334);
			this.tabAuto.TabIndex = 1;
			this.tabAuto.Text = "Auto";
			this.tabAuto.UseVisualStyleBackColor = true;
			// 
			// btnBrowseDefFolderAuto
			// 
			this.btnBrowseDefFolderAuto.Location = new System.Drawing.Point(576, 46);
			this.btnBrowseDefFolderAuto.Name = "btnBrowseDefFolderAuto";
			this.btnBrowseDefFolderAuto.Size = new System.Drawing.Size(75, 20);
			this.btnBrowseDefFolderAuto.TabIndex = 26;
			this.btnBrowseDefFolderAuto.Text = "Browse";
			this.btnBrowseDefFolderAuto.UseVisualStyleBackColor = true;
			this.btnBrowseDefFolderAuto.Click += new System.EventHandler(this.btnBrowseDefFolderAuto_Click);
			// 
			// txtDefaultFolderAuto
			// 
			this.txtDefaultFolderAuto.Location = new System.Drawing.Point(74, 46);
			this.txtDefaultFolderAuto.Name = "txtDefaultFolderAuto";
			this.txtDefaultFolderAuto.Size = new System.Drawing.Size(415, 20);
			this.txtDefaultFolderAuto.TabIndex = 25;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(3, 50);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(73, 13);
			this.label6.TabIndex = 24;
			this.label6.Text = "Default Folder";
			// 
			// txtPortAuto
			// 
			this.txtPortAuto.Location = new System.Drawing.Point(358, 20);
			this.txtPortAuto.Name = "txtPortAuto";
			this.txtPortAuto.Size = new System.Drawing.Size(131, 20);
			this.txtPortAuto.TabIndex = 23;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(326, 23);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(26, 13);
			this.label7.TabIndex = 22;
			this.label7.Text = "Port";
			// 
			// btnDisconnectAuto
			// 
			this.btnDisconnectAuto.Location = new System.Drawing.Point(576, 20);
			this.btnDisconnectAuto.Name = "btnDisconnectAuto";
			this.btnDisconnectAuto.Size = new System.Drawing.Size(75, 20);
			this.btnDisconnectAuto.TabIndex = 21;
			this.btnDisconnectAuto.Text = "Disconnect";
			this.btnDisconnectAuto.UseVisualStyleBackColor = true;
			this.btnDisconnectAuto.Click += new System.EventHandler(this.btnDisconnectAuto_Click);
			// 
			// btnConnectAuto
			// 
			this.btnConnectAuto.Location = new System.Drawing.Point(495, 20);
			this.btnConnectAuto.Name = "btnConnectAuto";
			this.btnConnectAuto.Size = new System.Drawing.Size(75, 20);
			this.btnConnectAuto.TabIndex = 20;
			this.btnConnectAuto.Text = "Connect";
			this.btnConnectAuto.UseVisualStyleBackColor = true;
			this.btnConnectAuto.Click += new System.EventHandler(this.btnConnectAuto_Click);
			// 
			// txtHostAuto
			// 
			this.txtHostAuto.Location = new System.Drawing.Point(74, 20);
			this.txtHostAuto.Name = "txtHostAuto";
			this.txtHostAuto.Size = new System.Drawing.Size(246, 20);
			this.txtHostAuto.TabIndex = 19;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(39, 23);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(29, 13);
			this.label8.TabIndex = 18;
			this.label8.Text = "Host";
			// 
			// lbTrace
			// 
			this.lbTrace.FormattingEnabled = true;
			this.lbTrace.Location = new System.Drawing.Point(4, 362);
			this.lbTrace.Name = "lbTrace";
			this.lbTrace.Size = new System.Drawing.Size(686, 342);
			this.lbTrace.TabIndex = 1;
			this.lbTrace.Visible = false;
			// 
			// Main
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(694, 361);
			this.Controls.Add(this.lbTrace);
			this.Controls.Add(this.tabApplicationMode);
			this.Name = "Main";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Main";
			this.Load += new System.EventHandler(this.Main_Load);
			this.tabApplicationMode.ResumeLayout(false);
			this.tabManual.ResumeLayout(false);
			this.tabManual.PerformLayout();
			this.tabAuto.ResumeLayout(false);
			this.tabAuto.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabApplicationMode;
        private System.Windows.Forms.TabPage tabManual;
        private System.Windows.Forms.TabPage tabAuto;
        private System.Windows.Forms.Button btnPutConfirm;
        private System.Windows.Forms.Button btnGetConfirm;
        private System.Windows.Forms.Button btnPutBrowse;
        private System.Windows.Forms.TextBox txtPutPath;
        private System.Windows.Forms.TextBox txtGetFileName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnBrowseDefFolderAuto;
        private System.Windows.Forms.TextBox txtDefaultFolderAuto;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtPortAuto;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btnDisconnectAuto;
        private System.Windows.Forms.Button btnConnectAuto;
        private System.Windows.Forms.TextBox txtHostAuto;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtRenameTo;
        private System.Windows.Forms.Button btnRenameConfirm;
        private System.Windows.Forms.Button btnRenameFromBrowse;
        private System.Windows.Forms.TextBox txtRenameFrom;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnDeleteConfirm;
        private System.Windows.Forms.TextBox txtDeleteFileName;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button btnNewFolderConfirm;
        private System.Windows.Forms.TextBox txtNewFolderName;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btnGetFileHashes;
		private System.Windows.Forms.ListBox lbTrace;

    }
}

