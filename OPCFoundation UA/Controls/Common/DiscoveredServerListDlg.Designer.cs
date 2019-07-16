/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client.Controls
{
    partial class DiscoveredServerListDlg
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
            this.ButtonsPN = new System.Windows.Forms.Panel();
            this.OkBTN = new System.Windows.Forms.Button();
            this.CancelBTN = new System.Windows.Forms.Button();
            this.MainPN = new System.Windows.Forms.Panel();
            this.ServersCTRL = new Opc.Ua.Client.Controls.DiscoveredServerListCtrl();
            this.TopPN = new System.Windows.Forms.Panel();
            this.HostNameLB = new System.Windows.Forms.Label();
            this.HostNameCTRL = new Opc.Ua.Client.Controls.SelectHostCtrl();
            this.ButtonsPN.SuspendLayout();
            this.MainPN.SuspendLayout();
            this.TopPN.SuspendLayout();
            this.SuspendLayout();
            // 
            // ButtonsPN
            // 
            this.ButtonsPN.Controls.Add(this.OkBTN);
            this.ButtonsPN.Controls.Add(this.CancelBTN);
            this.ButtonsPN.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ButtonsPN.Location = new System.Drawing.Point(5, 842);
            this.ButtonsPN.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.ButtonsPN.Name = "ButtonsPN";
            this.ButtonsPN.Size = new System.Drawing.Size(1795, 74);
            this.ButtonsPN.TabIndex = 0;
            // 
            // OkBTN
            // 
            this.OkBTN.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.OkBTN.Location = new System.Drawing.Point(11, 10);
            this.OkBTN.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.OkBTN.Name = "OkBTN";
            this.OkBTN.Size = new System.Drawing.Size(200, 55);
            this.OkBTN.TabIndex = 1;
            this.OkBTN.Text = "OK";
            this.OkBTN.UseVisualStyleBackColor = true;
            this.OkBTN.Click += new System.EventHandler(this.OkBTN_Click);
            // 
            // CancelBTN
            // 
            this.CancelBTN.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelBTN.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBTN.Location = new System.Drawing.Point(1584, 10);
            this.CancelBTN.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.CancelBTN.Name = "CancelBTN";
            this.CancelBTN.Size = new System.Drawing.Size(200, 55);
            this.CancelBTN.TabIndex = 0;
            this.CancelBTN.Text = "Cancel";
            this.CancelBTN.UseVisualStyleBackColor = true;
            // 
            // MainPN
            // 
            this.MainPN.Controls.Add(this.ServersCTRL);
            this.MainPN.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainPN.Location = new System.Drawing.Point(5, 55);
            this.MainPN.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.MainPN.Name = "MainPN";
            this.MainPN.Padding = new System.Windows.Forms.Padding(0, 7, 0, 0);
            this.MainPN.Size = new System.Drawing.Size(1795, 787);
            this.MainPN.TabIndex = 2;
            // 
            // ServersCTRL
            // 
            this.ServersCTRL.Cursor = System.Windows.Forms.Cursors.Default;
            this.ServersCTRL.DiscoveryTimeout = 0;
            this.ServersCTRL.DiscoveryUrl = null;
            this.ServersCTRL.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ServersCTRL.Instructions = null;
            this.ServersCTRL.Location = new System.Drawing.Point(0, 7);
            this.ServersCTRL.Margin = new System.Windows.Forms.Padding(21, 17, 21, 17);
            this.ServersCTRL.Name = "ServersCTRL";
            this.ServersCTRL.Size = new System.Drawing.Size(1795, 780);
            this.ServersCTRL.TabIndex = 0;
            this.ServersCTRL.ItemsPicked += new Opc.Ua.Client.Controls.ListItemActionEventHandler(this.ServersCTRL_ItemsPicked);
            this.ServersCTRL.ItemsSelected += new Opc.Ua.Client.Controls.ListItemActionEventHandler(this.ServersCTRL_ItemsSelected);
            // 
            // TopPN
            // 
            this.TopPN.Controls.Add(this.HostNameLB);
            this.TopPN.Controls.Add(this.HostNameCTRL);
            this.TopPN.Dock = System.Windows.Forms.DockStyle.Top;
            this.TopPN.Location = new System.Drawing.Point(5, 5);
            this.TopPN.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.TopPN.Name = "TopPN";
            this.TopPN.Size = new System.Drawing.Size(1795, 50);
            this.TopPN.TabIndex = 1;
            // 
            // HostNameLB
            // 
            this.HostNameLB.AutoSize = true;
            this.HostNameLB.Location = new System.Drawing.Point(0, 10);
            this.HostNameLB.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.HostNameLB.Name = "HostNameLB";
            this.HostNameLB.Size = new System.Drawing.Size(155, 32);
            this.HostNameLB.TabIndex = 0;
            this.HostNameLB.Text = "Host Name";
            this.HostNameLB.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // HostNameCTRL
            // 
            this.HostNameCTRL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.HostNameCTRL.CommandText = "Discover";
            this.HostNameCTRL.Location = new System.Drawing.Point(168, 0);
            this.HostNameCTRL.Margin = new System.Windows.Forms.Padding(0);
            this.HostNameCTRL.MaximumSize = new System.Drawing.Size(10923, 57);
            this.HostNameCTRL.MinimumSize = new System.Drawing.Size(1067, 50);
            this.HostNameCTRL.Name = "HostNameCTRL";
            this.HostNameCTRL.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.HostNameCTRL.Size = new System.Drawing.Size(1627, 50);
            this.HostNameCTRL.TabIndex = 1;
            this.HostNameCTRL.HostSelected += new System.EventHandler<Opc.Ua.Client.Controls.SelectHostCtrlEventArgs>(this.HostNameCTRL_HostSelected);
            this.HostNameCTRL.HostConnected += new System.EventHandler<Opc.Ua.Client.Controls.SelectHostCtrlEventArgs>(this.HostNameCTRL_HostConnected);
            this.HostNameCTRL.Load += new System.EventHandler(this.HostNameCTRL_Load);
            // 
            // DiscoveredServerListDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 31F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1805, 916);
            this.Controls.Add(this.MainPN);
            this.Controls.Add(this.TopPN);
            this.Controls.Add(this.ButtonsPN);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.MaximizeBox = false;
            this.Name = "DiscoveredServerListDlg";
            this.Padding = new System.Windows.Forms.Padding(5, 5, 5, 0);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Discover Servers";
            this.ButtonsPN.ResumeLayout(false);
            this.MainPN.ResumeLayout(false);
            this.TopPN.ResumeLayout(false);
            this.TopPN.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel ButtonsPN;
        private System.Windows.Forms.Button OkBTN;
        private System.Windows.Forms.Button CancelBTN;
        private System.Windows.Forms.Panel MainPN;
        private DiscoveredServerListCtrl ServersCTRL;
        private SelectHostCtrl HostNameCTRL;
        private System.Windows.Forms.Panel TopPN;
        private System.Windows.Forms.Label HostNameLB;
    }
}
