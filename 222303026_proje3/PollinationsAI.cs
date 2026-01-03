using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace _222303026_proje3
{
    public static class PollinationsAI
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<Image> CreateImageAsync(string prompt, int width, int height, Image referenceImage = null, CancellationToken ct = default)
        {
            try
            {
                // Pollinations.AI uses a simple GET request with URL parameters
                Random rnd = new Random();
                int seed = rnd.Next(1, 999999999); // Random seed for variability
                string referenceImageUrl = referenceImage != null ? await CreateTemporaryImageURL(referenceImage) : null;
                string encodedPrompt = HttpUtility.UrlEncode(prompt);
                string url = referenceImage != null ? 
                    $"https://image.pollinations.ai/prompt/{encodedPrompt}?width={width}&height={height}&nologo=true&seed={seed}&model=flux" :
                    $"https://image.pollinations.ai/prompt/{encodedPrompt}?width={width}&height={height}&nologo=true&seed={seed}&model=flux&image={referenceImageUrl}";

                Logger.Log($"Generating image with Pollinations.AI: {prompt}", Logger.LogTypes.Info);

                var response = await httpClient.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var image = Image.FromStream(stream);
                    Logger.Log("Image generated successfully with Pollinations.AI", Logger.LogTypes.Info);
                    return image;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Pollinations.AI error: {ex.Message}", Logger.LogTypes.Error);
                return null;
            }
        }
        private static async Task<string> CreateTemporaryImageURL(Image image)
        {
            // TODO: Replace with your actual imgbb.com API key
            const string apiKey = "YOUR_IMGBB_API_KEY";

            try
            {
                // Convert Image to byte array
                using (var memoryStream = new MemoryStream())
                {
                    image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    var imageBytes = memoryStream.ToArray();
                    var imageBase64 = Convert.ToBase64String(imageBytes);

                    // Prepare request content for imgbb API
                    var requestContent = new MultipartFormDataContent
                        {
                            { new StringContent(apiKey), "key" },
                            { new StringContent(imageBase64), "image" },
                            { new StringContent("temp_image_" + Guid.NewGuid()), "name" }
                        };

                    // Upload to imgbb
                    var response = await httpClient.PostAsync("https://api.imgbb.com/1/upload", requestContent);
                    response.EnsureSuccessStatusCode();

                    // Parse response to get the URL
                    var responseString = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JObject.Parse(responseString);
                    var imageUrl = jsonResponse["data"]?["url"]?.ToString();

                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        Logger.Log("imgbb.com upload error: Could not parse image URL from response.", Logger.LogTypes.Error);
                        return null;
                    }

                    Logger.Log($"Image uploaded to temporary URL: {imageUrl}", Logger.LogTypes.Info);
                    return imageUrl;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"imgbb.com upload error: {ex.Message}", Logger.LogTypes.Error);
                return null;
            }
        }
    }
}