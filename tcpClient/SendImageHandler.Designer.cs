namespace testTCP
{
	partial class SendImageHandler
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
			this.browseBtn = new System.Windows.Forms.Button();
			this.closeBtn = new System.Windows.Forms.Button();
			this.sendBtn = new System.Windows.Forms.Button();
			this.pictureBox = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// browseBtn
			// 
			this.browseBtn.Location = new System.Drawing.Point(21, 402);
			this.browseBtn.Name = "browseBtn";
			this.browseBtn.Size = new System.Drawing.Size(75, 36);
			this.browseBtn.TabIndex = 10;
			this.browseBtn.Text = "Browse";
			this.browseBtn.UseVisualStyleBackColor = true;
			this.browseBtn.Click += new System.EventHandler(this.browseBtn_Click);
			// 
			// closeBtn
			// 
			this.closeBtn.Location = new System.Drawing.Point(689, 408);
			this.closeBtn.Name = "closeBtn";
			this.closeBtn.Size = new System.Drawing.Size(75, 36);
			this.closeBtn.TabIndex = 9;
			this.closeBtn.Text = "Close";
			this.closeBtn.UseVisualStyleBackColor = true;
			this.closeBtn.Click += new System.EventHandler(this.closeBtn_Click);
			// 
			// sendBtn
			// 
			this.sendBtn.Location = new System.Drawing.Point(560, 408);
			this.sendBtn.Name = "sendBtn";
			this.sendBtn.Size = new System.Drawing.Size(75, 36);
			this.sendBtn.TabIndex = 8;
			this.sendBtn.Text = "Send";
			this.sendBtn.UseVisualStyleBackColor = true;
			this.sendBtn.Click += new System.EventHandler(this.sendBtn_Click);
			// 
			// pictureBox
			// 
			this.pictureBox.Location = new System.Drawing.Point(2, 6);
			this.pictureBox.Name = "pictureBox";
			this.pictureBox.Size = new System.Drawing.Size(796, 375);
			this.pictureBox.TabIndex = 7;
			this.pictureBox.TabStop = false;
			// 
			// SendImageHandler
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.browseBtn);
			this.Controls.Add(this.closeBtn);
			this.Controls.Add(this.sendBtn);
			this.Controls.Add(this.pictureBox);
			this.Name = "SendImageHandler";
			this.Text = "SendImageHandler";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button browseBtn;
		private System.Windows.Forms.Button closeBtn;
		private System.Windows.Forms.Button sendBtn;
		private System.Windows.Forms.PictureBox pictureBox;
	}
}