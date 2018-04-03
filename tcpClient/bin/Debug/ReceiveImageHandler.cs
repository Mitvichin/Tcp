using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageHandler
{
	public partial class ReceiveImageHandler : Form
	{
		byte[] imageData;

		public ReceiveImageHandler()
		{
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
			ReceiveImageHandler.ActiveForm.Close();
		}

		private void pictureBox_Click(object sender, EventArgs e)
		{

		}
	}
}
