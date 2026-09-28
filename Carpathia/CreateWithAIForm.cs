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

namespace Carpathia
{
    public partial class CreateWithAIForm : Form
    {
        // The feature to create images with AI that I struggled to implement because of the lack of free text-to-image AI models and I had to wait for Google to release their Gemini Image models.
        Image image;
        CancellationTokenSource cts = new CancellationTokenSource();
        bool imageIsCompleted = false; // To track if the image generation is completed
        String filePath;
        public CreateWithAIForm()
        {
            InitializeComponent();
            SystemThemeUtility.RegisterForm(this);
            SetTheme();
            SetFonts();
            numericUpDownWidth.Value = Settings1.Default.DefaultAIGeneratedImageSize.Width;
            numericUpDownHeight.Value = Settings1.Default.DefaultAIGeneratedImageSize.Height;
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
            TitleBarHelper.ApplyCustomTitleBar(this, false);
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
            TitleBarHelper.ApplyCustomTitleBar(this, true);
        }
        Image generatedImage = null; // To hold the generated image from the AI
        private async void button1_Click(object sender, EventArgs e)
        {
            Bitmap bitmap = null;

            try
            {
                setVisibilityOfProgressBarAndSomeControls(true);

                string prompt = textBoxPrompt.Text;
                int width = (int)numericUpDownWidth.Value;
                int height = (int)numericUpDownHeight.Value;

                if (!string.IsNullOrEmpty(filePath))
                {
                    bitmap = LoadInputImage(filePath);
                }

                generatedImage = await ImageGenerationCore.GenerateImage(
                    prompt,
                    bitmap,
                    width,
                    height,
                    cts.Token
                );

                if (generatedImage != null)
                {
                    image = generatedImage;
                    imageIsCompleted = true;
                }
            }
            catch (TaskCanceledException)
            {
                // Request was cancelled.
            }
            catch (OperationCanceledException)
            {
                // User closed/cancelled the generation.
            }
            catch (CoreApiException ex)
            {
                Logger.Log(
                    ex.Message,
                    Logger.LogTypes.Error
                );

                MessageForm.Show(
                    this,
                    ex.Message,
                    ex.DisplayTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch (Exception ex)
            {
                // Do NOT pass HuggingFace errors to PollinationsAI.
                Logger.Log(
                    "Hugging Face image generation error:\r\n" + ex,
                    Logger.LogTypes.Error
                );

                MessageForm.Show(
                    this,
                    "An error occurred while generating the image.\r\n\r\n" + ex.Message,
                    "Image Generation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                bitmap?.Dispose();

                setVisibilityOfProgressBarAndSomeControls(false);

                if (!IsDisposed)
                {
                    Close();
                }
            }
        }
        private static Bitmap LoadInputImage(string path)
        {
            using var original = new Bitmap(path);

            if (original.Width < 512 && original.Height < 512)
                return new Bitmap(original);

            int width, height;

            if (original.Width >= original.Height)
            {
                width = 512;
                height = Math.Max(1,
                    (int)Math.Round(original.Height * (510.0 / original.Width)));
            }
            else
            {
                height = 512;
                width = Math.Max(1,
                    (int)Math.Round(original.Width * (510.0 / original.Height)));
            }

            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode =
                    System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(original, 0, 0, width, height);
            }

            return resized;
        }
        private void setVisibilityOfProgressBarAndSomeControls(bool isVisible)
        {
            labelImageCreating.Visible = isVisible;
            progressBarImageCreating.Visible = isVisible;
            labelPrompt.Enabled = !isVisible;
            textBoxPrompt.Enabled = !isVisible;
            buttonCreate.Enabled = !isVisible;
            labelWidth.Enabled = !isVisible;
            labelHeight.Enabled = !isVisible;
            buttonUploadImage.Enabled = !isVisible;
            labelUploadedImage.Enabled = !isVisible;
            numericUpDownWidth.Enabled = !isVisible;
            numericUpDownHeight.Enabled = !isVisible;
        }
        private Image ResizeImage(Image image, int width, int height)
        {
            // Orijinal ve hedef görüntülerin en-boy oranlarını hesapla
            float sourceRatio = (float)image.Width / image.Height;
            float targetRatio = (float)width / height;

            // Kaynak görüntünün boyutlarını ve konumunu hesapla
            int sourceX = 0, sourceY = 0;
            int sourceWidth = image.Width, sourceHeight = image.Height;

            // En-boy oranlarını karşılaştır ve uygun şekilde kırpma yap
            if (sourceRatio > targetRatio) // Kaynak görüntü daha geniş, yatay kırpma yap
            {
                sourceWidth = (int)(image.Height * targetRatio);
                sourceX = (image.Width - sourceWidth) / 2; // Yatay merkezleme
            }
            else if (sourceRatio < targetRatio) // Kaynak görüntü daha uzun, dikey kırpma yap
            {
                sourceHeight = (int)(image.Width / targetRatio);
                sourceY = (image.Height - sourceHeight) / 2; // Dikey merkezleme
            }

            // Hedef görüntüyü oluştur
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
                    // Kaynak görüntüyü, hesaplanmış kaynak koordinatlarını kullanarak hedef görüntüye çiz
                    graphics.DrawImage(image, destRect, sourceX, sourceY, sourceWidth, sourceHeight, GraphicsUnit.Pixel, wrapMode);
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
            //HandleStatus();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            fileBrowser.Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap Image|*.bmp|GIF Image|*.gif|All Files|*.*";
            if (fileBrowser.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (IsImage(fileBrowser.FileName))
                    {
                        filePath = fileBrowser.FileName;
                        labelUploadedImage.Text = Path.GetFileName(filePath);
                        Logger.Log("Image uploaded: " + filePath, Logger.LogTypes.Info);
                    }
                    else
                    {
                        Logger.Log("Selected file is not a valid image.", Logger.LogTypes.Warning);
                        MessageForm.Show("Selected file is not a valid image. Please select an image file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    labelUploadedImage.Text = "No image is uploaded";
                    filePath = null;
                    Logger.Log("Error loading image: " + ex.Message, Logger.LogTypes.Error);
                    MessageForm.Show("Error loading image. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool IsImage(string filePath)
        {
            try
            {
                using (var img = Image.FromFile(filePath))
                {
                    return true;
                }
            }
            catch (OutOfMemoryException)
            {
                // The file does not have a valid image format or GDI+ does not support the pixel format of the file.
                return false;
            }
            catch (FileNotFoundException)
            {
                // The file does not exist.
                return false;
            }
        }
        enum StatusMode
        {
            CheckResult,
            ExceptionText
        }
        
        private void CreateWithAIForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!imageIsCompleted)
            {
                cts?.Cancel(); // Cancel any ongoing tasks if the form is closed
                image = null;
            }
        }
        public Image GetGeneratedImage()
        {
            return image;
        }
    }
}