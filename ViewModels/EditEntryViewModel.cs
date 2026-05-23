using System;
using System.Windows.Input;
using TimeLogger.Models;
using TimeLogger.Services;

namespace TimeLogger.ViewModels;

public class EditEntryViewModel
{
    private readonly Action<bool> _closeCallback;

    public string Description { get; set; }
    public string Url { get; set; }
    public string TimeInput { get; set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public TaskEntry EditedEntry { get; private set; }

    public EditEntryViewModel(TaskEntry entry, Action<bool> closeCallback)
    {
        _closeCallback = closeCallback;

        Description = entry.Description;
        Url = entry.Url;
        TimeInput = FormatTime(entry.TimeSpent);

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

    static string FormatTime(TimeSpan ts)
    {
        var parts = new List<string>();
        if (ts.Days > 0) parts.Add($"{ts.Days}d");
        if (ts.Hours > 0) parts.Add($"{ts.Hours}h");
        if (ts.Minutes > 0) parts.Add($"{ts.Minutes}m");
        return parts.Count == 0 ? "0m" : string.Join(" ", parts);
    }
}
