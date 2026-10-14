using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace ImageLab
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private ConcurrentDictionary<string, string> filePathsMap = new();

        private Button btnSelectFolder = new() { Text = "Выбрать папку", Top = 12, Left = 12, Width = 140, Height = 30, Font = new Font("Segoe UI", 9) };
        private ProgressBar progressBar = new() { Top = 15, Left = 165, Width = 350, Height = 23 };
        private Label lblStatus = new() { Text = "Статус: Готов", Top = 18, Left = 530, Width = 250, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        private DataGridView grid = new() { Top = 55, Left = 12, Width = 750, Height = 580, AutoGenerateColumns = true, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
        private PictureBox preview = new() { Top = 55, Left = 775, Width = 280, Height = 280, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightGray };
        private Label lblPreviewInfo = new() { Text = "Предпросмотр", Top = 345, Left = 775, Width = 280, Height = 60, TextAlign = ContentAlignment.TopCenter, Font = new Font("Segoe UI", 9, FontStyle.Italic) };

        public MainForm()
        {
            Text = "Лабораторная работа №2 — Парсер Метаданных";
            Width = 1090; 
            Height = 690;
            MinimumSize = new Size(1090, 690);
            StartPosition = FormStartPosition.CenterScreen;
            grid.BackgroundColor = Color.White;
            grid.RowHeadersVisible = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;


            Controls.AddRange([btnSelectFolder, progressBar, lblStatus, grid, preview, lblPreviewInfo]);

            btnSelectFolder.Click += async (s, e) => await ScanFolderAsync();
            grid.SelectionChanged += Grid_SelectionChanged;
        }

        private async Task ScanFolderAsync()
        {
            using FolderBrowserDialog fbd = new();
            if (fbd.ShowDialog() != DialogResult.OK) return;

            btnSelectFolder.Enabled = false;
            lblStatus.Text = "Статус: Поиск файлов...";
            grid.DataSource = null;
            filePathsMap.Clear();
            preview.Image?.Dispose();
            preview.Image = null;

            string[] extensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".pcx"];
            
            string[] files = await Task.Run(() => 
                Directory.EnumerateFiles(fbd.SelectedPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                .ToArray()
            );

            if (files.Length == 0)
            {
                MessageBox.Show("Графические файлы в выбранной папке не найдены.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnSelectFolder.Enabled = true;
                lblStatus.Text = "Статус: Файлы не найдены";
                return;
            }

            progressBar.Maximum = files.Length;
            progressBar.Value = 0;

            var results = new ConcurrentBag<ImageMetadata>();
            int processedCount = 0;

            await Task.Run(() =>
            {
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
                {
                    var meta = ImageParser.ParseFile(file);
                    results.Add(meta);
                    
                    filePathsMap[meta.FileName] = file;

                    int current = Interlocked.Increment(ref processedCount);
                    if (current % 5 == 0 || current == files.Length)
                    {
                        Invoke(() => {
                            progressBar.Value = current;
                            lblStatus.Text = $"Статус: Обработано {current} из {files.Length}";
                        });
                    }
                });
            });

            grid.DataSource = results.OrderBy(r => r.FileName).ToList();
            lblStatus.Text = "Статус: Успешно завершено!";
            btnSelectFolder.Enabled = true;
        }

        private void Grid_SelectionChanged(object? sender, EventArgs e)
        {
            if (grid.CurrentRow?.DataBoundItem is ImageMetadata selected)
            {
                try
                {
                    preview.Image?.Dispose();
                    preview.Image = null;

                    if (selected.Status == "OK" && filePathsMap.TryGetValue(selected.FileName, out string? fullPath))
                    {
                        preview.Image = Image.FromFile(fullPath);
                        lblPreviewInfo.Text = $"{selected.FileName}\nРазмер: {selected.Size}";
                    }
                    else
                    {
                        lblPreviewInfo.Text = $"Невозможно отобразить:\n{selected.Status}";
                    }
                }
                catch (Exception ex)
                {
                    preview.Image = null;
                    lblPreviewInfo.Text = $"Ошибка загрузки превью:\n{ex.Message}";
                }
            }
        }
    }
}
