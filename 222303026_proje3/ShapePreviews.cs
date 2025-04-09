using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _222303026_proje3
{
    public class ShapePreviews
    {
        public static Image RoundPreview(Bitmap bmp, Color color, int thickness, int startX,
            int startY, int endX, int endY)
        {
            Bitmap previewBitmap = new Bitmap(bmp); // Orijinal bitmap'in bir kopyasını oluştur
            using (Graphics graphics = Graphics.FromImage(previewBitmap)) // Kopya üzerinde çizim yap
            {
                Pen pen = new Pen(color, thickness);
                Rectangle rect = new Rectangle(
                    Math.Min(startX, endX),
                    Math.Min(startY, endY),
                    Math.Abs(endX - startX),
                    Math.Abs(endY - startY)
                );
                graphics.DrawEllipse(pen, rect);
            }
            return previewBitmap; // Güncellenmiş bitmap'i döndür
        }
        public static Image RectanglePreview(Bitmap bmp, Color color, int thickness, int startX,
            int startY, int endX, int endY)
        {
            Bitmap previewBitmap = new Bitmap(bmp); // Orijinal bitmap'in bir kopyasını oluştur
            using (Graphics graphics = Graphics.FromImage(previewBitmap)) // Kopya üzerinde çizim yap
            {
                Pen pen = new Pen(color, thickness);
                Rectangle rect = new Rectangle(
                    Math.Min(startX, endX),
                    Math.Min(startY, endY),
                    Math.Abs(endX - startX),
                    Math.Abs(endY - startY)
                );
                graphics.DrawRectangle(pen, rect);
            }
            return previewBitmap; // Güncellenmiş bitmap'i döndür
        }
    }
}
