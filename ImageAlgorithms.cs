using System;

namespace ImageProcessorApp
{
    public static class ImageAlgorithms
    {
        #region Конвертация цветовых пространств

        public static ColorHSV RGBtoHSV(ColorRGB rgb)
        {
            ColorHSV hsv = new ColorHSV();
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            hsv.V = max;

            hsv.S = max == 0 ? 0 : delta / max;

            if (delta == 0)
            {
                hsv.H = 0;
            }
            else
            {
                if (max == r)
                    hsv.H = 60 * (((g - b) / delta) % 6);
                else if (max == g)
                    hsv.H = 60 * (((b - r) / delta) + 2);
                else
                    hsv.H = 60 * (((r - g) / delta) + 4);

                if (hsv.H < 0) hsv.H += 360;
            }

            return hsv;
        }

        public static ColorRGB HSVtoRGB(ColorHSV hsv)
        {
            ColorRGB rgb = new ColorRGB();
            double c = hsv.V * hsv.S;
            double x = c * (1 - Math.Abs((hsv.H / 60.0) % 2 - 1));
            double m = hsv.V - c;

            double r = 0, g = 0, b = 0;

            if (hsv.H >= 0 && hsv.H < 60) { r = c; g = x; b = 0; }
            else if (hsv.H >= 60 && hsv.H < 120) { r = x; g = c; b = 0; }
            else if (hsv.H >= 120 && hsv.H < 180) { r = 0; g = c; b = x; }
            else if (hsv.H >= 180 && hsv.H < 240) { r = 0; g = x; b = c; }
            else if (hsv.H >= 240 && hsv.H < 300) { r = x; g = 0; b = c; }
            else if (hsv.H >= 300 && hsv.H <= 360) { r = c; g = 0; b = x; }

            rgb.R = (byte)Math.Min(255, Math.Max(0, (r + m) * 255));
            rgb.G = (byte)Math.Min(255, Math.Max(0, (g + m) * 255));
            rgb.B = (byte)Math.Min(255, Math.Max(0, (b + m) * 255));

            return rgb;
        }

        #endregion

        #region Вспомогательное построение гистограмм

        public static int[] CalculateHistogram(ColorRGB[,] buffer, char channel)
        {
            int[] hist = new int[256];
            int height = buffer.GetLength(0);
            int width = buffer.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (channel == 'R') hist[buffer[y, x].R]++;
                    else if (channel == 'G') hist[buffer[y, x].G]++;
                    else if (channel == 'B') hist[buffer[y, x].B]++;
                    else if (channel == 'Y')
                    {
                        int brightness = (int)(0.299 * buffer[y, x].R + 0.587 * buffer[y, x].G + 0.114 * buffer[y, x].B);
                        hist[brightness]++;
                    }
                }
            }
            return hist;
        }

        #endregion

        #region Алгоритмы улучшения контраста

        public static ColorRGB[,] LinearContrast(ColorRGB[,] input)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            ColorRGB[,] output = new ColorRGB[height, width];

            byte minR = 255, maxR = 0;
            byte minG = 255, maxG = 0;
            byte minB = 255, maxB = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (input[y, x].R < minR) minR = input[y, x].R;
                    if (input[y, x].R > maxR) maxR = input[y, x].R;

                    if (input[y, x].G < minG) minG = input[y, x].G;
                    if (input[y, x].G > maxG) maxG = input[y, x].G;

                    if (input[y, x].B < minB) minB = input[y, x].B;
                    if (input[y, x].B > maxB) maxB = input[y, x].B;
                }
            }

            int divR = (maxR - minR) == 0 ? 1 : (maxR - minR);
            int divG = (maxG - minG) == 0 ? 1 : (maxG - minG);
            int divB = (maxB - minB) == 0 ? 1 : (maxB - minB);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output[y, x].R = (byte)(((input[y, x].R - minR) * 255) / divR);
                    output[y, x].G = (byte)(((input[y, x].G - minG) * 255) / divG);
                    output[y, x].B = (byte)(((input[y, x].B - minB) * 255) / divB);
                }
            }

            return output;
        }

        public static ColorRGB[,] EqualizeRGB(ColorRGB[,] input)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            int totalPixels = height * width;
            ColorRGB[,] output = new ColorRGB[height, width];

            int[] histR = CalculateHistogram(input, 'R');
            int[] histG = CalculateHistogram(input, 'G');
            int[] histB = CalculateHistogram(input, 'B');

            int[] cdfR = new int[256];
            int[] cdfG = new int[256];
            int[] cdfB = new int[256];

            cdfR[0] = histR[0];
            cdfG[0] = histG[0];
            cdfB[0] = histB[0];

            for (int i = 1; i < 256; i++)
            {
                cdfR[i] = cdfR[i - 1] + histR[i];
                cdfG[i] = cdfG[i - 1] + histG[i];
                cdfB[i] = cdfB[i - 1] + histB[i];
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output[y, x].R = (byte)((cdfR[input[y, x].R] * 255) / totalPixels);
                    output[y, x].G = (byte)((cdfG[input[y, x].G] * 255) / totalPixels);
                    output[y, x].B = (byte)((cdfB[input[y, x].B] * 255) / totalPixels);
                }
            }

            return output;
        }

        public static ColorRGB[,] EqualizeHSV(ColorRGB[,] input)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            int totalPixels = height * width;
            ColorRGB[,] output = new ColorRGB[height, width];

            ColorHSV[,] hsvBuffer = new ColorHSV[height, width];
            int[] histV = new int[256];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    hsvBuffer[y, x] = RGBtoHSV(input[y, x]);
                    int vIndex = (int)(hsvBuffer[y, x].V * 255);
                    histV[vIndex]++;
                }
            }

            int[] cdfV = new int[256];
            cdfV[0] = histV[0];
            for (int i = 1; i < 256; i++)
            {
                cdfV[i] = cdfV[i - 1] + histV[i];
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int vIndex = (int)(hsvBuffer[y, x].V * 255);
                    hsvBuffer[y, x].V = (double)cdfV[vIndex] / totalPixels;

                    output[y, x] = HSVtoRGB(hsvBuffer[y, x]);
                }
            }

            return output;
        }

        #endregion

        #region Поэлементные операции

        public static ColorRGB[,] Invert(ColorRGB[,] input)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            ColorRGB[,] output = new ColorRGB[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output[y, x].R = (byte)(255 - input[y, x].R);
                    output[y, x].G = (byte)(255 - input[y, x].G);
                    output[y, x].B = (byte)(255 - input[y, x].B);
                }
            }
            return output;
        }

        public static ColorRGB[,] GammaCorrection(ColorRGB[,] input, double gamma)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            ColorRGB[,] output = new ColorRGB[height, width];

            byte[] lut = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                double normalized = i / 255.0;
                double corrected = Math.Pow(normalized, gamma);
                lut[i] = (byte)Math.Min(255, Math.Max(0, corrected * 255));
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output[y, x].R = lut[input[y, x].R];
                    output[y, x].G = lut[input[y, x].G];
                    output[y, x].B = lut[input[y, x].B];
                }
            }
            return output;
        }

        public static ColorRGB[,] Threshold(ColorRGB[,] input, int threshold)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            ColorRGB[,] output = new ColorRGB[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int brightness = (int)(0.299 * input[y, x].R + 0.587 * input[y, x].G + 0.114 * input[y, x].B);
                    byte value = (byte)(brightness >= threshold ? 255 : 0);

                    output[y, x].R = value;
                    output[y, x].G = value;
                    output[y, x].B = value;
                }
            }
            return output;
        }

        #endregion
    }
}
