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
        public static Image LinePreview(Bitmap bmp, Color color, int thickness, int startX,
            int startY, int endX, int endY)
        {
            Bitmap previewBitmap = new Bitmap(bmp); // Orijinal bitmap'in bir kopyasını oluştur
            using (Graphics graphics = Graphics.FromImage(previewBitmap)) // Kopya üzerinde çizim yap
            {
                Pen pen = new Pen(color, thickness);
                graphics.DrawLine(pen, startX, startY, endX, endY);
            }
            return previewBitmap; // Güncellenmiş bitmap'i döndür
        }
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
        public static Image RoundedRectanglePreview(Bitmap bmp, Color color, int thickness, int startX,
            int startY, int endX, int endY, int cornerRadius)
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
                System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(rect.X, rect.Y, cornerRadius, cornerRadius, 180, 90);
                path.AddArc(rect.X + rect.Width - cornerRadius, rect.Y, cornerRadius, cornerRadius, 270, 90);
                path.AddArc(rect.X + rect.Width - cornerRadius, rect.Y + rect.Height - cornerRadius,
                    cornerRadius, cornerRadius, 0, 90);
                path.AddArc(rect.X, rect.Y + rect.Height - cornerRadius,
                    cornerRadius, cornerRadius, 90, 90);
                path.CloseFigure();
                graphics.DrawPath(pen, path);
            }
            return previewBitmap; // Güncellenmiş bitmap'i döndür
        }
        public static Image TrianglePreview(Bitmap bmp, Color color, int thickness, int startX,
            int startY, int endX, int endY)
        {
            Bitmap previewBitmap = new Bitmap(bmp); // Orijinal bitmap'in bir kopyasını oluştur
            using (Graphics graphics = Graphics.FromImage(previewBitmap)) // Kopya üzerinde çizim yap
            {
                Pen pen = new Pen(color, thickness);
                Point[] points = new Point[]
                {
                    new Point(startX, endY),
                    new Point(endX, endY),
                    new Point((startX + endX) / 2, startY)
                };
                graphics.DrawPolygon(pen, points);
            }
            return previewBitmap; // Güncellenmiş bitmap'i döndür
        }
        public static Image HexagonPreview (Bitmap bmp, Color color, int thickness, int points, int startX,
            int startY, int endX, int endY)
        {
            Bitmap previewBitmap = new Bitmap(bmp);
            using (Graphics graphics = Graphics.FromImage(previewBitmap)) // Kopya üzerinde çizim yap
            {
                Pen pen = new Pen(color, thickness);
                Point[] hexagonPoints = new Point[points];
                double angle = 2 * Math.PI / points;
                for (int i = 0; i < points; i++)
                {
                    hexagonPoints[i] = new Point(
                        (int)(startX + (endX - startX) * Math.Cos(i * angle)),
                        (int)(startY + (endY - startY) * Math.Sin(i * angle))
                    );
                }
                graphics.DrawPolygon(pen, hexagonPoints);
            }
            return previewBitmap; // Güncellenmiş bitmap'i döndür
        }
    }
}
