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
                textBoxGeminiAPIKey.Text = EncryptionHelper.DecryptString(Settings1.Default.HashedAIAPIKey);
                Settings1.Default.Save();
                buttonResetGeminiAPIKey.Enabled = true;
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
            if (radioButtonShowStartup.Checked)
            {
                Settings1.Default.ShowRecentFiles = true;
            }
            else if (radioButtonDontShowStartup.Checked)
            {
                Settings1.Default.ShowRecentFiles = false;
            }
            Settings1.Default.Save(); // Save the settings to persist the changes
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (textBoxGeminiAPIKey.UseSystemPasswordChar)
            {
                textBoxGeminiAPIKey.UseSystemPasswordChar = false;
                buttonShowHideGeminiAPIKey.ImageIndex = 6;
                buttonShowHideGeminiAPIKey.Text = "Hide";
            }
            else
            {
                textBoxGeminiAPIKey.UseSystemPasswordChar = true;
                buttonShowHideGeminiAPIKey.ImageIndex = 5;
                buttonShowHideGeminiAPIKey.Text = "Show";
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxGeminiAPIKey.Text) ||
                textBoxGeminiAPIKey.Text == EncryptionHelper.DecryptString(Settings1.Default.HashedAIAPIKey))
            {
                buttonUpdateGeminiAPIKey.Enabled = false;
            }
            else
            {
                buttonUpdateGeminiAPIKey.Enabled = true;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                Settings1.Default.HashedAIAPIKey = EncryptionHelper.EncryptString(textBoxGeminiAPIKey.Text);
                Settings1.Default.Save(); // Save the settings to persist the changes
                buttonUpdateGeminiAPIKey.Enabled = false;
                buttonResetGeminiAPIKey.Enabled = true;
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
                buttonUpdateGeminiAPIKey.Enabled = false;
                buttonResetGeminiAPIKey.Enabled = false; // Disable the button after clearing the key  
                textBoxGeminiAPIKey.Clear(); // Clear the text box
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

        private void numericUpDownDefaultRadius_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultRadiusSize = (int)numericUpDownDefaultRadius.Value;
            Settings1.Default.Save();
        }

        private void comboBoxDefaultBrushStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultBrushStyle = comboBoxDefaultBrushStyle.SelectedIndex;
            Settings1.Default.Save();
        }

        private void numericUpDownDefaultBrushSize_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultBrushSize = (int)numericUpDownDefaultBrushSize.Value;
            Settings1.Default.Save();
        }

        private void numericUpDownDefaultPenSize_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultPenSize = (int)numericUpDownDefaultPenSize.Value;
            Settings1.Default.Save();
        }

        private void comboBoxDefaultPenStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultPenStyle = comboBoxDefaultPenStyle.SelectedIndex;
            Settings1.Default.Save();
        }

        private void numericUpDownDefaultSpraySize_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultSpraySize = (int)numericUpDownDefaultSpraySize.Value;
            Settings1.Default.Save();
        }

        private void numericUpDownDefaultEraserSize_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultEraserSize = (int)numericUpDownDefaultEraserSize.Value;
            Settings1.Default.Save();
        }

        private void numericUpDownDefaultShapeSize_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultShapeSize = (int)numericUpDownDefaultShapeSize.Value;
            Settings1.Default.Save();
        }

        private void numericUpDownDefaultTextSize_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultTextSize = (int)numericUpDownDefaultTextSize.Value;
            Settings1.Default.Save();
        }

        private void numericUpDownDefaultBucketTolerance_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultBucketTolerance = (int)numericUpDownDefaultBucketTolerance.Value;
            Settings1.Default.Save();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = Settings1.Default.PrimaryColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                Settings1.Default.PrimaryColor = colorDialog1.Color;
                panelPrimaryColorPreview.BackColor = colorDialog1.Color;
                Settings1.Default.Save();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = Settings1.Default.SecondaryColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                Settings1.Default.SecondaryColor = colorDialog1.Color;
                panelSecondaryColorPreview.BackColor = colorDialog1.Color;
                Settings1.Default.Save();
            }
        }
    }
}
