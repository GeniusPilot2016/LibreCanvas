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
    public partial class SettingsWindow : Form
    {
        public SettingsWindow()
        {
            InitializeComponent();
            comboBoxTheme.SelectedIndex = Settings1.Default.PreferredTheme;
        }

        private void comboBoxTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings1.Default.PreferredTheme = comboBoxTheme.SelectedIndex;
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged_1(object sender, EventArgs e)
        {
            if(radioButton1.Checked)
            {
                Settings1.Default.ShowRecentFiles = true;
            }
            else if (radioButton2.Checked)
            {
                Settings1.Default.ShowRecentFiles = false;
            }
        }
    }
}
