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

namespace _222303026_proje3
{
    public class ImageCreatingModel // Extremely primitive "Gemini Nano Banana-like" image creating model
    {
        public static Image CreateImage(string prompt, int width = 1024, int height = 1024, Image inputImage = null)
        {
            string promptHash = prompt.GetHashCode().ToString(); // Generate a hash from the prompt
            Bitmap bitmap = new Bitmap(width, height); // Create a blank bitmap with specified dimensions

            // Create a deterministic random generator based on prompt
            Random rng = new Random(prompt.GetHashCode());

            // Analyze prompt for semantic understanding
            PromptAnalysis analysis = AnalyzePrompt(prompt, rng);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Set high quality rendering (like a real model would produce)
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                // Fill background with analyzed base color
                g.Clear(analysis.BaseColor);

                // Apply multi-layered generation (simulating latent diffusion steps)
                for (int layer = 0; layer < 3; layer++)
                {
                    float layerOpacity = 1.0f - (layer * 0.2f);
                    ApplyGenerationLayer(g, analysis, width, height, rng, layer, layerOpacity);
                }

                // Apply style-specific refinements
                ApplyStyleRefinements(g, analysis, width, height, rng);

                // Add detail enhancement (simulating upscaling/refinement)
                AddDetailEnhancement(g, analysis, width, height, rng);
            }

            // If input image provided, use it as conditioning (img2img mode)
            if (inputImage != null)
            {
                ApplyImageToImageGeneration(bitmap, inputImage, analysis, rng);
            }

            // Apply final post-processing
            ApplyPostProcessing(bitmap, analysis, rng);

            return bitmap;
        }

        private static PromptAnalysis AnalyzePrompt(string prompt, Random rng)
        {
            string lowerPrompt = prompt.ToLower();
            var analysis = new PromptAnalysis();

            // Extract colors
            analysis.BaseColor = ExtractColorFromPrompt(prompt, rng);
            analysis.SecondaryColor = Color.FromArgb(
                rng.Next(256),
                rng.Next(256),
                rng.Next(256)
            );

            // Detect style keywords
            analysis.IsRealistic = lowerPrompt.Contains("realistic") || lowerPrompt.Contains("photo") || lowerPrompt.Contains("photograph");
            analysis.IsAbstract = lowerPrompt.Contains("abstract") || lowerPrompt.Contains("artistic");
            analysis.IsMinimalist = lowerPrompt.Contains("minimalist") || lowerPrompt.Contains("simple");

            // Detect subject keywords
            analysis.HasNature = lowerPrompt.Contains("nature") || lowerPrompt.Contains("landscape") ||
                                 lowerPrompt.Contains("tree") || lowerPrompt.Contains("mountain");
            analysis.HasGeometry = lowerPrompt.Contains("geometric") || lowerPrompt.Contains("pattern") ||
                                   lowerPrompt.Contains("shape");
            analysis.HasTexture = lowerPrompt.Contains("texture") || lowerPrompt.Contains("detailed");

            // Determine complexity from prompt length and keywords
            analysis.ComplexityLevel = Math.Min(10, (prompt.Length / 10) + prompt.Split(' ').Length / 3);

            // Extract mood/atmosphere
            if (lowerPrompt.Contains("bright") || lowerPrompt.Contains("vibrant") || lowerPrompt.Contains("colorful"))
                analysis.Brightness = 1.2f;
            else if (lowerPrompt.Contains("dark") || lowerPrompt.Contains("moody") || lowerPrompt.Contains("dim"))
                analysis.Brightness = 0.6f;
            else
                analysis.Brightness = 1.0f;

            // Determine pattern preference
            analysis.PatternType = Math.Abs(prompt.GetHashCode()) % 5;

            return analysis;
        }

        private static void ApplyGenerationLayer(Graphics g, PromptAnalysis analysis, int width, int height,
            Random rng, int layerIndex, float opacity)
        {
            // Simulate diffusion model's iterative refinement by applying multiple layers
            Color layerColor = BlendColors(analysis.BaseColor, analysis.SecondaryColor,
                (float)layerIndex / 3.0f);

            switch (analysis.PatternType)
            {
                case 0: // Organic shapes (nature-inspired)
                    DrawOrganicPattern(g, layerColor, width, height, rng, opacity, analysis);
                    break;
                case 1: // Geometric patterns
                    DrawGeometricPattern(g, layerColor, width, height, rng, opacity, analysis);
                    break;
                case 2: // Flow patterns (water, wind)
                    DrawFlowPattern(g, layerColor, width, height, rng, opacity);
                    break;
                case 3: // Gradient blend
                    DrawAdvancedGradient(g, analysis.BaseColor, analysis.SecondaryColor, width, height, opacity);
                    break;
                case 4: // Noise texture
                    DrawNoiseTexture(g, layerColor, width, height, rng, opacity);
                    break;
            }
        }

        private static void ApplyStyleRefinements(Graphics g, PromptAnalysis analysis, int width, int height, Random rng)
        {
            if (analysis.IsRealistic)
            {
                // Add noise for photorealistic feel
                AddRealisticNoise(g, width, height, rng);
            }

            if (analysis.HasGeometry)
            {
                // Add sharp geometric overlays
                DrawPreciseShapes(g, analysis.SecondaryColor, width, height, rng);
            }

            if (analysis.HasNature)
            {
                // Add organic elements
                DrawNaturalElements(g, analysis.BaseColor, width, height, rng);
            }
        }

        private static void AddDetailEnhancement(Graphics g, PromptAnalysis analysis, int width, int height, Random rng)
        {
            // Simulate detail enhancement pass
            int detailCount = analysis.ComplexityLevel * 5;

            using (Pen detailPen = new Pen(Color.FromArgb(30, analysis.SecondaryColor), 1))
            {
                for (int i = 0; i < detailCount; i++)
                {
                    int x1 = rng.Next(width);
                    int y1 = rng.Next(height);
                    int x2 = x1 + rng.Next(-50, 50);
                    int y2 = y1 + rng.Next(-50, 50);
                    g.DrawLine(detailPen, x1, y1, x2, y2);
                }
            }
        }

        private static void ApplyImageToImageGeneration(Bitmap output, Image input, PromptAnalysis analysis, Random rng)
        {
            // Simulate img2img by blending with variable strength based on prompt
            float strength = 0.5f; // Default 50% influence

            if (analysis.IsRealistic)
                strength = 0.7f; // More input image influence for realistic style

            using (Graphics g = Graphics.FromImage(output))
            {
                System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix
                {
                    Matrix33 = strength
                };
                System.Drawing.Imaging.ImageAttributes imgAttributes = new System.Drawing.Imaging.ImageAttributes();
                imgAttributes.SetColorMatrix(colorMatrix);

                g.DrawImage(input,
                    new Rectangle(0, 0, output.Width, output.Height),
                    0, 0, input.Width, input.Height,
                    GraphicsUnit.Pixel,
                    imgAttributes);
            }
        }

        private static void ApplyPostProcessing(Bitmap bitmap, PromptAnalysis analysis, Random rng)
        {
            // Apply brightness adjustment
            if (Math.Abs(analysis.Brightness - 1.0f) > 0.01f)
            {
                AdjustBrightness(bitmap, analysis.Brightness);
            }

            // Add subtle vignette for artistic effect
            if (analysis.IsAbstract || analysis.IsRealistic)
            {
                ApplyVignette(bitmap);
            }
        }

        private static void DrawOrganicPattern(Graphics g, Color color, int width, int height, Random rng, float opacity, PromptAnalysis analysis)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb((int)(opacity * 180), color)))
            {
                int shapeCount = 3 + analysis.ComplexityLevel;
                for (int i = 0; i < shapeCount; i++)
                {
                    // Create organic blob-like shapes
                    System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                    int points = 5 + rng.Next(8);
                    PointF[] curvePoints = new PointF[points];

                    int centerX = rng.Next(width);
                    int centerY = rng.Next(height);
                    int radius = 50 + rng.Next(150);

                    for (int j = 0; j < points; j++)
                    {
                        double angle = (2 * Math.PI * j) / points;
                        float variance = 0.7f + (float)rng.NextDouble() * 0.6f;
                        curvePoints[j] = new PointF(
                            centerX + (float)(Math.Cos(angle) * radius * variance),
                            centerY + (float)(Math.Sin(angle) * radius * variance)
                        );
                    }

                    path.AddCurve(curvePoints, 0.5f);
                    path.CloseFigure();
                    g.FillPath(brush, path);
                    path.Dispose();
                }
            }
        }

        private static void DrawGeometricPattern(Graphics g, Color color, int width, int height, Random rng, float opacity, PromptAnalysis analysis)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb((int)(opacity * 150), color)))
            using (Pen pen = new Pen(Color.FromArgb((int)(opacity * 200), color), 2))
            {
                int shapeCount = 4 + analysis.ComplexityLevel / 2;
                for (int i = 0; i < shapeCount; i++)
                {
                    int sides = 3 + rng.Next(5); // 3-7 sided polygons
                    PointF[] polygon = GeneratePolygon(
                        rng.Next(width),
                        rng.Next(height),
                        30 + rng.Next(120),
                        sides,
                        rng.Next(360));

                    g.FillPolygon(brush, polygon);
                    g.DrawPolygon(pen, polygon);
                }
            }
        }

        private static void DrawFlowPattern(Graphics g, Color color, int width, int height, Random rng, float opacity)
        {
            using (Pen pen = new Pen(Color.FromArgb((int)(opacity * 100), color), 3))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                int flowCount = 15 + rng.Next(25);
                for (int i = 0; i < flowCount; i++)
                {
                    List<PointF> flowPoints = new List<PointF>();
                    float x = rng.Next(width);
                    float y = rng.Next(height);
                    float angle = (float)(rng.NextDouble() * Math.PI * 2);

                    int steps = 20 + rng.Next(40);
                    for (int step = 0; step < steps; step++)
                    {
                        flowPoints.Add(new PointF(x, y));
                        angle += (float)((rng.NextDouble() - 0.5) * 0.3);
                        x += (float)Math.Cos(angle) * 5;
                        y += (float)Math.Sin(angle) * 5;

                        if (x < 0 || x >= width || y < 0 || y >= height)
                            break;
                    }

                    if (flowPoints.Count > 2)
                        g.DrawCurve(pen, flowPoints.ToArray(), 0.5f);
                }
            }
        }

        private static void DrawAdvancedGradient(Graphics g, Color color1, Color color2, int width, int height, float opacity)
        {
            // Multi-stop gradient for more sophisticated look
            using (System.Drawing.Drawing2D.LinearGradientBrush brush =
                new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, width, height),
                    Color.FromArgb((int)(opacity * 255), color1),
                    Color.FromArgb((int)(opacity * 255), color2),
                    45f))
            {
                System.Drawing.Drawing2D.ColorBlend blend = new System.Drawing.Drawing2D.ColorBlend();
                blend.Positions = new float[] { 0.0f, 0.5f, 1.0f };
                blend.Colors = new Color[] {
                        Color.FromArgb((int)(opacity * 255), color1),
                        Color.FromArgb((int)(opacity * 255), BlendColors(color1, color2, 0.5f)),
                        Color.FromArgb((int)(opacity * 255), color2)
                    };
                brush.InterpolationColors = blend;

                g.FillRectangle(brush, 0, 0, width, height);
            }
        }

        private static void DrawNoiseTexture(Graphics g, Color color, int width, int height, Random rng, float opacity)
        {
            // Perlin-like noise simulation
            int noiseScale = 20;
            for (int x = 0; x < width; x += noiseScale)
            {
                for (int y = 0; y < height; y += noiseScale)
                {
                    int noiseValue = rng.Next(256);
                    Color noiseColor = Color.FromArgb(
                        (int)(opacity * 50),
                        Math.Min(255, color.R + noiseValue / 2 - 64),
                        Math.Min(255, color.G + noiseValue / 2 - 64),
                        Math.Min(255, color.B + noiseValue / 2 - 64)
                    );

                    using (SolidBrush brush = new SolidBrush(noiseColor))
                    {
                        g.FillRectangle(brush, x, y, noiseScale, noiseScale);
                    }
                }
            }
        }

        private static void AddRealisticNoise(Graphics g, int width, int height, Random rng)
        {
            using (Bitmap noiseBitmap = new Bitmap(width, height))
            {
                for (int x = 0; x < width; x += 2)
                {
                    for (int y = 0; y < height; y += 2)
                    {
                        if (rng.Next(100) < 5)
                        {
                            int noise = rng.Next(30) - 15;
                            Color noiseColor = Color.FromArgb(15, 128 + noise, 128 + noise, 128 + noise);
                            noiseBitmap.SetPixel(x, y, noiseColor);
                        }
                    }
                }

                g.DrawImage(noiseBitmap, 0, 0);
            }
        }

        private static void DrawPreciseShapes(Graphics g, Color color, int width, int height, Random rng)
        {
            using (Pen pen = new Pen(Color.FromArgb(80, color), 2))
            {
                int shapeCount = 5 + rng.Next(10);
                for (int i = 0; i < shapeCount; i++)
                {
                    if (rng.Next(2) == 0)
                    {
                        g.DrawRectangle(pen, rng.Next(width - 100), rng.Next(height - 100),
                            50 + rng.Next(150), 50 + rng.Next(150));
                    }
                    else
                    {
                        g.DrawEllipse(pen, rng.Next(width - 100), rng.Next(height - 100),
                            50 + rng.Next(150), 50 + rng.Next(150));
                    }
                }
            }
        }

        private static void DrawNaturalElements(Graphics g, Color baseColor, int width, int height, Random rng)
        {
            // Tree-like or branch-like structures
            using (Pen pen = new Pen(Color.FromArgb(100, baseColor), 3))
            {
                int branches = 3 + rng.Next(5);
                for (int i = 0; i < branches; i++)
                {
                    int startX = rng.Next(width);
                    int startY = rng.Next(height);
                    DrawBranch(g, pen, startX, startY, rng.Next(360), 5 + rng.Next(3), 30 + rng.Next(50), rng);
                }
            }
        }

        private static void DrawBranch(Graphics g, Pen pen, int x, int y, float angle, int depth, int length, Random rng)
        {
            if (depth <= 0 || length < 5)
                return;

            int endX = x + (int)(Math.Cos(angle * Math.PI / 180) * length);
            int endY = y + (int)(Math.Sin(angle * Math.PI / 180) * length);

            g.DrawLine(pen, x, y, endX, endY);

            // Branch recursively
            DrawBranch(g, pen, endX, endY, angle - 25 + rng.Next(15), depth - 1, (int)(length * 0.7), rng);
            DrawBranch(g, pen, endX, endY, angle + 25 - rng.Next(15), depth - 1, (int)(length * 0.7), rng);
        }

        private static void AdjustBrightness(Bitmap bitmap, float brightness)
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                float[][] colorMatrixElements = {
                        new float[] {brightness, 0, 0, 0, 0},
                        new float[] {0, brightness, 0, 0, 0},
                        new float[] {0, 0, brightness, 0, 0},
                        new float[] {0, 0, 0, 1, 0},
                        new float[] {0, 0, 0, 0, 1}
                    };

                System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(colorMatrixElements);
                System.Drawing.Imaging.ImageAttributes attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);

                g.DrawImage(bitmap,
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    0, 0, bitmap.Width, bitmap.Height,
                    GraphicsUnit.Pixel,
                    attributes);
            }
        }

        private static void ApplyVignette(Bitmap bitmap)
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                int width = bitmap.Width;
                int height = bitmap.Height;
                System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddEllipse(0, 0, width, height);

                using (System.Drawing.Drawing2D.PathGradientBrush brush = new System.Drawing.Drawing2D.PathGradientBrush(path))
                {
                    brush.CenterColor = Color.FromArgb(0, 0, 0, 0);
                    brush.SurroundColors = new Color[] { Color.FromArgb(80, 0, 0, 0) };
                    brush.CenterPoint = new PointF(width / 2, height / 2);

                    g.FillRectangle(brush, 0, 0, width, height);
                }
                path.Dispose();
            }
        }

        private static PointF[] GeneratePolygon(int centerX, int centerY, int radius, int sides, float rotation)
        {
            PointF[] points = new PointF[sides];
            float rotationRad = rotation * (float)Math.PI / 180f;

            for (int i = 0; i < sides; i++)
            {
                float angle = rotationRad + (2f * (float)Math.PI * i / sides);
                points[i] = new PointF(
                    centerX + radius * (float)Math.Cos(angle),
                    centerY + radius * (float)Math.Sin(angle)
                );
            }

            return points;
        }

        private static Color BlendColors(Color color1, Color color2, float ratio)
        {
            ratio = Math.Max(0, Math.Min(1, ratio));
            return Color.FromArgb(
                (int)(color1.R * (1 - ratio) + color2.R * ratio),
                (int)(color1.G * (1 - ratio) + color2.G * ratio),
                (int)(color1.B * (1 - ratio) + color2.B * ratio)
            );
        }

        private static Color ExtractColorFromPrompt(string prompt, Random rng)
        {
            // Simple keyword-based color extraction
            string lowerPrompt = prompt.ToLower();

            if (lowerPrompt.Contains("red") || lowerPrompt.Contains("fire"))
                return Color.FromArgb(255, rng.Next(100), rng.Next(100));
            if (lowerPrompt.Contains("blue") || lowerPrompt.Contains("sky") || lowerPrompt.Contains("water"))
                return Color.FromArgb(rng.Next(100), rng.Next(100), 255);
            if (lowerPrompt.Contains("green") || lowerPrompt.Contains("forest") || lowerPrompt.Contains("grass"))
                return Color.FromArgb(rng.Next(100), 255, rng.Next(100));
            if (lowerPrompt.Contains("yellow") || lowerPrompt.Contains("sun") || lowerPrompt.Contains("banana"))
                return Color.FromArgb(255, 255, rng.Next(100));
            if (lowerPrompt.Contains("purple") || lowerPrompt.Contains("violet"))
                return Color.FromArgb(200, rng.Next(100), 200);
            if (lowerPrompt.Contains("black") || lowerPrompt.Contains("dark"))
                return Color.FromArgb(rng.Next(50), rng.Next(50), rng.Next(50));
            if (lowerPrompt.Contains("white") || lowerPrompt.Contains("light"))
                return Color.FromArgb(200 + rng.Next(56), 200 + rng.Next(56), 200 + rng.Next(56));

            // Default: generate color from prompt hash
            return Color.FromArgb(
                Math.Abs(prompt.GetHashCode()) % 256,
                Math.Abs(prompt.GetHashCode() * 7) % 256,
                Math.Abs(prompt.GetHashCode() * 13) % 256
            );
        }

        // Helper class to store prompt analysis results
        private class PromptAnalysis
        {
            public Color BaseColor { get; set; }
            public Color SecondaryColor { get; set; }
            public bool IsRealistic { get; set; }
            public bool IsAbstract { get; set; }
            public bool IsMinimalist { get; set; }
            public bool HasNature { get; set; }
            public bool HasGeometry { get; set; }
            public bool HasTexture { get; set; }
            public int ComplexityLevel { get; set; }
            public float Brightness { get; set; }
            public int PatternType { get; set; }
        }
    }
}
