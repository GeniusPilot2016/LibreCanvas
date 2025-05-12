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
        public readonly string DefaultGeminiAIModel = "gemini-2.0-flash-preview-image-generation";
        public readonly string DefaultHuggingFaceModel = "black-forest-labs/FLUX.1-dev";
        public readonly string DefaultHuggingFaceModel2 = "mit-han-lab/svdq-int4-flux.1-fill-dev";
        public SettingsWindow()
        {
            InitializeComponent();
            InitializeVariables();
            SetTheme();
        }
        private void InitializeVariables()
        {
            // Initialize variables or settings here if needed

            // General settings
            comboBoxTheme.SelectedIndex = Settings1.Default.PreferredTheme;
            if (!string.IsNullOrEmpty(Settings1.Default.HashedGeminiAIAPIKey))
            {
                textBoxGeminiAPIKey.Text = EncryptionHelper.DecryptString1(Settings1.Default.HashedGeminiAIAPIKey);
                buttonResetGeminiAPIKey.Enabled = true;
            }
            if (!string.IsNullOrEmpty(Settings1.Default.HashedHuggingFaceAPIKey))
            {
                textBoxHuggingFaceAPIKey.Text = EncryptionHelper.DecryptString2(Settings1.Default.HashedHuggingFaceAPIKey);
                buttonResetHuggingFaceAPIKey.Enabled = true;
            }
            switch (Settings1.Default.PreferredAI)
            {
                case 0: // Google Gemini
                    radioButtonGeminiAI.Checked = true;
                    break;
                case 1: // Hugging Face
                    radioButtonHuggingFace.Checked = true;
                    break;
            }
            textBoxGeminiModel.Text = Settings1.Default.PreferredGeminiModel;
            textBoxHuggingFaceCreatingModel.Text = Settings1.Default.PreferredHuggingFaceCreatingModel;
            textBoxHuggingFaceEditingModel.Text = Settings1.Default.PreferredHuggingFaceEditingModel;
            if (Settings1.Default.PreferredGeminiModel != DefaultGeminiAIModel)
            {
                buttonResetPreferredGeminiModel.Enabled = true;
            }
            if (Settings1.Default.PreferredHuggingFaceCreatingModel != DefaultHuggingFaceModel)
            {
                buttonResetHuggingFaceCreatingImageModel.Enabled = true;
            }
            if (Settings1.Default.PreferredHuggingFaceEditingModel != DefaultHuggingFaceModel2)
            {
                buttonResetHuggingFaceEditingImageModel.Enabled = true;
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
        }
        private void comboBoxTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings1.Default.PreferredTheme = comboBoxTheme.SelectedIndex;
            Settings1.Default.Save(); // Save the settings to persist the changes
            SetTheme();
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
                textBoxGeminiAPIKey.Text == EncryptionHelper.DecryptString1(Settings1.Default.HashedGeminiAIAPIKey))
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
                Settings1.Default.HashedGeminiAIAPIKey = EncryptionHelper.EncryptString1(textBoxGeminiAPIKey.Text);
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
                Settings1.Default.HashedGeminiAIAPIKey = string.Empty; // Clear the API key by setting it to an empty string  
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

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonGeminiAI.Checked)
            {
                Settings1.Default.PreferredAI = 0; // Google Gemini
                groupBoxGemini.Enabled = true;
                groupBoxHuggingFace.Enabled = false;
            }
            else if (radioButtonHuggingFace.Checked)
            {
                Settings1.Default.PreferredAI = 1; // Hugging Face
                groupBoxGemini.Enabled = false;
                groupBoxHuggingFace.Enabled = true;
            }
            Settings1.Default.Save(); // Save the settings to persist the changes
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            if (textBoxHuggingFaceAPIKey.UseSystemPasswordChar)
            {
                textBoxHuggingFaceAPIKey.UseSystemPasswordChar = false;
                buttonShowHideHuggingFaceAPIKey.ImageIndex = 6;
                buttonShowHideHuggingFaceAPIKey.Text = "Hide";
            }
            else
            {
                textBoxHuggingFaceAPIKey.UseSystemPasswordChar = true;
                buttonShowHideHuggingFaceAPIKey.ImageIndex = 5;
                buttonShowHideHuggingFaceAPIKey.Text = "Show";
            }
        }

        private void textBoxHuggingFaceAPIKey_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxHuggingFaceAPIKey.Text) ||
                textBoxHuggingFaceAPIKey.Text == EncryptionHelper.DecryptString2(Settings1.Default.HashedHuggingFaceAPIKey))
            {
                buttonUpdateHuggingFaceAPIKey.Enabled = false;
            }
            else
            {
                buttonUpdateHuggingFaceAPIKey.Enabled = true;
            }
        }

        private void buttonUpdateHuggingFaceAPIKey_ChangeUICues(object sender, UICuesEventArgs e)
        {
            try
            {
                Settings1.Default.HashedHuggingFaceAPIKey = EncryptionHelper.EncryptString2(textBoxHuggingFaceAPIKey.Text);
                Settings1.Default.Save(); // Save the settings to persist the changes
                buttonUpdateHuggingFaceAPIKey.Enabled = false;
                buttonUpdateHuggingFaceAPIKey.Enabled = true;
                MessageBox.Show("Hugging Face API key is saved successfully.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonResetHuggingFaceAPIKey_Click(object sender, EventArgs e)
        {
            try
            {
                Settings1.Default.HashedHuggingFaceAPIKey = string.Empty; // Clear the API key by setting it to an empty string  
                Settings1.Default.Save(); // Save the settings to persist the changes
                buttonUpdateHuggingFaceAPIKey.Enabled = false;
                buttonUpdateHuggingFaceAPIKey.Enabled = false; // Disable the button after clearing the key  
                textBoxHuggingFaceAPIKey.Clear(); // Clear the text box
                MessageBox.Show("Hugging Face API key has been cleared successfully.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Settings1.Default.PreferredGeminiModel = DefaultGeminiAIModel;
            Settings1.Default.Save();
            textBoxGeminiModel.Text = DefaultGeminiAIModel;
            buttonUpdateGeminiModel.Enabled = false;
            buttonResetPreferredGeminiModel.Enabled = false;
            MessageBox.Show("Google Gemini™ model has been reset to default.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void updateGeminiModel_Click(object sender, EventArgs e)
        {
            Settings1.Default.PreferredGeminiModel = textBoxGeminiModel.Text;
            Settings1.Default.Save();
            MessageBox.Show("Google Gemini™ model has been updated successfully.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (Settings1.Default.PreferredGeminiModel != DefaultGeminiAIModel)
            {
                buttonResetPreferredGeminiModel.Enabled = true;
            }
            else
            {
                buttonResetPreferredGeminiModel.Enabled = false;
            }
        }

        private void textBoxGeminiModel_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxGeminiModel.Text) &&
                textBoxGeminiModel.Text != Settings1.Default.PreferredGeminiModel)
            {
                buttonUpdateGeminiModel.Enabled = true;
            }
            else
            {
                buttonUpdateGeminiModel.Enabled = false;
            }
        }

        private void textBoxHuggingFaceEditingModel_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxHuggingFaceEditingModel.Text) &&
                textBoxHuggingFaceEditingModel.Text != Settings1.Default.PreferredHuggingFaceEditingModel)
            {
                buttonUpdateHuggingFaceEditingImageModel.Enabled = true;
            }
            else
            {
                buttonUpdateHuggingFaceEditingImageModel.Enabled = false;
            }
        }

        private void textBoxHuggingFaceCreatingModel_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxHuggingFaceCreatingModel.Text) &&
                textBoxHuggingFaceCreatingModel.Text != Settings1.Default.PreferredHuggingFaceCreatingModel)
            {
                buttonUpdateHuggingFaceCreatingImageModel.Enabled = true;
            }
            else
            {
                buttonUpdateHuggingFaceCreatingImageModel.Enabled = false;
            }
        }

        private void buttonUpdateHuggingFaceCreatingImageModel_Click(object sender, EventArgs e)
        {
            Settings1.Default.PreferredHuggingFaceCreatingModel = textBoxHuggingFaceCreatingModel.Text;
            Settings1.Default.Save();
            MessageBox.Show("Hugging Face image creation model has been updated successfully.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (Settings1.Default.PreferredHuggingFaceCreatingModel != DefaultHuggingFaceModel)
            {
                buttonResetHuggingFaceCreatingImageModel.Enabled = true;
            }
            else
            {
                buttonResetHuggingFaceCreatingImageModel.Enabled = false;
            }
        }

        private void buttonUpdateHuggingFaceEditingImageModel_Click(object sender, EventArgs e)
        {
            Settings1.Default.PreferredHuggingFaceEditingModel = textBoxHuggingFaceEditingModel.Text;
            Settings1.Default.Save();
            MessageBox.Show("Hugging Face image editing model has been updated successfully.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (Settings1.Default.PreferredHuggingFaceEditingModel != DefaultHuggingFaceModel2)
            {
                buttonResetHuggingFaceEditingImageModel.Enabled = true;
            }
            else
            {
                buttonResetHuggingFaceEditingImageModel.Enabled = false;
            }
        }

        private void buttonResetHuggingFaceCreatingImageModel_Click(object sender, EventArgs e)
        {
            Settings1.Default.PreferredHuggingFaceCreatingModel = DefaultHuggingFaceModel;
            Settings1.Default.Save();
            textBoxGeminiModel.Text = DefaultHuggingFaceModel;
            buttonUpdateHuggingFaceCreatingImageModel.Enabled = false;
            buttonResetHuggingFaceCreatingImageModel.Enabled = false;
            MessageBox.Show("Hugging Face image creation model has been reset to default.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonResetHuggingFaceEditingImageModel_Click(object sender, EventArgs e)
        {
            Settings1.Default.PreferredHuggingFaceEditingModel = DefaultHuggingFaceModel2;
            Settings1.Default.Save();
            textBoxGeminiModel.Text = DefaultHuggingFaceModel2;
            buttonUpdateHuggingFaceEditingImageModel.Enabled = false;
            buttonResetHuggingFaceEditingImageModel.Enabled = false;
            MessageBox.Show("Hugging Face image editing model has been reset to default.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonUpdateHuggingFaceAPIKey_Click(object sender, EventArgs e)
        {
            try
            {
                Settings1.Default.HashedHuggingFaceAPIKey = EncryptionHelper.EncryptString2(textBoxHuggingFaceAPIKey.Text);
                Settings1.Default.Save(); // Save the settings to persist the changes
                buttonUpdateHuggingFaceAPIKey.Enabled = false;
                buttonResetHuggingFaceAPIKey.Enabled = true;
                MessageBox.Show("Hugging Face API key is saved successfully.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
}
