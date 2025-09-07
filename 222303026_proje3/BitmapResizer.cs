using _222303026_proje3;
using System.Drawing;

public class BitmapResizer
{
    public static Bitmap ResizeBitmap(Bitmap originalBitmap, int newWidth, int newHeight)
    {
        Bitmap resizedBitmap = new Bitmap(newWidth, newHeight);
        using (Graphics graphics = Graphics.FromImage(resizedBitmap))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(originalBitmap, 0, 0, newWidth, newHeight);
        }
        Logger.Log($"Resized bitmap from {originalBitmap.Width}x{originalBitmap.Height} to {newWidth}x{newHeight}", Logger.LogTypes.Info);
        return resizedBitmap;
    }
}
