using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Carpathia
{
    public sealed class CoreApiException : Exception
    {
        public string DisplayTitle { get; }

        public CoreApiException(
            string message,
            string displayTitle = "Image Generation Error",
            Exception innerException = null)
            : base(message, innerException)
        {
            DisplayTitle = displayTitle;
        }
    }

    // Keeps the existing class and method name for callers.
    // Requests go to the LibreCanvas backend API
    public static class ImageGenerationCore
    {
        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        private static string BaseUrl =>
            (Environment.GetEnvironmentVariable("LIBRECANVAS_API_BASE_URL")
             ?? "https://librecanvas-backend.wasmer.app").TrimEnd('/');

        public static async Task<Image> GenerateImage(
            string prompt,
            Image image = null,
            int width = 1024,
            int height = 1024,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException(
                    "Prompt cannot be empty.", nameof(prompt));

            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(
                    "Width and height must be positive.");

            string endpoint = image == null
                ? "/api/text-to-image"
                : "/api/image-to-image";

            var payload = new JsonObject
            {
                ["text"] = prompt,
                ["width"] = width,
                ["height"] = height
            };

            if (image != null)
            {
                const int maxSide = 511;

                int sourceLongSide = Math.Max(image.Width, image.Height);
                double scale = sourceLongSide > maxSide
                    ? (double)maxSide / sourceLongSide
                    : 1.0;

                int inputWidth = Math.Max(1, (int)Math.Round(image.Width * scale));
                int inputHeight = Math.Max(1, (int)Math.Round(image.Height * scale));

                using var inputBitmap = new Bitmap(inputWidth, inputHeight);
                using (var graphics = Graphics.FromImage(inputBitmap))
                {
                    graphics.InterpolationMode =
                        System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    graphics.DrawImage(image, 0, 0, inputWidth, inputHeight);
                }

                if (inputWidth >= 512 || inputHeight >= 512)
                    throw new InvalidOperationException(
                        $"Input image is still {inputWidth}×{inputHeight}.");

                using var stream = new MemoryStream();
                inputBitmap.Save(stream, ImageFormat.Png);

                payload["image"] = "data:image/png;base64," +
                                   Convert.ToBase64String(stream.ToArray());

                System.Diagnostics.Debug.WriteLine(
                    $"Sending input image: {inputWidth}×{inputHeight}");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post, BaseUrl + endpoint)
            {
                Content = new StringContent(
                    payload.ToJsonString(),
                    Encoding.UTF8,
                    "application/json")
            };

            try
            {
                using HttpResponseMessage response =
                    await Client.SendAsync(
                        request,
                        cancellationToken).ConfigureAwait(false);

                string responseText =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken).ConfigureAwait(false);

                JsonNode result;
                try
                {
                    result = JsonNode.Parse(responseText);
                }
                catch (Exception ex)
                {
                    throw new CoreApiException(
                        "The image API returned invalid JSON.",
                        innerException: ex);
                }

                if (!response.IsSuccessStatusCode ||
                    result?["success"]?.GetValue<bool>() != true)
                {
                    string error =
                        result?["error"]?.ToString()
                        ?? $"Image API returned HTTP {(int)response.StatusCode}.";

                    throw new CoreApiException(error);
                }

                string imageUrl = result["image_url"]?.ToString();
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    throw new CoreApiException(
                        "The image API did not return an image.");
                }

                byte[] imageBytes;

                if (imageUrl.StartsWith(
                    "data:image/",
                    StringComparison.OrdinalIgnoreCase))
                {
                    int comma = imageUrl.IndexOf(',');
                    if (comma < 0)
                    {
                        throw new CoreApiException(
                            "The image API returned an invalid data URL.");
                    }

                    imageBytes = Convert.FromBase64String(
                        imageUrl.Substring(comma + 1));
                }
                else if (Uri.TryCreate(
                    imageUrl,
                    UriKind.Absolute,
                    out Uri uri) &&
                    uri.Scheme == Uri.UriSchemeHttps)
                {
                    imageBytes = await Client.GetByteArrayAsync(
                        uri,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new CoreApiException(
                        "The image API returned an unsupported image URL.");
                }

                using var imageStream = new MemoryStream(imageBytes);
                using Image decoded = Image.FromStream(imageStream);
                return new Bitmap(decoded);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (CoreApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CoreApiException(
                    ex.Message,
                    innerException: ex);
            }
        }
    }
}