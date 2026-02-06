using Avalonia.Controls;
using Avalonia.Interactivity;
using CalibrationApp.ViewModels;
using System.Reactive;

namespace CalibrationApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Подключение обработчиков событий
            var loadBtn = this.FindControl<Button>("LoadDataButton");
            if (loadBtn != null)
                loadBtn.Click += OnLoadDataClicked;
        }
        
        private void OnLoadDataClicked(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                // ReactiveCommand.Execute() возвращает IObservable; запускаем команду
                vm.LoadDataCommand.Execute().Subscribe(Observer.Create<Unit>(_ => { }));
            }
        }
    }
}