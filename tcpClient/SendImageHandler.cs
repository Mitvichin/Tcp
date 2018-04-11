using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;
using tcpClient;

namespace testTCP
{
	public partial class SendImageHandler : Form
	{
		private Socket client;

		public SendImageHandler(Socket client)
		{
			this.client = client;
			InitializeComponent();
		}

		private void browseBtn_Click(object sender, EventArgs e)
		{
			var fileDialog = new OpenFileDialog();

			fileDialog.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";

			if (fileDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fileDialog.FileName))
			{
				pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
				pictureBox.Image = new Bitmap(fileDialog.FileName);
			}
		}

		private void closeBtn_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void sendBtn_Click(object sender, EventArgs e)
		{
			if (pictureBox.Image != null)
			{
				Bitmap bmp = new Bitmap(pictureBox.Image);
				string imgBase64 = string.Empty;
				ImageConverter ic = new ImageConverter();
				byte[] buffer = (byte[])ic.ConvertTo(pictureBox.Image, typeof(byte[]));
				imgBase64 = Convert.ToBase64String(
					buffer,
					Base64FormattingOptions.InsertLineBreaks);

				Client.Send(client, imgBase64);
				Client.sendDone.WaitOne();
				Application.Exit();
			}
			else
			{
				string message = "You did not choose a picture!";
				string caption = "Error Detected in Input";
				MessageBoxButtons buttons = MessageBoxButtons.OK;
				DialogResult result;
				result = MessageBox.Show(message, caption, buttons);
			}
		}
	}
}
