// ArtFusion - The AI-enabled simple image editor for everyone, born as a school project by GeniusPilot2016
// Copyright (C) 2025 GeniusPilot2016
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Carpathia
{
    public static class CreateAIImages
    {
        // The core of the AI image generation and editing functionality, that I was implemented using Python scripts (because it didn't supported by Google.GenerativeAI package) to finish my project while I was in 3D modeling course, whille my friends were using 3D VR headsets to play VR games.
        public static async Task<Image> CreateImage(string prompt, int width, int height, string imagePath, CancellationTokenSource cancellationTokenSource = null)
        {
            try
            {
                string systemPrompt = "Do not create any NSFW, sexual, nude, or inappropriate content. Only generate safe-for-work, appropriate, and non-offensive images.";
                string promptWithSystemPrompt = $"{systemPrompt} {prompt}.";
                return await Task.Run(() =>
                {
                    try
                    {
                        string pythonExe = Path.Combine(Application.StartupPath, @"Python\python.exe");
                        string scriptPath = Path.Combine(Application.StartupPath, @"Python\AIImageCreator.py");
                        string apiKey = EncryptionHelper.DecryptString(Settings1.Default.HashedGeminiAIAPIKey);

                        var psi = new ProcessStartInfo
                        {
                            FileName = pythonExe,
                            Arguments = $"\"{scriptPath}\" \"{promptWithSystemPrompt}\" \"{apiKey}\" \"{width}\" \"{height}\" \"{imagePath}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8
                        };

                        using (var process = Process.Start(psi))
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit();

                            if (process.ExitCode != 0)
                            {
                                getError(error);
                                return null;
                            }

                            // Çıktı base64 string ise:
                            byte[] imageBytes = Convert.FromBase64String(output.Trim());
                            using (var ms = new MemoryStream(imageBytes))
                            {
                                return Image.FromStream(ms);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Error in CreateImage: " + ex.Message, Logger.LogTypes.Error);
                        MessageForm.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }, cancellationTokenSource.Token); // Düzeltme: cancellationTokenSource yerine cancellationTokenSource.Token
            }
            catch (Exception ex)
            {
                Logger.Log("Error in CreateImage: " + ex.Message, Logger.LogTypes.Error);
                MessageForm.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static async Task<Image> EditImage(Image inputImage, string prompt, CancellationTokenSource cancellationTokenSource = null)
        {
            string systemPrompt = "Do not edit image to create any NSFW, sexual, nude, or inappropriate content. Only generate safe-for-work, appropriate, and non-offensive images.";
            string promptWithSystemPrompt = $"{systemPrompt} {prompt}.";
            if (!string.IsNullOrEmpty(Settings1.Default.HashedGeminiAIAPIKey))
            {
                try
                {
                    return await Task.Run(() =>
                    {
                        try
                        {
                            string pythonExe = Path.Combine(Application.StartupPath, @"Python\python.exe");
                            string scriptPath = Path.Combine(Application.StartupPath, @"Python\AIImageEditor.py");
                            string apiKey = EncryptionHelper.DecryptString(Settings1.Default.HashedGeminiAIAPIKey);

                            // inputImage'ı geçici bir dosyaya kaydet
                            string tempImagePath = Path.GetTempFileName();
                            inputImage.Save(tempImagePath);

                            var psi = new ProcessStartInfo
                            {
                                FileName = pythonExe,
                                Arguments = $"\"{scriptPath}\" \"{tempImagePath}\" \"{promptWithSystemPrompt}\" \"{apiKey}\"",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                StandardOutputEncoding = Encoding.UTF8
                            };

                            using (var process = Process.Start(psi))
                            {
                                string output = process.StandardOutput.ReadToEnd();
                                string error = process.StandardError.ReadToEnd();
                                process.WaitForExit();

                                File.Delete(tempImagePath);

                                if (process.ExitCode != 0)
                                {
                                    getError(error);
                                    return null;
                                }

                                // Çıktı base64 string ise:
                                byte[] imageBytes = Convert.FromBase64String(output.Trim());
                                using (var ms = new MemoryStream(imageBytes))
                                {
                                    return Image.FromStream(ms);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Error in EditImage: " + ex.Message, Logger.LogTypes.Error);
                            MessageForm.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return null;
                        }
                    }, cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Logger.Log("Error in EditImage: " + ex.Message, Logger.LogTypes.Error);
                    MessageForm.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }
            else
            {
                Logger.Log("Google Gemini™ API key is not set.", Logger.LogTypes.Error);
                MessageForm.Show("Google Gemini™ API key is not set. Please set it in the settings.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
        public static bool isAPIKeyValidFormat(string APIKey)
        {
            // Google API keys typically start with "AIzaSy" followed by 33 alphanumeric characters, underscores, or hyphens
            if (string.IsNullOrWhiteSpace(APIKey))
                return false;

            // Regex pattern to match the Google API key format
            var regex = new Regex(@"AIzaSy[A-Za-z0-9_\-]{33}$");
            return regex.IsMatch(APIKey);
        }
        public static bool isImage(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                using (var img = Image.FromFile(filePath))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        private static void getError(string errorJson)
        {
            string userMessage = errorJson; // Entire error by default
            try
            {
                // If the error from Python is JSON, extract only the error_message field
                var doc = JsonDocument.Parse(errorJson);
                if (doc.RootElement.TryGetProperty("error_message", out var errorMessageElement))
                {
                    // If the error_message contains another JSON, take the first line
                    string errorMessage = errorMessageElement.GetString();
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        // Parse the first line or meaningful part if the error message in JSON format
                        int idx = errorMessage.IndexOf("message':");
                        if (idx != -1)
                        {
                            // Parse 'message': '...' section
                            int start = errorMessage.IndexOf("'", idx + 9) + 1;
                            int end = errorMessage.IndexOf("'", start);
                            if (start > 0 && end > start)
                                userMessage = errorMessage.Substring(start, end - start).Replace("\\n", Environment.NewLine);
                            else
                                userMessage = errorMessage.Replace("\\n", Environment.NewLine);
                        }
                        else
                        {
                            userMessage = errorMessage.Replace("\\n", Environment.NewLine);
                        }
                    }
                }
            }
            catch
            {
                // Show original error message if the JSON couldn't be parsed.
                userMessage = errorJson;
            }

            Logger.Log(userMessage, Logger.LogTypes.Error);
            MessageForm.Show(userMessage, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
