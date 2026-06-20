using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeLogger.Models;

public class DailyTaskSummary
{
    public DateTime Date { get; set; }
    public TimeSpan TotalTime { get; set; }
    public List<string>? Descriptions { get; set; }
    public List<string>? Urls { get; set; }
}

public class TimeSpentGroup
{
    public TimeSpan TimeSpent { get; set; }
    public List<TaskEntry> Entries { get; set; }
    public List<string> Descriptions { get; set; }
    public int Count => Entries?.Count ?? 0;
}

