using System;
namespace CalibrationApp.Models
{
    public class SensorData
    {
        public double[] Time { get; set; } = Array.Empty<double>(); // Инициализация пустым массивом
        public double[,] Sensors { get; set; } = new double[0, 0];
    }
    
    
    
    public class SensorStats
    {
        public int SensorNumber { get; set; }
        public double Mean { get; set; }
        public double Median { get; set; }
        public double StdDev { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
    }
}