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
            if (!string.IsNullOrEmpty(Settings1.Default.HashedAIAPIKey))
            {
                textBox1.Text = EncryptionHelper.DecryptString(Settings1.Default.HashedAIAPIKey);
                Settings1.Default.Save();
                button5.Enabled = true;
            }
        }

        private void comboBoxTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings1.Default.PreferredTheme = comboBoxTheme.SelectedIndex;
            Settings1.Default.Save(); // Save the settings to persist the changes
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged_1(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                Settings1.Default.ShowRecentFiles = true;
            }
            else if (radioButton2.Checked)
            {
                Settings1.Default.ShowRecentFiles = false;
            }
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.UseSystemPasswordChar)
            {
                textBox1.UseSystemPasswordChar = false;
                button3.ImageIndex = 6;
                button3.Text = "Hide";
            }
            else
            {
                textBox1.UseSystemPasswordChar = true;
                button3.ImageIndex = 5;
                button3.Text = "Show";
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text) ||
                textBox1.Text == EncryptionHelper.DecryptString(Settings1.Default.HashedAIAPIKey))
            {
                button4.Enabled = false;
            }
            else
            {
                button4.Enabled = true;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                Settings1.Default.HashedAIAPIKey = EncryptionHelper.EncryptString(textBox1.Text);
                Settings1.Default.Save(); // Save the settings to persist the changes
                button4.Enabled = false;
                button5.Enabled = true;
                MessageBox.Show("Google Gemini™ API key is saved successfully.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                Settings1.Default.HashedAIAPIKey = string.Empty; // Clear the API key by setting it to an empty string  
                Settings1.Default.Save(); // Save the settings to persist the changes
                button4.Enabled = false;
                button5.Enabled = false; // Disable the button after clearing the key  
                textBox1.Clear(); // Clear the text box
                MessageBox.Show("Google Gemini™ API key has been cleared successfully.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void numericUpDownDefaultAIGeneratedImageHeight_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultAIGeneratedImageSize = new Size((int)numericUpDownDefaultAIGeneratedImageWidth.Value,
                (int)numericUpDownDefaultAIGeneratedImageHeight.Value);
            Settings1.Default.Save();
        }
    }
}
