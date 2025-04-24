using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace _222303026_proje3
{
    public static class Filters
    {
        
        public class BasicFilters
        {
            public static Bitmap MirrorEffect(Bitmap image)
            {
                Bitmap Image1 = new Bitmap(image);
                Bitmap Image2 = (Bitmap)Image1.Clone();
                Image2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                Bitmap bitmap = new Bitmap(Image1.Width + Image2.Width, Math.Max(Image1.Height, Image2.Height));
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
            public static Bitmap Frozen(Bitmap image)
            {
                Image img = image;
                Bitmap InvertedBitmap = new Bitmap(img.Width, img.Height);
                ImageAttributes imageAttributes = new ImageAttributes();
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][] {
                    new float[] { 1+0.3f, 0, 0, 0, 0 },
                    new float[] { 0, 1, 0, 0, 0 },
                    new float[] { 0, 0, 1+5f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                });
                imageAttributes.SetColorMatrix(colorMatrix);
                Graphics g = Graphics.FromImage(InvertedBitmap);
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imageAttributes);
                g.Dispose();
                return InvertedBitmap;
            }
            public static Bitmap Winter(Bitmap image)
            {
                Image img = image;
                Bitmap InvertedBitmap = new Bitmap(img.Width, img.Height);
                ImageAttributes imageAttributes = new ImageAttributes();
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][] {
                    new float[] { 1, 0, 0, 0, 0 },
                    new float[] { 0, 1, 0, 0, 0 },
                    new float[] { 0, 0, 1, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 1, 0, 1 }
                });
                imageAttributes.SetColorMatrix(colorMatrix);
                Graphics g = Graphics.FromImage(InvertedBitmap);
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imageAttributes);
                g.Dispose();
                return InvertedBitmap;
            }
            public static Bitmap BlackAndWhite(Bitmap image)
            {
                Image img = image;
                Bitmap InvertedBitmap = new Bitmap(img.Width, img.Height);
                ImageAttributes imageAttributes = new ImageAttributes();
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][]
                    {
                        new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                        new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                        new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                        new float[] { 0, 0, 0, 1, 0 },
                        new float[] { 0, 0, 0, 0, 0 }
                    });
                imageAttributes.SetColorMatrix(colorMatrix);
                Graphics g = Graphics.FromImage(InvertedBitmap);
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imageAttributes);
                g.Dispose();
                return InvertedBitmap;
            }
            public static Bitmap OldImage(Bitmap image)
            {
                Image img = image;
                Bitmap  InvertedBitmap = new Bitmap(img.Width, img.Height);
                ImageAttributes imageAttributes = new ImageAttributes();
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][]
                    {
                        new float[] {0.393f, 0.349f, 0.272f, 0, 0 },
                        new float[] {0.769f, 0.686f, 0.534f, 0, 0 },
                        new float[] {0.189f, 0.168f, 0.131f, 0, 0 },
                        new float[] {0, 0, 0, 1, 0 },
                        new float[] {0, 0, 0, 0, 1 } 
                    });
                imageAttributes.SetColorMatrix(colorMatrix);
                Graphics g = Graphics.FromImage(InvertedBitmap);
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imageAttributes);
                g.Dispose();
                return InvertedBitmap;
            }
            public static Bitmap CherryFilter(Bitmap image)
            {
                Image img = image;
                Bitmap InvertedBitmap = new Bitmap(img.Width, img.Height);
                ImageAttributes imageAttributes = new ImageAttributes();
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][]
                    {
                        new float[] { 0.393f, 0.349f, 0.272f+1.3f, 0, 0 },
                        new float[] { 0.769f, 0.686f+0.5f, 0.534f, 0, 0 },
                        new float[] { 0.189f+2.3f, 0.168f, 0.131f, 0, 0 },
                        new float[] { 0, 0, 0, 1, 0 },
                        new float[] { 0, 0, 0, 0, 1 }
                    });
                imageAttributes.SetColorMatrix(colorMatrix);
                Graphics g = Graphics.FromImage(InvertedBitmap);
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imageAttributes);
                g.Dispose();
                return InvertedBitmap;
            }
            public static Bitmap LightAdd(Bitmap image)
            {
                Image img = image;
                Bitmap InvertedBitmap = new Bitmap(img.Width, img.Height);
                ImageAttributes imageAttributes = new ImageAttributes();
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][]
                    {
                        new float[] { 0.393f, 0.349f+0.5f, 0.272f, 0, 0 },
                        new float[] { 0.769f+0.3f, 0.686f, 0.534f, 0, 0 },
                        new float[] { 0.189f, 0.168f, 0.131f+0.5f, 0, 0 },
                        new float[] { 0, 0, 0, 1, 0 },
                        new float[] { 0, 0, 0, 0, 1 }
                    });
                imageAttributes.SetColorMatrix(colorMatrix);
                Graphics g = Graphics.FromImage(InvertedBitmap);
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imageAttributes);
                g.Dispose();
                return InvertedBitmap;
            }
            public static Bitmap PurpleEffect(Bitmap image)
            {
                Image img = image;
                Bitmap InvertedBitmap = new Bitmap(img.Width, img.Height);
                ImageAttributes imageAttributes = new ImageAttributes();
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][]
                    {
                        new float[] { 0.393f+0.3f, 0.349f, 0.272f, 0, 0 },
                        new float[] { 0.769f, 0.686f+0.2f, 0.534f, 0, 0 },
                        new float[] { 0.189f, 0.168f, 0.131f+0.9f, 0, 0 },
                        new float[] { 0, 0, 0, 1, 0 },
                        new float[] { 0, 0, 0, 0, 1 }
                    });
                imageAttributes.SetColorMatrix(colorMatrix);
                Graphics g = Graphics.FromImage(InvertedBitmap);
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imageAttributes);
                g.Dispose();
                return InvertedBitmap;
            }
            public static Bitmap FogEffect(Bitmap image)
            {
                Image img = image;
                Bitmap InvertedBitmap = new Bitmap(img.Width, img.Height);
                ImageAttributes imageAttributes = new ImageAttributes();
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][]
                    {
                        new float[] { 1+0.3f, 0, 0, 0, 0 },
                        new float[] { 0, 1+0.7f, 0, 0, 0 },
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
            public class ArtisticFilters
            {
                private static bool CheckThreshold(byte[] pixelBuffer,
                                   int offset1, int offset2,
                                   ref int gradientValue,
                                   byte threshold,
                                   int divideBy = 1)
                {
                    gradientValue +=
                    Math.Abs(pixelBuffer[offset1] -
                    pixelBuffer[offset2]) / divideBy;


                    gradientValue +=
                    Math.Abs(pixelBuffer[offset1 + 1] -
                    pixelBuffer[offset2 + 1]) / divideBy;


                    gradientValue +=
                    Math.Abs(pixelBuffer[offset1 + 2] -
                    pixelBuffer[offset2 + 2]) / divideBy;


                    return (gradientValue >= threshold);
                }
                public static Bitmap GradientBasedEdgeDetectionFilter(
                                this Bitmap sourceBitmap,
                                byte threshold = 0)
                {
                    BitmapData sourceData =
                               sourceBitmap.LockBits(new Rectangle(0, 0,
                               sourceBitmap.Width, sourceBitmap.Height),
                               ImageLockMode.ReadOnly,
                               PixelFormat.Format32bppArgb);


                    byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
                    byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];


                    Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);
                    sourceBitmap.UnlockBits(sourceData);


                    int sourceOffset = 0, gradientValue = 0;
                    bool exceedsThreshold = false;


                    for (int offsetY = 1; offsetY < sourceBitmap.Height - 1; offsetY++)
                    {
                        for (int offsetX = 1; offsetX < sourceBitmap.Width - 1; offsetX++)
                        {
                            sourceOffset = offsetY * sourceData.Stride + offsetX * 4;
                            gradientValue = 0;
                            exceedsThreshold = true;


                            // Horizontal Gradient 
                            CheckThreshold(pixelBuffer,
                                           sourceOffset - 4,
                                           sourceOffset + 4,
                                           ref gradientValue, threshold, 2);
                            // Vertical Gradient 
                            exceedsThreshold =
                            CheckThreshold(pixelBuffer,
                                           sourceOffset - sourceData.Stride,
                                           sourceOffset + sourceData.Stride,
                                           ref gradientValue, threshold, 2);


                            if (exceedsThreshold == false)
                            {
                                gradientValue = 0;


                                // Horizontal Gradient 
                                exceedsThreshold =
                                CheckThreshold(pixelBuffer,
                                               sourceOffset - 4,
                                               sourceOffset + 4,
                                               ref gradientValue, threshold);


                                if (exceedsThreshold == false)
                                {
                                    gradientValue = 0;

                                    // Vertical Gradient 
                                    exceedsThreshold =
                                    CheckThreshold(pixelBuffer,
                                                   sourceOffset - sourceData.Stride,
                                                   sourceOffset + sourceData.Stride,
                                                   ref gradientValue, threshold);


                                    if (exceedsThreshold == false)
                                    {
                                        gradientValue = 0;

                                        // Diagonal Gradient : NW-SE 
                                        CheckThreshold(pixelBuffer,
                                                       sourceOffset - 4 - sourceData.Stride,
                                                       sourceOffset + 4 + sourceData.Stride,
                                                       ref gradientValue, threshold, 2);
                                        // Diagonal Gradient : NE-SW 
                                        exceedsThreshold =
                                        CheckThreshold(pixelBuffer,
                                                       sourceOffset - sourceData.Stride + 4,
                                                       sourceOffset - 4 + sourceData.Stride,
                                                       ref gradientValue, threshold, 2);


                                        if (exceedsThreshold == false)
                                        {
                                            gradientValue = 0;

                                            // Diagonal Gradient : NW-SE 
                                            exceedsThreshold =
                                            CheckThreshold(pixelBuffer,
                                                           sourceOffset - 4 - sourceData.Stride,
                                                           sourceOffset + 4 + sourceData.Stride,
                                                           ref gradientValue, threshold);


                                            if (exceedsThreshold == false)
                                            {
                                                gradientValue = 0;

                                                // Diagonal Gradient : NE-SW 
                                                exceedsThreshold =
                                                CheckThreshold(pixelBuffer,
                                                               sourceOffset - sourceData.Stride + 4,
                                                               sourceOffset + sourceData.Stride - 4,
                                                               ref gradientValue, threshold);
                                            }
                                        }
                                    }
                                }
                            }


                            resultBuffer[sourceOffset] = (byte)(exceedsThreshold ? 255 : 0);
                            resultBuffer[sourceOffset + 1] = resultBuffer[sourceOffset];
                            resultBuffer[sourceOffset + 2] = resultBuffer[sourceOffset];
                            resultBuffer[sourceOffset + 3] = 255;
                        }
                    }


                    Bitmap resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);


                    BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0,
                                            resultBitmap.Width, resultBitmap.Height),
                                            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);


                    Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);
                    resultBitmap.UnlockBits(resultData);


                    return resultBitmap;
                }
                public static Bitmap CartoonFilter(this Bitmap sourceBitmap,
                                       int levels,
                                       int filterSize,
                                       byte threshold)
                {
                    Bitmap paintFilterImage =
                           sourceBitmap.OilPaintFilter(levels, filterSize);


                    Bitmap edgeDetectImage =
                    sourceBitmap.GradientBasedEdgeDetectionFilter(threshold);


                    BitmapData paintData =
                               paintFilterImage.LockBits(new Rectangle(0, 0,
                               paintFilterImage.Width, paintFilterImage.Height),
                               ImageLockMode.ReadOnly,
                               PixelFormat.Format32bppArgb);


                    byte[] paintPixelBuffer = new byte[paintData.Stride *
                                                      paintData.Height];


                    Marshal.Copy(paintData.Scan0, paintPixelBuffer, 0,
                                               paintPixelBuffer.Length);


                    paintFilterImage.UnlockBits(paintData);


                    BitmapData edgeData =
                               edgeDetectImage.LockBits(new Rectangle(0, 0,
                               edgeDetectImage.Width, edgeDetectImage.Height),
                               ImageLockMode.ReadOnly,
                               PixelFormat.Format32bppArgb);


                    byte[] edgePixelBuffer = new byte[edgeData.Stride *
                                                     edgeData.Height];


                    Marshal.Copy(edgeData.Scan0, edgePixelBuffer, 0,
                                              edgePixelBuffer.Length);


                    edgeDetectImage.UnlockBits(edgeData);


                    byte[] resultBuffer = new byte[edgeData.Stride *
                                                     edgeData.Height];


                    for (int k = 0; k + 4 < paintPixelBuffer.Length; k += 4)
                    {
                        if (edgePixelBuffer[k] == 255 ||
                            edgePixelBuffer[k + 1] == 255 ||
                            edgePixelBuffer[k + 2] == 255)
                        {
                            resultBuffer[k] = 0;
                            resultBuffer[k + 1] = 0;
                            resultBuffer[k + 2] = 0;
                            resultBuffer[k + 3] = 255;
                        }
                        else
                        {
                            resultBuffer[k] = paintPixelBuffer[k];
                            resultBuffer[k + 1] = paintPixelBuffer[k + 1];
                            resultBuffer[k + 2] = paintPixelBuffer[k + 2];
                            resultBuffer[k + 3] = 255;
                        }
                    }


                    Bitmap resultBitmap = new Bitmap(sourceBitmap.Width,
                                                     sourceBitmap.Height);


                    BitmapData resultData =
                               resultBitmap.LockBits(new Rectangle(0, 0,
                               resultBitmap.Width, resultBitmap.Height),
                               ImageLockMode.WriteOnly,
                               PixelFormat.Format32bppArgb);


                    Marshal.Copy(resultBuffer, 0, resultData.Scan0,
                                               resultBuffer.Length);


                    resultBitmap.UnlockBits(resultData);


                    return resultBitmap;
                }
            }
        }
    }
}
