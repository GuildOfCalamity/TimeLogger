using System;
using System.Windows;

namespace TimeLogger.Services;

public interface IDialogService
{
    void ShowInfo(string message);
    void ShowWarning(string message);
    void ShowOKCancel(string message);
}

public class DialogService : IDialogService
{
    MainWindow? Instance { get; set; } = null;

    public DialogService(MainWindow? owner = null)
    {
        Instance = owner ?? Application.Current.MainWindow as MainWindow ?? new MainWindow();
    }

    public void ShowInfo(string message)
    {
        WpfMessageBox.Show(message, false, false, owner: Instance);
    }

    public void ShowWarning(string message)
    {
        WpfMessageBox.Show(message, false, true, owner: Instance);
    }

    public void ShowOKCancel(string message)
    {
        WpfMessageBox.Show(message, true, false, owner: Instance);
    }
}
