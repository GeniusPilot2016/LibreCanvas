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
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent(); 
            SystemThemeUtility.RegisterForm(this);
            SetTheme();
            SetFonts();
            label3.Text = $"Version {GetInformations.GetVersionAndStatus().version} {GetInformations.GetVersionAndStatus().status}";
        }
        private void SetFonts()
        {
            UIFonts uiFonts = UIFonts.Instance;
            foreach (Control control in Controls)
            {
                control.Font = uiFonts.SetUIFont(control.Font.Size, control.Font.Style);
                foreach (Control logoParts in panel1.Controls)
                {
                    logoParts.Font = uiFonts.SetUIFont(logoParts.Font.Size, logoParts.Font.Style);
                }
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
            label4.ForeColor = Form.DefaultForeColor;
            label1.ForeColor = Form.DefaultForeColor;
            foreach (Button buttons in Controls.OfType<Button>())
            {
                buttons.BackColor = Color.Transparent;
                buttons.ForeColor = Button.DefaultForeColor;
            }
            listView1.BackColor = SystemColors.Window;
            listView1.ForeColor = ListView.DefaultForeColor;
            foreach (ListViewItem item in listView1.Items)
            {
                item.BackColor = SystemColors.Window;
                item.ForeColor = ListView.DefaultForeColor;
            }
            TitleBarHelper.ApplyCustomTitleBar(this, false);
        }
        private void DarkTheme()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            label4.ForeColor = Color.White;
            label1.ForeColor = Color.White;
            foreach (Button buttons in Controls.OfType<Button>())
            {
                buttons.BackColor = Color.FromArgb(32, 32, 32);
                buttons.ForeColor = Color.White;
            }
            listView1.BackColor = Color.Black;
            listView1.ForeColor = Color.White;
            foreach (ListViewItem item in listView1.Items)
            {
                item.BackColor = Color.Black;
                item.ForeColor = Color.White;
            }
            TitleBarHelper.ApplyCustomTitleBar(this, true);
        }
        private void About_Load(object sender, EventArgs e)
        {

        }

        private void buttonVisitIcons8_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://icons8.com/") { UseShellExecute = true });
        }

        private void buttonForkMeOnGithub_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/GeniusPilot2016/ArtFusion") { UseShellExecute = true });
        }

        private void buttonViewLicenseText_Click(object sender, EventArgs e)
        {
            LicenseWindow licenseWindow = new LicenseWindow();
            licenseWindow.ShowDialog();
        }
    }
}
