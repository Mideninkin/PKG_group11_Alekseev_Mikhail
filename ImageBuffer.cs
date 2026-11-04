using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImageProcessorApp
{
    public struct ColorRGB
    {
        public byte R;
        public byte G;
        public byte B;
    }

    public struct ColorHSV
    {
        public double H;
        public double S;
        public double V;
    }

    public static class ImageBuffer
    {
        public static ColorRGB[,] BitmapToBuffer(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            ColorRGB[,] buffer = new ColorRGB[height, width];

            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            int totalBytes = stride * height;
            byte[] rawBytes = new byte[totalBytes];

            Marshal.Copy(scan0, rawBytes, 0, totalBytes);
            bitmap.UnlockBits(bmpData);

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = rowOffset + x * 3;
                    buffer[y, x].B = rawBytes[pixelOffset];
                    buffer[y, x].G = rawBytes[pixelOffset + 1];
                    buffer[y, x].R = rawBytes[pixelOffset + 2];
                }
            }

            return buffer;
        }

        public static Bitmap BufferToBitmap(ColorRGB[,] buffer)
        {
            int height = buffer.GetLength(0);
            int width = buffer.GetLength(1);
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            int stride = bmpData.Stride;
            int totalBytes = stride * height;
            byte[] rawBytes = new byte[totalBytes];

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = rowOffset + x * 3;
                    rawBytes[pixelOffset] = buffer[y, x].B;
                    rawBytes[pixelOffset + 1] = buffer[y, x].G;
                    rawBytes[pixelOffset + 2] = buffer[y, x].R;
                }
            }

            Marshal.Copy(rawBytes, 0, bmpData.Scan0, totalBytes);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }
    }
}