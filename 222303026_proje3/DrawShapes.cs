using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _222303026_proje3
{
    public class DrawShapes
    {
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

    }
}
