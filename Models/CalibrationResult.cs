namespace CalibrationApp.Models;

public class CalibrationResult
{
    public double[] TimeClean { get; set; }
    public double[,] SensorsClean { get; set; }
    public double[] CoeffsMedian { get; set; }
    public double[] CoeffsLsq { get; set; }
}