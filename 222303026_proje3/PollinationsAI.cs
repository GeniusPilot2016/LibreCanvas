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
        
        public static async Task<Image> CreateImageAsync(string prompt, int width, int height, CancellationToken ct = default)
        {
            try
            {
                // Pollinations.AI uses a simple GET request with URL parameters
                Random rnd = new Random();
                int seed = rnd.Next(1, 999999999); // Random seed for variability
                string encodedPrompt = HttpUtility.UrlEncode(prompt);
                string url = $"https://image.pollinations.ai/prompt/{encodedPrompt}?width={width}&height={height}&nologo=true&seed={seed}&model=flux";

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
    }
}