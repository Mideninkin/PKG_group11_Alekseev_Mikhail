using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageProcessorApp
{
    public class MainForm : Form
    {
        private PictureBox pbOriginal;
        private PictureBox pbProcessed;
        private PictureBox pbHistogramOriginal;
        private PictureBox pbHistogramProcessed;
        
        private Button btnLoad;
        private Button btnLinear;
        private Button btnEqualizeRGB;
        private Button btnEqualizeHSV;
        
        // Новые элементы управления для поэлементных операций
        private Button btnInvert;
        private Button btnGamma;
        private TrackBar trkGamma;
        private Label lblGamma;
        
        private Button btnThreshold;
        private TrackBar trkThreshold;
        private Label lblThreshold;

        private Label lblStatus;

        private Bitmap originalBitmap;
        private ColorRGB[,] originalBuffer;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Обработка изображений — Вариант 1";
            this.Size = new Size(1150, 720);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Верхняя панель управления (Гистограммы / Контраст)
            btnLoad = new Button { Text = "Загрузить фото", Location = new Point(20, 20), Size = new Size(130, 35) };
            btnLinear = new Button { Text = "Линейное контр.", Location = new Point(160, 20), Size = new Size(130, 35), Enabled = false };
            btnEqualizeRGB = new Button { Text = "Эквализация RGB", Location = new Point(300, 20), Size = new Size(130, 35), Enabled = false };
            btnEqualizeHSV = new Button { Text = "Эквализация HSV", Location = new Point(440, 20), Size = new Size(130, 35), Enabled = false };
            
            // Нижняя панель управления (Поэлементные операции)
            btnInvert = new Button { Text = "Инверсия", Location = new Point(20, 620), Size = new Size(130, 35), Enabled = false };
            
            lblGamma = new Label { Text = "Гамма: 1.0", Location = new Point(160, 605), Size = new Size(100, 15) };
            trkGamma = new TrackBar { Location = new Point(160, 625), Size = new Size(150, 45), Minimum = 1, Maximum = 50, Value = 10, Enabled = false }; // 0.1 - 5.0
            btnGamma = new Button { Text = "Применить Гамму", Location = new Point(320, 620), Size = new Size(120, 35), Enabled = false };

            lblThreshold = new Label { Text = "Порог: 128", Location = new Point(460, 605), Size = new Size(100, 15) };
            trkThreshold = new TrackBar { Location = new Point(460, 625), Size = new Size(150, 45), Minimum = 0, Maximum = 255, Value = 128, Enabled = false };
            btnThreshold = new Button { Text = "Бинаризация", Location = new Point(620, 620), Size = new Size(120, 35), Enabled = false };

            lblStatus = new Label { Text = "Статус: Ожидание загрузки файла...", Location = new Point(600, 30), Size = new Size(500, 20), Font = new Font(this.Font, FontStyle.Bold) };

            // Окна картинок
            pbOriginal = new PictureBox { Location = new Point(20, 80), Size = new Size(400, 400), BorderStyle = BorderStyle.Fixed3D, SizeMode = PictureBoxSizeMode.Zoom };
            pbProcessed = new PictureBox { Location = new Point(440, 80), Size = new Size(400, 400), BorderStyle = BorderStyle.Fixed3D, SizeMode = PictureBoxSizeMode.Zoom };

            // Окна гистограмм
            pbHistogramOriginal = new PictureBox { Location = new Point(860, 80), Size = new Size(250, 190), BorderStyle = BorderStyle.FixedSingle };
            pbHistogramProcessed = new PictureBox { Location = new Point(860, 290), Size = new Size(250, 190), BorderStyle = BorderStyle.FixedSingle };

            // События
            btnLoad.Click += BtnLoad_Click;
            btnLinear.Click += BtnLinear_Click;
            btnEqualizeRGB.Click += BtnEqualizeRGB_Click;
            btnEqualizeHSV.Click += BtnEqualizeHSV_Click;
            
            btnInvert.Click += BtnInvert_Click;
            btnGamma.Click += BtnGamma_Click;
            btnThreshold.Click += BtnThreshold_Click;

            trkGamma.Scroll += (s, e) => lblGamma.Text = $"Гамма: {trkGamma.Value / 10.0:F1}";
            trkThreshold.Scroll += (s, e) => lblThreshold.Text = $"Порог: {trkThreshold.Value}";

            this.Controls.AddRange(new Control[] { 
                btnLoad, btnLinear, btnEqualizeRGB, btnEqualizeHSV, 
                btnInvert, btnGamma, trkGamma, lblGamma,
                btnThreshold, trkThreshold, lblThreshold, lblStatus,
                pbOriginal, pbProcessed, pbHistogramOriginal, pbHistogramProcessed 
            });
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    originalBitmap = new Bitmap(ofd.FileName);
                    pbOriginal.Image = originalBitmap;
                    
                    originalBuffer = ImageBuffer.BitmapToBuffer(originalBitmap);
                    DrawHistogram(originalBuffer, pbHistogramOriginal);

                    // Активируем все кнопки
                    btnLinear.Enabled = true;
                    btnEqualizeRGB.Enabled = true;
                    btnEqualizeHSV.Enabled = true;
                    btnInvert.Enabled = true;
                    btnGamma.Enabled = true;
                    trkGamma.Enabled = true;
                    btnThreshold.Enabled = true;
                    trkThreshold.Enabled = true;

                    lblStatus.Text = "Статус: Файл успешно загружен.";
                }
            }
        }

        private void BtnLinear_Click(object sender, EventArgs e)
        {
            ExecuteAndLog(() => ImageAlgorithms.LinearContrast(originalBuffer));
        }

        private void BtnEqualizeRGB_Click(object sender, EventArgs e)
        {
            ExecuteAndLog(() => ImageAlgorithms.EqualizeRGB(originalBuffer));
        }

        private void BtnEqualizeHSV_Click(object sender, EventArgs e)
        {
            ExecuteAndLog(() => ImageAlgorithms.EqualizeHSV(originalBuffer));
        }

        private void BtnInvert_Click(object sender, EventArgs e)
        {
            ExecuteAndLog(() => ImageAlgorithms.Invert(originalBuffer));
        }

        private void BtnGamma_Click(object sender, EventArgs e)
        {
            double gammaValue = trkGamma.Value / 10.0;
            ExecuteAndLog(() => ImageAlgorithms.GammaCorrection(originalBuffer, gammaValue));
        }

        private void BtnThreshold_Click(object sender, EventArgs e)
        {
            int thresh = trkThreshold.Value;
            ExecuteAndLog(() => ImageAlgorithms.Threshold(originalBuffer, thresh));
        }

        private void ExecuteAndLog(Func<ColorRGB[,]> algorithm)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            ColorRGB[,] resultBuffer = algorithm();
            watch.Stop();

            Bitmap resBmp = ImageBuffer.BufferToBitmap(resultBuffer);
            pbProcessed.Image = resBmp;
            DrawHistogram(resultBuffer, pbHistogramProcessed);
            lblStatus.Text = $"Статус: Выполнено за {watch.ElapsedMilliseconds} мс.";
        }

        private void DrawHistogram(ColorRGB[,] buffer, PictureBox pb)
        {
            int[] hist = ImageAlgorithms.CalculateHistogram(buffer, 'Y');
            Bitmap bmp = new Bitmap(pb.Width, pb.Height);
            
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                
                int max = 0;
                for (int i = 0; i < 256; i++) if (hist[i] > max) max = hist[i];

                if (max == 0) return;

                double scaleX = (double)pb.Width / 256;
                double scaleY = (double)(pb.Height - 15) / max;

                Pen pen = new Pen(Color.FromArgb(64, 64, 64), 1);
                for (int i = 0; i < 256; i++)
                {
                    int x = (int)(i * scaleX);
                    int y = (int)(hist[i] * scaleY);
                    g.DrawLine(pen, x, pb.Height, x, pb.Height - y);
                }
            }
            pb.Image = bmp;
        }
    }
}
