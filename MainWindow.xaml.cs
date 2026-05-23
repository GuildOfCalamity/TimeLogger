using System;
using System.Windows;
using TimeLogger.Services;
using TimeLogger.ViewModels;

namespace TimeLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(new DialogService(this));
        }
    }
}