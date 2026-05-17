using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TimeLogger.Services;

public static class WorkTimeParser
{
    private static readonly Regex Pattern = new(
        @"(?:(\d+)\s*d)?\s*(?:(\d+)\s*h)?\s*(?:(\d+)\s*m)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static TimeSpan Parse(string input)
    {
        var match = Pattern.Match(input);
        if (!match.Success)
            throw new FormatException("Invalid time format. Use: 1d 2h 30m");

        int days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
        int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

        return new TimeSpan(days, hours, minutes, 0);
    }
}
