using System;
using System.Collections.Generic;
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
        }
        public class ArtisticFilters
        {

        }
    }
}
