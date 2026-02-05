using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using System.Linq;
using CalibrationApp.Models;
using CalibrationApp.Services;

namespace CalibrationApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        // Сервисы
        private readonly IFileService _fileService;
        private readonly ISignalProcessingService _processingService;
        
        // Данные
        private double[] _time;
        private double[,] _sensors;
        private double[] _timeClean;
        private double[,] _sensorsClean;
        private double[] _coeffsMedian;
        private double[] _coeffsLsq;
        
        // Параметры обработки
        private int _windowSize = 5;
        private double _lowessFraction = 0.06;
        private double _outlierThreshold = 3.0;
        private string _outlierMethod = "zscore";
        private string _filterType = "moving_average";
        private int _currentSensor = 1;
        private string _calibMethod = "median";
        
        // Статус
        private string _statusMessage = "Готов к работе";
        private bool _isProcessing = false;
        
        public MainWindowViewModel(IFileService fileService, ISignalProcessingService processingService)
        {
            _fileService = fileService;
            _processingService = processingService;
            
            // Инициализация команд
            LoadDataCommand = ReactiveCommand.CreateFromTask(LoadDataAsync);
            ProcessDataCommand = ReactiveCommand.CreateFromTask(ProcessDataAsync);
            ExportDataCommand = ReactiveCommand.CreateFromTask(ExportDataAsync);
            ExitCommand = ReactiveCommand.Create(ExitApp);
            
            // Инициализация коллекций
            OutlierMethods = new ObservableCollection<string> { "zscore", "iqr", "mad" };
            FilterTypes = new ObservableCollection<string> { "moving_average", "savgol", "median", "butterworth" };
            CalibrationMethods = new ObservableCollection<string> { "median", "lsq" };
            
            // Инициализация ViewModel для графиков
            RawPlotViewModel = new PlotViewModel { Title = "Сырые данные" };
            CalibPlotViewModel = new PlotViewModel { Title = "Калиброванные данные" };
            ComparisonPlotViewModel = new PlotViewModel { Title = "Сравнение сенсоров" };
            
            // Инициализация статистики
            SensorStatistics = new ObservableCollection<SensorStat>();
        }
        
        // Свойства
        public int WindowSize
        {
            get => _windowSize;
            set => this.RaiseAndSetIfChanged(ref _windowSize, value);
        }
        
        public double LowessFraction
        {
            get => _lowessFraction;
            set => this.RaiseAndSetIfChanged(ref _lowessFraction, value);
        }
        
        public double OutlierThreshold
        {
            get => _outlierThreshold;
            set => this.RaiseAndSetIfChanged(ref _outlierThreshold, value);
        }
        
        public string OutlierMethod
        {
            get => _outlierMethod;
            set => this.RaiseAndSetIfChanged(ref _outlierMethod, value);
        }
        
        public string FilterType
        {
            get => _filterType;
            set => this.RaiseAndSetIfChanged(ref _filterType, value);
        }
        
        public int CurrentSensor
        {
            get => _currentSensor;
            set => this.RaiseAndSetIfChanged(ref _currentSensor, value);
        }
        
        public string CalibMethod
        {
            get => _calibMethod;
            set => this.RaiseAndSetIfChanged(ref _calibMethod, value);
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }
        
        public bool IsProcessing
        {
            get => _isProcessing;
            set => this.RaiseAndSetIfChanged(ref _isProcessing, value);
        }
        
        // Коллекции
        public ObservableCollection<string> OutlierMethods { get; }
        public ObservableCollection<string> FilterTypes { get; }
        public ObservableCollection<string> CalibrationMethods { get; }
        public ObservableCollection<SensorStat> SensorStatistics { get; }
        
        // ViewModel для графиков
        public PlotViewModel RawPlotViewModel { get; }
        public PlotViewModel CalibPlotViewModel { get; }
        public PlotViewModel ComparisonPlotViewModel { get; }
        
        // Команды
        public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }
        public ReactiveCommand<Unit, Unit> ProcessDataCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportDataCommand { get; }
        public ReactiveCommand<Unit, Unit> ExitCommand { get; }
        
        // Методы
        private async Task LoadDataAsync()
        {
            IsProcessing = true;
            StatusMessage = "Загрузка данных...";
            
            try
            {
                var data = await _fileService.LoadDataAsync();
                if (data != null)
                {
                    Time = data.Time;
                    Sensors = data.Sensors;
                    
                    int sensorCount = Sensors.GetLength(1);
                    int pointCount = Time.Length;
                    
                    StatusMessage = $"Загружено: {pointCount} точек, {sensorCount} сенсоров";
                    
                    // Обновляем интерфейс
                    UpdateInterfaceAfterLoad();
                }
                else
                {
                    StatusMessage = "Загрузка отменена";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private void UpdateInterfaceAfterLoad()
        {
            // Обновляем статистику
            UpdateStatistics();
            
            // Обновляем графики
            UpdateRawPlot();
        }
        
        private async Task ProcessDataAsync()
        {
            if (Time == null || Sensors == null)
            {
                StatusMessage = "Сначала загрузите данные";
                return;
            }
            
            IsProcessing = true;
            StatusMessage = "Обработка данных...";
            
            try
            {
                var result = await _processingService.ProcessDataAsync(
                    Time, 
                    Sensors, 
                    WindowSize, 
                    LowessFraction, 
                    OutlierThreshold, 
                    OutlierMethod, 
                    FilterType, 
                    CalibMethod
                );
                
                TimeClean = result.TimeClean;
                SensorsClean = result.SensorsClean;
                CoeffsMedian = result.CoeffsMedian;
                CoeffsLsq = result.CoeffsLsq;
                
                StatusMessage = "Данные успешно обработаны";
                
                // Обновляем интерфейс
                UpdateProcessedData();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка обработки: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private void UpdateProcessedData()
        {
            // Обновляем статистику
            UpdateStatistics();
            
            // Обновляем графики
            UpdateCalibratedPlot();
            UpdateComparisonPlot();
        }
        
        private void UpdateRawPlot()
        {
            if (Time == null || Sensors == null) return;
            
            int sensorIdx = CurrentSensor - 1;
            if (sensorIdx < 0 || sensorIdx >= Sensors.GetLength(1)) return;
            
            double[] xData = Time;
            double[] yData = new double[Time.Length];
            for (int i = 0; i < Time.Length; i++)
            {
                yData[i] = Sensors[i, sensorIdx];
            }
            
            RawPlotViewModel.UpdatePlot(xData, yData, $"Сенсор {CurrentSensor} (сырые)");
        }
        
        private void UpdateCalibratedPlot()
        {
            if (TimeClean == null || SensorsClean == null) return;
            
            int sensorIdx = CurrentSensor - 1;
            if (sensorIdx < 0 || sensorIdx >= SensorsClean.GetLength(1)) return;
            
            double coeff = CalibMethod == "median" ? CoeffsMedian[sensorIdx] : CoeffsLsq[sensorIdx];
            
            double[] xData = TimeClean;
            double[] yData = new double[TimeClean.Length];
            for (int i = 0; i < TimeClean.Length; i++)
            {
                yData[i] = SensorsClean[i, sensorIdx] * coeff;
            }
            
            CalibPlotViewModel.UpdatePlot(xData, yData, $"Сенсор {CurrentSensor} (калиброванные)");
        }
        
        private void UpdateComparisonPlot()
        {
            if (SensorsClean == null) return;
            
            ComparisonPlotViewModel.ClearPlot();
            
            // Показываем все сенсоры на одном графике
            for (int i = 0; i < Math.Min(8, SensorsClean.GetLength(1)); i++)
            {
                double coeff = CalibMethod == "median" ? CoeffsMedian[i] : CoeffsLsq[i];
                
                double[] yData = new double[TimeClean.Length];
                for (int j = 0; j < TimeClean.Length; j++)
                {
                    yData[j] = SensorsClean[j, i] * coeff;
                }
                
                ComparisonPlotViewModel.UpdatePlot(TimeClean, yData, $"Сенсор {i + 1}");
            }
        }
        
        private async Task ExportDataAsync()
        {
            if (TimeClean == null || SensorsClean == null)
            {
                StatusMessage = "Нет данных для экспорта";
                return;
            }
            
            try
            {
                await _fileService.ExportDataAsync(TimeClean, SensorsClean, CoeffsMedian, CoeffsLsq);
                StatusMessage = "Данные экспортированы";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка экспорта: {ex.Message}";
            }
        }
        
        private void ExitApp()
        {
            // Реализация выхода
            System.Environment.Exit(0);
        }
        
        private void UpdateStatistics()
        {
            if (SensorsClean == null) return;
            
            SensorStatistics.Clear();
            
            for (int i = 0; i < Math.Min(8, SensorsClean.GetLength(1)); i++)
            {
                var sensorData = new double[SensorsClean.GetLength(0)];
                for (int j = 0; j < sensorData.Length; j++)
                {
                    sensorData[j] = SensorsClean[j, i];
                }
                
                var stat = new SensorStat
                {
                    SensorNumber = i + 1,
                    Mean = sensorData.Average(),
                    Median = CalculateMedian(sensorData),
                    StdDev = CalculateStdDev(sensorData),
                    Min = sensorData.Min(),
                    Max = sensorData.Max()
                };
                
                SensorStatistics.Add(stat);
            }
        }
        
        private double CalculateMedian(double[] array)
        {
            var sorted = (double[])array.Clone();
            Array.Sort(sorted);
            int n = sorted.Length;
            return (n % 2 == 0) ? (sorted[n/2-1] + sorted[n/2]) / 2.0 : sorted[n/2];
        }
        
        private double CalculateStdDev(double[] array)
        {
            double avg = array.Average();
            double sum = array.Sum(val => (val - avg) * (val - avg));
            return Math.Sqrt(sum / (array.Length - 1));
        }
        
        // Свойства для данных
        public double[] Time
        {
            get => _time;
            private set => this.RaiseAndSetIfChanged(ref _time, value);
        }
        
        public double[,] Sensors
        {
            get => _sensors;
            private set => this.RaiseAndSetIfChanged(ref _sensors, value);
        }
        
        public double[] TimeClean
        {
            get => _timeClean;
            private set => this.RaiseAndSetIfChanged(ref _timeClean, value);
        }
        
        public double[,] SensorsClean
        {
            get => _sensorsClean;
            private set => this.RaiseAndSetIfChanged(ref _sensorsClean, value);
        }
        
        public double[] CoeffsMedian
        {
            get => _coeffsMedian;
            private set => this.RaiseAndSetIfChanged(ref _coeffsMedian, value);
        }
        
        public double[] CoeffsLsq
        {
            get => _coeffsLsq;
            private set => this.RaiseAndSetIfChanged(ref _coeffsLsq, value);
        }
    }
    
    // Класс для статистики сенсоров
    public class SensorStat
    {
        public int SensorNumber { get; set; }
        public double Mean { get; set; }
        public double Median { get; set; }
        public double StdDev { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
    }
}