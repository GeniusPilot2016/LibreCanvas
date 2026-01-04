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

namespace Carpathia
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
                "The RAW image format is used by many professional photographers for its high quality and flexibility.",
                "The first version of GIMP was released in 1996 as a free and open-source alternative to Photoshop.",
                "The PNG format supports transparency and was created as a replacement for GIF in 1996.",
                "The first version of Paint.NET was released in 2004 as a free image editor for Windows.",
                "The first version of Krita was released in 2005 as a free and open-source image editor for digital painting.",
                "In 2018, Adobe introduced AI-powered features in Photoshop, such as 'Select Subject' and 'Content-Aware Fill'.",
                "The first version of Affinity Photo was released in 2015 as a professional image editor for macOS and Windows.",
                "Generative Fill and similar AI image editing tools have revolutionized the way images are edited, allowing for more complex and realistic edits with less effort.",
                "The use of AI in image editing has raised ethical concerns about the authenticity and manipulation of images.",
                "Gemini Nano Banana is an AI model developed by Google DeepMind, known for its advanced capabilities in natural language processing and understanding.",
                "The integration of AI models like Gemini Nano Banana into image editing software has opened up new possibilities for creative expression and efficiency in the digital art world."
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
