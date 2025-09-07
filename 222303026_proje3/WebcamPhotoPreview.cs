using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _222303026_proje3
{
    public partial class WebcamPhotoPreview : Form
    {
        public event EventHandler PhotoAccepted;
        public event EventHandler PhotoDiscarded;

        public WebcamPhotoPreview(Image image)
        {
            InitializeComponent();
            pictureBox1.Image = image;
            ThemeManager.RegisterForm(this); 
            SetTheme();
            SetFonts();
        }
        private void SetFonts()
        {
            UIFonts uiFonts = UIFonts.Instance;
            foreach (Control control in Controls)
            {
                control.Font = uiFonts.SetUIFont(control.Font.Size, control.Font.Style);
            }
        }
        private void SetTheme()
        {
            switch (Settings1.Default.PreferredTheme)
            {
                case 0: // System theme
                    if (CheckSystemTheme.IsDarkTheme())
                    {
                        DarkTheme();
                    }
                    else
                    {
                        LightTheme();
                    }
                    break;
                case 1: // Light theme
                    LightTheme();
                    break;
                case 2: // Dark theme
                    DarkTheme();
                    break;
            }
        }
        private void LightTheme()
        {
            this.BackColor = Form.DefaultBackColor;
            this.ForeColor = Form.DefaultForeColor;
            buttonAccept.BackColor = Color.Transparent;
            buttonDiscard.BackColor = Color.Transparent;
            TitleBarHelper.ApplyCustomTitleBar(this, false);
        }
        private void DarkTheme()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            buttonAccept.BackColor = Color.FromArgb(32, 32, 32);
            buttonDiscard.BackColor = Color.FromArgb(32, 32, 32);
            TitleBarHelper.ApplyCustomTitleBar(this, true);
        }
        private void buttonAccept_Click(object sender, EventArgs e)
        {
            PhotoAccepted?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void buttonDiscard_Click(object sender, EventArgs e)
        {
            PhotoDiscarded?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void WebcamPhotoPreview_FormClosed(object sender, FormClosedEventArgs e)
        {
            PhotoDiscarded?.Invoke(this, EventArgs.Empty);
        }
    }
}
