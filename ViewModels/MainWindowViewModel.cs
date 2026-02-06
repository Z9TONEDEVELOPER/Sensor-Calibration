using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using CalibrationApp.Models;
using System.Linq;
using CalibrationApp.Services;

namespace CalibrationApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        // Инициализируем поля пустыми значениями
        private double[] _time = Array.Empty<double>();
        private double[,] _sensors = new double[0, 0];
        private double[] _timeClean = Array.Empty<double>();
        private double[,] _sensorsClean = new double[0, 0];
        private double[] _coeffsMedian = Array.Empty<double>();
        private double[] _coeffsLsq = Array.Empty<double>();
        
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
        
        // Сервисы
        private readonly IFileService _fileService;
        private readonly ISignalProcessingService _processingService;
        
        public MainWindowViewModel(
            IFileService fileService, 
            ISignalProcessingService processingService)
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
        
        // Свойства (только для чтения, изменение через методы)
        public double[] Time => _time;
        public double[,] Sensors => _sensors;
        public double[] TimeClean => _timeClean;
        public double[,] SensorsClean => _sensorsClean;
        public double[] CoeffsMedian => _coeffsMedian;
        public double[] CoeffsLsq => _coeffsLsq;
        
        // Параметры
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
                    // Обновляем данные
                    _time = data.Time ?? Array.Empty<double>();
                    _sensors = data.Sensors ?? new double[0, 0];
                    
                    int sensorCount = _sensors.GetLength(1);
                    int pointCount = _time.Length;
                    
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
        
        private void UpdateRawPlot()
        {
            if (_time.Length > 0 && _sensors.GetLength(1) > 0)
            {
                int sensorIdx = _currentSensor - 1;
                if (sensorIdx >= 0 && sensorIdx < _sensors.GetLength(1))
                {
                    double[] xData = _time;
                    double[] yData = new double[_time.Length];
                    for (int i = 0; i < _time.Length; i++)
                    {
                        yData[i] = _sensors[i, sensorIdx];
                    }
                    
                    RawPlotViewModel.UpdatePlot(xData, yData, $"Сенсор {_currentSensor} (сырые)");
                }
            }
        }
        
        private async Task ProcessDataAsync()
        {
            if (_time.Length == 0 || _sensors.GetLength(0) == 0)
            {
                StatusMessage = "Сначала загрузите данные";
                return;
            }
            
            IsProcessing = true;
            StatusMessage = "Обработка данных...";
            
            try
            {
                var result = await _processingService.ProcessDataAsync(
                    _time, 
                    _sensors, 
                    _windowSize, 
                    _lowessFraction, 
                    _outlierThreshold, 
                    _outlierMethod, 
                    _filterType, 
                    _calibMethod
                );
                
                _timeClean = result.TimeClean;
                _sensorsClean = result.SensorsClean;
                _coeffsMedian = result.CoeffsMedian;
                _coeffsLsq = result.CoeffsLsq;
                
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
        
        private void UpdateCalibratedPlot()
        {
            if (_timeClean.Length > 0 && _sensorsClean.GetLength(1) > 0)
            {
                int sensorIdx = _currentSensor - 1;
                if (sensorIdx >= 0 && sensorIdx < _sensorsClean.GetLength(1))
                {
                    double coeff = _calibMethod == "median" ? 
                        _coeffsMedian[sensorIdx] : 
                        _coeffsLsq[sensorIdx];
                    
                    double[] xData = _timeClean;
                    double[] yData = new double[_timeClean.Length];
                    for (int i = 0; i < _timeClean.Length; i++)
                    {
                        yData[i] = _sensorsClean[i, sensorIdx] * coeff;
                    }
                    
                    CalibPlotViewModel.UpdatePlot(xData, yData, $"Сенсор {_currentSensor} (калиброванные)");
                }
            }
        }
        
        private void UpdateComparisonPlot()
        {
            if (_sensorsClean.GetLength(1) > 0)
            {
                ComparisonPlotViewModel.ClearPlot();
                
                // Показываем все сенсоры на одном графике
                for (int i = 0; i < Math.Min(8, _sensorsClean.GetLength(1)); i++)
                {
                    double coeff = _calibMethod == "median" ? _coeffsMedian[i] : _coeffsLsq[i];
                    
                    double[] yData = new double[_timeClean.Length];
                    for (int j = 0; j < _timeClean.Length; j++)
                    {
                        yData[j] = _sensorsClean[j, i] * coeff;
                    }
                    
                    ComparisonPlotViewModel.UpdatePlot(_timeClean, yData, $"Сенсор {i + 1}");
                }
            }
        }
        
        private async Task ExportDataAsync()
        {
            if (_timeClean.Length == 0 || _sensorsClean.GetLength(0) == 0)
            {
                StatusMessage = "Нет данных для экспорта";
                return;
            }
            
            try
            {
                await _fileService.ExportDataAsync(_timeClean, _sensorsClean, _coeffsMedian, _coeffsLsq);
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
            if (_sensorsClean.GetLength(1) == 0) return;
            
            SensorStatistics.Clear();
            
            for (int i = 0; i < Math.Min(8, _sensorsClean.GetLength(1)); i++)
            {
                var sensorData = new double[_sensorsClean.GetLength(0)];
                for (int j = 0; j < sensorData.Length; j++)
                {
                    sensorData[j] = _sensorsClean[j, i];
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
    }
    
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