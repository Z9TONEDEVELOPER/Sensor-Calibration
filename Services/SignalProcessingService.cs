using System;
using System.Threading.Tasks;
using CalibrationApp.Models;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using MathNet.Filtering;
using MathNet.Numerics.Threading;
using System.Linq;
using System.Collections.Generic;

namespace CalibrationApp.Services
{
    public class SignalProcessingService : ISignalProcessingService
    {
        public async Task<CalibrationResult> ProcessDataAsync(
            double[] time, 
            double[,] sensors, 
            int windowSize, 
            double lowessFraction, 
            double outlierThreshold, 
            string outlierMethod, 
            string filterType, 
            string calibMethod)
        {
            return await Task.Run(() =>
            {
                var result = new CalibrationResult();
                
                // Копируем время (оно не изменяется)
                result.TimeClean = (double[])time.Clone();
                
                // Создаем массив для очищенных данных
                int points = sensors.GetLength(0);
                int sensorsCount = sensors.GetLength(1);
                result.SensorsClean = new double[points, sensorsCount];
                
                // Обработка каждого сенсора отдельно
                for (int i = 0; i < sensorsCount; i++)
                {
                    var sensorData = new double[points];
                    for (int j = 0; j < points; j++)
                    {
                        sensorData[j] = sensors[j, i];
                    }
                    
                    // 1. Удаление выбросов
                    var cleanData = RemoveOutliers(sensorData, outlierMethod, outlierThreshold);
                    
                    // 2. Фильтрация
                    var filteredData = ApplyFilter(cleanData, filterType, windowSize);
                    
                    // 3. Сохранение очищенных данных
                    for (int j = 0; j < points; j++)
                    {
                        result.SensorsClean[j, i] = filteredData[j];
                    }
                }
                
                // 4. Вычисление коэффициентов калибровки
                result.CoeffsMedian = CalculateCoeffsMedian(result.SensorsClean);
                result.CoeffsLsq = CalculateCoeffsLSQ(result.SensorsClean);
                
                return result;
            });
        }
        
        private double[] RemoveOutliers(double[] data, string method, double threshold)
        {
            var mask = new bool[data.Length];
            Array.Fill(mask, true);
            
            switch (method.ToLower())
            {
                case "zscore":
                    var mean = data.Where(d => !double.IsNaN(d)).Average();
                    var std = Math.Sqrt(data.Where(d => !double.IsNaN(d)).Select(d => Math.Pow(d - mean, 2)).Average());
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (!double.IsNaN(data[i]))
                        {
                            var zscore = Math.Abs((data[i] - mean) / (std + 1e-10));
                            mask[i] = zscore <= threshold;
                        }
                    }
                    break;
                    
                case "iqr":
                    var sorted = data.Where(d => !double.IsNaN(d)).OrderBy(d => d).ToArray();
                    if (sorted.Length > 0)
                    {
                        var q1 = sorted[(int)(0.25 * sorted.Length)];
                        var q3 = sorted[(int)(0.75 * sorted.Length)];
                        var iqr = q3 - q1;
                        var lower = q1 - 1.5 * iqr;
                        var upper = q3 + 1.5 * iqr;
                        
                        for (int i = 0; i < data.Length; i++)
                        {
                            mask[i] = !double.IsNaN(data[i]) && data[i] >= lower && data[i] <= upper;
                        }
                    }
                    break;
                    
                case "mad":
                    var sortedMad = data.Where(d => !double.IsNaN(d)).OrderBy(d => d).ToArray();
                    if (sortedMad.Length > 0)
                    {
                        var median = sortedMad[sortedMad.Length / 2];
                        var deviations = sortedMad.Select(d => Math.Abs(d - median)).OrderBy(d => d).ToArray();
                        var mad = deviations[deviations.Length / 2];
                        
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (!double.IsNaN(data[i]))
                            {
                                var modified_z_score = 0.6745 * Math.Abs(data[i] - median) / (mad + 1e-10);
                                mask[i] = modified_z_score <= threshold;
                            }
                        }
                    }
                    break;
            }
            
            // Создаем массив без выбросов (заменяем выбросы медианой соседних значений)
            var cleaned = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                if (mask[i])
                {
                    cleaned[i] = data[i];
                }
                else
                {
                    // Заменяем выброс ближайшим валидным значением
                    cleaned[i] = FindNearestValidValue(data, mask, i);
                }
            }
            
            return cleaned;
        }
        
        private double FindNearestValidValue(double[] data, bool[] mask, int index)
        {
            // Сначала ищем вперед
            for (int i = index + 1; i < data.Length; i++)
            {
                if (mask[i])
                    return data[i];
            }
            
            // Затем назад
            for (int i = index - 1; i >= 0; i--)
            {
                if (mask[i])
                    return data[i];
            }
            
            // Если не нашли, возвращаем 0
            return 0;
        }
        
        private double[] ApplyFilter(double[] data, string filterType, int windowSize)
        {
            switch (filterType.ToLower())
            {
                case "moving_average":
                    return MovingAverageFilter(data, windowSize);
                    
                case "savgol":
                    // Имитация фильтра Савицкого-Голея (требует MathNet.Numerics.Signal)
                    return SavitzkyGolayFilter(data, windowSize);
                    
                case "median":
                    return MedianFilter(data, windowSize);
                    
                case "butterworth":
                    return ButterworthFilter(data, windowSize);
                    
                default:
                    return data;
            }
        }
        
        private double[] MovingAverageFilter(double[] data, int windowSize)
        {
            var result = new double[data.Length];
            
            for (int i = 0; i < data.Length; i++)
            {
                double sum = 0;
                int count = 0;
                
                for (int j = Math.Max(0, i - windowSize / 2); j <= Math.Min(data.Length - 1, i + windowSize / 2); j++)
                {
                    sum += data[j];
                    count++;
                }
                
                result[i] = sum / count;
            }
            
            return result;
        }
        
        private double[] SavitzkyGolayFilter(double[] data, int windowSize)
        {
            // Имитация фильтра (в реальном приложении использовать MathNet.Numerics.Signal)
            return MovingAverageFilter(data, windowSize);
        }
        
        private double[] MedianFilter(double[] data, int windowSize)
        {
            var result = new double[data.Length];
            
            for (int i = 0; i < data.Length; i++)
            {
                var window = new double[windowSize];
                int start = Math.Max(0, i - windowSize / 2);
                int end = Math.Min(data.Length - 1, i + windowSize / 2);
                
                var values = new List<double>();
                for (int j = start; j <= end; j++)
                {
                    values.Add(data[j]);
                }
                
                values.Sort();
                result[i] = values[values.Count / 2];
            }
            
            return result;
        }
        
        private double[] ButterworthFilter(double[] data, int windowSize)
        {
            // Имитация фильтра (в реальном приложении использовать MathNet.Filtering)
            return MovingAverageFilter(data, windowSize);
        }
        
        private double[] CalculateCoeffsMedian(double[,] sensors)
        {
            int nSensors = sensors.GetLength(1);
            var coeffs = new double[nSensors];
            
            for (int i = 0; i < nSensors; i++)
            {
                var sensorData = new double[sensors.GetLength(0)];
                for (int j = 0; j < sensorData.Length; j++)
                {
                    sensorData[j] = sensors[j, i];
                }
                
                // Вычисляем медиану
                var sorted = (double[])sensorData.Clone();
                Array.Sort(sorted);
                var median = sorted.Length % 2 == 0 
                    ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2.0 
                    : sorted[sorted.Length / 2];
                    
                coeffs[i] = median != 0 ? median : 1.0;
            }
            
            return coeffs;
        }
        
        private double[] CalculateCoeffsLSQ(double[,] sensors)
        {
            int nPoints = sensors.GetLength(0);
            int nSensors = sensors.GetLength(1);
            var coeffs = new double[nSensors];
            
            // Используем среднее по всем сенсорам как эталонный сигнал
            var reference = new double[nPoints];
            for (int i = 0; i < nPoints; i++)
            {
                double sum = 0;
                for (int j = 0; j < nSensors; j++)
                {
                    sum += sensors[i, j];
                }
                reference[i] = sum / nSensors;
            }
            
            for (int i = 0; i < nSensors; i++)
            {
                double numerator = 0, denominator = 0;
                
                for (int j = 0; j < nPoints; j++)
                {
                    var val = sensors[j, i];
                    numerator += val * reference[j];
                    denominator += val * val;
                }
                
                coeffs[i] = denominator != 0 ? numerator / denominator : 1.0;
            }
            
            return coeffs;
        }
    }
}