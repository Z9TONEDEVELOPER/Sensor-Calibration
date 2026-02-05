using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CalibrationApp.Services;
using CalibrationApp.ViewModels;
using CalibrationApp.Views;

namespace CalibrationApp
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Создаем сервисы
                var fileService = new FileService();
                var processingService = new SignalProcessingService();
                
                // Создаем ViewModel с зависимостями
                var mainViewModel = new MainWindowViewModel(fileService, processingService);
                
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}