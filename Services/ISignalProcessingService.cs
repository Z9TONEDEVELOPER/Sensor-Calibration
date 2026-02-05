using CalibrationApp.Models;
using System.Threading.Tasks;
namespace CalibrationApp.Services
{
    public interface ISignalProcessingService
    {
        Task<CalibrationResult> ProcessDataAsync(
            double[] time, 
            double[,] sensors, 
            int windowSize, 
            double lowessFraction, 
            double outlierThreshold, 
            string outlierMethod, 
            string filterType, 
            string calibMethod
        );
    }
}