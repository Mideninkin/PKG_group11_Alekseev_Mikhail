using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lab4_Raster
{
    public interface IRasterTarget
    {
        void SetPixel(int x, int y, double intensity);
    }

    public class VirtualGrid : IRasterTarget
    {
        private readonly double[,] _cells;
        public int HalfW { get; private set; }
        public int HalfH { get; private set; }
        public bool Blend { get; set; }

        public VirtualGrid(int halfW, int halfH)
        {
            HalfW = halfW;
            HalfH = halfH;
            _cells = new double[halfW * 2 + 1, halfH * 2 + 1];
            Blend = true;
        }

        public void Clear()
        {
            Array.Clear(_cells, 0, _cells.Length);
        }

        public void SetPixel(int x, int y, double intensity)
        {
            int ix = x + HalfW, iy = y + HalfH;
            if (ix < 0 || ix >= _cells.GetLength(0) || iy < 0 || iy >= _cells.GetLength(1) || intensity <= 0) return;
            if (intensity > 1.0) intensity = 1.0;
            _cells[ix, iy] = Blend ? Math.Max(_cells[ix, iy], intensity) : intensity;
        }

        public double GetPixel(int x, int y)
        {
            int ix = x + HalfW, iy = y + HalfH;
            if (ix < 0 || ix >= _cells.GetLength(0) || iy < 0 || iy >= _cells.GetLength(1)) return 0;
            return _cells[ix, iy];
        }
    }

    public static class RasterEngine
    {
        public static void StepByStepLine(int x1, int y1, int x2, int y2, IRasterTarget target, List<string> log = null)
        {
            if (x1 == x2 && y1 == y2) { target.SetPixel(x1, y1, 1.0); return; }
            int dx = x2 - x1, dy = y2 - y1;

            if (Math.Abs(dx) >= Math.Abs(dy))
            {
                double k = (double)dy / dx, b = y1 - k * x1;
                int step = dx > 0 ? 1 : -1;
                if (log != null) log.Add(string.Format(CultureInfo.InvariantCulture, "Пошаговый (ось X): k={0:F3}, b={1:F3}", k, b));
                for (int x = x1; ; x += step)
                {
                    double exactY = k * x + b;
                    int y = (int)Math.Round(exactY, MidpointRounding.AwayFromZero);
                    target.SetPixel(x, y, 1.0);
                    if (log != null && log.Count < 120) log.Add(string.Format(CultureInfo.InvariantCulture, "x={0}, y_точн={1:F2} -> ({0},{2})", x, exactY, y));
                    if (x == x2) break;
                }
            }
            else
            {
                double invK = (double)dx / dy, b = x1 - invK * y1;
                int step = dy > 0 ? 1 : -1;
                if (log != null) log.Add(string.Format(CultureInfo.InvariantCulture, "Пошаговый (ось Y): 1/k={0:F3}, bx={1:F3}", invK, b));
                for (int y = y1; ; y += step)
                {
                    double exactX = invK * y + b;
                    int x = (int)Math.Round(exactX, MidpointRounding.AwayFromZero);
                    target.SetPixel(x, y, 1.0);
                    if (log != null && log.Count < 120) log.Add(string.Format(CultureInfo.InvariantCulture, "y={0}, x_точн={1:F2} -> ({2},{0})", y, exactX, x));
                    if (y == y2) break;
                }
            }
        }

        public static void DDALine(int x1, int y1, int x2, int y2, IRasterTarget target, List<string> log = null)
        {
            int dx = x2 - x1, dy = y2 - y1;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (steps == 0) { target.SetPixel(x1, y1, 1.0); return; }

            double xInc = (double)dx / steps, yInc = (double)dy / steps;
            double x = x1, y = y1;
            if (log != null) log.Add(string.Format(CultureInfo.InvariantCulture, "ЦДА: L={0}, dx_step={1:F3}, dy_step={2:F3}", steps, xInc, yInc));

            for (int i = 0; i <= steps; i++)
            {
                int px = (int)Math.Round(x, MidpointRounding.AwayFromZero);
                int py = (int)Math.Round(y, MidpointRounding.AwayFromZero);
                target.SetPixel(px, py, 1.0);
                if (log != null && log.Count < 120) log.Add(string.Format(CultureInfo.InvariantCulture, "Шаг {0}: ({1:F2}, {2:F2}) -> ({3},{4})", i, x, y, px, py));
                x += xInc;
                y += yInc;
            }
        }

        public static void BresenhamLine(int x1, int y1, int x2, int y2, IRasterTarget target, List<string> log = null)
        {
            int dx = Math.Abs(x2 - x1), dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1, sy = y1 < y2 ? 1 : -1;
            int err = dx - dy, x = x1, y = y1, step = 0;
            if (log != null) log.Add(string.Format("Брезенхем: dx={0}, dy={1}, sx={2}, sy={3}, err0={4}", dx, dy, sx, sy, err));

            while (true)
            {
                target.SetPixel(x, y, 1.0);
                if (log != null && log.Count < 120) log.Add(string.Format("Шаг {0}: ({1},{2}), err={3}", step++, x, y, err));
                if (x == x2 && y == y2) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
        }

        public static void BresenhamCircle(int xc, int yc, int r, IRasterTarget target, List<string> log = null)
        {
            if (r < 0) return;
            int x = 0, y = r, d = 3 - 2 * r, step = 0;
            if (log != null) log.Add(string.Format("Окружность Брезенхема: центр ({0},{1}), R={2}, d0={3}", xc, yc, r, d));

            while (y >= x)
            {
                target.SetPixel(xc + x, yc + y, 1.0); target.SetPixel(xc - x, yc + y, 1.0);
                target.SetPixel(xc + x, yc - y, 1.0); target.SetPixel(xc - x, yc - y, 1.0);
                target.SetPixel(xc + y, yc + x, 1.0); target.SetPixel(xc - y, yc + x, 1.0);
                target.SetPixel(xc + y, yc - x, 1.0); target.SetPixel(xc - y, yc - x, 1.0);

                if (log != null && log.Count < 120) log.Add(string.Format("Шаг {0}: ({1},{2}), d={3} -> 8 точек", step++, x, y, d));
                x++;
                if (d > 0) { y--; d += 4 * (x - y) + 10; }
                else { d += 4 * x + 6; }
            }
        }

        public static void WuLine(int x0, int y0, int x1, int y1, IRasterTarget target, List<string> log = null)
        {
            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (steep) { Swap(ref x0, ref y0); Swap(ref x1, ref y1); }
            if (x0 > x1) { Swap(ref x0, ref x1); Swap(ref y0, ref y1); }

            double dx = x1 - x0, dy = y1 - y0;
            double grad = (dx == 0.0) ? 1.0 : dy / dx;
            if (log != null) log.Add(string.Format(CultureInfo.InvariantCulture, "Алгоритм Ву: steep={0}, grad={1:F3}", steep, grad));

            PlotWu(steep, x0, y0, 1.0, target);
            PlotWu(steep, x1, y1, 1.0, target);
            double intery = y0 + grad;

            for (int x = x0 + 1; x < x1; x++)
            {
                int ipart = (int)Math.Floor(intery);
                double fpart = intery - ipart;
                PlotWu(steep, x, ipart, 1.0 - fpart, target);
                PlotWu(steep, x, ipart + 1, fpart, target);
                if (log != null && log.Count < 120)
                    log.Add(string.Format(CultureInfo.InvariantCulture, "x={0}, y={1:F2} -> Alpha1={2:F2}, Alpha2={3:F2}", x, intery, 1.0 - fpart, fpart));
                intery += grad;
            }
        }

        private static void PlotWu(bool steep, int x, int y, double a, IRasterTarget t)
        {
            if (steep) t.SetPixel(y, x, a); else t.SetPixel(x, y, a);
        }

        
        public static void CastlePitewayCurve(int x0, int y0, int x1, int y1, int x2, int y2, IRasterTarget target, List<string> log = null)
        {
            if (log != null) log.Add(string.Format("Кастл-Питвей: P0({0},{1}), P1({2},{3}), P2({4},{5})", x0, y0, x1, y1, x2, y2));
            SubdivideCastle(x0, y0, x1, y1, x2, y2, target, log, 0);
        }

        private static void SubdivideCastle(int x0, int y0, int x1, int y1, int x2, int y2, IRasterTarget t, List<string> log, int depth)
        {
            if ((Math.Abs(x1 - x0) <= 1 && Math.Abs(y1 - y0) <= 1 && Math.Abs(x2 - x1) <= 1 && Math.Abs(y2 - y1) <= 1) || depth > 14)
            {
                t.SetPixel(x0, y0, 1.0);
                t.SetPixel(x2, y2, 1.0);
                if (log != null && log.Count < 120) log.Add(string.Format("Глубина {0}: пиксели ({1},{2}) и ({3},{4})", depth, x0, y0, x2, y2));
                return;
            }
            int x01 = (x0 + x1) >> 1, y01 = (y0 + y1) >> 1;
            int x12 = (x1 + x2) >> 1, y12 = (y1 + y2) >> 1;
            int xm = (x01 + x12 + 1) >> 1, ym = (y01 + y12 + 1) >> 1;

            SubdivideCastle(x0, y0, x01, y01, xm, ym, t, log, depth + 1);
            SubdivideCastle(xm, ym, x12, y12, x2, y2, t, log, depth + 1);
        }

        private static void Swap(ref int a, ref int b) { int t = a; a = b; b = t; }
    }
}
