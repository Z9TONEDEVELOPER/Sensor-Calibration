using Avalonia.Controls;
using Avalonia.Interactivity;
using CalibrationApp.ViewModels;
using System.Reactive.Linq;
namespace CalibrationApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        private async void UploadData(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.LoadDataCommand.Execute();
            }
        }
        
        private async void ProcessData(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.ProcessDataCommand.Execute();
            }
        }
    }
}