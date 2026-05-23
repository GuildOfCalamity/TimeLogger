using System.Windows;
using TimeLogger.Services;
using TimeLogger.ViewModels;

namespace TimeLogger;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(new DialogService(this));
    }
}