using _222303026_proje3.Properties;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _222303026_proje3
{
    public class UIFonts
    {
        PrivateFontCollection privateFonts = new PrivateFontCollection();
        public UIFonts()
        {
            string blackFontFile = Path.GetTempFileName();
            File.WriteAllBytes(blackFontFile, Resources.HarmonyOS_Sans_Black);
            privateFonts.AddFontFile(blackFontFile);
            string blackItalicFontFile = Path.GetTempFileName();
            File.WriteAllBytes(blackItalicFontFile, Resources.HarmonyOS_Sans_Black_Italic);
            privateFonts.AddFontFile(blackItalicFontFile);
            string boldFontFile = Path.GetTempFileName();
            File.WriteAllBytes(boldFontFile, Resources.HarmonyOS_Sans_Bold);
            privateFonts.AddFontFile(boldFontFile);
            string boldItalicFontFile = Path.GetTempFileName();
            File.WriteAllBytes(boldItalicFontFile, Resources.HarmonyOS_Sans_Bold_Italic);
            privateFonts.AddFontFile(boldItalicFontFile);
            string lightFontFile = Path.GetTempFileName();
            File.WriteAllBytes(lightFontFile, Resources.HarmonyOS_Sans_Light);
            privateFonts.AddFontFile(lightFontFile);
            string lightItalicFontFile = Path.GetTempFileName();
            File.WriteAllBytes(lightItalicFontFile, Resources.HarmonyOS_Sans_Light_Italic);
            privateFonts.AddFontFile(lightItalicFontFile);
            string mediumFontFile = Path.GetTempFileName();
            File.WriteAllBytes(mediumFontFile, Resources.HarmonyOS_Sans_Medium);
            privateFonts.AddFontFile(mediumFontFile);
            string mediumItalicFontFile = Path.GetTempFileName();
            File.WriteAllBytes(mediumItalicFontFile, Resources.HarmonyOS_Sans_Medium_Italic);
            privateFonts.AddFontFile(mediumItalicFontFile);
            string regularFontFile = Path.GetTempFileName();
            File.WriteAllBytes(regularFontFile, Resources.HarmonyOS_Sans_Regular);
            privateFonts.AddFontFile(regularFontFile);
            string thinFontFile = Path.GetTempFileName();
            File.WriteAllBytes(thinFontFile, Resources.HarmonyOS_Sans_Thin);
            privateFonts.AddFontFile(thinFontFile);
            string thinItalicFontFile = Path.GetTempFileName();
            File.WriteAllBytes(thinItalicFontFile, Resources.HarmonyOS_Sans_Thin_Italic);
            privateFonts.AddFontFile(thinItalicFontFile);
        }
        public Font SetUIFont(float size, FontStyle style)
        {
            Font font = new Font(privateFonts.Families[0], size, style);
            return font;
        }
        public Font SetUIFont(float size)
        {
            Font font = new Font(privateFonts.Families[0], size);
            return font;
        }
    }
}
