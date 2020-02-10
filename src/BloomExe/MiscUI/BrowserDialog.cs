using System.Drawing;
using System.Windows.Forms;

namespace Bloom.MiscUI
{
	public partial class BrowserDialog : Form
	{
		// called by ProblemDialog.tsx Quit or Close buttons
		public void CloseDialog(string dummy)
		{
			if (CurrentDialog !=null)
			{
				CurrentDialog.Close();
				// caller will dispose
				CurrentDialog = null;
			}
			Browser.RemoveMessageEventListener("closeDialog");
		}

		public static Form CurrentDialog;
		public Browser Browser { get; }

		public BrowserDialog(string url)
		{
			InitializeComponent();
			this.FormBorderStyle = FormBorderStyle.None;

			// The Size setting is needed on Linux to keep the browser from coming up as a small
			// rectangle in the upper left corner...
			Browser = new Browser { Dock = DockStyle.Fill, Location = new Point(3, 3), Size = new Size(this.Width - 6, this.Height - 6) };
			Browser.BackColor = Color.White;
			Browser.AddMessageEventListener("closeDialog", CloseDialog);

			var dummy = Browser.Handle; // gets the WebBrowser created
			Browser.WebBrowser.DocumentCompleted += (sender, args) =>
			{
				// If the control gets added to the tab before it has navigated somewhere,
				// it shows as solid black, despite setting the BackColor to white.
				// So just don't show it at all until it contains what we want to see.
				this.Controls.Add(Browser);
			};
			Browser.Navigate(url, false);
			Browser.Focus();
			CurrentDialog = this;
		}
	}
}
