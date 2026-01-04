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

namespace Carpathia
{
    public class ImageCreatingModel // Simulated AI Diffusion Pipeline with Image Dataset
    {
        // Path to our "Knowledge Base" (Folder containing images)
        private static string _datasetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Datasets");

        // Main entry point resembling a Diffusion Pipeline (e.g., Stable Diffusion)
        public static Image CreateImage(string prompt, int width = 1024, int height = 1024, Image inputImage = null, int inferenceSteps = 20)
        {
            // 0. Retrieval-Augmented Generation (RAG) Simulation
            // Try to find a "memory" (image) in our dataset that matches the prompt
            Bitmap referenceImage = FindMatchingImageInDataset(prompt, width, height);

            try
            {
                // 1. Text Encoder (Simulating CLIP)
                int[] promptEmbeddings = EncodeTextToEmbeddings(prompt);

                // 2. Latent Generation (Simulating VAE Encoder or Gaussian Noise)
                int[] latents = inputImage != null
                    ? EncodeImageToLatents(inputImage)
                    : GenerateRandomNoiseLatents(width, height);

                // 3. Denoising Loop (Simulating U-Net + Scheduler)
                // We pass the reference image to "guide" the denoising process
                latents = DenoiseLatents(latents, promptEmbeddings, inferenceSteps, width, height, referenceImage);

                // 4. Image Decoder (Simulating VAE Decoder)
                Image rawImage = DecodeLatentsToImage(latents, width, height);

                // 5. Post-Processing
                return ApplyPostProcessing(rawImage);
            }
            finally
            {
                // Clean up the reference image memory
                referenceImage?.Dispose();
            }
        }

        private static Bitmap FindMatchingImageInDataset(string prompt, int targetWidth, int targetHeight)
        {
            // If dataset folder doesn't exist, return null (no knowledge)
            if (!Directory.Exists(_datasetPath)) return null;

            // Split prompt into keywords (e.g., "blue ocean" -> ["blue", "ocean"])
            var words = prompt.Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);

            // Get all image files
            var files = Directory.GetFiles(_datasetPath, "*.*")
                                 .Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg"));

            foreach (var file in files)
            {
                string filename = Path.GetFileNameWithoutExtension(file);

                // Check if the filename contains any of the prompt words
                // e.g. Prompt: "sunset over sea", File: "sea_view.jpg" -> Match!
                if (words.Any(w => filename.Contains(w, StringComparison.OrdinalIgnoreCase)))
                {
                    // Load the image and resize it to match our canvas (Simulating internal feature mapping)
                    using (var original = new Bitmap(file))
                    {
                        return new Bitmap(original, targetWidth, targetHeight);
                    }
                }
            }
            return null; // No matching concept found in dataset
        }

        private static int[] EncodeTextToEmbeddings(string input)
        {
            if (string.IsNullOrEmpty(input)) return new int[0];
            return input.Select(c => (int)c * 12345).ToArray();
        }

        private static int[] EncodeImageToLatents(Image image)
        {
            Bitmap bitmap = new Bitmap(image);
            List<int> latents = new List<int>();
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    latents.Add(bitmap.GetPixel(x, y).ToArgb());
                }
            }
            return latents.ToArray();
        }

        private static int[] GenerateRandomNoiseLatents(int width, int height)
        {
            Random rnd = new Random();
            int size = width * height;
            int[] noise = new int[size];
            for (int i = 0; i < size; i++)
            {
                int val = rnd.Next(100, 200); // Soft gray noise
                noise[i] = Color.FromArgb(255, val, val, val).ToArgb();
            }
            return noise;
        }

        private static int[] DenoiseLatents(int[] latents, int[] promptEmbeddings, int steps, int width, int height, Bitmap referenceImage)
        {
            int[] currentLatents = (int[])latents.Clone();

            // Seed generator with prompt to keep procedural patterns consistent
            int seed = promptEmbeddings.Length > 0 ? promptEmbeddings.Sum() : 0;
            Random rng = new Random(seed);

            // Procedural parameters (The "Hallucination" part)
            double freqX = (rng.NextDouble() * 0.02) + 0.005;
            double freqY = (rng.NextDouble() * 0.02) + 0.005;

            for (int step = 0; step < steps; step++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int i = y * width + x;
                        Color currentPixel = Color.FromArgb(currentLatents[i]);

                        // 1. Calculate Procedural Pattern (AI "Dreaming")
                        double val = Math.Sin(x * freqX + step * 0.2) * Math.Cos(y * freqY + step * 0.2);
                        int rProc = (int)((val + 1) * 127.5);
                        int gProc = (int)((val + 1) * 127.5);
                        int bProc = (int)((val + 1) * 127.5);

                        // 2. Calculate Target Pixel (AI "Remembering")
                        int rTarget, gTarget, bTarget;

                        if (referenceImage != null)
                        {
                            // If we found a matching image in dataset, use its colors
                            Color refColor = referenceImage.GetPixel(x, y);

                            // Blend procedural noise with the real image
                            // This makes it look like the AI is "constructing" the image
                            double memoryInfluence = 0.65; // 65% Dataset, 35% Hallucination

                            rTarget = (int)(rProc * (1 - memoryInfluence) + refColor.R * memoryInfluence);
                            gTarget = (int)(gProc * (1 - memoryInfluence) + refColor.G * memoryInfluence);
                            bTarget = (int)(bProc * (1 - memoryInfluence) + refColor.B * memoryInfluence);
                        }
                        else
                        {
                            // No dataset match? Just hallucinate patterns
                            rTarget = rProc;
                            gTarget = gProc;
                            bTarget = bProc;
                        }

                        // 3. Scheduler Step (Converging towards target)
                        double alpha = 0.15; // Learning rate per step
                        int r = (int)(currentPixel.R * (1 - alpha) + rTarget * alpha);
                        int g = (int)(currentPixel.G * (1 - alpha) + gTarget * alpha);
                        int b = (int)(currentPixel.B * (1 - alpha) + bTarget * alpha);

                        // Clamp values to valid color range
                        r = Math.Clamp(r, 0, 255);
                        g = Math.Clamp(g, 0, 255);
                        b = Math.Clamp(b, 0, 255);

                        currentLatents[i] = Color.FromArgb(255, r, g, b).ToArgb();
                    }
                }
            }
            return currentLatents;
        }

        private static Image DecodeLatentsToImage(int[] latents, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height);
            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (index < latents.Length)
                    {
                        Color pixelColor = Color.FromArgb(latents[index]);
                        bitmap.SetPixel(x, y, pixelColor);
                        index++;
                    }
                }
            }
            return bitmap;
        }

        private static Image ApplyPostProcessing(Image image)
        {
            // Simple post-processing: slight contrast enhancement
            // We removed grayscale to allow dataset colors to shine through
            return new Bitmap(image);
        }
    }
}
