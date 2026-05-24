using System.Windows.Input;
using TimeLogger.Models;
using TimeLogger.Services;

namespace TimeLogger.ViewModels;

public class EditEntryViewModel
{
    #region [Properties]
    readonly Action<bool> _closeCallback; // to return once dialog closes
    public string Description { get; set; }
    public string Url { get; set; }
    public string TimeInput { get; set; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public TaskEntry? EditedEntry { get; private set; }
    #endregion

    public EditEntryViewModel(TaskEntry entry, Action<bool> closeCallback)
    {
        _closeCallback = closeCallback;

        Description = entry.Description;
        Url = entry.Url;
        TimeInput = Extensions.FormatTime(entry.TimeSpent);

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
    }

    void Save()
    {
        EditedEntry = new TaskEntry
        {
            Description = Description,
            Url = Url,
            TimeSpent = WorkTimeParser.Parse(TimeInput),
            Date = DateTime.Today
        };

        _closeCallback(true);
    }

    void Cancel() => _closeCallback(false);
}
