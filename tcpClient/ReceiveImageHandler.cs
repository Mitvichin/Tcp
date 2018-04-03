using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace testTCP
{
	public partial class ReceiveImageHandler : Form
	{
		private string imgBase64 = "";

		public ReceiveImageHandler(string imgBase64)
		{
			this.imgBase64 = imgBase64;
			InitializeComponent();
		}

		private void saveBtn_Click(object sender, EventArgs e)
		{
			using (var folderBrowserDialog = new FolderBrowserDialog())
			{
				DialogResult dialogResult = folderBrowserDialog.ShowDialog();

				if (DialogResult == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
				{
					pictureBox.Image.Save(folderBrowserDialog.SelectedPath);
				}
			}
		}

		private void closeBtn_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void ReceiveImageHandler_Load(object sender, EventArgs e)
		{
			DisplayImg();
		}

		private void DisplayImg()
		{
			if (!string.IsNullOrEmpty(imgBase64))
			{
				byte[] imgData = Convert.FromBase64String(imgBase64);
				using (MemoryStream ms = new MemoryStream(imgData))
				{
					pictureBox.Image = Image.FromStream(ms);
				}
			}
		}
	}
}
