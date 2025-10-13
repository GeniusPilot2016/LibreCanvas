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

using System.Drawing.Drawing2D;
public class BrushShapes
{
    public static void DrawOilBrush(Graphics g, Color color, int size, Point location)
    {
        Random rand = new Random();

        // Create multiple layers of slightly offset, varying opacity ellipses
        for (int layer = 0; layer < 3; layer++) // Adjust number of layers for more texture
        {
            for (int i = 0; i < size * 1.5; i++) // More density
            {
                int xOffset = rand.Next(-size / 8, size / 8);
                int yOffset = rand.Next(-size / 8, size / 8);
                int x = location.X + rand.Next(size / 4, 3 * size / 4) + xOffset;
                int y = location.Y + rand.Next(size / 4, 3 * size / 4) + yOffset;
                int width = rand.Next(size / 3, size / 2);
                int height = rand.Next(size / 3, size / 2);
                int alpha = rand.Next(150, 255); // Vary opacity
                using (var brush = new SolidBrush(Color.FromArgb(alpha, color)))
                {
                    // Slightly rotate ellipses for a more organic feel
                    g.TranslateTransform(x + width / 2, y + height / 2);
                    g.RotateTransform((float)(rand.NextDouble() * 20 - 10));
                    g.FillEllipse(brush, -width / 2, -height / 2, width, height);
                    g.ResetTransform();
                }
            }
        }
        double brightness = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;
        int shadowAdjustment = (int)(brightness * 0.1); // Set shadow adjustment based on brightness
        // Shadow color
        Color shadowColor = Color.FromArgb(255,
            Math.Max(0, color.R - shadowAdjustment),
            Math.Max(0, color.G - shadowAdjustment),
            Math.Max(0, color.B - shadowAdjustment));

        // Highlight color
        int highlightAdjustment = (int)((255 - brightness) * 0.1); // Set highlight adjustment based on brightness
        Color highlightColor = Color.FromArgb(255,
            Math.Min(255, color.R + highlightAdjustment),
            Math.Min(255, color.G + highlightAdjustment),
            Math.Min(255, color.B + highlightAdjustment));
        // Add some subtle inner shadows/highlights for depth
        for (int i = 0; i < size; i += size / 8)
        {
            using (var shadowBrush = new SolidBrush(Color.FromArgb(20, shadowColor)))
            using (var highlightBrush = new SolidBrush(Color.FromArgb(20, highlightColor)))
            {
                g.FillEllipse(shadowBrush, location.X + i, location.Y + i, size / 4, size / 4);
                g.FillEllipse(highlightBrush, location.X + i + size / 8, location.Y + i + size / 8, size / 4, size / 4);
            }
        }
        // Gölge ve aydýnlatma için bulanýklýk eklenmiþ versiyon
        for (int i = 0; i < size; i += size / 8)
        {
            using (var shadowBrush = new SolidBrush(Color.FromArgb(15, shadowColor)))
            using (var highlightBrush = new SolidBrush(Color.FromArgb(15, highlightColor)))
            {
                // Gölge için daha büyük ve daha þeffaf elipsler
                g.FillEllipse(shadowBrush, location.X + i - size / 16, location.Y + i - size / 16, size / 3, size / 3);
                g.FillEllipse(shadowBrush, location.X + i, location.Y + i, size / 4, size / 4);

                // Aydýnlatma için daha büyük ve daha þeffaf elipsler
                g.FillEllipse(highlightBrush, location.X + i + size / 8 - size / 16, location.Y + i + size / 8 - size / 16, size / 3, size / 3);
                g.FillEllipse(highlightBrush, location.X + i + size / 8, location.Y + i + size / 8, size / 4, size / 4);
            }
        }
    }

    public static void DrawWatercolorBrush(Graphics g, Color color, int size, Point location)
    {
        Random rand = new Random();

        // Create larger, more transparent shapes
        for (int i = 0; i < size / 2; i++)
        {
            int x = location.X + rand.Next(-size / 4, size * 3 / 4);
            int y = location.Y + rand.Next(-size / 4, size * 3 / 4);
            int width = rand.Next(size / 2, size);
            int height = rand.Next(size / 2, size);
            int alpha = rand.Next(80, 180); // Higher transparency
            using (var brush = new SolidBrush(Color.FromArgb(alpha, color)))
            {
                g.FillEllipse(brush, x, y, width, height);
            }
        }

        // Add some smaller, slightly darker/lighter variations for texture
        for (int i = 0; i < size; i++)
        {
            int x = location.X + rand.Next(size);
            int y = location.Y + rand.Next(size);
            int smallSize = rand.Next(size / 8, size / 4);
            int alphaVariation = rand.Next(-30, 30);
            int clampedAlpha = Math.Max(0, Math.Min(255, 150 + alphaVariation)); // Keep alpha within bounds
            Color variationColor = Color.FromArgb(clampedAlpha, color);
            using (var brush = new SolidBrush(variationColor))
            {
                g.FillEllipse(brush, x, y, smallSize, smallSize);
            }
        }
    }

    public static void DrawCircleBrush(Graphics g, Color color, int size, Point location)
    {
        using (var brush = new SolidBrush(color))
        {
            g.FillEllipse(brush, location.X, location.Y, size, size);
        }
    }

    public static void DrawSquareBrush(Graphics g, Color color, int size, Point location)
    {
        using (var brush = new SolidBrush(color))
        {
            g.FillRectangle(brush, location.X, location.Y, size, size);
        }
    }
    public static void DrawCalligraphyBrush(Graphics g, Color color, int size, Point location)
    {
        using (var brush = new SolidBrush(color))
        {
            GraphicsPath path = new GraphicsPath();
            path.AddLine(location.X, location.Y, location.X + size, location.Y + size / 2);
            path.AddLine(location.X + size, location.Y + size / 2, location.X, location.Y + size);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }
    public static void DrawCrayonBrush(Graphics g, Color color, int size, Point location)
    {
        Random rand = new Random();
        using (var brush = new SolidBrush(color))
        {
            for (int i = 0; i < size * 10; i++)
            {
                int x = location.X + rand.Next(-size / 2, size / 2);
                int y = location.Y + rand.Next(-size / 2, size / 2);
                g.FillRectangle(brush, x, y, 1, 1);
            }
        }
    }
    public static void DrawMarkerBrush(Graphics g, Color color, int size, Point location)
    {
        Random rand = new Random();
        using (var brush = new SolidBrush(Color.FromArgb(180, color)))
        {
            for (int i = 0; i < size * 5; i++)
            {
                int x = location.X + rand.Next(-size / 4, size / 4);
                int y = location.Y + rand.Next(-size / 4, size / 4);
                int width = rand.Next(size / 4, size / 2);
                int height = rand.Next(size / 4, size / 2);
                g.FillRectangle(brush, x, y, width, height);
            }
        }
    }
    public static void DrawSprayBrush(Graphics graphics, Color color, int size, Point location)
    {
        Random rand = new Random();
        for (int i = 0; i < size * 10; i++)
        {
            int offsetX = rand.Next(-size, size);
            int offsetY = rand.Next(-size, size);
            if (offsetX * offsetX + offsetY * offsetY <= size * size)
            {
                graphics.FillRectangle(new SolidBrush(color), location.X + offsetX, location.Y + offsetY, 1, 1);
            }
        }
    }
}