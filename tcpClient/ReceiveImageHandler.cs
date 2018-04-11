using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
			Bitmap imgBmp = new Bitmap(pictureBox.Image);
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";

			if (sfd.ShowDialog() == DialogResult.OK)
			{

				imgBmp.Save(sfd.FileName, pictureBox.Image.RawFormat);
			}

			Application.Exit();
		}

		private void closeBtn_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void ReceiveImageHandler_Load(object sender, EventArgs e)
		{

			Focus();
			DisplayImg();
		}

		private void DisplayImg()
		{
			pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

			if (!string.IsNullOrEmpty(imgBase64))
			{

				string converted = imgBase64.Replace('-', '+');
				converted = converted.Replace('_', '/');
				byte[] imgData = Convert.FromBase64String(converted);

				ImageConverter ic = new ImageConverter();
				pictureBox.Image = ic.ConvertFrom(imgData) as Image;
			}
		}
	}
}
