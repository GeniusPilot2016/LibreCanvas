using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;
using System.Drawing;
using System.IO;

namespace _222303026_proje3
{
    public static class CreateAIImages
    {
        public static async Task<Image> CreateImage(string prompt, int width, int height)
        {
            try
            {
                string systemPrompt = "Do not create any NSFW, sexual, nude, or inappropriate content. Only generate safe-for-work, appropriate, and non-offensive images.";
                string promptWithSystemPrompt = $"{systemPrompt} {prompt}.";
                return await Task.Run(() =>
                {
                    try
                    {
                        Application.DoEvents();
                        string pythonExe = Path.Combine(Application.StartupPath, @"Python\python.exe");
                        string scriptPath = Path.Combine(Application.StartupPath, @"Python\AIImageCreator.py");
                        string apiKey = EncryptionHelper.DecryptString(Settings1.Default.HashedGeminiAIAPIKey);

                        var psi = new ProcessStartInfo
                        {
                            FileName = pythonExe,
                            Arguments = $"\"{scriptPath}\" \"{promptWithSystemPrompt}\" \"{apiKey}\" \"{width}\" \"{height}\"",
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
                                Logger.Log("Python script error: " + error, Logger.LogTypes.Error);
                                MessageBox.Show("Python error: " + error, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    catch(Exception ex)
                    {
                        Logger.Log("Error in CreateImage: " + ex.Message, Logger.LogTypes.Error);
                        MessageBox.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log("Error in CreateImage: " + ex.Message, Logger.LogTypes.Error);
                MessageBox.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static async Task<Image> EditImage(Image inputImage, string prompt)
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
                            Application.DoEvents();
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
                                    Logger.Log("Python script error: " + error, Logger.LogTypes.Error);
                                    MessageBox.Show("Python error: " + error, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        catch(Exception ex)
                        {
                            Logger.Log("Error in EditImage: " + ex.Message, Logger.LogTypes.Error);
                            MessageBox.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return null;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.Log("Error in EditImage: " + ex.Message, Logger.LogTypes.Error);
                    MessageBox.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }
            else
            {
                Logger.Log("Google Gemini™ API key is not set.", Logger.LogTypes.Error);
                MessageBox.Show("Google Gemini™ API key is not set. Please set it in the settings.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }
}
