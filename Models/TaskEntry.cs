using System;

namespace TimeLogger.Models;

public class TaskEntry
{
    public string Description { get; set; }
    public string Url { get; set; }
    public TimeSpan TimeSpent { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
}
