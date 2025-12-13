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

namespace _222303026_proje3
{
    // The program that's my old school project, a simple image editor with AI features.
    // But my teacher prohibited to exhibit our projects until he grades them, so I didn't exhibit it on my school's end of year exhibition.
    // So, I'm so sad and disappointed about that, and I was about to abandon this project until this update, after Gemini Nano Banana was released and I still don't want to share it with anyone until I exhibit it on my school's next end of year exhibition. (However, I don't know if I can exhibit it or not, because I'm graduated from that school.)
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            try
            {
                ApplicationConfiguration.Initialize();
                Logger.Log("Application started.", Logger.LogTypes.Info);
                UpgradeSettingsIfNeeded();
                try
                {
                    EncryptionHelper.DecryptString(Settings1.Default.HashedGeminiAIAPIKey);
                }
                catch (Exception)
                {
                    EncryptionHelper.ChangeKeyAndIV();
                    Settings1.Default.HashedGeminiAIAPIKey = string.Empty;
                    Settings1.Default.Save();
                    Logger.Log("The Google Gemini™ API key is corrupted. Please re-enter the key in the settings.", Logger.LogTypes.Error);
                    MessageForm.Show("The Google Gemini™ API key is corrupted. Please re-enter the key in the settings.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Application.Run(new SplashScreen()); // Start with the splash screen

            }
            catch (Exception ex)
            {
                Logger.Log($"Unhandled exception: {ex.Message}\n{ex.StackTrace}", Logger.LogTypes.Error);
                MessageForm.Show($"An unexpected error occurred:\n{ex.Message}\nThe application will now close.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Logger.Log("Application exited.", Logger.LogTypes.Info);
            }
        }
        private static void UpgradeSettingsIfNeeded()
        {
            if (!Settings1.Default.settingsUpgraded)
            {
                Settings1.Default.Upgrade(); // Upgrade user settings and cryptographic settings
                CryptographicSettings.Default.Upgrade();
                Settings1.Default.settingsUpgraded = true;
                Settings1.Default.Save();
                CryptographicSettings.Default.Save();
            }
        }
    }
}