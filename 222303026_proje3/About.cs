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
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
            SetTheme();
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
            listView1.BackColor = ListView.DefaultBackColor;
            listView1.ForeColor = ListView.DefaultForeColor;
            foreach (ListViewItem item in listView1.Items)
            {
                item.BackColor = ListView.DefaultBackColor;
                item.ForeColor = ListView.DefaultForeColor;
            }
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
    }
}
