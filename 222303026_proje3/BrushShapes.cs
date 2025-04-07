using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public class BrushShapes
{
    public static Brush CreateOilBrush(Color color, int size)
    {
        Bitmap bitmap = new Bitmap(size, size);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Random rand = new Random();

            // Create multiple layers of slightly offset, varying opacity ellipses
            for (int layer = 0; layer < 3; layer++) // Adjust number of layers for more texture
            {
                for (int i = 0; i < size * 1.5; i++) // More density
                {
                    int xOffset = rand.Next(-size / 8, size / 8);
                    int yOffset = rand.Next(-size / 8, size / 8);
                    int x = rand.Next(size / 4, 3 * size / 4) + xOffset;
                    int y = rand.Next(size / 4, 3 * size / 4) + yOffset;
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

            // Add some subtle inner shadows/highlights for depth
            for (int i = 0; i < size; i += size / 8)
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(20, Color.Black)))
                using (var highlightBrush = new SolidBrush(Color.FromArgb(20, Color.White)))
                {
                    g.FillEllipse(shadowBrush, i, i, size / 4, size / 4);
                    g.FillEllipse(highlightBrush, i + size / 8, i + size / 8, size / 4, size / 4);
                }
            }
        }
        return new TextureBrush(bitmap);
    }

    public static Brush CreateWatercolorBrush(Color color, int size)
    {
        Bitmap bitmap = new Bitmap(size, size);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Random rand = new Random();

            // Create larger, more transparent shapes
            for (int i = 0; i < size / 2; i++)
            {
                int x = rand.Next(-size / 4, size * 3 / 4);
                int y = rand.Next(-size / 4, size * 3 / 4);
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
                int x = rand.Next(size);
                int y = rand.Next(size);
                int smallSize = rand.Next(size / 8, size / 4);
                int alphaVariation = rand.Next(-30, 30);
                int clampedAlpha = Math.Max(0, Math.Min(255, 150 + alphaVariation)); // Keep alpha within bounds
                Color variationColor = Color.FromArgb(clampedAlpha, color);
                using (var brush = new SolidBrush(variationColor))
                {
                    g.FillEllipse(brush, x, y, smallSize, smallSize);
                }
            }

            // Apply a subtle Gaussian blur to simulate bleeding (requires System.Drawing.Imaging)
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            try
            {
                int stride = bmpData.Stride;
                IntPtr Scan0 = bmpData.Scan0;

                // You would need to implement a Gaussian blur algorithm here or use a library
                // This is a simplified placeholder and won't actually blur.
                // For a real implementation, consider external libraries or writing your own
                // pixel manipulation logic.
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }
        return new TextureBrush(bitmap);
    }
    public static Brush CreateCircleBrush(Color color, int size)
    {
        Bitmap bitmap = new Bitmap(size, size);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillEllipse(new SolidBrush(color), 0, 0, size, size);
        }
        return new TextureBrush(bitmap);
    }

    public static Brush CreateSquareBrush(Color color, int size)
    {
        Bitmap bitmap = new Bitmap(size, size);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillRectangle(new SolidBrush(color), 0, 0, size, size);
        }
        return new TextureBrush(bitmap);
    }

    public static Brush CreateStarBrush(Color color, int size)
    {
        Bitmap bitmap = new Bitmap(size, size);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            GraphicsPath path = new GraphicsPath();
            PointF[] points = new PointF[10];
            double angle = Math.PI / 5;
            for (int i = 0; i < 10; i++)
            {
                float r = (i % 2 == 0) ? size / 2 : size / 4;
                points[i] = new PointF(
                    (float)(size / 2 + r * Math.Cos(i * angle)),
                    (float)(size / 2 - r * Math.Sin(i * angle))
                );
            }
            path.AddPolygon(points);
            g.FillPath(new SolidBrush(color), path);
        }
        return new TextureBrush(bitmap);
    }
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

        // Add some subtle inner shadows/highlights for depth
        for (int i = 0; i < size; i += size / 8)
        {
            using (var shadowBrush = new SolidBrush(Color.FromArgb(20, Color.Black)))
            using (var highlightBrush = new SolidBrush(Color.FromArgb(20, Color.White)))
            {
                g.FillEllipse(shadowBrush, location.X + i, location.Y + i, size / 4, size / 4);
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
}
