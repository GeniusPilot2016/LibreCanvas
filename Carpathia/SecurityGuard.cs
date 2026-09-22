using System.Drawing;
using System.Net.Http.Headers;

namespace Carpathia
{
    internal static class SecurityGuard
    {
        public const long MaxLocalImageBytes = 100L * 1024 * 1024;
        public const long MaxRemoteImageBytes = 40L * 1024 * 1024;
        public const long MaxPixels = 100_000_000L;
        public const int MaxDimension = 20_000;

        public static Bitmap LoadLocalImage(string fileName)
        {
            var fi = new FileInfo(fileName);
            if (!fi.Exists) throw new FileNotFoundException("Image file was not found.", fileName);
            if (fi.Length <= 0 || fi.Length > MaxLocalImageBytes)
                throw new InvalidDataException("Image file is empty or exceeds the 100 MB safety limit.");

            using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var image = Image.FromStream(fs, useEmbeddedColorManagement: true, validateImageData: true);
            ValidateDimensions(image.Width, image.Height);
            return new Bitmap(image);
        }

        public static void ValidateDimensions(int width, int height)
        {
            if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension || (long)width * height > MaxPixels)
                throw new InvalidDataException($"Image dimensions exceed the safety limit ({MaxDimension}px per side / {MaxPixels:N0} pixels).");
        }

        public static bool IsAllowedImageContentType(MediaTypeHeaderValue? contentType)
        {
            if (contentType?.MediaType == null) return false;
            return contentType.MediaType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
                || contentType.MediaType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
                || contentType.MediaType.Equals("image/webp", StringComparison.OrdinalIgnoreCase)
                || contentType.MediaType.Equals("image/gif", StringComparison.OrdinalIgnoreCase)
                || contentType.MediaType.Equals("image/bmp", StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<byte[]> ReadBoundedAsync(HttpContent content, long maxBytes, CancellationToken ct)
        {
            if (content.Headers.ContentLength is long len && (len <= 0 || len > maxBytes))
                throw new InvalidDataException("Remote image exceeds the download safety limit.");

            await using var input = await content.ReadAsStreamAsync(ct);
            using var output = new MemoryStream();
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                total += read;
                if (total > maxBytes) throw new InvalidDataException("Remote image exceeds the download safety limit.");
                output.Write(buffer, 0, read);
            }
            return output.ToArray();
        }
    }
}
