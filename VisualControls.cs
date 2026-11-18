using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Lab4_Raster
{
    public class GridPainter : Panel
    {
        private int _cellSize = 20;
        private Point _pan = Point.Empty;
        private Point _dragStart;
        private bool _dragging;

        public VirtualGrid Grid { get; set; }
        public event Action<int, int, double> CellHovered;

        public int CellSize
        {
            get { return _cellSize; }
            set { _cellSize = Math.Max(3, Math.Min(60, value)); Invalidate(); }
        }

        public GridPainter()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
        }

        public void ResetView() { _pan = Point.Empty; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = e.Location; Cursor = Cursors.SizeAll; }
        }

        protected override void OnMouseUp(MouseEventArgs e) { _dragging = false; Cursor = Cursors.Default; }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                _pan.X += e.X - _dragStart.X;
                _pan.Y += e.Y - _dragStart.Y;
                _dragStart = e.Location;
                Invalidate();
            }
            if (Grid != null && CellHovered != null)
            {
                int ox = Width / 2 + _pan.X, oy = Height / 2 + _pan.Y;
                int gx = (int)Math.Floor((double)(e.X - ox) / _cellSize);
                int gy = (int)Math.Floor((double)(oy - e.Y) / _cellSize);
                CellHovered(gx, gy, Grid.GetPixel(gx, gy));
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            CellSize += e.Delta > 0 ? 2 : -2;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Grid == null) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;

            int ox = Width / 2 + _pan.X, oy = Height / 2 + _pan.Y;
            int minX = Math.Max(-Grid.HalfW, -ox / _cellSize - 1);
            int maxX = Math.Min(Grid.HalfW, (Width - ox) / _cellSize + 1);
            int minY = Math.Max(-Grid.HalfH, (oy - Height) / _cellSize - 1);
            int maxY = Math.Min(Grid.HalfH, oy / _cellSize + 1);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    double val = Grid.GetPixel(x, y);
                    if (val > 0)
                    {
                        int alpha = Math.Min(255, (int)Math.Round(val * 255));
                        using (SolidBrush br = new SolidBrush(Color.FromArgb(alpha, 25, 95, 210)))
                            g.FillRectangle(br, ox + x * _cellSize, oy - (y + 1) * _cellSize, _cellSize, _cellSize);
                    }
                }
            }

            if (_cellSize >= 5)
            {
                using (Pen p = new Pen(Color.FromArgb(225, 225, 225)))
                {
                    for (int x = minX; x <= maxX + 1; x++) g.DrawLine(p, ox + x * _cellSize, 0, ox + x * _cellSize, Height);
                    for (int y = minY; y <= maxY + 1; y++) g.DrawLine(p, 0, oy - y * _cellSize, Width, oy - y * _cellSize);
                }
            }

            using (Pen axis = new Pen(Color.Black, 2))
            {
                g.DrawLine(axis, 0, oy, Width, oy);
                g.DrawLine(axis, ox, 0, ox, Height);
            }

            using (Font f = new Font("Segoe UI", 8f))
            {
                g.DrawString("X", f, Brushes.Black, Width - 16, oy + 4);
                g.DrawString("Y", f, Brushes.Black, ox + 6, 4);
                g.DrawString("0", f, Brushes.Black, ox + 2, oy + 2);
                int step = _cellSize < 8 ? 10 : (_cellSize < 15 ? 5 : 1);
                for (int x = minX; x <= maxX; x++)
                    if (x != 0 && x % step == 0) g.DrawString(x.ToString(), f, Brushes.DimGray, ox + x * _cellSize + 2, oy + 2);
                for (int y = minY; y <= maxY; y++)
                    if (y != 0 && y % step == 0) g.DrawString(y.ToString(), f, Brushes.DimGray, ox + 4, oy - (y + 1) * _cellSize + 2);
            }
        }
    }

    public class ChartSeries
    {
        public string Name;
        public Color Color;
        public List<PointF> Points = new List<PointF>();
        public ChartSeries(string name, Color color) { Name = name; Color = color; }
    }

    public class BenchmarkChart : Panel
    {
        public string Title { get; set; }
        public string XLabel { get; set; }
        public List<ChartSeries> Series { get; private set; }

        public BenchmarkChart()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            Series = new List<ChartSeries>();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(58, 32, Math.Max(40, Width - 78), Math.Max(40, Height - 72));

            using (Font tf = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (Font af = new Font("Segoe UI", 8f))
            {
                g.DrawString(Title, tf, Brushes.Black, r.Left, 6);
                g.DrawString(XLabel, af, Brushes.Black, r.Left + r.Width / 2 - 45, Height - 18);
                g.DrawLine(Pens.Black, r.Left, r.Top, r.Left, r.Bottom);
                g.DrawLine(Pens.Black, r.Left, r.Bottom, r.Right, r.Bottom);

                var pts = Series.SelectMany(s => s.Points).ToList();
                if (pts.Count == 0) { g.DrawString("Запустите тест для построения графика", af, Brushes.Gray, r.Left + 15, r.Top + r.Height / 2); return; }

                float maxX = Math.Max(1f, pts.Max(p => p.X));
                float maxY = Math.Max(0.1f, pts.Max(p => p.Y) * 1.15f);

                using (Pen gp = new Pen(Color.FromArgb(230, 230, 230)))
                {
                    for (int i = 0; i <= 4; i++)
                    {
                        float k = i / 4f;
                        int py = r.Bottom - (int)(k * r.Height);
                        g.DrawLine(gp, r.Left, py, r.Right, py);
                        g.DrawString((k * maxY).ToString("F1", CultureInfo.InvariantCulture) + " мс", af, Brushes.DimGray, 4, py - 7);

                        int px = r.Left + (int)(k * r.Width);
                        g.DrawLine(gp, px, r.Top, px, r.Bottom);
                        g.DrawString(((int)(k * maxX)).ToString(), af, Brushes.DimGray, px - 14, r.Bottom + 4);
                    }
                }

                int ly = r.Top + 6;
                foreach (var s in Series)
                {
                    if (s.Points.Count == 0) continue;
                    PointF[] scr = s.Points.Select(p => new PointF(r.Left + p.X / maxX * r.Width, r.Bottom - p.Y / maxY * r.Height)).ToArray();
                    using (Pen pen = new Pen(s.Color, 2f))
                    using (SolidBrush br = new SolidBrush(s.Color))
                    {
                        if (scr.Length > 1) g.DrawLines(pen, scr);
                        foreach (var pt in scr) g.FillEllipse(br, pt.X - 3, pt.Y - 3, 6, 6);
                        g.FillRectangle(br, r.Left + 10, ly + 3, 12, 8);
                        g.DrawString(s.Name, af, Brushes.Black, r.Left + 26, ly);
                        ly += 15;
                    }
                }
            }
        }
    }
}
