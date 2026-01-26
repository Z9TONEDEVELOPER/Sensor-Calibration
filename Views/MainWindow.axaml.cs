using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls;

namespace CalibrationApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    public void UploadData(object? sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Title = "Select Data File";
        openFileDialog.Filters.Add(new FileDialogFilter() { Name = "Data Files", Extensions = { "csv", "txt" } });
        openFileDialog.AllowMultiple = false;

        var result = openFileDialog.ShowAsync(this);
    }
}