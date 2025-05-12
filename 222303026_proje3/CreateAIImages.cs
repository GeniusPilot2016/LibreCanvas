using GenerativeAI;
using GenerativeAI.Types;
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
        public static async Task<Image[]> GenerateImagesWithSize(string prompt, int width, int height)
        {
            try
            {
                Image[] result = null;
                switch (Settings1.Default.PreferredAI)
                {
                    case 0:
                        string geminiApiKey = EncryptionHelper.DecryptString1(Settings1.Default.HashedGeminiAIAPIKey); // API anahtarınızı buraya ekleyin
                        var googleAi = new GoogleAi(geminiApiKey);

                        // 2. Create the Imagen model instance with your chosen model name.  
                        var imageModel = googleAi.CreateImageModel(Settings1.Default.PreferredGeminiModel);

                        // 3. Generate images by providing a text prompt.  
                        var request = new GenerateImageRequest
                        {
                            Instances = new List<ImageGenerationInstance>
                           {
                               new ImageGenerationInstance
                               {
                                   Prompt = prompt // Move the prompt here, as 'ImageGenerationInstance' likely supports it
                               }
                           },
                            Parameters = new ImageGenerationParameters
                            {
                                AspectRatio = $"{width}:{height}" // Corrected to use AspectRatio for width and height.
                            }
                        };
                        var geminiResponse = await imageModel.GenerateImagesAsync(request);

                        // Assuming the response contains image data in BytesBase64Encoded property  
                        if (geminiResponse.Predictions != null && geminiResponse.Predictions.Count > 0)
                        {
                            // Assuming the first prediction contains the base64-encoded image data  
                            var base64EncodedImage = geminiResponse.Predictions[0].BytesBase64Encoded; // Corrected property name  

                            if (!string.IsNullOrEmpty(base64EncodedImage))
                            {
                                var imageBytes = Convert.FromBase64String(base64EncodedImage);

                                result = new Image[]
                                {
                              Image.FromStream(new MemoryStream(imageBytes))
                                };
                            }
                        }
                        break;
                    case 1:
                        string HuggingFaceApiKey = EncryptionHelper.DecryptString2(Settings1.Default.HashedHuggingFaceAPIKey); // API anahtarınızı buraya ekleyin
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HuggingFaceApiKey); // API anahtarını ekleyin

                        var requestData = new
                        {
                            inputs = prompt,
                            width = width,
                            height = height
                        };

                        var json = JsonConvert.SerializeObject(requestData);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var huggingFaceResponse = await client.PostAsync($"https://api-inference.huggingface.co/models/{Settings1.Default.PreferredHuggingFaceCreatingModel}", content);

                        if (huggingFaceResponse.IsSuccessStatusCode)
                        {
                            var contentType = huggingFaceResponse.Content.Headers.ContentType.MediaType;
                            if (contentType == "image/jpeg" || contentType == "image/png")
                            {
                                var imageBytes = await huggingFaceResponse.Content.ReadAsByteArrayAsync();
                                using (MemoryStream ms = new MemoryStream(imageBytes))
                                {
                                    Image image = Image.FromStream(ms);
                                    return new Image[] { image };
                                }
                            }
                            else if (contentType == "application/json")
                            {
                                var responseJson = await huggingFaceResponse.Content.ReadAsStringAsync();
                                dynamic responseObject = JsonConvert.DeserializeObject(responseJson);
                                // JSON yanıtını işleyin ve görüntü dizisine dönüştürün
                                // Bu kısım, yanıtın gerçek yapısına göre uygulanmalıdır
                                result = new Image[0]; // Yer tutucu, gerçek görüntü dizisi ile değiştirin
                            }
                            else
                            {
                                Debug.WriteLine($"Unexpected content type: {contentType}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Error: {huggingFaceResponse.StatusCode}");
                        }
                        break;
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating images: {ex.Message}");
                return null;
            }
        }
        // Fix for CS0117: 'ImageGenerationInstance' does not contain a definition for 'ImageSource'  
        // The correct property name in the 'ImageGenerationInstance' class is 'Image'.  
        // Update the code to use the correct property.

        public static async Task<Image[]> GenerateImagesFromExistingImage(Image inputImage, string prompt, int width, int height)
        {
            try
            {
                Image[] result = null;
                switch (Settings1.Default.PreferredAI)
                {
                    case 0:
                        string geminiApiKey = EncryptionHelper.DecryptString1(Settings1.Default.HashedGeminiAIAPIKey); // API anahtarınızı buraya ekleyin
                        var googleAi = new GoogleAi(geminiApiKey);

                        // Convert the input image to a base64 string
                        string base64Image;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            inputImage.Save(ms, inputImage.RawFormat);
                            base64Image = Convert.ToBase64String(ms.ToArray());
                        }

                        // Create the Imagen model instance with your chosen model name
                        var imageModel = googleAi.CreateImageModel(Settings1.Default.PreferredGeminiModel);

                        var request = new GenerateImageRequest
                        {
                            Instances = new List<ImageGenerationInstance>
                           {
                               new ImageGenerationInstance
                               {
                                   Image = new ImageSource // Corrected property name
                                   {
                                       BytesBase64Encoded = base64Image
                                   },
                                   Prompt = prompt // Move the prompt here, as 'ImageGenerationInstance' likely supports it
                               }
                           },
                            Parameters = new ImageGenerationParameters
                            {
                                AspectRatio = $"{width}:{height}" // Corrected to use AspectRatio for width and height.
                            }
                        };
                        var geminiResponse = await imageModel.GenerateImagesAsync(request);

                        // Assuming the response contains image data in BytesBase64Encoded property
                        if (geminiResponse.Predictions != null && geminiResponse.Predictions.Count > 0)
                        {
                            // Assuming the first prediction contains the base64-encoded image data
                            var base64EncodedImage = geminiResponse.Predictions[0].BytesBase64Encoded;

                            if (!string.IsNullOrEmpty(base64EncodedImage))
                            {
                                var imageBytes = Convert.FromBase64String(base64EncodedImage);

                                result = new Image[]
                                {
                                   Image.FromStream(new MemoryStream(imageBytes))
                                };
                            }
                        }
                        break;
                    case 1:
                        string HuggingFaceApiKey = EncryptionHelper.DecryptString2(Settings1.Default.HashedHuggingFaceAPIKey); // API anahtarınızı buraya ekleyin
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HuggingFaceApiKey); // API anahtarını ekleyin

                        var requestData = new
                        {
                            image = inputImage,
                            inputs = prompt,
                            width = width,
                            height = height
                        };

                        var json = JsonConvert.SerializeObject(requestData);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var huggingFaceResponse = await client.PostAsync($"https://api-inference.huggingface.co/models/{Settings1.Default.PreferredHuggingFaceEditingModel}", content);

                        if (huggingFaceResponse.IsSuccessStatusCode)
                        {
                            var contentType = huggingFaceResponse.Content.Headers.ContentType.MediaType;
                            if (contentType == "image/jpeg" || contentType == "image/png")
                            {
                                var imageBytes = await huggingFaceResponse.Content.ReadAsByteArrayAsync();
                                using (MemoryStream ms = new MemoryStream(imageBytes))
                                {
                                    Image image = Image.FromStream(ms);
                                    return new Image[] { image };
                                }
                            }
                            else if (contentType == "application/json")
                            {
                                var responseJson = await huggingFaceResponse.Content.ReadAsStringAsync();
                                dynamic responseObject = JsonConvert.DeserializeObject(responseJson);
                                // JSON yanıtını işleyin ve görüntü dizisine dönüştürün
                                // Bu kısım, yanıtın gerçek yapısına göre uygulanmalıdır
                                result = new Image[0]; // Yer tutucu, gerçek görüntü dizisi ile değiştirin
                            }
                            else
                            {
                                Debug.WriteLine($"Unexpected content type: {contentType}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Error: {huggingFaceResponse.StatusCode}");
                        }
                        break;
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating images: {ex.Message}");
                return null;
            }
        }
    }
}
