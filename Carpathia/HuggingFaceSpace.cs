using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Carpathia
{
    public sealed class HuggingFaceApiException : Exception
    {
        public string DisplayTitle { get; }

        public HuggingFaceApiException(
            string message,
            string displayTitle = "Image Generation Error",
            Exception innerException = null)
            : base(message, innerException)
        {
            DisplayTitle = displayTitle;
        }
    }

    public static class HuggingFaceSpace
    {
        private const string BaseHost =
            "https://hugging-apps-qwen-image-2-1.hf.space";

        private const string BaseUrl =
            BaseHost + "/gradio_api";

        private static readonly HttpClient client = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        /*
         * Plain Hugging Face API key.
         *
         * Set this before calling GenerateImage().
         */

        private sealed class PrepareResult
        {
            public string Prompt { get; set; }

            public double Seed { get; set; }

            public string AspectRatio { get; set; }
        }

        // =========================================================
        // AUTHORIZATION
        // =========================================================

        private static void AddAuthorizationHeader(
    HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request)
                );
            }

            string apiKey =
                GetApiKey();

            // API key yoksa Authorization header gönderilmez.
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return;
            }

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey
                );
        }

        // =========================================================
        // GENERATE IMAGE
        // =========================================================

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
                    nameof(prompt)
                );
            }

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width)
                );
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height)
                );
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string uploadedFilePath = null;

                // ---------------------------------------------
                // Upload source image if image editing is used
                // ---------------------------------------------

                if (image != null)
                {
                    uploadedFilePath =
                        await UploadImageAsync(
                            image,
                            cancellationToken
                        ).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                string requestedAspectRatio =
                    GetAspectRatio(
                        width,
                        height
                    );

                // ---------------------------------------------
                // Get random seed from /prepare
                // ---------------------------------------------

                PrepareResult prepareResult =
                    await PrepareAsync(
                        prompt,
                        uploadedFilePath,
                        requestedAspectRatio,
                        cancellationToken
                    ).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                // ---------------------------------------------
                // Generate image
                // ---------------------------------------------

                string eventId =
                    await StartGenerationAsync(
                        prepareResult.Prompt,
                        uploadedFilePath,
                        width,
                        height,
                        prepareResult.Seed,
                        prepareResult.AspectRatio,
                        cancellationToken
                    ).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                string generatedImageLocation =
                    await WaitForGenerationAsync(
                        eventId,
                        cancellationToken
                    ).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(
                    generatedImageLocation))
                {
                    throw new HuggingFaceApiException(
                        "The Hugging Face Space completed the request " +
                        "but did not return an image."
                    );
                }

                return await DownloadImageAsync(
                    generatedImageLocation,
                    cancellationToken
                ).ConfigureAwait(false);
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
                    "Image Generation Error",
                    ex
                );
            }
        }

        // =========================================================
        // UPLOAD IMAGE
        // =========================================================

        private static async Task<string> UploadImageAsync(
            Image image,
            CancellationToken cancellationToken)
        {
            using var memoryStream =
                new MemoryStream();

            image.Save(
                memoryStream,
                ImageFormat.Png
            );

            memoryStream.Position = 0;

            using var multipart =
                new MultipartFormDataContent();

            using var imageContent =
                new StreamContent(
                    memoryStream
                );

            imageContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "image/png"
                );

            multipart.Add(
                imageContent,
                "files",
                "input.png"
            );

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    BaseUrl + "/upload"
                );

            AddAuthorizationHeader(request);

            request.Content =
                multipart;

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken
                ).ConfigureAwait(false);

            string responseText =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken
                    )
                    .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw ParseHuggingFaceError(
                    responseText,
                    GetHttpFallbackMessage(
                        response,
                        "Image upload failed."
                    )
                );
            }

            JsonNode root;

            try
            {
                root =
                    JsonNode.Parse(
                        responseText
                    );
            }
            catch (Exception ex)
            {
                throw new HuggingFaceApiException(
                    "Hugging Face returned an invalid upload response.",
                    "Upload Error",
                    ex
                );
            }

            if (root is not JsonArray array ||
                array.Count == 0 ||
                array[0] == null)
            {
                throw new HuggingFaceApiException(
                    "Hugging Face did not return an uploaded file path.",
                    "Upload Error"
                );
            }

            string uploadedPath =
                array[0].ToString();

            if (string.IsNullOrWhiteSpace(
                uploadedPath))
            {
                throw new HuggingFaceApiException(
                    "The uploaded file path returned by Hugging Face was empty.",
                    "Upload Error"
                );
            }

            return uploadedPath;
        }

        // =========================================================
        // CREATE INPUT IMAGE ARRAY
        // =========================================================

        private static JsonArray CreateInputImages(
            string uploadedFilePath)
        {
            var inputImages =
                new JsonArray();

            if (string.IsNullOrWhiteSpace(
                uploadedFilePath))
            {
                return inputImages;
            }

            inputImages.Add(
                new JsonObject
                {
                    ["image"] =
                        new JsonObject
                        {
                            ["path"] =
                                JsonValue.Create(
                                    uploadedFilePath
                                ),

                            ["meta"] =
                                new JsonObject
                                {
                                    ["_type"] =
                                        JsonValue.Create(
                                            "gradio.FileData"
                                        )
                                }
                        },

                    ["caption"] =
                        null
                }
            );

            return inputImages;
        }

        // =========================================================
        // PREPARE
        //
        // POST /call/v2/prepare
        //
        // Random seed is generated here.
        // =========================================================

        private static async Task<PrepareResult> PrepareAsync(
            string prompt,
            string uploadedFilePath,
            string aspectRatio,
            CancellationToken cancellationToken)
        {
            JsonArray inputImages =
                CreateInputImages(
                    uploadedFilePath
                );

            var payload =
                new JsonObject
                {
                    ["prompt"] =
                        JsonValue.Create(
                            prompt
                        ),

                    ["input_images"] =
                        inputImages,

                    ["enhance"] =
                        JsonValue.Create(
                            false
                        ),

                    ["seed"] =
                        JsonValue.Create(
                            0.0
                        ),

                    ["randomize_seed"] =
                        JsonValue.Create(
                            true
                        ),

                    ["aspect_ratio"] =
                        JsonValue.Create(
                            aspectRatio
                        )
                };

            string jsonPayload =
                payload.ToJsonString();

            using var content =
                new StringContent(
                    jsonPayload,
                    Encoding.UTF8,
                    "application/json"
                );

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    BaseUrl + "/call/v2/prepare"
                );

            AddAuthorizationHeader(request);

            request.Content =
                content;

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken
                ).ConfigureAwait(false);

            string responseText =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken
                    )
                    .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw ParseHuggingFaceError(
                    responseText,
                    GetHttpFallbackMessage(
                        response,
                        "Unable to prepare image generation."
                    )
                );
            }

            JsonNode root;

            try
            {
                root =
                    JsonNode.Parse(
                        responseText
                    );
            }
            catch (Exception ex)
            {
                throw new HuggingFaceApiException(
                    "Hugging Face returned an invalid prepare response.",
                    "Image Generation Error",
                    ex
                );
            }

            string eventId =
                root?["event_id"]
                    ?.ToString();

            if (string.IsNullOrWhiteSpace(
                eventId))
            {
                throw ParseHuggingFaceError(
                    responseText,
                    "Hugging Face did not return an event ID " +
                    "for the prepare request."
                );
            }

            return await WaitForPrepareAsync(
                eventId,
                prompt,
                aspectRatio,
                cancellationToken
            ).ConfigureAwait(false);
        }

        // =========================================================
        // WAIT FOR PREPARE
        // =========================================================

        private static async Task<PrepareResult> WaitForPrepareAsync(
            string eventId,
            string fallbackPrompt,
            string fallbackAspectRatio,
            CancellationToken cancellationToken)
        {
            string requestUrl =
                BaseUrl +
                "/call/prepare/" +
                Uri.EscapeDataString(
                    eventId
                );

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    requestUrl
                );

            AddAuthorizationHeader(request);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "text/event-stream"
                )
            );

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                ).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string responseText =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken
                        )
                        .ConfigureAwait(false);

                throw ParseHuggingFaceError(
                    responseText,
                    GetHttpFallbackMessage(
                        response,
                        "Unable to retrieve the prepared random seed."
                    )
                );
            }

            using Stream stream =
                await response.Content
                    .ReadAsStreamAsync(
                        cancellationToken
                    )
                    .ConfigureAwait(false);

            using var reader =
                new StreamReader(stream);

            string currentEvent =
                null;

            while (!reader.EndOfStream)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                string line =
                    await reader
                        .ReadLineAsync()
                        .ConfigureAwait(false);

                if (line == null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(
                    line))
                {
                    continue;
                }

                if (line.StartsWith(
                    "event:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    currentEvent =
                        line.Substring(
                            "event:".Length
                        ).Trim();

                    continue;
                }

                if (!line.StartsWith(
                    "data:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string data =
                    line.Substring(
                        "data:".Length
                    ).Trim();

                if (string.IsNullOrWhiteSpace(
                    data))
                {
                    continue;
                }

                if (string.Equals(
                    currentEvent,
                    "error",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw ParseHuggingFaceError(
                        data,
                        "Hugging Face could not prepare image generation."
                    );
                }

                JsonNode node;

                try
                {
                    node =
                        JsonNode.Parse(
                            data
                        );
                }
                catch
                {
                    continue;
                }

                if (node == null)
                {
                    continue;
                }

                if (string.Equals(
                    currentEvent,
                    "complete",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (node is JsonObject errorObject &&
                        errorObject["error"] != null)
                    {
                        throw ParseHuggingFaceError(
                            data,
                            "Hugging Face could not prepare image generation."
                        );
                    }

                    PrepareResult result =
                        ParsePrepareResult(
                            node,
                            fallbackPrompt,
                            fallbackAspectRatio
                        );

                    if (result != null)
                    {
                        return result;
                    }
                }

                if (node is JsonObject objectNode)
                {
                    string message =
                        objectNode["msg"]
                            ?.ToString();

                    if (string.Equals(
                        message,
                        "process_completed",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        JsonNode outputData =
                            objectNode["output"]
                                ?["data"];

                        PrepareResult result =
                            ParsePrepareResult(
                                outputData,
                                fallbackPrompt,
                                fallbackAspectRatio
                            );

                        if (result != null)
                        {
                            return result;
                        }
                    }

                    if (string.Equals(
                        message,
                        "process_error",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        string rawError =
                            objectNode["output"]
                                ?["error"]
                                ?.ToString()
                            ??
                            objectNode["error"]
                                ?.ToString()
                            ??
                            data;

                        throw ParseHuggingFaceError(
                            rawError,
                            "Hugging Face could not prepare image generation."
                        );
                    }
                }
            }

            throw new HuggingFaceApiException(
                "Hugging Face did not return a randomized seed.",
                "Image Generation Error"
            );
        }

        // =========================================================
        // PARSE PREPARE RESULT
        // =========================================================

        private static PrepareResult ParsePrepareResult(
            JsonNode node,
            string fallbackPrompt,
            string fallbackAspectRatio)
        {
            if (node is not JsonArray array ||
                array.Count < 4)
            {
                return null;
            }

            string preparedPrompt =
                array[0]?.ToString();

            string rewrittenPrompt =
                array[1]?.ToString();

            string seedText =
                array[2]?.ToString();

            string preparedAspectRatio =
                array[3]?.ToString();

            if (!double.TryParse(
                seedText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double seed))
            {
                throw new HuggingFaceApiException(
                    "Hugging Face returned an invalid randomized seed: " +
                    seedText
                );
            }

            string finalPrompt =
                !string.IsNullOrWhiteSpace(
                    rewrittenPrompt)
                    ? rewrittenPrompt
                    : preparedPrompt;

            if (string.IsNullOrWhiteSpace(
                finalPrompt))
            {
                finalPrompt =
                    fallbackPrompt;
            }

            if (string.IsNullOrWhiteSpace(
                preparedAspectRatio))
            {
                preparedAspectRatio =
                    fallbackAspectRatio;
            }

            return new PrepareResult
            {
                Prompt =
                    finalPrompt,

                Seed =
                    seed,

                AspectRatio =
                    preparedAspectRatio
            };
        }

        // =========================================================
        // START GENERATION
        // =========================================================

        private static async Task<string> StartGenerationAsync(
            string prompt,
            string uploadedFilePath,
            int width,
            int height,
            double seed,
            string aspectRatio,
            CancellationToken cancellationToken)
        {
            JsonArray inputImages =
                CreateInputImages(
                    uploadedFilePath
                );

            int resolution =
                GetResolution(
                    width,
                    height
                );

            var payload =
                new JsonObject
                {
                    ["prompt"] =
                        JsonValue.Create(
                            prompt
                        ),

                    ["input_images"] =
                        inputImages,

                    ["negative_prompt"] =
                        JsonValue.Create(
                            string.Empty
                        ),

                    ["true_cfg_scale"] =
                        JsonValue.Create(
                            1.0
                        ),

                    ["num_inference_steps"] =
                        JsonValue.Create(
                            40.0
                        ),

                    ["seed"] =
                        JsonValue.Create(
                            seed
                        ),

                    ["resolution"] =
                        JsonValue.Create(
                            resolution
                        ),

                    ["aspect_ratio"] =
                        JsonValue.Create(
                            aspectRatio
                        )
                };

            string jsonPayload =
                payload.ToJsonString();

            using var content =
                new StringContent(
                    jsonPayload,
                    Encoding.UTF8,
                    "application/json"
                );

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    BaseUrl + "/call/v2/generate"
                );

            AddAuthorizationHeader(request);

            request.Content =
                content;

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken
                ).ConfigureAwait(false);

            string responseText =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken
                    )
                    .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw ParseHuggingFaceError(
                    responseText,
                    GetHttpFallbackMessage(
                        response,
                        "Unable to start image generation."
                    )
                );
            }

            JsonNode root;

            try
            {
                root =
                    JsonNode.Parse(
                        responseText
                    );
            }
            catch (Exception ex)
            {
                throw new HuggingFaceApiException(
                    "Hugging Face returned an invalid generation response.",
                    "Image Generation Error",
                    ex
                );
            }

            string eventId =
                root?["event_id"]
                    ?.ToString();

            if (string.IsNullOrWhiteSpace(
                eventId))
            {
                throw ParseHuggingFaceError(
                    responseText,
                    "Hugging Face did not return an event ID."
                );
            }

            return eventId;
        }

        // =========================================================
        // WAIT FOR GENERATION
        // =========================================================

        private static async Task<string> WaitForGenerationAsync(
            string eventId,
            CancellationToken cancellationToken)
        {
            string requestUrl =
                BaseUrl +
                "/call/generate/" +
                Uri.EscapeDataString(
                    eventId
                );

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    requestUrl
                );

            AddAuthorizationHeader(request);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "text/event-stream"
                )
            );

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                ).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string responseText =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken
                        )
                        .ConfigureAwait(false);

                throw ParseHuggingFaceError(
                    responseText,
                    GetHttpFallbackMessage(
                        response,
                        "Unable to retrieve the generated image."
                    )
                );
            }

            using Stream stream =
                await response.Content
                    .ReadAsStreamAsync(
                        cancellationToken
                    )
                    .ConfigureAwait(false);

            using var reader =
                new StreamReader(
                    stream
                );

            string currentEvent =
                null;

            while (!reader.EndOfStream)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                string line =
                    await reader
                        .ReadLineAsync()
                        .ConfigureAwait(false);

                if (line == null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(
                    line))
                {
                    continue;
                }

                if (line.StartsWith(
                    "event:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    currentEvent =
                        line.Substring(
                            "event:".Length
                        ).Trim();

                    continue;
                }

                if (!line.StartsWith(
                    "data:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string data =
                    line.Substring(
                        "data:".Length
                    ).Trim();

                if (string.IsNullOrWhiteSpace(
                    data))
                {
                    continue;
                }

                if (string.Equals(
                    currentEvent,
                    "error",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw ParseHuggingFaceError(
                        data,
                        "Hugging Face could not generate the image."
                    );
                }

                JsonNode node;

                try
                {
                    node =
                        JsonNode.Parse(
                            data
                        );
                }
                catch
                {
                    continue;
                }

                if (node == null)
                {
                    continue;
                }

                if (string.Equals(
                    currentEvent,
                    "complete",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (node is JsonObject completeObject &&
                        completeObject["error"] != null)
                    {
                        throw ParseHuggingFaceError(
                            data,
                            "Hugging Face could not generate the image."
                        );
                    }

                    string generatedFile =
                        ExtractGeneratedFile(
                            node
                        );

                    if (!string.IsNullOrWhiteSpace(
                        generatedFile))
                    {
                        return generatedFile;
                    }
                }

                if (node is JsonObject objectNode)
                {
                    string message =
                        objectNode["msg"]
                            ?.ToString();

                    if (string.Equals(
                        message,
                        "process_completed",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        JsonNode outputData =
                            objectNode["output"]
                                ?["data"];

                        string generatedFile =
                            ExtractGeneratedFile(
                                outputData
                            );

                        if (!string.IsNullOrWhiteSpace(
                            generatedFile))
                        {
                            return generatedFile;
                        }
                    }

                    if (string.Equals(
                        message,
                        "process_error",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        string rawError =
                            objectNode["output"]
                                ?["error"]
                                ?.ToString()
                            ??
                            objectNode["error"]
                                ?.ToString()
                            ??
                            data;

                        throw ParseHuggingFaceError(
                            rawError,
                            "Hugging Face could not generate the image."
                        );
                    }
                }
            }

            throw new HuggingFaceApiException(
                "The generation stream ended without returning an image."
            );
        }

        // =========================================================
        // EXTRACT GENERATED FILE
        // =========================================================

        private static string ExtractGeneratedFile(
            JsonNode node)
        {
            if (node == null)
            {
                return null;
            }

            if (node is JsonArray array)
            {
                foreach (JsonNode item in array)
                {
                    string result =
                        ExtractGeneratedFile(
                            item
                        );

                    if (!string.IsNullOrWhiteSpace(
                        result))
                    {
                        return result;
                    }
                }

                return null;
            }

            if (node is JsonObject obj)
            {
                string url =
                    obj["url"]
                        ?.ToString();

                if (!string.IsNullOrWhiteSpace(
                    url))
                {
                    return url;
                }

                string path =
                    obj["path"]
                        ?.ToString();

                if (!string.IsNullOrWhiteSpace(
                    path))
                {
                    return path;
                }

                if (obj["data"] != null)
                {
                    string result =
                        ExtractGeneratedFile(
                            obj["data"]
                        );

                    if (!string.IsNullOrWhiteSpace(
                        result))
                    {
                        return result;
                    }
                }

                if (obj["output"] != null)
                {
                    string result =
                        ExtractGeneratedFile(
                            obj["output"]
                        );

                    if (!string.IsNullOrWhiteSpace(
                        result))
                    {
                        return result;
                    }
                }

                return null;
            }

            if (node is JsonValue)
            {
                string value =
                    node.ToString();

                if (!string.IsNullOrWhiteSpace(
                    value))
                {
                    return value;
                }
            }

            return null;
        }

        // =========================================================
        // DOWNLOAD IMAGE
        // =========================================================

        private static async Task<Image> DownloadImageAsync(
            string fileLocation,
            CancellationToken cancellationToken)
        {
            string imageUrl =
                ResolveFileUrl(
                    fileLocation
                );

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    imageUrl
                );

            AddAuthorizationHeader(request);

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                ).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string responseText =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken
                        )
                        .ConfigureAwait(false);

                throw ParseHuggingFaceError(
                    responseText,
                    GetHttpFallbackMessage(
                        response,
                        "The generated image could not be downloaded."
                    )
                );
            }

            byte[] imageBytes =
                await response.Content
                    .ReadAsByteArrayAsync(
                        cancellationToken
                    )
                    .ConfigureAwait(false);

            if (imageBytes == null ||
                imageBytes.Length == 0)
            {
                throw new HuggingFaceApiException(
                    "Hugging Face returned an empty image."
                );
            }

            using var memoryStream =
                new MemoryStream(
                    imageBytes
                );

            using Image temporaryImage =
                Image.FromStream(
                    memoryStream
                );

            return new Bitmap(
                temporaryImage
            );
        }

        // =========================================================
        // RESOLVE IMAGE URL
        // =========================================================

        private static string ResolveFileUrl(
            string fileLocation)
        {
            if (string.IsNullOrWhiteSpace(
                fileLocation))
            {
                throw new HuggingFaceApiException(
                    "The generated image path is empty."
                );
            }

            fileLocation =
                fileLocation
                    .Trim()
                    .Trim('"');

            if (Uri.TryCreate(
                    fileLocation,
                    UriKind.Absolute,
                    out Uri uri)
                &&
                (uri.Scheme ==
                    Uri.UriSchemeHttp
                 ||
                 uri.Scheme ==
                    Uri.UriSchemeHttps))
            {
                return uri.ToString();
            }

            if (fileLocation.StartsWith(
                "/gradio_api/",
                StringComparison.OrdinalIgnoreCase))
            {
                return BaseHost +
                       fileLocation;
            }

            if (fileLocation.StartsWith(
                "gradio_api/",
                StringComparison.OrdinalIgnoreCase))
            {
                return BaseHost +
                       "/" +
                       fileLocation;
            }

            if (fileLocation.StartsWith(
                "/file=",
                StringComparison.OrdinalIgnoreCase))
            {
                return BaseUrl +
                       fileLocation;
            }

            if (fileLocation.StartsWith(
                "file=",
                StringComparison.OrdinalIgnoreCase))
            {
                return BaseUrl +
                       "/" +
                       fileLocation;
            }

            return BaseUrl +
                   "/file=" +
                   fileLocation;
        }

        // =========================================================
        // ERROR PARSER
        // =========================================================

        private static HuggingFaceApiException ParseHuggingFaceError(
            string rawText,
            string fallbackMessage)
        {
            if (string.IsNullOrWhiteSpace(
                rawText))
            {
                return new HuggingFaceApiException(
                    fallbackMessage
                );
            }

            string jsonText =
                ExtractJsonObject(
                    rawText
                );

            string errorMessage =
                null;

            string title =
                null;

            if (!string.IsNullOrWhiteSpace(
                jsonText))
            {
                try
                {
                    JsonNode node =
                        JsonNode.Parse(
                            jsonText
                        );

                    errorMessage =
                        node?["error"]
                            ?.ToString()
                        ??
                        node?["message"]
                            ?.ToString()
                        ??
                        node?["detail"]
                            ?.ToString();

                    title =
                        node?["title"]
                            ?.ToString();
                }
                catch
                {
                }
            }

            if (string.IsNullOrWhiteSpace(
                errorMessage))
            {
                errorMessage =
                    rawText;
            }

            // ---------------------------------------------
            // Authentication errors
            // ---------------------------------------------

            if (errorMessage.IndexOf(
                    "unauthorized",
                    StringComparison.OrdinalIgnoreCase) >= 0
                ||
                errorMessage.IndexOf(
                    "invalid token",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new HuggingFaceApiException(
                    "The Hugging Face API key is invalid or was rejected.",
                    "Hugging Face Authentication Error"
                );
            }

            // ---------------------------------------------
            // ZeroGPU quota
            // ---------------------------------------------

            if (errorMessage.IndexOf(
                    "ZeroGPU quota",
                    StringComparison.OrdinalIgnoreCase) >= 0
                ||
                errorMessage.IndexOf(
                    "quota exceeded",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ParseZeroGpuQuotaError(
                    errorMessage
                );
            }

            // ---------------------------------------------
            // Gradio dropdown error
            // ---------------------------------------------

            if (errorMessage.IndexOf(
                    "not in the list of choices",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new HuggingFaceApiException(
                    errorMessage.Trim(),
                    "Hugging Face API Error"
                );
            }

            return new HuggingFaceApiException(
                errorMessage.Trim(),
                string.IsNullOrWhiteSpace(
                    title)
                    ? "Image Generation Error"
                    : title
            );
        }
        private static string GetApiKey()
        {
            string encryptedApiKey =
                Settings1.Default.HuggingFaceAPIKeyBase64;

            // Empty ise decrypt etmeye çalışma.
            if (string.IsNullOrWhiteSpace(encryptedApiKey))
            {
                return null;
            }

            try
            {
                EncryptionHelper encryptionHelper =
                    new EncryptionHelper();

                string apiKey =
                    encryptionHelper.DecryptStringFromBase64(
                        encryptedApiKey
                    );

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return null;
                }

                return apiKey.Trim();
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // ZERO GPU ERROR
        // =========================================================

        private static HuggingFaceApiException ParseZeroGpuQuotaError(
            string errorMessage)
        {
            Match quotaMatch =
                Regex.Match(
                    errorMessage,
                    @"\((?<requested>\d+)s\s+requested\s+vs\.\s+(?<remaining>\d+)s\s+left\)",
                    RegexOptions.IgnoreCase
                );

            Match retryMatch =
                Regex.Match(
                    errorMessage,
                    @"Try\s+again\s+in\s+(?<time>\d{1,3}:\d{2}:\d{2})",
                    RegexOptions.IgnoreCase
                );

            var message =
                new StringBuilder();

            message.AppendLine(
                "The Hugging Face ZeroGPU quota has been exceeded."
            );

            if (quotaMatch.Success)
            {
                message.AppendLine();

                message.AppendLine(
                    "Requested GPU time: " +
                    quotaMatch
                        .Groups["requested"]
                        .Value +
                    " seconds"
                );

                message.AppendLine(
                    "Remaining GPU time: " +
                    quotaMatch
                        .Groups["remaining"]
                        .Value +
                    " seconds"
                );
            }

            if (retryMatch.Success)
            {
                message.AppendLine();

                message.AppendLine(
                    "You can try again in: " +
                    retryMatch
                        .Groups["time"]
                        .Value
                );
            }

            string apiKey =
    GetApiKey();

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                message.AppendLine();

                message.Append(
                    "A Hugging Face API key is already configured, " +
                    "but the available ZeroGPU quota is currently insufficient."
                );
            }
            else
            {
                message.AppendLine();

                message.Append(
                    "Configure a Hugging Face API key to use your authenticated quota."
                );
            }

            return new HuggingFaceApiException(
                message.ToString(),
                "ZeroGPU Quota Exceeded"
            );
        }

        // =========================================================
        // JSON ERROR EXTRACTION
        // =========================================================

        private static string ExtractJsonObject(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return null;
            }

            text =
                text.Trim();

            if (text.StartsWith("{") &&
                text.EndsWith("}"))
            {
                return text;
            }

            int start =
                text.IndexOf('{');

            int end =
                text.LastIndexOf('}');

            if (start >= 0 &&
                end > start)
            {
                return text.Substring(
                    start,
                    end - start + 1
                );
            }

            return null;
        }

        // =========================================================
        // HTTP ERROR FALLBACK
        // =========================================================

        private static string GetHttpFallbackMessage(
            HttpResponseMessage response,
            string fallback)
        {
            if (response == null)
            {
                return fallback;
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                return
                    "The Hugging Face API key is missing, invalid, or unauthorized.";
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Forbidden)
            {
                return
                    "The Hugging Face API key does not have permission " +
                    "to access this resource.";
            }

            return fallback +
                   "\r\n\r\nHTTP " +
                   (int)response.StatusCode +
                   " " +
                   response.ReasonPhrase;
        }

        // =========================================================
        // RESOLUTION
        // =========================================================

        private static int GetResolution(
            int width,
            int height)
        {
            int largest =
                Math.Max(
                    width,
                    height
                );

            if (largest <= 1024)
            {
                return 1024;
            }

            if (largest <= 1536)
            {
                return 1536;
            }

            return 2048;
        }

        // =========================================================
        // ASPECT RATIO
        // =========================================================

        private static string GetAspectRatio(
            int width,
            int height)
        {
            if (width <= 0 ||
                height <= 0)
            {
                return "Auto";
            }

            double requestedRatio =
                (double)width /
                height;

            string[] names =
            {
                "1:1",
                "4:3",
                "3:4",
                "3:2",
                "2:3",
                "16:9",
                "9:16"
            };

            double[] ratios =
            {
                1.0,
                4.0 / 3.0,
                3.0 / 4.0,
                3.0 / 2.0,
                2.0 / 3.0,
                16.0 / 9.0,
                9.0 / 16.0
            };

            int closestIndex =
                0;

            double closestDifference =
                Math.Abs(
                    requestedRatio -
                    ratios[0]
                );

            for (int i = 1;
                 i < ratios.Length;
                 i++)
            {
                double difference =
                    Math.Abs(
                        requestedRatio -
                        ratios[i]
                    );

                if (difference <
                    closestDifference)
                {
                    closestDifference =
                        difference;

                    closestIndex =
                        i;
                }
            }

            return names[
                closestIndex
            ];
        }
    }
}