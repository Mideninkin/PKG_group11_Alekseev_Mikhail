using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace Lab4_Raster
{
    public class MainForm : Form
    {
        private readonly VirtualGrid _grid = new VirtualGrid(150, 150);
        private GridPainter _painter;
        private ComboBox _cmbAlg;
        private NumericUpDown _x1, _y1, _x2, _y2, _x3, _y3, _rad;
        private CheckBox _chkKeep;
        private TextBox _txtLog, _txtBench;
        private Label _lblZoom, _lblHover;
        private BenchmarkChart _chartCount, _chartLen;

        public MainForm()
        {
            Text = "ЛР №4 — Растровые алгоритмы";
            Size = new Size(1220, 760);
            MinimumSize = new Size(1050, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BuildUI();
            ExecuteDraw();
        }

                private void BuildUI()
        {
            TabControl tabs = new TabControl { Dock = DockStyle.Fill };

            TabPage t1 = new TabPage("Визуализация и вычисления");

            Panel left = new Panel
            {
                Dock = DockStyle.Left,
                Width = 365,
                BackColor = Color.FromArgb(245, 246, 248),
                BorderStyle = BorderStyle.FixedSingle
            };

            _cmbAlg = new ComboBox { Location = new Point(12, 12), Width = 338, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbAlg.Items.AddRange(new object[] {
                "1. Пошаговый алгоритм",
                "2. Алгоритм ЦДА",
                "3. Алгоритм Брезенхема (отрезок, 8 октантов)",
                "4. Алгоритм Брезенхема (окружность)",
                "5. Алгоритм Ву (сглаживание)",
                "6. Алгоритм Кастла-Питвея (кривая P0-P1-P2)"
            });
            _cmbAlg.SelectedIndex = 2;
            _cmbAlg.SelectedIndexChanged += (s, e) => UpdateFields();
            left.Controls.Add(_cmbAlg);

            _x1 = AddNum(left, "X1 (Xc):", -10, 12, 44);
            _y1 = AddNum(left, "Y1 (Yc):", -6, 190, 44);
            _x2 = AddNum(left, "X2 (P1.X):", 14, 12, 86);
            _y2 = AddNum(left, "Y2 (P1.Y):", 9, 190, 86);
            _x3 = AddNum(left, "P2.X (Кастл):", 18, 12, 128);
            _y3 = AddNum(left, "P2.Y (Кастл):", -8, 190, 128);
            _rad = AddNum(left, "Радиус R:", 11, 12, 170);
            _rad.Minimum = 0;

            _chkKeep = new CheckBox { Text = "Наложение примитивов", Location = new Point(190, 188), AutoSize = true };
            left.Controls.Add(_chkKeep);

            Button btnDraw = new Button { Text = "Построить", Location = new Point(12, 220), Width = 164, Height = 30 };
            btnDraw.Click += (s, e) => ExecuteDraw();
            Button btnClear = new Button { Text = "Очистить", Location = new Point(186, 220), Width = 164, Height = 30 };
            btnClear.Click += (s, e) => { _grid.Clear(); _painter.Invalidate(); _txtLog.Clear(); };
            left.Controls.Add(btnDraw);
            left.Controls.Add(btnClear);

            _lblZoom = new Label { Text = "Масштаб: 20 px", Location = new Point(12, 260), AutoSize = true };
            TrackBar zoom = new TrackBar { Location = new Point(12, 278), Width = 245, Minimum = 4, Maximum = 50, Value = 20, TickFrequency = 5 };
            zoom.ValueChanged += (s, e) => { _painter.CellSize = zoom.Value; _lblZoom.Text = "Масштаб: " + zoom.Value + " px"; };
            Button btnCenter = new Button { Text = "Центр", Location = new Point(265, 282), Width = 85, Height = 26 };
            btnCenter.Click += (s, e) => _painter.ResetView();
            left.Controls.AddRange(new Control[] { _lblZoom, zoom, btnCenter });

            Label lblBind = new Label {
                Text = "Привязка: пиксель (x, y) занимает клетку [x, x+1]×[y, y+1] (центр в x+0.5, y+0.5). Колесо — зум, ЛКМ — сдвиг.",
                Location = new Point(12, 326), Size = new Size(338, 42), ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8f)
            };
            left.Controls.Add(lblBind);

            _txtLog = new TextBox {
                Location = new Point(12, 372), Size = new Size(338, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Font = new Font("Consolas", 8.5f)
            };
            left.Controls.Add(_txtLog);

            Panel right = new Panel { Dock = DockStyle.Fill };
            _painter = new GridPainter { Dock = DockStyle.Fill, Grid = _grid };
            _painter.CellHovered += (x, y, a) => _lblHover.Text = string.Format(CultureInfo.InvariantCulture, "Координаты: ({0}, {1}) | Alpha: {2:F2}", x, y, a);
            _lblHover = new Label { Dock = DockStyle.Bottom, Height = 24, BackColor = Color.WhiteSmoke, Text = "Наведите курсор на сетку" };
            right.Controls.Add(_painter);
            right.Controls.Add(_lblHover);

            t1.Controls.Add(right);
            t1.Controls.Add(left);

            TabPage t2 = new TabPage("Стресс-тест (Benchmark)");
            TableLayoutPanel tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(8) };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

            _chartCount = new BenchmarkChart { Dock = DockStyle.Fill, Title = "Время от числа примитивов (до 100 000)", XLabel = "Количество (шт.)" };
            _chartLen = new BenchmarkChart { Dock = DockStyle.Fill, Title = "Время от длины отрезка (20 000 итер.)", XLabel = "Длина (пикс.)" };

            Panel bottom = new Panel { Dock = DockStyle.Fill };
            Button btnBench = new Button { Text = "Запустить стресс-тест (100 000 отрезков + 50 000 окружностей)", Location = new Point(4, 4), Size = new Size(440, 32) };
            btnBench.Click += (s, e) => RunBenchmark(btnBench);
            _txtBench = new TextBox {
                Location = new Point(4, 42), Size = new Size(1100, 210), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Font = new Font("Consolas", 9f)
            };
            bottom.Controls.Add(btnBench);
            bottom.Controls.Add(_txtBench);

            tbl.Controls.Add(_chartCount, 0, 0);
            tbl.Controls.Add(_chartLen, 1, 0);
            tbl.SetColumnSpan(bottom, 2);
            tbl.Controls.Add(bottom, 0, 1);
            t2.Controls.Add(tbl);

            tabs.TabPages.Add(t1);
            tabs.TabPages.Add(t2);
            Controls.Add(tabs);

            UpdateFields();
        }


        private NumericUpDown AddNum(Panel p, string text, int val, int x, int y)
        {
            p.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true });
            NumericUpDown n = new NumericUpDown { Location = new Point(x, y + 16), Width = 150, Minimum = -140, Maximum = 140, Value = val };
            p.Controls.Add(n);
            return n;
        }

        private void UpdateFields()
        {
            int idx = _cmbAlg.SelectedIndex;
            _x2.Enabled = _y2.Enabled = (idx != 3);
            _x3.Enabled = _y3.Enabled = (idx == 5);
            _rad.Enabled = (idx == 3);
        }

        private void ExecuteDraw()
        {
            if (!_chkKeep.Checked) _grid.Clear();
            int x1 = (int)_x1.Value, y1 = (int)_y1.Value, x2 = (int)_x2.Value, y2 = (int)_y2.Value, x3 = (int)_x3.Value, y3 = (int)_y3.Value, r = (int)_rad.Value;
            List<string> log = new List<string>();
            Stopwatch sw = Stopwatch.StartNew();

            switch (_cmbAlg.SelectedIndex)
            {
                case 0: RasterEngine.StepByStepLine(x1, y1, x2, y2, _grid, log); break;
                case 1: RasterEngine.DDALine(x1, y1, x2, y2, _grid, log); break;
                case 2: RasterEngine.BresenhamLine(x1, y1, x2, y2, _grid, log); break;
                case 3: RasterEngine.BresenhamCircle(x1, y1, r, _grid, log); break;
                case 4: RasterEngine.WuLine(x1, y1, x2, y2, _grid, log); break;
                case 5: RasterEngine.CastlePitewayCurve(x1, y1, x2, y2, x3, y3, _grid, log); break;
            }
            sw.Stop();
            log.Insert(0, string.Format(CultureInfo.InvariantCulture, "Время расчёта в массив: {0:F4} мс", sw.Elapsed.TotalMilliseconds));
            _txtLog.Text = string.Join(Environment.NewLine, log.ToArray());
            _painter.Invalidate();
        }

        private void RunBenchmark(Button btn)
        {
            btn.Enabled = false;
            Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            try
            {
                VirtualGrid bg = new VirtualGrid(250, 250) { Blend = false };
                int maxL = 100000, maxC = 50000;
                int[] x2 = new int[maxL], y2 = new int[maxL], radii = new int[maxC];
                Random rnd = new Random(1);

                for (int i = 0; i < maxL; i++)
                {
                    double ang = 2.0 * Math.PI * i / maxL;
                    int len = rnd.Next(30, 220);
                    x2[i] = (int)Math.Round(Math.Cos(ang) * len);
                    y2[i] = (int)Math.Round(Math.Sin(ang) * len);
                }
                for (int i = 0; i < maxC; i++) radii[i] = rnd.Next(5, 220);

                RasterEngine.BresenhamLine(0, 0, 50, 50, bg);

                int[] counts = { 10000, 25000, 50000, 75000, 100000 };
                var sStep = new ChartSeries("Пошаговый", Color.Crimson);
                var sDda = new ChartSeries("ЦДА", Color.DarkOrange);
                var sBres = new ChartSeries("Брезенхем", Color.RoyalBlue);
                var sWu = new ChartSeries("Алгоритм Ву", Color.SeaGreen);
                foreach (int c in counts)
                {
                    sStep.Points.Add(new PointF(c, (float)BenchLines(c, x2, y2, bg, RasterEngine.StepByStepLine)));
                    sDda.Points.Add(new PointF(c, (float)BenchLines(c, x2, y2, bg, RasterEngine.DDALine)));
                    sBres.Points.Add(new PointF(c, (float)BenchLines(c, x2, y2, bg, RasterEngine.BresenhamLine)));
                    sWu.Points.Add(new PointF(c, (float)BenchLines(c, x2, y2, bg, RasterEngine.WuLine)));
                }
                _chartCount.Series.Clear();
                _chartCount.Series.AddRange(new[] { sStep, sDda, sBres, sWu });
                _chartCount.Invalidate();
                int[] lens = { 20, 60, 100, 160, 220 };
                var lStep = new ChartSeries("Пошаговый", Color.Crimson);
                var lDda = new ChartSeries("ЦДА", Color.DarkOrange);
                var lBres = new ChartSeries("Брезенхем", Color.RoyalBlue);
                var lWu = new ChartSeries("Алгоритм Ву", Color.SeaGreen);
                foreach (int l in lens)
                {
                    lStep.Points.Add(new PointF(l, (float)BenchLen(20000, l, bg, RasterEngine.StepByStepLine)));
                    lDda.Points.Add(new PointF(l, (float)BenchLen(20000, l, bg, RasterEngine.DDALine)));
                    lBres.Points.Add(new PointF(l, (float)BenchLen(20000, l, bg, RasterEngine.BresenhamLine)));
                    lWu.Points.Add(new PointF(l, (float)BenchLen(20000, l, bg, RasterEngine.WuLine)));
                }
                _chartLen.Series.Clear();
                _chartLen.Series.AddRange(new[] { lStep, lDda, lBres, lWu });
                _chartLen.Invalidate();
                bg.Clear();
                Stopwatch swC = Stopwatch.StartNew();
                for (int i = 0; i < maxC; i++) RasterEngine.BresenhamCircle(0, 0, radii[i], bg);
                swC.Stop();
                bg.Clear();
                Stopwatch swP = Stopwatch.StartNew();
                for (int i = 0; i < maxC; i++) RasterEngine.CastlePitewayCurve(-80, -40, 0, 120, 80, -40, bg);
                swP.Stop();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Результаты стресс-теста:");
                sb.AppendFormat(CultureInfo.InvariantCulture, "Пошаговый (100 000 отрезков):     {0:F2} мс\r\n", sStep.Points[4].Y);
                sb.AppendFormat(CultureInfo.InvariantCulture, "ЦДА (100 000 отрезков):           {0:F2} мс\r\n", sDda.Points[4].Y);
                sb.AppendFormat(CultureInfo.InvariantCulture, "Брезенхем (100 000 отрезков):    {0:F2} мс\r\n", sBres.Points[4].Y);
                sb.AppendFormat(CultureInfo.InvariantCulture, "Алгоритм Ву (100 000 отрезков):  {0:F2} мс\r\n", sWu.Points[4].Y);
                sb.AppendFormat(CultureInfo.InvariantCulture, "Брезенхем (50 000 окружностей):  {0:F2} мс\r\n", swC.Elapsed.TotalMilliseconds);
                sb.AppendFormat(CultureInfo.InvariantCulture, "Кастл-Питвей (50 000 кривых):     {0:F2} мс\r\n\r\n", swP.Elapsed.TotalMilliseconds);
                _txtBench.Text = sb.ToString();
            }
            finally { btn.Enabled = true; Cursor = Cursors.Default; }
        }

                private double BenchLines(int count, int[] x2, int[] y2, VirtualGrid g, Action<int, int, int, int, IRasterTarget, List<string>> alg)
        {
            g.Clear();
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++) alg(0, 0, x2[i], y2[i], g, null);
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }

        private double BenchLen(int count, int len, VirtualGrid g, Action<int, int, int, int, IRasterTarget, List<string>> alg)
        {
            g.Clear();
            int lx = (int)(len * 0.8), ly = (int)(len * 0.6);
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++) alg(0, 0, lx, ly, g, null);
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }
    }
}