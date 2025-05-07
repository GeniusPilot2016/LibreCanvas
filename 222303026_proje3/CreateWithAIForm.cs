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
        private static readonly HttpClient client = new HttpClient();
        private readonly string apiKey = Environment.GetEnvironmentVariable("APIKey"); // API anahtarınızı buraya ekleyin
        Image image;
        public CreateWithAIForm()
        {
            InitializeComponent();
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

        private async Task<Image[]> GenerateImages(string prompt, int width, int height)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey); // API anahtarını ekleyin

            var requestData = new
            {
                inputs = prompt,
                width = width,
                height = height
            };

            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api-inference.huggingface.co/models/black-forest-labs/FLUX.1-dev", content);

            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType.MediaType;
                if (contentType == "image/jpeg" || contentType == "image/png")
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        Image image = Image.FromStream(ms);
                        return new Image[] { image };
                    }
                }
                else if (contentType == "application/json")
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    dynamic responseObject = JsonConvert.DeserializeObject(responseJson);
                    // JSON yanıtını işleyin ve görüntü dizisine dönüştürün
                    // Bu kısım, yanıtın gerçek yapısına göre uygulanmalıdır
                    return new Image[0]; // Yer tutucu, gerçek görüntü dizisi ile değiştirin
                }
                else
                {
                    Debug.WriteLine($"Unexpected content type: {contentType}");
                    return null;
                }
            }
            else
            {
                Debug.WriteLine($"Error: {response.StatusCode}");
                return null;
            }
            /*
var googleAi = new GoogleAi(apiKey);

// 2. Create the Imagen model instance with your chosen model name.  
var imageModel = googleAi.CreateImageModel("gemini-2.0-flash-exp-image-generation");

// 3. Generate images by providing a text prompt.  
var response = await imageModel.GenerateImagesAsync(textBox1.Text);

// Assuming the response contains image data in BytesBase64Encoded property  
if (response.Predictions != null && response.Predictions.Count > 0)
{
    var imageBytes = Convert.FromBase64String(response.BytesBase64Encoded); // Corrected property name  

    return new Image[]
    {
       Image.FromStream(new MemoryStream(imageBytes))
    };
}*/
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            EnableDisableControls(false);
            string prompt = textBox1.Text;
            int width = (int)numericUpDownWidth.Value;
            int height = (int)numericUpDownHeight.Value;
            Image[] generatedImages = await GenerateImages(prompt, width, height);

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