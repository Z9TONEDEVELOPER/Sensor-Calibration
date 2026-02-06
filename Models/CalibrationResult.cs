using System;
namespace CalibrationApp.Models;

public class CalibrationResult
{
    public double[] TimeClean { get; set; } = Array.Empty<double>();
    public double[,] SensorsClean { get; set; } = new double[0, 0];
    public double[] CoeffsMedian { get; set; } = Array.Empty<double>();
    public double[] CoeffsLsq { get; set; } = Array.Empty<double>();
}