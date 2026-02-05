namespace CalibrationApp.Models
{
    public class SensorData
    {
        public double[] Time { get; set; }
        public double[,] Sensors { get; set; }
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