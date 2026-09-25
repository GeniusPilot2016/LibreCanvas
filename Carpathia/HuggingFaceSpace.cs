using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Carpathia
{
    public static class HuggingFaceSpace
    {
        private const string BaseHost =
            "https://rioshiina-imagegen.hf.space";

        private const string BaseUrl =
            BaseHost + "/gradio_api";

        private const string Model =
            "black-forest-labs/FLUX.2-dev";

        // Generation is done at a controlled size first.
        // The final image is then resized/cropped to the exact
        // requested pixel dimensions.
        private const int GenerationLongSide = 1024;

        private static readonly HttpClient client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        // ============================================================
        // PUBLIC METHOD
        // ============================================================

        // DO NOT RENAME THIS METHOD
        public static async Task<Image> GenerateImage(
            string prompt,
            Image image = null,
            int width = 1024,
            int height = 1024,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException(
                    "Prompt cannot be empty.",
                    nameof(prompt));
            }

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    "Width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    "Height must be greater than zero.");
            }

            try
            {
                // ----------------------------------------------------
                // 1. Select nearest supported aspect ratio
                // ----------------------------------------------------

                AspectRatioInfo aspectRatio =
                    GetNearestAspectRatio(width, height);

                // ----------------------------------------------------
                // 2. Calculate generation dimensions
                // ----------------------------------------------------

                GenerationSize generationSize =
                    GetGenerationSize(aspectRatio);

                // ----------------------------------------------------
                // 3. Upload input image if supplied
                // ----------------------------------------------------

                string uploadedFilePath = null;

                if (image != null)
                {
                    uploadedFilePath =
                        await UploadImageAsync(
                            image,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                // ----------------------------------------------------
                // 4. Prepare
                // ----------------------------------------------------

                PrepareResult prepareResult =
                    await PrepareAsync(
                        prompt,
                        uploadedFilePath,
                        generationSize.Width,
                        generationSize.Height,
                        aspectRatio.Name,
                        cancellationToken)
                    .ConfigureAwait(false);

                // ----------------------------------------------------
                // 5. Start generation
                // ----------------------------------------------------

                string eventId =
                    await StartGenerationAsync(
                        prepareResult.PreparedPrompt,
                        uploadedFilePath,
                        generationSize.Width,
                        generationSize.Height,
                        prepareResult.Seed,
                        cancellationToken)
                    .ConfigureAwait(false);

                // ----------------------------------------------------
                // 6. Wait for generation
                // ----------------------------------------------------

                string generatedFile =
                    await WaitForGenerationAsync(
                        eventId,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(generatedFile))
                {
                    throw new HuggingFaceApiException(
                        "Generation finished but no image was returned.",
                        "Hugging Face API");
                }

                // ----------------------------------------------------
                // 7. Download generated image
                // ----------------------------------------------------

                using Image generatedImage =
                    await DownloadImageAsync(
                        generatedFile,
                        cancellationToken)
                        .ConfigureAwait(false);

                // ----------------------------------------------------
                // 8. Resize / upscale / downscale / crop
                // ----------------------------------------------------

                return ResizeAndCropToExactSize(
                    generatedImage,
                    width,
                    height);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HuggingFaceApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HuggingFaceApiException(
                    ex.Message,
                    "Hugging Face API",
                    ex);
            }
        }

        // ============================================================
        // AUTHORIZATION
        // ============================================================

        private static void AddAuthorizationHeader(
            HttpRequestMessage request)
        {
            string base64Key =
                Settings1.Default.HuggingFaceAPIKeyBase64;

            // No API key configured:
            // simply don't add Authorization header.
            if (string.IsNullOrWhiteSpace(base64Key))
                return;

            string token;

            try
            {
                EncryptionHelper encryptionHelper =
                    new EncryptionHelper();

                token =
                    encryptionHelper.DecryptStringFromBase64(
                        base64Key);
            }
            catch (Exception ex)
            {
                throw new HuggingFaceApiException(
                    "Could not decrypt Hugging Face API key.",
                    "Hugging Face Authentication",
                    ex);
            }

            // Don't send an empty Authorization header.
            if (string.IsNullOrWhiteSpace(token))
                return;

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }

        // ============================================================
        // UPLOAD
        // ============================================================

        private static async Task<string> UploadImageAsync(
            Image image,
            CancellationToken cancellationToken)
        {
            using var memoryStream =
                new MemoryStream();

            image.Save(
                memoryStream,
                ImageFormat.Png);

            memoryStream.Position = 0;

            using var content =
                new MultipartFormDataContent();

            using var imageContent =
                new StreamContent(memoryStream);

            imageContent.Headers.ContentType =
                new MediaTypeHeaderValue("image/png");

            content.Add(
                imageContent,
                "files",
                "input.png");

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    BaseUrl + "/upload");

            AddAuthorizationHeader(request);

            request.Content =
                content;

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    cancellationToken)
                    .ConfigureAwait(false);

            string responseText =
                await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string message =
                    ExtractErrorMessage(responseText);

                throw new HuggingFaceApiException(
                    message,
                    $"Hugging Face Upload ({(int)response.StatusCode})");
            }

            try
            {
                JsonNode json =
                    JsonNode.Parse(responseText);

                string path =
                    json?[0]?.GetValue<string>();

                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new HuggingFaceApiException(
                        "Upload succeeded but no file path was returned.",
                        "Hugging Face Upload");
                }

                return path;
            }
            catch (HuggingFaceApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HuggingFaceApiException(
                    "Could not parse upload response.\n\n" +
                    responseText,
                    "Hugging Face Upload",
                    ex);
            }
        }

        // ============================================================
        // INPUT IMAGE
        // ============================================================

        private static JsonArray CreateInputImages(
            string uploadedFilePath)
        {
            if (string.IsNullOrWhiteSpace(uploadedFilePath))
                return null;

            return new JsonArray
            {
                new JsonObject
                {
                    ["image"] = new JsonObject
                    {
                        ["path"] = uploadedFilePath,

                        ["meta"] = new JsonObject
                        {
                            ["_type"] =
                                "gradio.FileData"
                        }
                    },

                    ["caption"] = null
                }
            };
        }

        // ============================================================
        // PREPARE
        // ============================================================

        private sealed class PrepareResult
        {
            public string PreparedPrompt { get; set; }

            public int Seed { get; set; }

            public string AspectRatio { get; set; }
        }

        private static async Task<PrepareResult> PrepareAsync(
            string prompt,
            string uploadedFilePath,
            int width,
            int height,
            string aspectRatio,
            CancellationToken cancellationToken)
        {
            // The new API /call/v2/run does not require
            // the old /prepare endpoint.

            await Task.CompletedTask
                .ConfigureAwait(false);

            return new PrepareResult
            {
                PreparedPrompt = prompt,
                Seed = -1,
                AspectRatio = aspectRatio
            };
        }

        // ============================================================
        // START GENERATION
        // ============================================================

        private static async Task<string> StartGenerationAsync(
    string prompt,
    string uploadedFilePath,
    int width,
    int height,
    int seed,
    CancellationToken cancellationToken)
        {
            string requestUrl =
                BaseUrl + "/call/v2/run";

            bool hasInputImage =
                !string.IsNullOrWhiteSpace(uploadedFilePath);

            // ============================================================
            // BASE PARAMETERS
            // ============================================================

            var parameters =
                new JsonObject
                {
                    // Reference editing in this Space is implemented as
                    // a chain attached to txt2img.
                    ["task_type"] = "txt2img",

                    ["model"] = Model,

                    ["prompt"] = prompt,

                    ["width"] = width,

                    ["height"] = height
                };

            // ============================================================
            // SEED
            // ============================================================

            if (seed >= 0)
            {
                parameters["seed"] = seed;
            }

            // ============================================================
            // REFERENCE IMAGE
            // ============================================================
            //
            // THIS IS THE IMPORTANT PART.
            //
            // Do NOT use:
            //
            //   input_image
            //   img2img_image
            //   reference_latent_data
            //
            // directly here.
            //
            // The high-level API expects a "chains" array and then
            // converts the reference_latent chain internally.
            //
            // ============================================================

            if (hasInputImage)
            {
                var chains =
                    new JsonArray();

                chains.Add(
                    new JsonObject
                    {
                        ["type"] =
                            "reference_latent",

                        // Send the uploaded file path as a STRING.
                        //
                        // The server-side _parse_image_param() accepts
                        // a file path and loads the image itself.
                        ["image"] =
                            uploadedFilePath
                    });

                parameters["chains"] =
                    chains;
            }

            // ============================================================
            // WRAP json_params
            // ============================================================

            var body =
                new JsonObject
                {
                    ["json_params"] =
                        parameters.ToJsonString()
                };

            string debugJson =
                body.ToJsonString();

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    requestUrl);

            AddAuthorizationHeader(request);

            request.Content =
                new StringContent(
                    debugJson,
                    Encoding.UTF8,
                    "application/json");

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    cancellationToken)
                    .ConfigureAwait(false);

            string responseText =
                await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string message =
                    ExtractErrorMessage(responseText);

                throw new HuggingFaceApiException(
                    message,
                    $"Generation Error ({(int)response.StatusCode})");
            }

            string eventId =
                ExtractEventId(responseText);

            if (string.IsNullOrWhiteSpace(eventId))
            {
                throw new HuggingFaceApiException(
                    "Hugging Face did not return an event_id.\n\n" +
                    responseText,
                    "Hugging Face API");
            }

            return eventId;
        }

        // ============================================================
        // WAIT FOR GENERATION
        // ============================================================

        private static async Task<string> WaitForGenerationAsync(
            string eventId,
            CancellationToken cancellationToken)
        {
            string responseText =
                await GetSseResultAsync(
                    "/call/run/",
                    eventId,
                    cancellationToken)
                    .ConfigureAwait(false);

            JsonNode result =
                ParseJsonFromSse(responseText);

            if (result == null)
            {
                throw new HuggingFaceApiException(
                    "RUN returned no JSON result.\n\n" +
                    responseText,
                    "Hugging Face API");
            }

            string status =
                FindStringProperty(
                    result,
                    "status");

            // --------------------------------------------------------
            // FAILED
            // --------------------------------------------------------

            if (string.Equals(
                status,
                "failed",
                StringComparison.OrdinalIgnoreCase))
            {
                throw CreateApiExceptionFromJson(
                    result,
                    "Hugging Face API");
            }

            // --------------------------------------------------------
            // Direct image result
            // --------------------------------------------------------

            string directFile =
                ExtractGeneratedFile(result);

            if (!string.IsNullOrWhiteSpace(directFile))
                return directFile;

            // --------------------------------------------------------
            // Task ID
            // --------------------------------------------------------

            string taskId =
                FindStringProperty(
                    result,
                    "task_id");

            if (string.IsNullOrWhiteSpace(taskId))
            {
                throw new HuggingFaceApiException(
                    "RUN completed but returned neither an image " +
                    "nor a task_id.\n\n" +
                    responseText,
                    "Hugging Face API");
            }

            return await WaitForTaskStatusAsync(
                taskId,
                cancellationToken)
                .ConfigureAwait(false);
        }

        // ============================================================
        // TASK STATUS
        // ============================================================

        private static async Task<string> WaitForTaskStatusAsync(
            string taskId,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                string eventId =
                    await PostSimpleCallAsync(
                        "/call/v2/get_task_status",
                        new JsonObject
                        {
                            ["task_id"] =
                                taskId
                        },
                        cancellationToken)
                        .ConfigureAwait(false);

                string responseText =
                    await GetSseResultAsync(
                        "/call/get_task_status/",
                        eventId,
                        cancellationToken)
                        .ConfigureAwait(false);

                JsonNode result =
                    ParseJsonFromSse(responseText);

                if (result == null)
                {
                    throw new HuggingFaceApiException(
                        "Task status returned invalid JSON.\n\n" +
                        responseText,
                        "Hugging Face API");
                }

                string status =
                    FindStringProperty(
                        result,
                        "status");

                // ----------------------------------------------------
                // FAILED
                // ----------------------------------------------------

                if (string.Equals(
                    status,
                    "failed",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw CreateApiExceptionFromJson(
                        result,
                        "Hugging Face API");
                }

                // ----------------------------------------------------
                // IMAGE
                // ----------------------------------------------------

                string generatedFile =
                    ExtractGeneratedFile(result);

                if (!string.IsNullOrWhiteSpace(generatedFile))
                    return generatedFile;

                // ----------------------------------------------------
                // COMPLETED BUT NO IMAGE
                // ----------------------------------------------------

                if (string.Equals(
                    status,
                    "completed",
                    StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                    status,
                    "success",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new HuggingFaceApiException(
                        "Task completed but no image was returned.\n\n" +
                        responseText,
                        "Hugging Face API");
                }

                await Task.Delay(
                    1000,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // ============================================================
        // GENERIC POST
        // ============================================================

        private static async Task<string> PostSimpleCallAsync(
            string endpoint,
            JsonObject body,
            CancellationToken cancellationToken)
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    BaseUrl + endpoint);

            AddAuthorizationHeader(request);

            request.Content =
                new StringContent(
                    body?.ToJsonString() ?? "{}",
                    Encoding.UTF8,
                    "application/json");

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    cancellationToken)
                    .ConfigureAwait(false);

            string responseText =
                await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string message =
                    ExtractErrorMessage(responseText);

                throw new HuggingFaceApiException(
                    message,
                    $"Hugging Face API ({(int)response.StatusCode})");
            }

            string eventId =
                ExtractEventId(responseText);

            if (string.IsNullOrWhiteSpace(eventId))
            {
                throw new HuggingFaceApiException(
                    "No event_id returned.\n\n" +
                    responseText,
                    "Hugging Face API");
            }

            return eventId;
        }

        // ============================================================
        // GET SSE
        // ============================================================

        private static async Task<string> GetSseResultAsync(
            string endpoint,
            string eventId,
            CancellationToken cancellationToken)
        {
            string url =
                BaseUrl +
                endpoint +
                Uri.EscapeDataString(eventId);

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url);

            AddAuthorizationHeader(request);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "text/event-stream"));

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                    .ConfigureAwait(false);

            string responseText =
                await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string message =
                    ExtractErrorMessage(responseText);

                throw new HuggingFaceApiException(
                    message,
                    $"Hugging Face API ({(int)response.StatusCode})");
            }

            return responseText;
        }

        // ============================================================
        // SSE PARSING
        // ============================================================

        private static JsonNode ParseJsonFromSse(
            string sse)
        {
            if (string.IsNullOrWhiteSpace(sse))
                return null;

            string[] lines =
                sse.Split(
                    new[]
                    {
                        "\r\n",
                        "\n",
                        "\r"
                    },
                    StringSplitOptions.None);

            // Parse the data lines individually, starting from
            // the end because the last SSE event is usually the
            // final result.
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line =
                    lines[i];

                if (!line.StartsWith(
                    "data:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string data =
                    line.Substring(5).Trim();

                if (string.IsNullOrWhiteSpace(data))
                    continue;

                try
                {
                    JsonNode node =
                        JsonNode.Parse(data);

                    if (node != null)
                        return node;
                }
                catch
                {
                }
            }

            // Fallback: concatenate all data lines.
            var dataBuilder =
                new StringBuilder();

            foreach (string line in lines)
            {
                if (!line.StartsWith(
                    "data:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string data =
                    line.Substring(5).Trim();

                if (!string.IsNullOrWhiteSpace(data))
                    dataBuilder.Append(data);
            }

            string jsonText =
                dataBuilder.ToString();

            if (string.IsNullOrWhiteSpace(jsonText))
                return null;

            try
            {
                return JsonNode.Parse(jsonText);
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        // EVENT ID
        // ============================================================

        private static string ExtractEventId(
            string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return null;

            // --------------------------------------------------------
            // Normal JSON:
            // {"event_id":"123456"}
            // --------------------------------------------------------

            try
            {
                JsonNode json =
                    JsonNode.Parse(responseText);

                if (json is JsonObject obj)
                {
                    string eventId =
                        obj["event_id"]?
                            .GetValue<string>();

                    if (!string.IsNullOrWhiteSpace(eventId))
                        return eventId;
                }
            }
            catch
            {
            }

            // --------------------------------------------------------
            // SSE fallback
            // --------------------------------------------------------

            string[] lines =
                responseText.Split(
                    new[]
                    {
                        "\r\n",
                        "\n",
                        "\r"
                    },
                    StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (!line.StartsWith(
                    "data:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string data =
                    line.Substring(5).Trim();

                try
                {
                    JsonNode json =
                        JsonNode.Parse(data);

                    string eventId =
                        FindStringProperty(
                            json,
                            "event_id");

                    if (!string.IsNullOrWhiteSpace(eventId))
                        return eventId;
                }
                catch
                {
                }
            }

            return null;
        }

        // ============================================================
        // ERROR PARSING
        // ============================================================

        private static HuggingFaceApiException
            CreateApiExceptionFromJson(
                JsonNode root,
                string defaultTitle)
        {
            JsonObject obj =
                GetFirstJsonObject(root);

            if (obj == null)
            {
                return new HuggingFaceApiException(
                    ExtractErrorMessageFromJson(root),
                    defaultTitle);
            }

            string message = null;
            string code = null;

            // --------------------------------------------------------
            // error.message
            // --------------------------------------------------------

            if (obj["error"] is JsonObject errorObject)
            {
                try
                {
                    message =
                        errorObject["message"]?
                            .GetValue<string>();
                }
                catch
                {
                }

                try
                {
                    code =
                        errorObject["code"]?
                            .GetValue<string>();
                }
                catch
                {
                }
            }

            // --------------------------------------------------------
            // Direct message
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(message))
            {
                try
                {
                    message =
                        obj["message"]?
                            .GetValue<string>();
                }
                catch
                {
                }
            }

            // --------------------------------------------------------
            // Recursive fallback
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(message))
            {
                message =
                    ExtractErrorMessageFromJson(root);
            }

            // --------------------------------------------------------
            // Fallback code
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(code))
            {
                code =
                    FindStringProperty(
                        root,
                        "code");
            }

            string title =
                string.IsNullOrWhiteSpace(code)
                    ? defaultTitle
                    : $"{defaultTitle} - {code}";

            return new HuggingFaceApiException(
                string.IsNullOrWhiteSpace(message)
                    ? "Unknown Hugging Face API error."
                    : message,
                title);
        }

        private static string ExtractErrorMessageFromJson(
            JsonNode root)
        {
            if (root == null)
                return "Unknown Hugging Face API error.";

            JsonObject obj =
                GetFirstJsonObject(root);

            if (obj != null)
            {
                // ----------------------------------------------------
                // Nested error.message
                // ----------------------------------------------------

                if (obj["error"] is JsonObject errorObject)
                {
                    try
                    {
                        string message =
                            errorObject["message"]?
                                .GetValue<string>();

                        if (!string.IsNullOrWhiteSpace(message))
                            return message;
                    }
                    catch
                    {
                    }
                }

                // ----------------------------------------------------
                // Direct message
                // ----------------------------------------------------

                try
                {
                    string message =
                        obj["message"]?
                            .GetValue<string>();

                    if (!string.IsNullOrWhiteSpace(message))
                        return message;
                }
                catch
                {
                }

                // ----------------------------------------------------
                // Error can itself be a string
                // ----------------------------------------------------

                try
                {
                    string error =
                        obj["error"]?
                            .GetValue<string>();

                    if (!string.IsNullOrWhiteSpace(error))
                        return error;
                }
                catch
                {
                }
            }

            // --------------------------------------------------------
            // Recursive fallback
            // --------------------------------------------------------

            if (root is JsonArray array)
            {
                foreach (JsonNode child in array)
                {
                    string message =
                        ExtractErrorMessageFromJson(child);

                    if (!string.IsNullOrWhiteSpace(message) &&
                        !message.StartsWith(
                            "{",
                            StringComparison.Ordinal))
                    {
                        return message;
                    }

                    if (!string.IsNullOrWhiteSpace(message) &&
                        !message.StartsWith(
                            "[",
                            StringComparison.Ordinal))
                    {
                        return message;
                    }
                }
            }

            return root.ToJsonString();
        }

        // ============================================================
        // JSON OBJECT HELPER
        // ============================================================

        private static JsonObject GetFirstJsonObject(
            JsonNode node)
        {
            if (node == null)
                return null;

            if (node is JsonObject obj)
                return obj;

            if (node is JsonArray array)
            {
                foreach (JsonNode child in array)
                {
                    if (child is JsonObject childObject)
                        return childObject;

                    JsonObject nested =
                        GetFirstJsonObject(child);

                    if (nested != null)
                        return nested;
                }
            }

            return null;
        }

        private static string ExtractErrorMessage(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "Unknown Hugging Face API error.";

            JsonNode root =
                ParseJsonFromSse(json);

            if (root != null)
            {
                string message =
                    ExtractErrorMessageFromJson(root);

                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }

            try
            {
                root =
                    JsonNode.Parse(json);

                if (root != null)
                {
                    string message =
                        ExtractErrorMessageFromJson(root);

                    if (!string.IsNullOrWhiteSpace(message))
                        return message;
                }
            }
            catch
            {
            }

            return json;
        }

        // ============================================================
        // JSON PROPERTY SEARCH
        // ============================================================

        private static string FindStringProperty(
            JsonNode node,
            string propertyName)
        {
            if (node == null)
                return null;

            if (node is JsonObject obj)
            {
                foreach (var property in obj)
                {
                    if (string.Equals(
                        property.Key,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            string value =
                                property.Value?
                                    .GetValue<string>();

                            if (!string.IsNullOrWhiteSpace(value))
                                return value;
                        }
                        catch
                        {
                        }
                    }

                    string result =
                        FindStringProperty(
                            property.Value,
                            propertyName);

                    if (!string.IsNullOrWhiteSpace(result))
                        return result;
                }
            }
            else if (node is JsonArray array)
            {
                foreach (JsonNode child in array)
                {
                    string result =
                        FindStringProperty(
                            child,
                            propertyName);

                    if (!string.IsNullOrWhiteSpace(result))
                        return result;
                }
            }

            return null;
        }

        // ============================================================
        // IMAGE RESULT EXTRACTION
        // ============================================================

        private static string ExtractGeneratedFile(
            JsonNode node)
        {
            if (node == null)
                return null;

            if (node is JsonArray array)
            {
                foreach (JsonNode child in array)
                {
                    string result =
                        ExtractGeneratedFile(child);

                    if (!string.IsNullOrWhiteSpace(result))
                        return result;
                }

                return null;
            }

            if (node is JsonObject obj)
            {
                // ----------------------------------------------------
                // Direct FileData path
                // ----------------------------------------------------

                try
                {
                    string path =
                        obj["path"]?
                            .GetValue<string>();

                    if (IsLikelyImagePath(path))
                        return path;
                }
                catch
                {
                }

                // ----------------------------------------------------
                // Direct URL
                // ----------------------------------------------------

                try
                {
                    string url =
                        obj["url"]?
                            .GetValue<string>();

                    if (IsLikelyImagePath(url))
                        return url;
                }
                catch
                {
                }

                // ----------------------------------------------------
                // Recursive search
                // ----------------------------------------------------

                foreach (var property in obj)
                {
                    string result =
                        ExtractGeneratedFile(
                            property.Value);

                    if (!string.IsNullOrWhiteSpace(result))
                        return result;
                }

                return null;
            }

            if (node is JsonValue value)
            {
                try
                {
                    string text =
                        value.GetValue<string>();

                    if (IsLikelyImagePath(text))
                        return text;
                }
                catch
                {
                }
            }

            return null;
        }

        private static bool IsLikelyImagePath(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (value.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string lower =
                value.ToLowerInvariant();

            return lower.Contains(".png") ||
                   lower.Contains(".jpg") ||
                   lower.Contains(".jpeg") ||
                   lower.Contains(".webp") ||
                   lower.Contains("/file=") ||
                   lower.Contains("gradio_api");
        }

        // ============================================================
        // DOWNLOAD
        // ============================================================

        private static async Task<Image> DownloadImageAsync(
            string fileLocation,
            CancellationToken cancellationToken)
        {
            string url =
                ResolveFileUrl(fileLocation);

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url);

            AddAuthorizationHeader(request);

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    cancellationToken)
                    .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string responseText =
                    await response.Content
                        .ReadAsStringAsync(cancellationToken)
                        .ConfigureAwait(false);

                string message =
                    ExtractErrorMessage(responseText);

                throw new HuggingFaceApiException(
                    message,
                    $"Image Download ({(int)response.StatusCode})");
            }

            byte[] bytes =
                await response.Content
                    .ReadAsByteArrayAsync(cancellationToken)
                    .ConfigureAwait(false);

            using var memoryStream =
                new MemoryStream(bytes);

            using Image temp =
                Image.FromStream(memoryStream);

            // Return a detached bitmap so it is no longer dependent
            // on the MemoryStream.
            return new Bitmap(temp);
        }

        // ============================================================
        // URL RESOLUTION
        // ============================================================

        private static string ResolveFileUrl(
            string fileLocation)
        {
            if (string.IsNullOrWhiteSpace(fileLocation))
            {
                throw new ArgumentException(
                    "File location is empty.",
                    nameof(fileLocation));
            }

            if (Uri.TryCreate(
                fileLocation,
                UriKind.Absolute,
                out Uri absoluteUri))
            {
                return absoluteUri.ToString();
            }

            if (fileLocation.StartsWith(
                "/gradio_api/",
                StringComparison.OrdinalIgnoreCase))
            {
                return BaseHost + fileLocation;
            }

            if (fileLocation.StartsWith(
                "gradio_api/",
                StringComparison.OrdinalIgnoreCase))
            {
                return BaseHost + "/" + fileLocation;
            }

            if (fileLocation.StartsWith(
                "/file=",
                StringComparison.OrdinalIgnoreCase))
            {
                return BaseUrl + fileLocation;
            }

            if (fileLocation.StartsWith(
                "file=",
                StringComparison.OrdinalIgnoreCase))
            {
                return BaseUrl + "/" + fileLocation;
            }

            return BaseUrl +
                   "/file=" +
                   fileLocation.TrimStart('/');
        }

        // ============================================================
        // ASPECT RATIO
        // ============================================================

        private sealed class AspectRatioInfo
        {
            public string Name { get; }

            public double Ratio { get; }

            public AspectRatioInfo(
                string name,
                double ratio)
            {
                Name = name;
                Ratio = ratio;
            }
        }

        private static readonly AspectRatioInfo[] SupportedAspectRatios =
        {
            new AspectRatioInfo("1:1", 1.0),
            new AspectRatioInfo("4:3", 4.0 / 3.0),
            new AspectRatioInfo("3:4", 3.0 / 4.0),
            new AspectRatioInfo("16:9", 16.0 / 9.0),
            new AspectRatioInfo("9:16", 9.0 / 16.0)
        };

        private static AspectRatioInfo GetNearestAspectRatio(
            int width,
            int height)
        {
            double targetRatio =
                (double)width / height;

            AspectRatioInfo nearest =
                SupportedAspectRatios[0];

            double smallestDifference =
                Math.Abs(
                    targetRatio -
                    nearest.Ratio);

            for (int i = 1;
                 i < SupportedAspectRatios.Length;
                 i++)
            {
                AspectRatioInfo candidate =
                    SupportedAspectRatios[i];

                double difference =
                    Math.Abs(
                        targetRatio -
                        candidate.Ratio);

                if (difference < smallestDifference)
                {
                    smallestDifference =
                        difference;

                    nearest =
                        candidate;
                }
            }

            return nearest;
        }

        // ============================================================
        // GENERATION SIZE
        // ============================================================

        private sealed class GenerationSize
        {
            public int Width { get; }

            public int Height { get; }

            public GenerationSize(
                int width,
                int height)
            {
                Width = width;
                Height = height;
            }
        }

        private static GenerationSize GetGenerationSize(
            AspectRatioInfo aspectRatio)
        {
            if (aspectRatio == null)
            {
                return new GenerationSize(
                    GenerationLongSide,
                    GenerationLongSide);
            }

            // Long side is fixed at 1024.
            // This keeps generation cost predictable.
            //
            // 1:1   -> 1024 x 1024
            // 4:3   -> 1024 x 768
            // 3:4   -> 768 x 1024
            // 16:9  -> 1024 x 576
            // 9:16  -> 576 x 1024

            if (aspectRatio.Name == "1:1")
            {
                return new GenerationSize(
                    GenerationLongSide,
                    GenerationLongSide);
            }

            if (aspectRatio.Name == "4:3")
            {
                return new GenerationSize(
                    GenerationLongSide,
                    RoundToMultiple(
                        GenerationLongSide * 3 / 4,
                        8));
            }

            if (aspectRatio.Name == "3:4")
            {
                return new GenerationSize(
                    RoundToMultiple(
                        GenerationLongSide * 3 / 4,
                        8),
                    GenerationLongSide);
            }

            if (aspectRatio.Name == "16:9")
            {
                return new GenerationSize(
                    GenerationLongSide,
                    RoundToMultiple(
                        GenerationLongSide * 9 / 16,
                        8));
            }

            if (aspectRatio.Name == "9:16")
            {
                return new GenerationSize(
                    RoundToMultiple(
                        GenerationLongSide * 9 / 16,
                        8),
                    GenerationLongSide);
            }

            return new GenerationSize(
                GenerationLongSide,
                GenerationLongSide);
        }

        private static int RoundToMultiple(
            int value,
            int multiple)
        {
            if (multiple <= 1)
                return value;

            return (int)(
                Math.Round(
                    (double)value / multiple) *
                multiple);
        }

        // ============================================================
        // FINAL RESIZE + CENTER CROP
        // ============================================================

        private static Bitmap ResizeAndCropToExactSize(
            Image source,
            int targetWidth,
            int targetHeight)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (targetWidth <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(targetWidth));

            if (targetHeight <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(targetHeight));

            int sourceWidth =
                source.Width;

            int sourceHeight =
                source.Height;

            if (sourceWidth <= 0 ||
                sourceHeight <= 0)
            {
                throw new InvalidOperationException(
                    "Generated image has invalid dimensions.");
            }

            // Scale enough to COVER the entire target rectangle.
            //
            // This means:
            // - smaller target -> image is reduced
            // - larger target  -> image is enlarged
            // - different ratio -> excess area is cropped
            //
            // No stretching occurs.
            double scaleX =
                (double)targetWidth /
                sourceWidth;

            double scaleY =
                (double)targetHeight /
                sourceHeight;

            double scale =
                Math.Max(
                    scaleX,
                    scaleY);

            int scaledWidth =
                Math.Max(
                    targetWidth,
                    (int)Math.Ceiling(
                        sourceWidth * scale));

            int scaledHeight =
                Math.Max(
                    targetHeight,
                    (int)Math.Ceiling(
                        sourceHeight * scale));

            var result =
                new Bitmap(
                    targetWidth,
                    targetHeight,
                    PixelFormat.Format32bppArgb);

            result.SetResolution(
                source.HorizontalResolution > 0
                    ? source.HorizontalResolution
                    : 96f,
                source.VerticalResolution > 0
                    ? source.VerticalResolution
                    : 96f);

            using Graphics graphics =
                Graphics.FromImage(result);

            graphics.CompositingMode =
                CompositingMode.SourceCopy;

            graphics.CompositingQuality =
                CompositingQuality.HighQuality;

            graphics.InterpolationMode =
                InterpolationMode.HighQualityBicubic;

            graphics.SmoothingMode =
                SmoothingMode.HighQuality;

            graphics.PixelOffsetMode =
                PixelOffsetMode.HighQuality;

            graphics.PageUnit =
                GraphicsUnit.Pixel;

            int x =
                (targetWidth - scaledWidth) / 2;

            int y =
                (targetHeight - scaledHeight) / 2;

            Rectangle destinationRectangle =
                new Rectangle(
                    x,
                    y,
                    scaledWidth,
                    scaledHeight);

            graphics.DrawImage(
                source,
                destinationRectangle,
                0,
                0,
                sourceWidth,
                sourceHeight,
                GraphicsUnit.Pixel);

            return result;
        }
    }

    // ================================================================
    // EXCEPTION
    // ================================================================

    public class HuggingFaceApiException : Exception
    {
        public string DisplayTitle { get; }

        public HuggingFaceApiException(
            string message,
            string displayTitle)
            : base(message)
        {
            DisplayTitle =
                displayTitle;
        }

        public HuggingFaceApiException(
            string message,
            string displayTitle,
            Exception innerException)
            : base(
                message,
                innerException)
        {
            DisplayTitle =
                displayTitle;
        }
    }
}