using CalibrationApp.Models;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CalibrationApp.Services
{
    public interface IFileService
    {
        Task<SensorData?> LoadDataAsync();
        Task ExportDataAsync(double[] time, double[,] sensors, double[] coeffsMedian, double[] coeffsLsq);
    }
    
    public class FileService : IFileService
    {
        private IClassicDesktopStyleApplicationLifetime? GetDesktopLifetime()
        {
            return Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        }

        public async Task<SensorData?> LoadDataAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл с данными",
                Filters = new System.Collections.Generic.List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Файлы данных", Extensions = { "txt", "csv", "dat" } },
                    new FileDialogFilter { Name = "Все файлы", Extensions = { "*" } }
                },
                AllowMultiple = false
            };

            var desktop = GetDesktopLifetime();
            var owner = desktop?.MainWindow;

            var result = await openFileDialog.ShowAsync(owner);
            if (result != null && result.Length > 0)
            {
                var filePath = result[0];
                return await ReadDataFileAsync(filePath);
            }
            
            return null; // Явно возвращаем null
        }
        
        private async Task<SensorData> ReadDataFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length < 2) throw new InvalidOperationException("Файл пуст или содержит только заголовок");
                
                var rows = lines.Length - 1; // -1 для заголовка
                var cols = lines[1].Split(new char[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                
                if (cols < 2) throw new InvalidOperationException("Файл должен содержать минимум 2 столбца (время и 1 сенсор)");
                
                var time = new double[rows];
                var sensors = new double[rows, cols - 1]; // -1 для времени
                
                for (int i = 1; i < lines.Length; i++) // начиная с 1 для пропуска заголовка
                {
                    var parts = lines[i].Split(new char[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2) // убедимся, что у нас есть хотя бы время и один сенсор
                    {
                        time[i - 1] = double.Parse(parts[0]);
                        
                        for (int j = 1; j < parts.Length && j <= sensors.GetLength(1); j++)
                        {
                            sensors[i - 1, j - 1] = double.Parse(parts[j]);
                        }
                    }
                }
                
                return new SensorData { Time = time, Sensors = sensors };
            });
        }
        
        public async Task ExportDataAsync(double[] time, double[,] sensors, double[] coeffsMedian, double[] coeffsLsq)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Сохранить результаты",
                Filters = new System.Collections.Generic.List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "CSV файлы", Extensions = { "csv" } },
                    new FileDialogFilter { Name = "Все файлы", Extensions = { "*" } }
                },
                InitialFileName = "calibration_results.csv"
            };

            var desktop = GetDesktopLifetime();
            var owner = desktop?.MainWindow;

            var result = await saveFileDialog.ShowAsync(owner);
            if (!string.IsNullOrEmpty(result))
            {
                await WriteResultsToFileAsync(result, time, sensors, coeffsMedian, coeffsLsq);
            }
        }
        
        private async Task WriteResultsToFileAsync(string filePath, double[] time, double[,] sensors, double[] coeffsMedian, double[] coeffsLsq)
        {
            await Task.Run(() =>
            {
                using var writer = new StreamWriter(filePath);
                
                // Заголовок
                writer.Write("time,");
                for (int i = 0; i < sensors.GetLength(1); i++)
                {
                    writer.Write($"sensor_{i + 1}_raw,sensor_{i + 1}_calib,");
                }
                writer.WriteLine("coeff_median,coeff_lsq");
                
                // Данные
                for (int i = 0; i < time.Length; i++)
                {
                    writer.Write($"{time[i]},");
                    for (int j = 0; j < sensors.GetLength(1); j++)
                    {
                        double coeff = j < coeffsMedian.Length ? coeffsMedian[j] : 1.0;
                        writer.Write($"{sensors[i, j]},{sensors[i, j] * coeff},");
                    }
                    writer.WriteLine();
                }
                
                // Коэффициенты
                writer.WriteLine("coefficients,,");
                for (int i = 0; i < Math.Min(coeffsMedian.Length, coeffsLsq.Length); i++)
                {
                    writer.WriteLine($"sensor_{i + 1},{coeffsMedian[i]},{coeffsLsq[i]}");
                }
            });
        }
    }
}