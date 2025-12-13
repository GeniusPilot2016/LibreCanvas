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
    public partial class SettingsWindow : Form
    {
        public SettingsWindow()
        {
            InitializeComponent();
            InitializeVariables();
            SetTheme();
            SetFonts();
        }
        private void SetFonts()
        {
            UIFonts uiFonts = UIFonts.Instance;
            foreach (TabPage tabpage in tabControl1.TabPages)
            {
                tabpage.Font = uiFonts.SetUIFont(tabpage.Font.Size, tabpage.Font.Style);
                foreach (Control control in tabpage.Controls)
                {
                    if (control is GroupBox groupBox)
                    {
                        foreach (Control groupedControl in groupBox.Controls)
                        {
                            groupedControl.Font = uiFonts.SetUIFont(groupedControl.Font.Size, groupedControl.Font.Style);
                            foreach(Control childGroupedControl in groupedControl.Controls)
                            {
                                childGroupedControl.Font = uiFonts.SetUIFont(childGroupedControl.Font.Size, childGroupedControl.Font.Style);
                            }
                        }
                    }
                }
            }
        }
        private void InitializeVariables()
        {
            // Initialize variables or settings here if needed

            // General settings
            comboBoxTheme.SelectedIndex = Settings1.Default.PreferredTheme;
            if (!string.IsNullOrEmpty(Settings1.Default.HashedGeminiAIAPIKey))
            {
                textBoxGeminiAPIKey.Text = EncryptionHelper.DecryptString(Settings1.Default.HashedGeminiAIAPIKey);
                buttonResetGeminiAPIKey.Enabled = true;
            }
            numericUpDownDefaultAIGeneratedImageWidth.Value = Settings1.Default.DefaultAIGeneratedImageSize.Width;
            numericUpDownDefaultAIGeneratedImageHeight.Value = Settings1.Default.DefaultAIGeneratedImageSize.Height;
            switch (Settings1.Default.ShowRecentFiles)
            {
                case true:
                    radioButtonShowStartup.Checked = true;
                    break;
                case false:
                    radioButtonDontShowStartup.Checked = true;
                    break;
            }

            // Canvas settings
            numericUpDownDefaultCanvasWidth.Value = Settings1.Default.DefaultCanvasSize.Width;
            numericUpDownDefaultCanvasHeight.Value = Settings1.Default.DefaultCanvasSize.Height;

            // Tools/Brushes settings
            // Brush size and style
            numericUpDownDefaultBrushSize.Value = Settings1.Default.DefaultBrushSize;
            comboBoxDefaultBrushStyle.SelectedIndex = Settings1.Default.DefaultBrushStyle;
            // Pen size and style
            numericUpDownDefaultPenSize.Value = Settings1.Default.DefaultPenSize;
            comboBoxDefaultPenStyle.SelectedIndex = Settings1.Default.DefaultPenStyle;
            // Eraser size
            numericUpDownDefaultEraserSize.Value = Settings1.Default.DefaultEraserSize;
            // Spray size
            numericUpDownDefaultSpraySize.Value = Settings1.Default.DefaultSpraySize;
            // Shape size
            numericUpDownDefaultShapeSize.Value = Settings1.Default.DefaultShapeSize;
            numericUpDownDefaultRadius.Value = Settings1.Default.DefaultRadiusSize;
            numericUpDownDefaultPoints.Value = Settings1.Default.DefaultPointsCount;
            // Text size
            numericUpDownDefaultTextSize.Value = Settings1.Default.DefaultTextSize;
            // Bucket tolerance
            numericUpDownDefaultBucketTolerance.Value = Settings1.Default.DefaultBucketTolerance;
            // Filter settings
            numericUpDownDefaultCartoonFilterSize.Value = Settings1.Default.DefaultCartoonFilterSize;
            numericUpDownDefaultCartoonFilterIntensity.Value = Settings1.Default.DefaultCartoonFilterIntensity;
            numericUpDownDefaultCartoonFilterThreshold.Value = Settings1.Default.DefaultCartoonFilterThreshold;
            numericUpDownDefaultOilPaintFilterSize.Value = Settings1.Default.DefaultOilPaintFilterSize;
            numericUpDownDefaultOilPaintFilterIntensity.Value = Settings1.Default.DefaultOilPaintFilterIntensity;
            numericUpDownDefaultOilPaintFilterThreshold.Value = Settings1.Default.DefaultOilPaintFilterThreshold;
            numericUpDownDefaultPixelationSize.Value = Settings1.Default.DefaultPixelationSize;
            numericUpDownDefaultOffsetX.Value = Settings1.Default.DefaultPixelationOffsetX;
            numericUpDownDefaultOffsetY.Value = Settings1.Default.DefaultPixelationOffsetY;
            numericUpDownDefaultGaussianBlurRadius.Value = (decimal)Settings1.Default.DefaultGaussianBlurRadius;
            // Color settings
            panelPrimaryColorPreview.BackColor = Settings1.Default.PrimaryColor;
            panelSecondaryColorPreview.BackColor = Settings1.Default.SecondaryColor;

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
        public void LightTheme()
        {
            this.BackColor = SystemColors.Control;
            foreach (TabPage tabPage in tabControl1.TabPages)
            {
                tabPage.BackColor = SystemColors.Window;
                tabPage.ForeColor = SystemColors.ControlText;
                foreach (Control control in tabPage.Controls)
                {
                    foreach (GroupBox groupBox in tabPage.Controls.OfType<GroupBox>())
                    {
                        foreach (NumericUpDown numericUpDown in groupBox.Controls.OfType<NumericUpDown>())
                        {
                            numericUpDown.BackColor = SystemColors.Window;
                            numericUpDown.ForeColor = SystemColors.WindowText;
                        }
                        groupBox.ForeColor = SystemColors.ControlText;
                        foreach (ComboBox comboBoxes in groupBox.Controls.OfType<ComboBox>())
                        {
                            comboBoxes.BackColor = SystemColors.Window;
                            comboBoxes.ForeColor = SystemColors.WindowText;
                        }
                        foreach (Button buttons in groupBox.Controls.OfType<Button>())
                        {
                            buttons.BackColor = Color.Transparent;
                        }
                        foreach (TextBox textBox in groupBox.Controls.OfType<TextBox>())
                        {
                            textBox.BackColor = SystemColors.Window;
                            textBox.ForeColor = SystemColors.WindowText;
                        }
                        foreach (GroupBox subGroupBox in groupBox.Controls.OfType<GroupBox>())
                        {
                            foreach (GroupBox subGrupboxOfSubGroupBoxes in subGroupBox.Controls.OfType<GroupBox>())
                            {
                                subGrupboxOfSubGroupBoxes.ForeColor = SystemColors.ControlText;
                                foreach (TextBox textBoxes in subGrupboxOfSubGroupBoxes.Controls.OfType<TextBox>())
                                {
                                    textBoxes.BackColor = SystemColors.Window;
                                    textBoxes.ForeColor = SystemColors.WindowText;
                                }
                                foreach (Button buttons in subGrupboxOfSubGroupBoxes.Controls.OfType<Button>())
                                {
                                    buttons.BackColor = Color.Transparent;
                                }
                            }
                            foreach (ComboBox comboBoxes in subGroupBox.Controls.OfType<ComboBox>())
                            {
                                comboBoxes.BackColor = SystemColors.Window;
                                comboBoxes.ForeColor = SystemColors.WindowText;
                            }
                            foreach (NumericUpDown numericUpDown in subGroupBox.Controls.OfType<NumericUpDown>())
                            {
                                numericUpDown.BackColor = SystemColors.Window;
                                numericUpDown.ForeColor = SystemColors.WindowText;
                            }
                            subGroupBox.ForeColor = SystemColors.ControlText;
                            foreach (TextBox textBox in subGroupBox.Controls.OfType<TextBox>())
                            {
                                textBox.BackColor = SystemColors.Window;
                                textBox.ForeColor = SystemColors.WindowText;
                            }
                            foreach (Button buttons in subGroupBox.Controls.OfType<Button>())
                            {
                                buttons.BackColor = SystemColors.Control;
                            }
                        }
                    }
                }
            }
            TitleBarHelper.ApplyCustomTitleBar(this, false);
        }
        public void DarkTheme()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            foreach (TabPage tabPage in tabControl1.TabPages)
            {
                tabPage.BackColor = Color.FromArgb(32, 32, 32);
                tabPage.ForeColor = Color.White;
                foreach (Control control in tabPage.Controls)
                {
                    foreach (GroupBox groupBox in tabPage.Controls.OfType<GroupBox>())
                    {
                        foreach (ComboBox comboBoxes in groupBox.Controls.OfType<ComboBox>())
                        {
                            comboBoxes.BackColor = Color.Black;
                            comboBoxes.ForeColor = Color.White;
                        }
                        foreach (TextBox textBox in groupBox.Controls.OfType<TextBox>())
                        {
                            textBox.BackColor = Color.Black;
                            textBox.ForeColor = Color.White;
                        }
                        foreach (NumericUpDown numericUpDown in groupBox.Controls.OfType<NumericUpDown>())
                        {
                            numericUpDown.BackColor = Color.Black;
                            numericUpDown.ForeColor = Color.White;
                        }
                        groupBox.ForeColor = Color.White;
                        foreach (Button buttons in groupBox.Controls.OfType<Button>())
                        {
                            buttons.BackColor = Color.FromArgb(32, 32, 32);
                        }
                        foreach (GroupBox subGroupBox in groupBox.Controls.OfType<GroupBox>())
                        {
                            foreach (GroupBox subGrupboxOfSubGroupBoxes in subGroupBox.Controls.OfType<GroupBox>())
                            {
                                subGrupboxOfSubGroupBoxes.ForeColor = Color.White;
                                foreach (TextBox textBoxes in subGrupboxOfSubGroupBoxes.Controls.OfType<TextBox>())
                                {
                                    textBoxes.BackColor = Color.Black;
                                    textBoxes.ForeColor = Color.White;
                                }
                                foreach (Button buttons in subGrupboxOfSubGroupBoxes.Controls.OfType<Button>())
                                {
                                    buttons.BackColor = Color.FromArgb(32, 32, 32);
                                }
                            }
                            foreach (ComboBox comboBoxes in subGroupBox.Controls.OfType<ComboBox>())
                            {
                                comboBoxes.BackColor = Color.Black;
                                comboBoxes.ForeColor = Color.White;
                            }
                            foreach (TextBox textBox in subGroupBox.Controls.OfType<TextBox>())
                            {
                                textBox.BackColor = Color.Black;
                                textBox.ForeColor = Color.White;
                            }
                            foreach (NumericUpDown numericUpDown in subGroupBox.Controls.OfType<NumericUpDown>())
                            {
                                numericUpDown.BackColor = Color.Black;
                                numericUpDown.ForeColor = Color.White;
                            }
                            subGroupBox.ForeColor = Color.White;
                            foreach (Button buttons in subGroupBox.Controls.OfType<Button>())
                            {
                                buttons.BackColor = Color.FromArgb(32, 32, 32);
                            }
                        }
                    }
                }
            }
            TitleBarHelper.ApplyCustomTitleBar(this, true);
        }
        private void comboBoxTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBoxTheme.SelectedIndex != Settings1.Default.PreferredTheme)
            {
                Settings1.Default.PreferredTheme = comboBoxTheme.SelectedIndex;
                Settings1.Default.Save(); // Save the settings to persist the changes

                // Apply theme to this form first
                SetTheme();

                // Apply theme to all registered forms
                SystemThemeUtility.ApplyThemeToAllForms();
            }
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
                textBoxGeminiAPIKey.Text == EncryptionHelper.DecryptString(Settings1.Default.HashedGeminiAIAPIKey))
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
                if(CreateAIImages.isAPIKeyValidFormat(textBoxGeminiAPIKey.Text))
                {
                    EncryptionHelper.ChangeKeyAndIV(); // Change the key and IV for encryption

                    Settings1.Default.HashedGeminiAIAPIKey = EncryptionHelper.EncryptString(textBoxGeminiAPIKey.Text);
                    Settings1.Default.Save(); // Save the settings to persist the changes
                    buttonUpdateGeminiAPIKey.Enabled = false;
                    buttonResetGeminiAPIKey.Enabled = true;
                    Logger.Log("Google Gemini™ API key is saved.", Logger.LogTypes.Info);
                    MessageForm.Show("Google Gemini™ API key is saved successfully.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Logger.Log("The provided Google Gemini™ API key format is invalid.", Logger.LogTypes.Error);
                    MessageForm.Show("The provided Google Gemini™ API key format is invalid. Please check and try again.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error saving Google Gemini™ API key: " + ex.Message, Logger.LogTypes.Error);
                MessageForm.Show("Error: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                EncryptionHelper.ChangeKeyAndIV(); // Change the key and IV for encryption
                Settings1.Default.HashedGeminiAIAPIKey = string.Empty; // Clear the API key by setting it to an empty string  
                Settings1.Default.Save(); // Save the settings to persist the changes
                buttonUpdateGeminiAPIKey.Enabled = false;
                buttonResetGeminiAPIKey.Enabled = false; // Disable the button after clearing the key  
                textBoxGeminiAPIKey.Clear(); // Clear the text box
                Logger.Log("Google Gemini™ API key has been cleared.", Logger.LogTypes.Info);
                MessageForm.Show("Google Gemini™ API key has been cleared successfully.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Log("Error clearing Google Gemini™ API key: " + ex.Message, Logger.LogTypes.Error);
                MessageForm.Show("Error: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void numericUpDownDefaultPoints_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultPointsCount = (int)numericUpDownDefaultPoints.Value;
            Settings1.Default.Save();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultCartoonFilterSize = (int)numericUpDownDefaultCartoonFilterSize.Value;
            Settings1.Default.Save();
            FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize = (int)numericUpDownDefaultCartoonFilterSize.Value;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultCartoonFilterIntensity = (int)numericUpDownDefaultCartoonFilterIntensity.Value;
            Settings1.Default.Save();
            FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity = (int)numericUpDownDefaultCartoonFilterIntensity.Value;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultCartoonFilterThreshold = (int)numericUpDownDefaultCartoonFilterThreshold.Value;
            Settings1.Default.Save();
            FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold = (int)numericUpDownDefaultCartoonFilterThreshold.Value;
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultOilPaintFilterSize = (int)numericUpDownDefaultOilPaintFilterSize.Value;
            Settings1.Default.Save();
            FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize = (int)numericUpDownDefaultOilPaintFilterSize.Value;
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultOilPaintFilterIntensity = (int)numericUpDownDefaultOilPaintFilterIntensity.Value;
            Settings1.Default.Save();
            FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity = (int)numericUpDownDefaultOilPaintFilterIntensity.Value;
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultOilPaintFilterThreshold = (int)numericUpDownDefaultOilPaintFilterThreshold.Value;
            Settings1.Default.Save();
            FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold = (int)numericUpDownDefaultOilPaintFilterThreshold.Value;
        }

        private void numericUpDownDefaultPixelationSize_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultPixelationSize = (int)numericUpDownDefaultPixelationSize.Value;
            Settings1.Default.Save();
        }

        private void numericUpDownDefaultOffsetX_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultPixelationOffsetX = (int)numericUpDownDefaultOffsetX.Value;
            Settings1.Default.Save();
        }

        private void numericUpDownDefaultOffsetY_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultPixelationOffsetY = (int)numericUpDownDefaultOffsetY.Value;
            Settings1.Default.Save();
        }

        private void numericUpDownDefaultGaussianBlurRadius_ValueChanged(object sender, EventArgs e)
        {
            Settings1.Default.DefaultGaussianBlurRadius = (float)numericUpDownDefaultGaussianBlurRadius.Value;
            Settings1.Default.Save();
        }
    }
}
