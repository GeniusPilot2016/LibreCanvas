// LibreCanvas - The AI-enabled simple image editor for everyone, born as a school project by GeniusPilot2016
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

using System.Drawing.Drawing2D;

namespace Carpathia
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
        public static void DrawTriangleOnCanvas(Bitmap bitmap, PictureBox pictureBox, Color color, int thickness, Point start, Point end)
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            using (Pen pen = new Pen(color, thickness))
            {
                Point[] points = new Point[]
                {
            new Point(start.X, end.Y),
            new Point(end.X, end.Y),
            new Point((start.X + end.X) / 2, start.Y)
                };
                g.DrawPolygon(pen, points);
            }
            pictureBox.Invalidate();
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
