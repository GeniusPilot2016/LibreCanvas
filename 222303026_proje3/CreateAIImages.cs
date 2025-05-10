using GenerativeAI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;

namespace _222303026_proje3
{
    public static class CreateAIImages
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string apiKey = Environment.GetEnvironmentVariable("APIKey"); // API anahtarınızı buraya ekleyin
        public static async Task<Image[]> GenerateImagesWithSize(string prompt, int width, int height)
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

            /*Image[] result = null;

            var googleAi = new GoogleAi(apiKey);

            // 2. Create the Imagen model instance with your chosen model name.  
            var imageModel = googleAi.CreateImageModel("gemini-2.0-flash-exp-image-generation");

            // 3. Generate images by providing a text prompt.  
            var response = await imageModel.GenerateImagesAsync(prompt);

            // Assuming the response contains image data in BytesBase64Encoded property  
            if (response.Predictions != null && response.Predictions.Count > 0)
                {
                    // Assuming the first prediction contains the base64-encoded image data  
                    var base64EncodedImage = response.Predictions[0].BytesBase64Encoded; // Corrected property name  

                    if (!string.IsNullOrEmpty(base64EncodedImage))
                    {
                        var imageBytes = Convert.FromBase64String(base64EncodedImage);

                        result = new Image[]
                        {
                           Image.FromStream(new MemoryStream(imageBytes))
                        };
                    }
                }
            return result;*/
            }
        /*public static async Task ListAvailableModels()
        {
            var googleAi = new GoogleAi(apiKey); // Ensure your API key is correctly set
            var response = await googleAi.ListModelsAsync(); // Call the ListModels API

            if (response.Models != null)
            {
                foreach (var model in response.Models) // Access the Models property of ListModelsResponse
                {
                    Debug.WriteLine($"Model Name: {model.Name}, Description: {model.Description}, Methods: {model.SupportedGenerationMethods}");
                }
            }
            else
            {
                Debug.WriteLine("No models available.");
            }
        }*/

        public static async Task<Image[]> GenerateImagesFromExistingImage(Image originalImage, string prompt, int width, int height)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey); // API anahtarını ekleyin

            var requestData = new
            {
                inputs = prompt,
                image = originalImage,
                width = width,
                height = height
            };

            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api-inference.huggingface.co/models/mit-han-lab/svdq-int4-flux.1-fill-dev", content);

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
    }
}
