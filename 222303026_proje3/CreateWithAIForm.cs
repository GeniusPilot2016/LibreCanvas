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
        }

        private async Task<Image[]> GenerateImages(string prompt)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey); // API anahtarını ekleyin

            var requestData = new
            {
                inputs = prompt,
            };

            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api-inference.huggingface.co/models/Keltezaa/Dall_E3_meet_FLUX_v0.1", content);

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
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            string prompt = textBox1.Text;
            Image[] generatedImages = await GenerateImages(prompt);

            if (generatedImages != null && generatedImages.Length > 0)
            {
                // İlk resmi PictureBox'ta gösterin (veya istediğiniz gibi işleyin)
                image = generatedImages[0];
                this.Close();
            }
            else
            {
                MessageBox.Show("Image generation failed.");   
            }
        }
    }
}