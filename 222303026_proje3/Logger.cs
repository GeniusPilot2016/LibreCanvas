using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _222303026_proje3
{
    public static class Logger
    {
        // Logger class to handle logging messages to console and file that I implemented after Gemini Nano Banana is released, that after nearly abandoning the project for 4 months I decided to continue working on it (but I'm struggling to continue the project due to decision of my teacher to not exhibit the projects in the exhibition, so I don't have much motivation to continue working on it, but I'll try my best to continue working on it and finish it as soon as possible)
        // Also, I'm so sad when I see my program because of decision of my teacher and I want to exhibit it, even in the school's end of year exhibition in dream at night and it doesn't matter it's on dream or real life.

        static String LogText;
        static Logger()
        {
            LogText += "     _         _   _____          _             \r\n" +
                "    / \\   _ __| |_|  ___|   _ ___(_) ___  _ __  \r\n" +
                "   / _ \\ | '__| __| |_ | | | / __| |/ _ \\| '_ \\ \r\n" +
                "  / ___ \\| |  | |_|  _|| |_| \\__ \\ | (_) | | | |\r\n" +
                " /_/   \\_\\_|   \\__|_|   \\__,_|___/_|\\___/|_| |_|\r\n" +
                "                                                ";
            LogText += "\nNew Dimension of Digital Art \r\n";
            LogText += "\nhttps://github.com/GeniusPilot2016/ArtFusion \r\n\n";
            LogText += $"ArtFusion Version {GetInformations.GetVersionAndStatus().version} {GetInformations.GetVersionAndStatus().status}\r\n";
            LogText += GetInformations.getSystemInfo();
            string[] funFacts = new string[]
            {
                "The first digital image editor, 'Paint', was released with Windows 1.0 in 1985.",
                "Photoshop was originally called 'Display' and created by two brothers.",
                "The PNG image format was developed as a free alternative to GIF in 1996.",
                "Layers were first introduced in Photoshop 3.0 in 1994.",
                "GIMP stands for 'GNU Image Manipulation Program'.",
                "The term 'photoshopping' is now used as a verb for editing images.",
                "Some image editors can handle gigapixel images with billions of pixels.",
                "Early image editing was done on mainframe computers.",
                "Many image editors support scripting to automate tasks.",
                "The first color photo ever edited digitally was in 1957.",
                "AI-powered image editing tools are becoming increasingly popular.",
                "The TIFF format supports multiple layers and pages.",
                "The BMP file format was developed by Microsoft and IBM in the early 1990s.",
                "The first version of Adobe Photoshop was released in 1990.",
                "The GIF format supports animation and was introduced by CompuServe in 1987.",
                "The JPEG format was created by the Joint Photographic Experts Group in 1992.",
                "The RAW image format is used by many professional photographers for its high quality and flexibility."
            };
            int funFactIndex = new Random().Next(funFacts.Length);
            LogText += $"\r\nFun Fact: {funFacts[funFactIndex]}\r\n\r\n";
            Debug.WriteLine(LogText);
            WriteLogToFile(LogText.TrimEnd());
        }
        public enum LogTypes
        {
            Info,
            Warning,
            Error
        }
        public static void Log(string message, LogTypes logTypes)
        {
            string LoggingType;
            switch (logTypes)
            {
                case LogTypes.Info:
                    LoggingType = "Info";
                    break;
                case LogTypes.Warning:
                    LoggingType = "Warning";
                    break;
                case LogTypes.Error:
                    LoggingType = "Error";
                    break;
                default:
                    LoggingType = "Info";
                    break;
            }
            string logMessage = $"[{DateTime.Now:HH:mm:ss}] - [{LoggingType}] {message}";
            LogText += logMessage + "\r\n";
            WriteLogToFile(LogText);
            Debug.WriteLine(logMessage);
        }

        private static void WriteLogToFile(string content)
        {
            try
            {
                using (FileStream fs = new FileStream("DebugLog.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.Write(content);
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"File access error: {ex.Message}");
            }
        }
    }
}
