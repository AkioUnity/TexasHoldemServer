namespace TexasHoldemServer
{
    partial class Main
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.StartServer = new System.Windows.Forms.Button();
            this.ServerLog = new System.Windows.Forms.ListBox();
            this.ServerIPAddressLabel = new System.Windows.Forms.Label();
            this.btDebugAllSetMoney = new System.Windows.Forms.Button();
            this.btUserBlock = new System.Windows.Forms.Button();
            this.tbBlockID = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // StartServer
            // 
            this.StartServer.Location = new System.Drawing.Point(534, 12);
            this.StartServer.Name = "StartServer";
            this.StartServer.Size = new System.Drawing.Size(116, 23);
            this.StartServer.TabIndex = 0;
            this.StartServer.Text = "StartServer";
            this.StartServer.UseVisualStyleBackColor = true;
            this.StartServer.Click += new System.EventHandler(this.StartServer_Click);
            // 
            // ServerLog
            // 
            this.ServerLog.FormattingEnabled = true;
            this.ServerLog.HorizontalScrollbar = true;
            this.ServerLog.ItemHeight = 12;
            this.ServerLog.Location = new System.Drawing.Point(12, 41);
            this.ServerLog.Name = "ServerLog";
            this.ServerLog.Size = new System.Drawing.Size(838, 324);
            this.ServerLog.TabIndex = 1;
            // 
            // ServerIPAddressLabel
            // 
            this.ServerIPAddressLabel.AutoSize = true;
            this.ServerIPAddressLabel.Location = new System.Drawing.Point(12, 17);
            this.ServerIPAddressLabel.Name = "ServerIPAddressLabel";
            this.ServerIPAddressLabel.Size = new System.Drawing.Size(115, 12);
            this.ServerIPAddressLabel.TabIndex = 2;
            this.ServerIPAddressLabel.Text = "Server IP Address :";
            // 
            // btDebugAllSetMoney
            // 
            this.btDebugAllSetMoney.Location = new System.Drawing.Point(12, 256);
            this.btDebugAllSetMoney.Name = "btDebugAllSetMoney";
            this.btDebugAllSetMoney.Size = new System.Drawing.Size(0, 0);  //213,23
            this.btDebugAllSetMoney.TabIndex = 3;
            this.btDebugAllSetMoney.Text = "Debug All Money 100000";
            this.btDebugAllSetMoney.UseVisualStyleBackColor = true;
            this.btDebugAllSetMoney.Click += new System.EventHandler(this.btDebugAllSetMoney_Click);
            // 
            // btUserBlock
            // 
            this.btUserBlock.Location = new System.Drawing.Point(122, 184);
            this.btUserBlock.Name = "btUserBlock";
            this.btUserBlock.Size = new System.Drawing.Size(0, 0); //90,21
            this.btUserBlock.TabIndex = 4;
            this.btUserBlock.Text = "BlockUser";
            this.btUserBlock.UseVisualStyleBackColor = true;
            this.btUserBlock.Click += new System.EventHandler(this.btUserBlock_Click);
            // 
            // tbBlockID
            // 
            this.tbBlockID.Location = new System.Drawing.Point(12, 184);
            this.tbBlockID.Name = "tbBlockID";
            this.tbBlockID.Size = new System.Drawing.Size(0, 0); // (104, 21);
            this.tbBlockID.TabIndex = 5;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(862, 391);
            this.Controls.Add(this.tbBlockID);
            this.Controls.Add(this.btUserBlock);
            this.Controls.Add(this.btDebugAllSetMoney);
            this.Controls.Add(this.ServerIPAddressLabel);
            this.Controls.Add(this.ServerLog);
            this.Controls.Add(this.StartServer);
            this.Name = "Main";
            this.Text = "Server";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button StartServer;
        private System.Windows.Forms.ListBox ServerLog;
        private System.Windows.Forms.Label ServerIPAddressLabel;
        private System.Windows.Forms.Button btDebugAllSetMoney;
        private System.Windows.Forms.Button btUserBlock;
        private System.Windows.Forms.TextBox tbBlockID;
    }
}

