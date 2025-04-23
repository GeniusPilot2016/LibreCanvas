using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _222303026_proje3
{
    public class Filters
    {
        public class BasicFilters
        {
            public static Bitmap MirrorEffect(Bitmap image)
            {
                Bitmap Image1 = new Bitmap(image);
                Bitmap Image2 = (Bitmap)Image1.Clone();
                Image2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                Bitmap bitmap = new Bitmap(Image1.Width+Image2.Width,Math.Max(Image1.Height, Image2.Height));
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(Image1, 0, 0);
                    g.DrawImage(Image2, Image1.Width, 0);
                }
                return bitmap;
            }
            public static Bitmap Flash(Bitmap image)
            {
                Image img = image;
                Bitmap InvertedBitmap = new Bitmap(img.Width, img.Height);
                ImageAttributes imageAttributes = new ImageAttributes();
                ColorMatrix colorMatrix = new ColorMatrix(new float[][] {
                    new float[] { 1+0.9f, 0, 0, 0, 0 },
                    new float[] { 0, 1+1.5f, 0, 0, 0 },
                    new float[] { 0, 0, 1+1.3f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                });
                imageAttributes.SetColorMatrix(colorMatrix);
                Graphics g = Graphics.FromImage(InvertedBitmap);
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imageAttributes);
                g.Dispose();
                return InvertedBitmap;
            }
        }
        public class ArtisticFilters
        {

        }
    }
}
