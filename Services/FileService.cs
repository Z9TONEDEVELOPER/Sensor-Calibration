using CalibrationApp.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
namespace CalibrationApp.Services
{
    public interface IFileService
    {
        Task<SensorData> LoadDataAsync();
        Task ExportDataAsync(double[] time, double[,] sensors, double[] coeffsMedian, double[] coeffsLsq);
    }
    
    public class FileService : IFileService
    {
        public async Task<SensorData> LoadDataAsync()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Выберите файл с данными";
            openFileDialog.Filters.Add(new FileDialogFilter() { Name = "Файлы данных", Extensions = { "csv", "txt", "dat" } });
            openFileDialog.AllowMultiple = false;

            var result = await openFileDialog.ShowAsync(null); // Здесь нужно передать родительское окно
            if (result != null && result.Length > 0)
            {
                var filePath = result[0];
                var data = await ReadDataFileAsync(filePath);
                return data;
            }
            
            return null;
        }
        
        private async Task<SensorData> ReadDataFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var lines = File.ReadAllLines(filePath);
                var rows = lines.Length;
                var cols = lines[0].Split(new char[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                
                var time = new double[rows - 1]; // -1 для заголовка
                var sensors = new double[rows - 1, cols - 1]; // -1 для времени
                
                for (int i = 1; i < rows; i++) // начиная с 1 для пропуска заголовка
                {
                    var parts = lines[i].Split(new char[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    time[i - 1] = double.Parse(parts[0]);
                    
                    for (int j = 1; j < parts.Length && j <= sensors.GetLength(1); j++)
                    {
                        sensors[i - 1, j - 1] = double.Parse(parts[j]);
                    }
                }
                
                return new SensorData { Time = time, Sensors = sensors };
            });
        }
        
        public async Task ExportDataAsync(double[] time, double[,] sensors, double[] coeffsMedian, double[] coeffsLsq)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Сохранить результаты";
            saveFileDialog.Filters.Add(new FileDialogFilter() { Name = "CSV файлы", Extensions = { "csv" } });
            saveFileDialog.InitialFileName = "calibration_results.csv";

            var result = await saveFileDialog.ShowAsync(null); // Здесь нужно передать родительское окно
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
                    writer.Write($"sensor_{i + 1},calibrated_{i + 1},");
                }
                writer.WriteLine("coeff_median,coeff_lsq");
                
                // Данные
                for (int i = 0; i < time.Length; i++)
                {
                    writer.Write($"{time[i]},");
                    for (int j = 0; j < sensors.GetLength(1); j++)
                    {
                        writer.Write($"{sensors[i, j]},{sensors[i, j] * coeffsMedian[j]},");
                    }
                    writer.WriteLine();
                }
                
                // Коэффициенты
                writer.WriteLine("coefficients,,");
                for (int i = 0; i < coeffsMedian.Length; i++)
                {
                    writer.WriteLine($"sensor_{i + 1},{coeffsMedian[i]},{coeffsLsq[i]}");
                }
            });
        }
    }
}