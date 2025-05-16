using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _222303026_proje3
{
    public partial class CreateWithAIForm : Form
    {
        Image image;
        public CreateWithAIForm()
        {
            InitializeComponent();
            SetTheme();
            //CreateAIImages.ListAvailableModels(); // Call the ListModels API
            numericUpDownWidth.Value = Settings1.Default.DefaultAIGeneratedImageSize.Width;
            numericUpDownHeight.Value = Settings1.Default.DefaultAIGeneratedImageSize.Height;
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
            foreach (Control control in Controls)
            {
                if (control is Button)
                {
                    control.BackColor = Color.Transparent;
                    control.ForeColor = SystemColors.ControlText;
                }
                if (control is TextBox)
                {
                    control.BackColor = SystemColors.Window;
                    control.ForeColor = SystemColors.WindowText;
                }
                if (control is NumericUpDown)
                {
                    control.BackColor = SystemColors.Window;
                    control.ForeColor = SystemColors.WindowText;
                }
            }
        }
        private void DarkTheme()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            foreach (Control control in Controls)
            {
                if (control is Button)
                {
                    control.BackColor = Color.FromArgb(32, 32, 32);
                    control.ForeColor = Color.White;
                }
                if (control is TextBox)
                {
                    control.BackColor = Color.Black;
                    control.ForeColor = Color.White;
                }
                if (control is NumericUpDown)
                {
                    control.BackColor = Color.Black;
                    control.ForeColor = Color.White;
                }
            }
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            setVisibilityOfProgressBar(true);
            string prompt = textBoxPrompt.Text;
            int width = (int)numericUpDownWidth.Value;
            int height = (int)numericUpDownHeight.Value;
            Image generatedImage = await CreateAIImages.CreateImage(prompt);

            if (generatedImage != null)
            {
                // İlk resmi yeniden boyutlandırın
                Application.DoEvents();
                setVisibilityOfProgressBar(false);
                image = ResizeImage(generatedImage, width, height);
                this.Close();
            }
            else
            {
                setVisibilityOfProgressBar(false);
                MessageBox.Show("Image generation failed.");
            }
        }
        private void setVisibilityOfProgressBar(bool isVisible)
        {
            if (isVisible)
            {
                labelImageCreating.Visible = true;
                progressBarImageCreating.Visible = true;
            }
            else
            {
                labelImageCreating.Visible = false;
                progressBarImageCreating.Visible = false;
            }
        }
        private Image ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                using (var wrapMode = new System.Drawing.Imaging.ImageAttributes())
                {
                    wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private void textBoxPrompt_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxPrompt.Text))
            {
                buttonCreate.Enabled = true;
            }
            else
            {
                buttonCreate.Enabled = false;
            }
        }

        private void CreateWithAIForm_Load(object sender, EventArgs e)
        {

        }
    }
}