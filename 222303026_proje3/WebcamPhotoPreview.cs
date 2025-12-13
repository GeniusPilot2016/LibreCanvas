// ArtFusion - The AI-enabled simple image editor for everyone, born as a school project by GeniusPilot2016
// Copyright (C) 2025 GeniusPilot2016
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

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
            SystemThemeUtility.RegisterForm(this); 
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
