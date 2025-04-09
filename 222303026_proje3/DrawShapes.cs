using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _222303026_proje3
{
    public class DrawShapes
    {
        public static Image DrawLineOnCanvas(Bitmap bitmap, PictureBox canvas, Color color, int thickness,
            Point startPoint, Point endPoint)
        {
            if (bitmap == null)
            {
                bitmap = new Bitmap(canvas.Width, canvas.Height);
            }
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                Pen pen = new Pen(color, thickness); // Color and thickness settings
                graphics.DrawLine(pen, startPoint, endPoint);
            }
            return bitmap;
        }
        public static Image DrawRoundOnCanvas(Bitmap bitmap, PictureBox canvas, Color color,
            int thickness, Point startPoint, Point endPoint)
        {
            if (bitmap == null)
            {
                bitmap = new Bitmap(canvas.Width, canvas.Height);
            }

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                Pen pen = new Pen(color, thickness); // Color and thickness settings
                Rectangle rect = new Rectangle(
                    Math.Min(startPoint.X, endPoint.X),
                    Math.Min(startPoint.Y, endPoint.Y),
                    Math.Abs(endPoint.X - startPoint.X),
                    Math.Abs(endPoint.Y - startPoint.Y)
                );
                graphics.DrawEllipse(pen, rect);
            }

            return bitmap;
        }
        public static Image DrawRectangleOnCanvas(Bitmap bitmap, PictureBox canvas, Color color,
            int thickness, Point startPoint, Point endPoint)
        {
            if (bitmap == null)
            {
                bitmap = new Bitmap(canvas.Width, canvas.Height);
            }

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                Pen pen = new Pen(color, thickness); // Color and thickness settings
                Rectangle rect = new Rectangle(
                    Math.Min(startPoint.X, endPoint.X),
                    Math.Min(startPoint.Y, endPoint.Y),
                    Math.Abs(endPoint.X - startPoint.X),
                    Math.Abs(endPoint.Y - startPoint.Y)
                );
                graphics.DrawRectangle(pen, rect);
            }
            return bitmap;
        }
        public static Image DrawRoundedRectangleOnCanvas(Bitmap bitmap, PictureBox canvas, Color color,
            int thickness, Point startPoint, Point endPoint, int cornerRadius)
        {
            if (bitmap == null)
            {
                bitmap = new Bitmap(canvas.Width, canvas.Height);
            }
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                Pen pen = new Pen(color, thickness); // Color and thickness settings
                GraphicsPath path = new GraphicsPath();
                Rectangle rect = new Rectangle(
                    Math.Min(startPoint.X, endPoint.X),
                    Math.Min(startPoint.Y, endPoint.Y),
                    Math.Abs(endPoint.X - startPoint.X),
                    Math.Abs(endPoint.Y - startPoint.Y)
                );
                path.AddArc(rect.X, rect.Y, cornerRadius, cornerRadius, 180, 90);
                path.AddArc(rect.Right - cornerRadius, rect.Y, cornerRadius, cornerRadius, 270, 90);
                path.AddArc(rect.Right - cornerRadius, rect.Bottom - cornerRadius,
                    cornerRadius, cornerRadius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - cornerRadius,
                    cornerRadius, cornerRadius, 90, 90);
                path.CloseFigure();
                graphics.DrawPath(pen, path);
            }
            return bitmap;
        }
        public static Image DrawTriangleOnCanvas(Bitmap bitmap, PictureBox canvas, Color color,
            int thickness, Point startPoint, Point endPoint)
        {
            if (bitmap == null)
            {
                bitmap = new Bitmap(canvas.Width, canvas.Height);
            }
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                Pen pen = new Pen(color, thickness); // Color and thickness settings
                Point[] points = new Point[3];
                points[0] = startPoint;
                points[1] = new Point(endPoint.X, startPoint.Y);
                points[2] = endPoint;
                graphics.DrawPolygon(pen, points);
            }
            return bitmap;
        }
        public static Image DrawHexagonOnCanvas(Bitmap bitmap, PictureBox canvas, Color color,
            int thickness, int points, Point startPoint, Point endPoint)
        {
            if (bitmap == null)
            {
                bitmap = new Bitmap(canvas.Width, canvas.Height);
            }
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                Pen pen = new Pen(color, thickness); // Color and thickness settings
                Point[] hexagonPoints = new Point[points];
                double angle = 2 * Math.PI / points;
                for (int i = 0; i < points; i++)
                {
                    double x = startPoint.X + (endPoint.X - startPoint.X) * Math.Cos(i * angle);
                    double y = startPoint.Y + (endPoint.Y - startPoint.Y) * Math.Sin(i * angle);
                    hexagonPoints[i] = new Point((int)x, (int)y);
                }
                graphics.DrawPolygon(pen, hexagonPoints);
            }
            return bitmap;
        }
    }
}
