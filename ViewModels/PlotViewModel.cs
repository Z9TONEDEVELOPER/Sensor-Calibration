using ReactiveUI;
using System.Reactive;
using System;
using ScottPlot;

namespace CalibrationApp.ViewModels
{
    public class PlotViewModel : ViewModelBase
    {
        private string _title = "График";
        private string _statusMessage = "Готов";
        
        // Свойства для данных
        private double[]? _lastXs;
        private double[]? _lastYs;
        private string _lastLabel = string.Empty;
        
        public PlotViewModel()
        {
            // Инициализация команд
            ZoomInCommand = ReactiveCommand.Create(ZoomIn);
            ZoomOutCommand = ReactiveCommand.Create(ZoomOut);
            ResetViewCommand = ReactiveCommand.Create(ResetView);
            SavePlotCommand = ReactiveCommand.CreateFromTask(SavePlotAsync);
        }
        
        // Свойства
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }
        
        // Свойства данных
        public double[]? LastXs
        {
            get => _lastXs;
            private set => this.RaiseAndSetIfChanged(ref _lastXs, value);
        }
        
        public double[]? LastYs
        {
            get => _lastYs;
            private set => this.RaiseAndSetIfChanged(ref _lastYs, value);
        }
        
        public string LastLabel
        {
            get => _lastLabel;
            private set => this.RaiseAndSetIfChanged(ref _lastLabel, value);
        }
        
        // Команды
        public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
        public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetViewCommand { get; }
        public ReactiveCommand<Unit, Unit> SavePlotCommand { get; }
        
        // Методы
        public void UpdatePlot(double[] xs, double[] ys, string label = "")
        {
            LastXs = xs;
            LastYs = ys;
            LastLabel = label;
            
            StatusMessage = $"Обновлено: {xs?.Length ?? 0} точек";
        }
        
        public void ClearPlot()
        {
            LastXs = null;
            LastYs = null;
            LastLabel = string.Empty;
            
            StatusMessage = "График очищен";
        }
        
        private void ZoomIn()
        {
            StatusMessage = "Увеличение (реализация в View)";
        }
        
        private void ZoomOut()
        {
            StatusMessage = "Уменьшение (реализация в View)";
        }
        
        private void ResetView()
        {
            StatusMessage = "Сброс вида (реализация в View)";
        }
        
        private async System.Threading.Tasks.Task SavePlotAsync()
        {
            // Реализация сохранения будет в View
            await System.Threading.Tasks.Task.Delay(100);
            StatusMessage = "Сохранение графика...";
        }
    }
}