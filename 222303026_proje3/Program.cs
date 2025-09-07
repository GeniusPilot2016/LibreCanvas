namespace _222303026_proje3
{
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
                    MessageBox.Show("The Google Gemini™ API key is corrupted. Please re-enter the key in the settings.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                switch (Settings1.Default.ShowRecentFiles)
                {
                    case true:
                        Application.Run(new MainForm());
                        break;
                    case false:
                        Application.Run(new ImageEditor());
                        break;
                }

            }
            catch (Exception ex)
            {
                Logger.Log($"Unhandled exception: {ex.Message}\n{ex.StackTrace}", Logger.LogTypes.Error);
                MessageBox.Show($"An unexpected error occurred:\n{ex.Message}\nThe application will now close.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Logger.Log("Application exited.", Logger.LogTypes.Info);
            }
        }
    }
}