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
            //CreateAIImages.ListAvailableModels(); // Call the ListModels API
            numericUpDownWidth.Value = Settings1.Default.DefaultAIGeneratedImageSize.Width;
            numericUpDownHeight.Value = Settings1.Default.DefaultAIGeneratedImageSize.Height;
        }
        private void EnableDisableControls(bool enabled)
        {
            foreach (Control control in Controls)
            {
                control.Enabled = enabled;
            }
        }

        
        private async void button1_Click(object sender, EventArgs e)
        {
            EnableDisableControls(false);
            string prompt = textBox1.Text;
            int width = (int)numericUpDownWidth.Value;
            int height = (int)numericUpDownHeight.Value;
            Image[] generatedImages = await CreateAIImages.GenerateImagesWithSize(prompt, width, height);

            if (generatedImages != null && generatedImages.Length > 0)
            {
                // İlk resmi yeniden boyutlandırın
                image = ResizeImage(generatedImages[0], width, height);
                EnableDisableControls(true);
                this.Close();
            }
            else
            {
                MessageBox.Show("Image generation failed.");
                EnableDisableControls(true);
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
    }
}