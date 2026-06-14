using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TimeLogger;

public enum LogLevel { Debug = 0, Info = 1, Warning = 2, Error = 3, Success = 4 }

public enum ColorTilt { Red, Orange, Yellow, Green, Blue, Purple }

public static class Extensions
{
    public static Uri IconEnabled = new Uri(@"Assets\Icon1.png", UriKind.Relative);
    public static Uri IconDisabled = new Uri(@"Assets\Icon2.png", UriKind.Relative);

    #region [Logger with automatic duplicate checking]
    static HashSet<string> _logCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static DateTime _logCacheUpdated = DateTime.Now;
    static int _repeatAllowedSeconds = 30;
    public static void WriteToLog(this string message, LogLevel level = LogLevel.Info, string fileName = "AppLog.txt", bool debugOnly = false)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_logCache.Add(message))
        {
            _logCacheUpdated = DateTime.Now;
            if (debugOnly)
            {
                Debug.WriteLine(message);
            }
            else
            {
                try { System.IO.File.AppendAllText(fileName, $"[{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt")}] [{level}] {message}{Environment.NewLine}"); }
                catch (Exception) { }
            }
        }
        else
        {
            var diff = DateTime.Now - _logCacheUpdated;
            if (diff.Seconds > _repeatAllowedSeconds)
                _logCache.Clear();
            else
            {
                if (!debugOnly)
                    Debug.WriteLine($"[WARNING] Duplicate not allowed: {diff.Seconds}secs < {_repeatAllowedSeconds}secs");
            }
        }
    }
    #endregion

    #region [Date, Time, and Formatting]
    /// <summary>
    /// Display a readable sentence as to when the time will happen.
    /// e.g. "in one second" or "in 2 days"
    /// </summary>
    public static string ToReadableTime(this TimeSpan value)
    {
        double delta = value.TotalSeconds;
        if (delta < 60) { return value.Seconds == 1 ? "one second" : value.Seconds + " seconds"; }
        if (delta < 120) { return "a minute"; }
        if (delta < 3000) { return value.Minutes + " minutes"; } // 50 * 60
        if (delta < 5400) { return "an hour"; } // 90 * 60
        if (delta < 86400) { return value.Hours + " hours"; } // 24 * 60 * 60
        if (delta < 172800) { return "one day"; } // 48 * 60 * 60
        if (delta < 2592000) { return value.Days + " days"; } // 30 * 24 * 60 * 60
        if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
        {
            int months = Convert.ToInt32(Math.Floor((double)value.Days / 30));
            return months <= 1 ? "one month" : months + " months";
        }
        int years = Convert.ToInt32(Math.Floor((double)value.Days / 365));
        return years <= 1 ? "one year" : years + " years";
    }

    /// <summary>
    /// Converts <see cref="TimeSpan"/> objects to a simple human-readable string.
    /// e.g. 420 milliseconds, 3.1 seconds, 2 minutes, 4.231 hours, etc.
    /// </summary>
    /// <param name="span"><see cref="TimeSpan"/></param>
    /// <param name="significantDigits">number of right side digits in output (precision)</param>
    /// <returns></returns>
    public static string ToTimeString(this TimeSpan span, int significantDigits = 3)
    {
        var format = $"G{significantDigits}";
        return span.TotalMilliseconds < 1000 ? span.TotalMilliseconds.ToString(format) + " milliseconds"
                : (span.TotalSeconds < 60 ? span.TotalSeconds.ToString(format) + " seconds"
                : (span.TotalMinutes < 60 ? span.TotalMinutes.ToString(format) + " minutes"
                : (span.TotalHours < 24 ? span.TotalHours.ToString(format) + " hours"
                : span.TotalDays.ToString(format) + " days")));
    }

    /// <summary>
    /// Converts <see cref="TimeSpan"/> objects to a simple human-readable string.
    /// e.g. 420 milliseconds, 3.1 seconds, 2 minutes, 4.231 hours, etc.
    /// </summary>
    /// <param name="span"><see cref="TimeSpan"/></param>
    /// <param name="significantDigits">number of right side digits in output (precision)</param>
    /// <returns></returns>
    public static string ToTimeString(this TimeSpan? span, int significantDigits = 3)
    {
        var format = $"G{significantDigits}";
        return span?.TotalMilliseconds < 1000 ? span?.TotalMilliseconds.ToString(format) + " milliseconds"
                : (span?.TotalSeconds < 60 ? span?.TotalSeconds.ToString(format) + " seconds"
                : (span?.TotalMinutes < 60 ? span?.TotalMinutes.ToString(format) + " minutes"
                : (span?.TotalHours < 24 ? span?.TotalHours.ToString(format) + " hours"
                : span?.TotalDays.ToString(format) + " days")));
    }

    /// <summary>
    /// Display a readable sentence as to when the time will happen.
    /// e.g. "in one second" or "in 2 days"
    /// </summary>
    /// <param name="value"><see cref="TimeSpan"/>the future time to compare from now</param>
    /// <returns>human friendly format</returns>
    public static string ToReadableTime(this TimeSpan value, bool reportMilliseconds = false)
    {
        double delta = value.TotalSeconds;
        if (delta < 1 && !reportMilliseconds) { return "less than one second"; }
        if (delta < 1 && reportMilliseconds) { return $"{value.TotalMilliseconds:N1} milliseconds"; }
        if (delta < 60) { return value.Seconds == 1 ? "one second" : value.Seconds + " seconds"; }
        if (delta < 120) { return "a minute"; }                  // 2 * 60
        if (delta < 3000) { return value.Minutes + " minutes"; } // 50 * 60
        if (delta < 5400) { return "an hour"; }                  // 90 * 60
        if (delta < 86400) { return value.Hours + " hours"; }    // 24 * 60 * 60
        if (delta < 172800) { return "one day"; }                // 48 * 60 * 60
        if (delta < 2592000) { return value.Days + " days"; }    // 30 * 24 * 60 * 60
        if (delta < 31104000)                                    // 12 * 30 * 24 * 60 * 60
        {
            int months = Convert.ToInt32(Math.Floor((double)value.Days / 30));
            return months <= 1 ? "one month" : months + " months";
        }
        int years = Convert.ToInt32(Math.Floor((double)value.Days / 365));
        return years <= 1 ? "one year" : years + " years";
    }

    /// <summary>
    /// Similar to <see cref="GetReadableTime(TimeSpan)"/>.
    /// </summary>
    /// <param name="timeSpan"><see cref="TimeSpan"/></param>
    /// <returns>formatted text</returns>
    public static string ToReadableString(this TimeSpan span)
    {
        var parts = new StringBuilder();
        if (span.Days > 0)
            parts.Append($"{span.Days} day{(span.Days == 1 ? string.Empty : "s")} ");
        if (span.Hours > 0)
            parts.Append($"{span.Hours} hour{(span.Hours == 1 ? string.Empty : "s")} ");
        if (span.Minutes > 0)
            parts.Append($"{span.Minutes} minute{(span.Minutes == 1 ? string.Empty : "s")} ");
        if (span.Seconds > 0)
            parts.Append($"{span.Seconds} second{(span.Seconds == 1 ? string.Empty : "s")} ");
        if (span.Milliseconds > 0)
            parts.Append($"{span.Milliseconds} millisecond{(span.Milliseconds == 1 ? string.Empty : "s")} ");

        if (parts.Length == 0) // result was less than 1 millisecond
            return $"{span.TotalMilliseconds:N4} milliseconds"; // similar to span.Ticks
        else
            return parts.ToString().Trim();
    }

    /// <summary>
    /// Display a readable sentence as to when that time happened.
    /// e.g. "5 minutes ago" or "in 2 days"
    /// </summary>
    /// <param name="value"><see cref="DateTime"/>the past/future time to compare from now</param>
    /// <returns>human friendly format</returns>
    public static string ToReadableTime(this DateTime value, bool useUTC = false)
    {
        TimeSpan ts;
        if (useUTC) { ts = new TimeSpan(DateTime.UtcNow.Ticks - value.Ticks); }
        else { ts = new TimeSpan(DateTime.Now.Ticks - value.Ticks); }

        double delta = ts.TotalSeconds;
        if (delta < 0) // in the future
        {
            delta = Math.Abs(delta);
            if (delta < 1) { return "in less than one second"; }
            if (delta < 60) { return Math.Abs(ts.Seconds) == 1 ? "in one second" : "in " + Math.Abs(ts.Seconds) + " seconds"; }
            if (delta < 120) { return "in a minute"; }
            if (delta < 3000) { return "in " + Math.Abs(ts.Minutes) + " minutes"; } // 50 * 60
            if (delta < 5400) { return "in an hour"; } // 90 * 60
            if (delta < 86400) { return "in " + Math.Abs(ts.Hours) + " hours"; } // 24 * 60 * 60
            if (delta < 172800) { return "tomorrow"; } // 48 * 60 * 60
            if (delta < 2592000) { return "in " + Math.Abs(ts.Days) + " days"; } // 30 * 24 * 60 * 60
            if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32(Math.Floor((double)Math.Abs(ts.Days) / 30));
                return months <= 1 ? "in one month" : "in " + months + " months";
            }
            int years = Convert.ToInt32(Math.Floor((double)Math.Abs(ts.Days) / 365));
            return years <= 1 ? "in one year" : "in " + years + " years";
        }
        else // in the past
        {
            if (delta < 1) { return "less than one second ago"; }
            if (delta < 60) { return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago"; }
            if (delta < 120) { return "a minute ago"; }
            if (delta < 3000) { return ts.Minutes + " minutes ago"; } // 50 * 60
            if (delta < 5400) { return "an hour ago"; } // 90 * 60
            if (delta < 86400) { return ts.Hours + " hours ago"; } // 24 * 60 * 60
            if (delta < 172800) { return "yesterday"; } // 48 * 60 * 60
            if (delta < 2592000) { return ts.Days + " days ago"; } // 30 * 24 * 60 * 60
            if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }
    }

    /// <summary>
    /// Display a readable sentence as to when the time will happen.
    /// e.g. "8 minutes 0 milliseconds"
    /// </summary>
    /// <param name="milliseconds">integer value</param>
    /// <returns>human friendly format</returns>
    public static string ToReadableTime(int milliseconds)
    {
        if (milliseconds < 0)
            throw new ArgumentException("Milliseconds cannot be negative.");

        TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);

        if (timeSpan.TotalHours >= 1)
        {
            return string.Format("{0:0} hour{1} {2:0} minute{3}",
                timeSpan.Hours, timeSpan.Hours == 1 ? "" : "s",
                timeSpan.Minutes, timeSpan.Minutes == 1 ? "" : "s");
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            return string.Format("{0:0} minute{1} {2:0} second{3}",
                timeSpan.Minutes, timeSpan.Minutes == 1 ? "" : "s",
                timeSpan.Seconds, timeSpan.Seconds == 1 ? "" : "s");
        }
        else
        {
            return string.Format("{0:0} second{1} {2:0} millisecond{3}",
                timeSpan.Seconds, timeSpan.Seconds == 1 ? "" : "s",
                timeSpan.Milliseconds, timeSpan.Milliseconds == 1 ? "" : "s");
        }
    }

    /// <summary>
    /// Formats a <see cref="TimeSpan"/> into a string of the format "hh:mm:ss" or "dd:hh:mm:ss" if days are present.
    /// </summary>
    /// <param name="ts">The TimeSpan to format.</param>
    /// <returns>A string representing the TimeSpan in a human-readable format.</returns>
    public static string ToHoursMinutesSeconds(this TimeSpan ts) => ts.Days > 0 ? (ts.Days * 24 + ts.Hours) + ts.ToString("':'mm':'ss") : ts.ToString("hh':'mm':'ss");

    /// <summary>
    /// Converts a time specified by hour, minute, and second to 100-nanosecond ticks.
    /// </summary>
    /// <remarks>Computes total seconds as hour * 3600 + minute * 60 + second, multiplies by
    /// TimeSpan.TicksPerSecond, and verifies the result fits within Int64.</remarks>
    /// <param name="hour">The number of hours.</param>
    /// <param name="minute">The number of minutes.</param>
    /// <param name="second">The number of seconds.</param>
    /// <returns>The total number of 100-nanosecond ticks that represent the specified time.</returns>
    /// <exception cref="Exception">Thrown when the computed time in ticks is outside the range that can be represented by a 64-bit signed integer.</exception>
    public static long TimeToTicks(int hour, int minute, int second)
    {
        long MaxSeconds = long.MaxValue / 10000000; // => MaxValue / TimeSpan.TicksPerSecond
        long MinSeconds = long.MinValue / 10000000; // => MinValue / TimeSpan.TicksPerSecond

        // "totalSeconds" is bounded by 2^31 * 2^12 + 2^31 * 2^8 + 2^31,
        // which is less than 2^44, meaning we won't overflow totalSeconds.
        long totalSeconds = (long)hour * 3600 + (long)minute * 60 + (long)second;

        if (totalSeconds > MaxSeconds || totalSeconds < MinSeconds)
            throw new Exception("Argument out of range: TimeSpan too long.");

        return totalSeconds * 10000000; // => totalSeconds * TimeSpan.TicksPerSecond
    }

    /// <summary>
    /// Converts a <see cref="TimeSpan"/> into a human-friendly readable string.
    /// </summary>
    /// <param name="timeSpan"><see cref="TimeSpan"/> to convert (can be negative)</param>
    /// <returns>human-friendly string representation of the given <see cref="TimeSpan"/></returns>
    public static string ToHumanFriendlyString(this TimeSpan timeSpan)
    {
        if (timeSpan == TimeSpan.Zero)
            return "0 seconds";

        bool isNegative = false;
        List<string> parts = new List<string>();

        // Check for negative TimeSpan.
        if (timeSpan < TimeSpan.Zero)
        {
            isNegative = true;
            timeSpan = timeSpan.Negate(); // Make it positive for the calculations.
        }

        if (timeSpan.Days > 0)
            parts.Add($"{timeSpan.Days} day{(timeSpan.Days > 1 ? "s" : "")}");
        if (timeSpan.Hours > 0)
            parts.Add($"{timeSpan.Hours} hour{(timeSpan.Hours > 1 ? "s" : "")}");
        if (timeSpan.Minutes > 0)
            parts.Add($"{timeSpan.Minutes} minute{(timeSpan.Minutes > 1 ? "s" : "")}");
        if (timeSpan.Seconds > 0)
            parts.Add($"{timeSpan.Seconds} second{(timeSpan.Seconds > 1 ? "s" : "")}");

        // If no large amounts so far, try milliseconds.
        if (parts.Count == 0 && timeSpan.Milliseconds > 0)
            parts.Add($"{timeSpan.Milliseconds} millisecond{(timeSpan.Milliseconds > 1 ? "s" : "")}");

        // If no milliseconds, use ticks (nanoseconds).
        if (parts.Count == 0 && timeSpan.Ticks > 0)
        {
            // A tick is equal to 100 nanoseconds. While this maps well into units of time
            // such as hours and days, any periods longer than that aren't representable in
            // a succinct fashion, e.g. a month can be between 28 and 31 days, while a year
            // can contain 365 or 366 days. A decade can have between 1 and 3 leap-years,
            // depending on when you map the TimeSpan into the calendar. This is why TimeSpan
            // does not provide a "Years" property or a "Months" property.
            // Internally TimeSpan uses long (Int64) for its values, so:
            //  - TimeSpan.MaxValue = long.MaxValue
            //  - TimeSpan.MinValue = long.MinValue
            //  - TimeSpan.TicksPerMicrosecond = 10 (not available in older .NET versions)
            parts.Add($"{(timeSpan.Ticks * 10)} microsecond{((timeSpan.Ticks * 10) > 1 ? "s" : "")}");
        }

        // Join the sections with commas & "and" for the last one.
        if (parts.Count == 1)
            return isNegative ? $"Negative {parts[0]}" : parts[0];
        else if (parts.Count == 2)
            return isNegative ? $"Negative {string.Join(" and ", parts)}" : string.Join(" and ", parts);
        else
        {
            string lastPart = parts[parts.Count - 1];
            parts.RemoveAt(parts.Count - 1);
            return isNegative ? $"Negative " + string.Join(", ", parts) + " and " + lastPart : string.Join(", ", parts) + " and " + lastPart;
        }
    }

    /// <summary>
    /// uint max = 4,294,967,295 (4.29 Gbps)
    /// </summary>
    /// <returns>formatted bit-rate string</returns>
    public static string FormatBitrate(this uint amount)
    {
        var sizes = new string[]
        {
                "bps",
                "Kbps", // kilo
                "Mbps", // mega
                "Gbps", // giga
                "Tbps", // tera
        };
        var order = amount.OrderOfMagnitude();
        var speed = amount / Math.Pow(1000, order);
        return $"{speed:0.##} {sizes[order]}";
    }

    /// <summary>
    /// ulong max = 18,446,744,073,709,551,615 (18.45 Ebps)
    /// </summary>
    /// <returns>formatted bit-rate string</returns>
    public static string FormatBitrate(this ulong amount)
    {
        var sizes = new string[]
        {
                "bps",
                "Kbps", // kilo
                "Mbps", // mega
                "Gbps", // giga
                "Tbps", // tera
                "Pbps", // peta
                "Ebps", // exa
                "Zbps", // zetta
                "Ybps"  // yotta
        };
        var order = amount.OrderOfMagnitude();
        var speed = amount / Math.Pow(1000, order);
        return $"{speed:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Returns the order of magnitude (10^3)
    /// </summary>
    public static int OrderOfMagnitude(this ulong amount) => (int)Math.Floor(Math.Log(amount, 1000));

    /// <summary>
    /// Returns the order of magnitude (10^3)
    /// </summary>
    public static int OrderOfMagnitude(this uint amount) => (int)Math.Floor(Math.Log(amount, 1000));

    /// <summary>
    /// Returns a browser-style file size for the user.
    /// </summary>
    public static string ToFileSize(this long size)
    {
        if (size < 1024) { return (size).ToString("F0") + " Bytes"; }
        if (size < Math.Pow(1024, 2)) { return (size / 1024).ToString("F0") + " KB"; }
        if (size < Math.Pow(1024, 3)) { return (size / Math.Pow(1024, 2)).ToString("F0") + " MB"; }
        if (size < Math.Pow(1024, 4)) { return (size / Math.Pow(1024, 3)).ToString("F0") + " GB"; }
        if (size < Math.Pow(1024, 5)) { return (size / Math.Pow(1024, 4)).ToString("F0") + " TB"; }
        if (size < Math.Pow(1024, 6)) { return (size / Math.Pow(1024, 5)).ToString("F0") + " PB"; }
        return (size / Math.Pow(1024, 6)).ToString("F0") + " EB";
    }

    /// <summary>
    /// Checks to see if a date is between <paramref name="begin"/> and <paramref name="end"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c> if <paramref name="dt"/> is between <paramref name="begin"/> and <paramref name="end"/>, otherwise <c>false</c>
    /// </returns>
    public static bool IsBetween(this DateTime dt, DateTime begin, DateTime end) => dt.Ticks >= begin.Ticks && dt.Ticks <= end.Ticks;

    /// <summary>
    /// Determine if the current time is between two <see cref="TimeSpan"/>s.
    /// </summary>
    /// <param name="ts">DateTime.Now.TimeOfDay</param>
    /// <param name="start">TimeSpan.Parse("23:00:00")</param>
    /// <param name="end">TimeSpan.Parse("02:30:00")</param>
    /// <returns><c>true</c> if between start and end, <c>false</c> otherwise</returns>
    public static bool IsBetween(this TimeSpan ts, TimeSpan start, TimeSpan end)
    {
        // Are we in the same day.
        if (start <= end)
            return ts >= start && ts <= end;

        // Are we on different days.
        return ts >= start || ts <= end;
    }

    /// <summary>
    /// Compares the current <see cref="DateTime.Now.TimeOfDay"/> to the 
    /// given <paramref name="start"/> and <paramref name="end"/> times.
    /// </summary>
    /// <returns><c>true</c> if between start and end, <c>false</c> otherwise</returns>
    public static bool IsNowBetween(string start = "10:00:00", string end = "14:00:00")
    {
        try
        {
            var tsNow = DateTime.Now.TimeOfDay;
            var tsStart = TimeSpan.Parse(start);
            var tsEnd = TimeSpan.Parse(end);
            if (tsStart <= tsEnd)
                return tsNow >= tsStart && tsNow <= tsEnd;

            return tsNow >= tsStart || tsNow <= tsEnd;
        }
        catch (Exception ex) { Debug.WriteLine($"[ERROR] IsNowBetween: {ex.Message}"); }
        return false;
    }

    /// <summary>
    /// Compares two <see cref="DateTime"/>s ignoring the hours, minutes and seconds.
    /// </summary>
    public static bool AreDatesSimilar(this DateTime? date1, DateTime? date2)
    {
        if (date1 is null && date2 is null)
            return true;

        if (date1 is null || date2 is null)
            return false;

        return date1.Value.Year == date2.Value.Year &&
               date1.Value.Month == date2.Value.Month &&
               date1.Value.Day == date2.Value.Day;
    }

    /// <summary>
    /// Returns the start of the day (midnight) for a given <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dateTime"><see cref="DateTime"/></param>
    /// <returns>A new DateTime representing the start of the day</returns>
    public static DateTime StartOfDay(this DateTime dateTime) => dateTime.Date; // or new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);

    /// <summary>
    /// Returns the end of the day (23:59:59.999) for a given <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dateTime"><see cref="DateTime"/></param>
    /// <returns>A new DateTime representing the end of the day</returns>
    public static DateTime EndOfDay(this DateTime dateTime) => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 23, 59, 59, 999);

    /// <summary>
    /// Returns a range of <see cref="DateTime"/> objects matching the criteria provided.
    /// </summary>
    /// <example>
    /// IEnumerable<DateTime> dateRange = DateTime.Now.GetDateRangeTo(DateTime.Now.AddDays(80));
    /// </example>
    /// <param name="self"><see cref="DateTime"/></param>
    /// <param name="toDate"><see cref="DateTime"/></param>
    /// <returns><see cref="IEnumerable{DateTime}"/></returns>
    public static IEnumerable<DateTime> GetDateRangeTo(this DateTime self, DateTime toDate)
    {
        // Query Syntax:
        //IEnumerable<int> range = Enumerable.Range(0, new TimeSpan(toDate.Ticks - self.Ticks).Days);
        //IEnumerable<DateTime> dates = from p in range select self.Date.AddDays(p);

        // Method Syntax:
        IEnumerable<DateTime> dates = Enumerable.Range(0, new TimeSpan(toDate.Ticks - self.Ticks).Days).Select(p => self.Date.AddDays(p));

        return dates;
    }

    /// <summary>
    /// Returns an inclusive sequence of <see cref="TimeSpan"/>s from <paramref name="start"/> 
    /// to <paramref name="end"/>, stepping by <paramref name="step"/> each iteration.
    /// </summary>
    /// <param name="start">The first <see cref="TimeSpan"/> in the sequence.</param>
    /// <param name="end">The last <see cref="TimeSpan"/> in the sequence (inclusive).</param>
    /// <param name="step">The increment between consecutive <see cref="TimeSpan"/>s.</param>
    /// <returns><see cref="IEnumerable{T}"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="step"/> is zero or negative,
    /// or if <paramref name="end"/> is earlier than <paramref name="start"/>.
    /// </exception>
    public static IEnumerable<TimeSpan> Range(TimeSpan start, TimeSpan end, TimeSpan step)
    {
        if (step <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(step), "Step must be positive.");
        if (end < start)
            throw new ArgumentOutOfRangeException(nameof(end), "End must be greater than or equal to start.");

        // Calculate how many steps will fit (inclusive)
        long totalTicks = end.Ticks - start.Ticks;
        long stepTicks = step.Ticks;
        int stepCount = (int)(totalTicks / stepTicks) + 1;

        return Enumerable.Range(0, stepCount).Select(i => TimeSpan.FromTicks(start.Ticks + i * stepTicks));
    }

    /// <summary>
    /// Returns an inclusive sequence of <see cref="TimeSpan"/>s from <paramref name="start"/> 
    /// to <paramref name="end"/>, stepping by 1 tick each iteration.
    /// </summary>
    /// <param name="start">The first <see cref="TimeSpan"/> in the sequence.</param>
    /// <param name="end">The last <see cref="TimeSpan"/> in the sequence (inclusive).</param>
    /// <returns><see cref="IEnumerable{T}"/></returns>
    public static IEnumerable<TimeSpan> Range(TimeSpan start, TimeSpan end)
    {
        return Range(start, end, TimeSpan.FromTicks(1));
    }
    #endregion

    #region [Precision Helpers]
    public const double Epsilon = 0.000000000001;
    public static bool IsZeroOrLess(this double value) => value < Epsilon;
    public static bool IsZero(this double value) => Math.Abs(value) < Epsilon;
    public static bool IsInvalid(this double value)
    {
        if (value == double.NaN || value == double.NegativeInfinity || value == double.PositiveInfinity)
            return true;

        return false;
    }
    public static bool IsInvalidOrZero(this double value)
    {
        if (value == double.NaN || value == double.NegativeInfinity || value == double.PositiveInfinity || value <= 0)
            return true;

        return false;
    }
    public static bool IsInvalidOrZero(this System.Windows.Size value)
    {
        if (value.Width == double.NaN || value.Width == double.NegativeInfinity || value.Width == double.PositiveInfinity || value.Width <= 0)
            return true;
        if (value.Height == double.NaN || value.Height == double.NegativeInfinity || value.Height == double.PositiveInfinity || value.Height <= 0)
            return true;

        return false;
    }
    public static bool IsOne(this double value)
    {
        return Math.Abs(value) >= 1d - Epsilon && Math.Abs(value) <= 1d + Epsilon;
    }
    public static bool AreClose(this double left, double right)
    {
        if (left == right)
            return true;

        double a = (Math.Abs(left) + Math.Abs(right) + 10.0d) * Epsilon;
        double b = left - right;
        return (-a < b) && (a > b);
    }

    /// <summary>
    /// Clamping function for any value of type <see cref="IComparable{T}"/>.
    /// </summary>
    /// <param name="val">initial value</param>
    /// <param name="min">lowest range</param>
    /// <param name="max">highest range</param>
    /// <returns>clamped value</returns>
    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        return val.CompareTo(min) < 0 ? min : (val.CompareTo(max) > 0 ? max : val);
    }
    #endregion

    #region [Color Brush Methods]
    /// <summary>
    /// Blends two Color values.
    /// </summary>
    /// <param name="first">Starting color.</param>
    /// <param name="second">Ending color.</param>
    /// <param name="amount">
    /// Blend amount from 0.0 to 1.0
    ///   0.0 = first color
    ///   1.0 = second color
    ///   0.5 = equal mix
    /// </param>
    public static Color Blend(Color first, Color second, double amount = 0.5)
    {
        amount = Math.Max(0.0, Math.Min(1.0, amount));

        return Color.FromArgb(
            (byte)(first.A + ((second.A - first.A) * amount)),
            (byte)(first.R + ((second.R - first.R) * amount)),
            (byte)(first.G + ((second.G - first.G) * amount)),
            (byte)(first.B + ((second.B - first.B) * amount)));
    }

    /// <summary>
    /// Blends two SolidColorBrush instances.
    /// </summary>
    public static SolidColorBrush Blend(SolidColorBrush first, SolidColorBrush second, double amount = 0.5)
    {
        if (first == null)
            throw new ArgumentNullException(nameof(first));

        if (second == null)
            throw new ArgumentNullException(nameof(second));

        var brush = new SolidColorBrush(
            Blend(first.Color, second.Color, amount));

        if (brush.CanFreeze)
        {
            brush.Freeze();
        }

        return brush;
    }

    public static (byte A, byte R, byte G, byte B) ParseHexColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            WriteToLog("Hex color string cannot be null or empty.", LogLevel.Warning);

        // Normalize: remove leading '#'
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length != 6 && hex.Length != 8)
            WriteToLog("Hex color string must be 6 (RRGGBB) or 8 (AARRGGBB) characters.", LogLevel.Warning);

        int index = 0;

        byte a = 255; // default opaque

        if (hex.Length == 8)
        {
            a = Convert.ToByte(hex.Substring(index, 2), 16);
            index += 2;
        }

        byte r = Convert.ToByte(hex.Substring(index, 2), 16);
        byte g = Convert.ToByte(hex.Substring(index + 2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(index + 4, 2), 16);

        return (a, r, g, b);
    }

    public static RadialGradientBrush CreateRadialBrush(string hex, double opacity = 0.6)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            WriteToLog("Hex color string cannot be null or empty.", LogLevel.Warning);
            return null;
        }

        // Normalize input (strip leading # if present)
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length != 6)
        {
            WriteToLog("Hex color string must be 6 characters (RRGGBB).", LogLevel.Warning);
            return null;
        }

        // Parse hex into Color
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        var baseColor = Color.FromRgb(r, g, b);

        // Create lighter/darker variants
        Color lighter = Colors.White;
        //Color lighter = BrightenGamma(baseColor, 2.0); // 100% lighter
        Color darker = DarkenGamma(baseColor, 0.1); // 90% darker

        var brush = new RadialGradientBrush
        {
            Opacity = opacity,
            GradientOrigin = new System.Windows.Point(0.75, 0.25),
            Center = new System.Windows.Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5
        };

        brush.GradientStops.Add(new GradientStop(lighter, 0.0));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.6));
        brush.GradientStops.Add(new GradientStop(darker, 1.0));

        return brush;
    }

    /// <summary>
    /// Gamma‑corrected brighten (perceptually smoother)
    /// <code>
    ///   var brighter = BrightenGamma(baseColor, 1.5); // 50% brighter
    /// </code>
    /// </summary>
    public static Color BrightenGamma(Color color, double factor = 1.5, double gamma = 2.2)
    {
        // Convert sRGB ⇨ linear
        double r = Math.Pow(color.R / 255.0, gamma);
        double g = Math.Pow(color.G / 255.0, gamma);
        double b = Math.Pow(color.B / 255.0, gamma);

        // Apply brighten factor in linear space
        r = Math.Min(1.0, r * factor);
        g = Math.Min(1.0, g * factor);
        b = Math.Min(1.0, b * factor);

        // Convert back linear ⇨ sRGB
        byte R = (byte)(Math.Pow(r, 1.0 / gamma) * 255);
        byte G = (byte)(Math.Pow(g, 1.0 / gamma) * 255);
        byte B = (byte)(Math.Pow(b, 1.0 / gamma) * 255);

        return Color.FromArgb(color.A, R, G, B);
    }

    /// <summary>
    /// Gamma‑corrected darken (perceptually smoother)
    /// <code>
    ///   var darker = DarkenGamma(baseColor, 0.7); // Darken to 70% brightness
    /// </code>
    /// </summary>
    public static Color DarkenGamma(Color color, double factor = 0.7, double gamma = 2.2)
    {
        // factor < 1.0 will darken, factor = 1.0 no change
        if (factor > 1.0) factor = 1.0;
        if (factor < 0.0) factor = 0.0;

        // Convert sRGB ⇨ linear
        double r = Math.Pow(color.R / 255.0, gamma);
        double g = Math.Pow(color.G / 255.0, gamma);
        double b = Math.Pow(color.B / 255.0, gamma);

        // Apply darken factor in linear space
        r *= factor;
        g *= factor;
        b *= factor;

        // Convert back linear ⇨ sRGB
        byte R = (byte)(Math.Pow(r, 1.0 / gamma) * 255);
        byte G = (byte)(Math.Pow(g, 1.0 / gamma) * 255);
        byte B = (byte)(Math.Pow(b, 1.0 / gamma) * 255);

        return Color.FromArgb(color.A, R, G, B);
    }

    /// <summary>
    /// Generates a random <see cref="System.Windows.Media.Color"/>.
    /// </summary>
    /// <returns><see cref="System.Windows.Media.Color"/> with 255 alpha</returns>
    public static System.Windows.Media.Color GenerateRandomColor()
    {
        return System.Windows.Media.Color.FromRgb((byte)new Random().Next(0, 256), (byte)new Random().Next(0, 256), (byte)new Random().Next(0, 256));
    }

    /// <summary>
    /// Generates a random <see cref="LinearGradientBrush"/> using two <see cref="System.Windows.Media.Color"/>s.
    /// </summary>
    /// <returns><see cref="LinearGradientBrush"/></returns>
    public static LinearGradientBrush CreateGradientBrush(Color c1, Color c2)
    {
        var gs1 = new GradientStop(c1, 0);
        var gs3 = new GradientStop(c2, 1);
        var gsc = new GradientStopCollection { gs1, gs3 };
        var lgb = new LinearGradientBrush
        {
            ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(0, 1),
            GradientStops = gsc
        };
        return lgb;
    }

    /// <summary>
    /// Generates a random <see cref="LinearGradientBrush"/> using three <see cref="System.Windows.Media.Color"/>s.
    /// </summary>
    /// <returns><see cref="LinearGradientBrush"/></returns>
    public static LinearGradientBrush CreateGradientBrush(Color c1, Color c2, Color c3)
    {
        var gs1 = new GradientStop(c1, 0);
        var gs2 = new GradientStop(c2, 0.6);
        var gs3 = new GradientStop(c3, 1);
        var gsc = new GradientStopCollection { gs1, gs2, gs3 };
        var lgb = new LinearGradientBrush
        {
            ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(0, 1),
            GradientStops = gsc
        };
        return lgb;
    }

    /// <summary>
    /// Generates a random <see cref="SolidColorBrush"/>.
    /// </summary>
    /// <returns><see cref="SolidColorBrush"/> with 255 alpha</returns>
    public static SolidColorBrush CreateRandomBrush()
    {
        byte r = (byte)new Random().Next(0, 256);
        byte g = (byte)new Random().Next(0, 256);
        byte b = (byte)new Random().Next(0, 256);
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
    }

    /// <summary>
    /// Avoids near-white values by using high saturation ranges prevent desaturation.
    /// </summary>
    public static SolidColorBrush CreateRandomLightBrush(byte alpha = 255)
    {
        return CreateRandomHsvBrush(
            hue: new Random().NextDouble() * 360.0,
            saturation: Lerp(0.65, 1.0, new Random().NextDouble()), // high saturation to avoid gray
            value: Lerp(0.85, 1.0, new Random().NextDouble()),      // bright
            alpha: alpha);
    }

    /// <summary>
    /// Avoids near-black values by using high saturation ranges prevent desaturation.
    /// </summary>
    public static SolidColorBrush CreateRandomDarkBrush(byte alpha = 255)
    {
        return CreateRandomHsvBrush(
            hue: new Random().NextDouble() * 360.0,
            saturation: Lerp(0.65, 1.0, new Random().NextDouble()), // high saturation to avoid gray
            value: Lerp(0.2, 0.45, new Random().NextDouble()),      // dark
            alpha: alpha);
    }

    public static SolidColorBrush CreateRandomHsvBrush(double hue, double saturation, double value, byte alpha)
    {
        var (r, g, b) = HsvToRgb(hue, saturation, value);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, r, g, b));
        if (brush.CanFreeze)
            brush.Freeze(); // freeze for performance (if animation is not needed)
        return brush;
    }

    static (byte r, byte g, byte b) HsvToRgb(double h, double s, double v)
    {
        // h: [0,360), s,v: [0,1]
        if (s <= 0.00001)
        {
            // If saturation is approx zero then return achromatic (grey)
            byte grey = (byte)Math.Round(v * 255.0);
            return (grey, grey, grey);
        }

        h = (h % 360 + 360) % 360; // normalize
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = v - c;
        double r1, g1, b1;
        if (h < 60) { r1 = c; g1 = x; b1 = 0; }
        else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
        else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
        else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
        else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
        else { r1 = c; g1 = 0; b1 = x; }
        byte r = (byte)Math.Round((r1 + m) * 255.0);
        byte g = (byte)Math.Round((g1 + m) * 255.0);
        byte b = (byte)Math.Round((b1 + m) * 255.0);
        return (r, g, b);
    }

    static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        // Hue
        if (delta < 0.00001) { h = 0; }
        else if (max == rd) { h = 60 * (((gd - bd) / delta) % 6); }
        else if (max == gd) { h = 60 * (((bd - rd) / delta) + 2); }
        else { h = 60 * (((rd - gd) / delta) + 4); }
        if (h < 0) { h += 360; }

        // Saturation
        s = (max <= 0) ? 0 : delta / max;

        // Value
        v = max;
    }

    static double Lerp(double a, double b, double t) => a + (b - a) * t;

    /// <summary>
    /// Generates a random <see cref="SolidColorBrush"/> based on a given <see cref="ColorTilt"/>.
    /// </summary>
    public static SolidColorBrush CreateRandomLightBrush(ColorTilt tilt, double tiltStrength = 30, byte alpha = 255)
    {
        double hue = GetTiltedHue(tilt, tiltStrength);
        double saturation = Lerp(0.65, 1.0, new Random().NextDouble()); // high saturation to avoid gray
        double value = Lerp(0.85, 1.0, new Random().NextDouble());      // bright
        return CreateBrushFromHsv(hue, saturation, value, alpha);
    }

    /// <summary>
    /// Generates a random <see cref="SolidColorBrush"/> based on a given <see cref="ColorTilt"/>.
    /// </summary>
    public static SolidColorBrush CreateRandomDarkBrush(ColorTilt tilt, double tiltStrength = 30, byte alpha = 255)
    {
        double hue = GetTiltedHue(tilt, tiltStrength);
        double saturation = Lerp(0.65, 1.0, new Random().NextDouble()); // high saturation to avoid gray
        double value = Lerp(0.2, 0.45, new Random().NextDouble());      // dark
        return CreateBrushFromHsv(hue, saturation, value, alpha);
    }

    /// <summary>
    /// Generates a random <see cref="SolidColorBrush"/> based on a given dictionary of <see cref="ColorTilt"/>s.
    /// </summary>
    public static SolidColorBrush CreateRandomLightBrush(Dictionary<ColorTilt, double> tiltWeights, double tiltStrength = 30, byte alpha = 255)
    {
        double hue = GetBlendedTiltedHue(tiltWeights, tiltStrength);
        double saturation = Lerp(0.65, 1.0, new Random().NextDouble()); // high saturation to avoid gray
        double value = Lerp(0.85, 1.0, new Random().NextDouble());      // bright
        return CreateBrushFromHsv(hue, saturation, value, alpha);
    }

    /// <summary>
    /// Generates a random <see cref="SolidColorBrush"/> based on a given dictionary of <see cref="ColorTilt"/>s.
    /// </summary>
    public static SolidColorBrush CreateRandomDarkBrush(Dictionary<ColorTilt, double> tiltWeights, double tiltStrength = 30, byte alpha = 255)
    {
        double hue = GetBlendedTiltedHue(tiltWeights, tiltStrength);
        double saturation = Lerp(0.65, 1.0, new Random().NextDouble()); // high saturation to avoid gray
        double value = Lerp(0.2, 0.45, new Random().NextDouble());      // dark
        return CreateBrushFromHsv(hue, saturation, value, alpha);
    }

    static SolidColorBrush CreateBrushFromHsv(double hue, double saturation, double value, byte alpha)
    {
        var (r, g, b) = HsvToRgb(hue, saturation, value);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, r, g, b));
        if (brush.CanFreeze) { brush.Freeze(); }
        return brush;
    }

    static double GetTiltedHue(ColorTilt tilt, double variance = 30)
    {
        // Hue centers in degrees for basic colors
        double centerHue;
        switch (tilt)
        {
            case ColorTilt.Red:
                centerHue = 0.0;      // also wraps near 360
                break;
            case ColorTilt.Orange:
                centerHue = 30.0;
                break;
            case ColorTilt.Yellow:
                centerHue = 60.0;
                break;
            case ColorTilt.Green:
                centerHue = 120.0;
                break;
            case ColorTilt.Blue:
                centerHue = 240.0;
                break;
            case ColorTilt.Purple:
                centerHue = 280.0; // between magenta (300) and blue
                break;
            default:
                centerHue = 0.0;
                break;
        }

        // Clamp variance to [0,180]
        variance = Math.Max(0, Math.Min(variance, 180));

        // Allow ±30° variation for variety
        double minHue = centerHue - variance;
        double maxHue = centerHue + variance;

        double hue = minHue + new Random().NextDouble() * (maxHue - minHue);
        // Wrap around 0–360
        if (hue < 0) { hue += 360; }
        if (hue >= 360) { hue -= 360; }

        return hue;
    }

    static double GetBlendedTiltedHue(Dictionary<ColorTilt, double> tiltWeights, double tiltStrength)
    {
        if (tiltWeights == null || tiltWeights.Count == 0)
            return new Random().NextDouble() * 360.0;

        // Normalize weights
        double total = tiltWeights.Values.Sum();
        if (total <= 0) return new Random().NextDouble() * 360.0;

        // Pick a tilt based on weighted random
        double roll = new Random().NextDouble() * total;
        double cumulative = 0;
        ColorTilt chosenTilt = tiltWeights.First().Key;

        foreach (var kvp in tiltWeights)
        {
            cumulative += kvp.Value;
            if (roll <= cumulative)
            {
                chosenTilt = kvp.Key;
                break;
            }
        }

        // Get center hue for chosen tilt
        double centerHue = GetCenterHue(chosenTilt);

        // Clamp tiltStrength
        tiltStrength = Math.Max(0, Math.Min(tiltStrength, 180));

        // ± tiltStrength variation
        double minHue = centerHue - tiltStrength;
        double maxHue = centerHue + tiltStrength;

        double hue = minHue + new Random().NextDouble() * (maxHue - minHue);
        if (hue < 0) hue += 360;
        if (hue >= 360) hue -= 360;

        return hue;
    }

    static double GetCenterHue(ColorTilt tilt)
    {
        switch (tilt)
        {
            case ColorTilt.Red: return 0.0;
            case ColorTilt.Orange: return 30.0;
            case ColorTilt.Yellow: return 60.0;
            case ColorTilt.Green: return 120.0;
            case ColorTilt.Blue: return 240.0;
            case ColorTilt.Purple: return 280.0;
            default: return 0.0;
        }
    }

    public static SolidColorBrush BrightenBrush(SolidColorBrush brush, double amount)
    {
        if (brush == null)
            throw new ArgumentNullException(nameof(brush));

        // Clamp amount to [0, 1]
        amount = Math.Max(0, Math.Min(amount, 1));

        Color color = brush.Color;

        // Convert to HSV
        double h, s, v;
        RgbToHsv(color.R, color.G, color.B, out h, out s, out v);

        // Increase brightness
        v = Math.Min(1.0, v + amount);

        // Convert back to RGB
        var (r, g, b) = HsvToRgb(h, s, v);

        var newBrush = new SolidColorBrush(Color.FromArgb(color.A, r, g, b));
        if (newBrush.CanFreeze) { newBrush.Freeze(); }
        return newBrush;
    }

    public static SolidColorBrush DarkenBrush(SolidColorBrush brush, double amount)
    {
        if (brush == null)
            throw new ArgumentNullException(nameof(brush));

        // Clamp amount to [0, 1]
        amount = Math.Max(0, Math.Min(amount, 1));

        Color color = brush.Color;

        // Convert to HSV
        double h, s, v;
        RgbToHsv(color.R, color.G, color.B, out h, out s, out v);

        // Decrease brightness
        v = Math.Max(0.0, v - amount);

        // Convert back to RGB
        var (r, g, b) = HsvToRgb(h, s, v);

        var newBrush = new SolidColorBrush(Color.FromArgb(color.A, r, g, b));
        if (newBrush.CanFreeze) newBrush.Freeze();
        return newBrush;
    }

    /// <summary><code>
    ///  /* Brighten by 20%, no saturation change */
    ///  var brighter = Extensions.AdjustBrush(baseBrush, brightnessDelta: 0.2);
    ///  /* Darken by 30%, mute by 20% */
    ///  var darkerMuted = Extensions.AdjustBrush(baseBrush, brightnessDelta: -0.3, saturationDelta: -0.2);
    ///  /* Keep brightness, boost saturation */
    ///  var vivid = Extensions.AdjustBrush(baseBrush, saturationDelta: 0.3);
    /// </code></summary>
    public static SolidColorBrush AdjustBrush(SolidColorBrush brush, double brightnessDelta = 0.0, double saturationDelta = 0.0)
    {
        if (brush == null)
            throw new ArgumentNullException(nameof(brush));

        Color color = brush.Color;

        // Convert to HSV
        double h, s, v;
        RgbToHsv(color.R, color.G, color.B, out h, out s, out v);

        // Apply deltas
        v = Math.Max(0.0, Math.Min(1.0, v + brightnessDelta));
        s = Math.Max(0.0, Math.Min(1.0, s + saturationDelta));

        // Convert back to RGB
        var (r, g, b) = HsvToRgb(h, s, v);

        var adjusted = new SolidColorBrush(Color.FromArgb(color.A, r, g, b));
        if (adjusted.CanFreeze) { adjusted.Freeze(); }
        return adjusted;
    }

    public static SolidColorBrush ShiftSaturation(SolidColorBrush brush, double amount)
    {
        if (brush == null)
            throw new ArgumentNullException(nameof(brush));

        // amount can be positive (more vivid) or negative (more muted)
        // Clamp to [-1, 1] so we don't overshoot
        amount = Math.Max(-1, Math.Min(amount, 1));

        Color color = brush.Color;

        // Convert to HSV
        double h, s, v;
        RgbToHsv(color.R, color.G, color.B, out h, out s, out v);

        // Adjust saturation
        s = Math.Max(0.0, Math.Min(1.0, s + amount));

        // Convert back to RGB
        var (r, g, b) = HsvToRgb(h, s, v);

        var newBrush = new SolidColorBrush(Color.FromArgb(color.A, r, g, b));
        if (newBrush.CanFreeze)
            newBrush.Freeze();

        return newBrush;
    }

    /// <summary>
    /// Returns the Euclidean distance between two <see cref="System.Windows.Media.Color"/>s.
    /// </summary>
    /// <param name="color1">1st <see cref="System.Windows.Media.Color"/></param>
    /// <param name="color2">2nd <see cref="System.Windows.Media.Color"/></param>
    public static double ColorDistance(System.Windows.Media.Color color1, System.Windows.Media.Color color2)
    {
        return Math.Sqrt(Math.Pow(color1.R - color2.R, 2) + Math.Pow(color1.G - color2.G, 2) + Math.Pow(color1.B - color2.B, 2));
    }

    /// <summary>
    /// Fetch all <see cref="System.Windows.Media.Brushes"/>.
    /// </summary>
    /// <returns><see cref="List{T}"/></returns>
    public static List<Brush> GetAllMediaBrushes()
    {
        List<Brush> brushes = new List<Brush>();
        Type brushesType = typeof(Brushes);

        //TypeAttributes ta = typeof(Brushes).Attributes;
        //Debug.WriteLine($"[INFO] TypeAttributes: {ta}");

        // Iterate through the static properties of the Brushes class type.
        foreach (PropertyInfo pi in brushesType.GetProperties(BindingFlags.Static | BindingFlags.Public))
        {
            // Check if the property type is Brush/SolidColorBrush
            if (pi != null && (pi.PropertyType == typeof(Brush) || pi.PropertyType == typeof(SolidColorBrush)))
            {
                if (pi.Name.Contains("Transparent"))
                    continue;

                Debug.WriteLine($"[INFO] Adding brush '{pi.Name}'");

                // Get the brush value from the static property
                var br = (Brush?)pi?.GetValue(null, null);
                if (br != null)
                    brushes.Add(br);
            }
        }
        return brushes;
    }

    /// <summary>
    /// 'BitmapCacheBrush','DrawingBrush','GradientBrush','ImageBrush',
    /// 'LinearGradientBrush','RadialGradientBrush','SolidColorBrush',
    /// 'TileBrush','VisualBrush','ImplicitInputBrush'
    /// </summary>
    /// <returns><see cref="List{T}"/></returns>
    public static List<Type> GetAllDerivedBrushClasses()
    {
        List<Type> derivedBrushes = new List<Type>();
        // Get the assembly containing the Brush class
        Assembly assembly = typeof(Brush).Assembly;
        try
        {   // Iterate through all types in the assembly
            foreach (Type type in assembly.GetTypes())
            {
                // Check if the type is a subclass of Brush
                if (type.IsSubclassOf(typeof(Brush)))
                {
                    //Debug.WriteLine($"[INFO] Adding type '{type.Name}'");
                    derivedBrushes.Add(type);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetAllDerivedBrushClasses: {ex.Message}");
        }
        return derivedBrushes;
    }
    #endregion

    #region [Controls and UIElements]
    /// <summary>
    /// Fetch all derived types from a super class.
    /// </summary>
    /// <returns><see cref="List{T}"/></returns>
    public static List<Type> GetDerivedSubClasses<T>(T objectClass) where T : class
    {
        List<Type> derivedClasses = new List<Type>();
        // Get the assembly containing the base class
        Assembly assembly = typeof(T).Assembly;
        try
        {   // Iterate through all types in the assembly
            foreach (Type type in assembly.GetTypes())
            {
                // Check if the type is a subclass of T
                if (type.IsSubclassOf(typeof(T)))
                {
                    //Debug.WriteLine($"[INFO] Adding subclass type '{type.Name}'");
                    derivedClasses.Add(type);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetDerivedClasses: {ex.Message}");
        }
        return derivedClasses;
    }

    /// <summary>
    /// Example of <see cref="UIElement"/> traversal.
    /// </summary>
    public static void IterateAllUIElements(DockPanel dock)
    {
        UIElementCollection uic = dock.Children;

        foreach (Grid uie in uic)
            uie.Background = new SolidColorBrush(Colors.Green);

        foreach (Border uie in uic)
            uie.Background = new SolidColorBrush(Colors.Orange);

        foreach (StackPanel uie in uic)
            uie.Background = new SolidColorBrush(Colors.Blue);

        foreach (Button uie in uic)
        {
            uie.Background = new SolidColorBrush(Colors.Yellow);

            // Example of restoring default properties
            var locallySetProperties = uie.GetLocalValueEnumerator();
            while (locallySetProperties.MoveNext())
            {
                DependencyProperty propertyToClear = locallySetProperties.Current.Property;
                if (!propertyToClear.ReadOnly)
                    uie.ClearValue(propertyToClear);
            }
        }
    }

    /// <summary>
    /// FindVisualChild element in a control group.
    /// <code>
    ///   /* Getting the ContentPresenter of myListBoxItem */
    ///   var myContentPresenter = FindVisualChild<ContentPresenter>(myListBoxItem);
    ///   
    ///   /* Getting the currently selected ListBoxItem. Note that the ListBox must have IsSynchronizedWithCurrentItem set to True for this to work */
    ///   var myListBoxItem = (ListBoxItem)(myListBox.ItemContainerGenerator.ContainerFromItem(myListBox.Items.CurrentItem));
    ///   
    ///   /* Finding textBlock from the DataTemplate that is set on that ContentPresenter */
    ///   var myDataTemplate = myContentPresenter.ContentTemplate;
    ///   var myTextBlock = (TextBlock)myDataTemplate.FindName("textBlock", myContentPresenter);
    ///
    ///   /* Do something to the DataTemplate-generated TextBlock */
    ///   MessageBox.Show($"The text of the TextBlock of the selected list item: {myTextBlock.Text}");
    /// </code>
    /// </summary>
    public static TChildItem? FindVisualChild<TChildItem>(this DependencyObject obj) where TChildItem : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is TChildItem)
                return (TChildItem)child;
            var childOfChild = FindVisualChild<TChildItem>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }

    /// <summary>
    /// Find & return a WPF control based on its resource key name.
    /// </summary>
    public static T FindControl<T>(this FrameworkElement control, string resourceKey) where T : FrameworkElement
    {
        return (T)control.FindResource(resourceKey);
    }

    /// <summary>
    /// Find & return a WPF control based on its resource key name.
    /// </summary>
    public static T? FindChild<T>(this DependencyObject parent, string childName) where T : FrameworkElement
    {
        if (parent == null)
            return null;

        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T fe && fe.Name == childName)
                return fe;

            var result = FindChild<T>(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// <code>
    ///   IEnumerable<DependencyObject> cntrls = this.FindUIElements();
    /// </code>
    /// If you're struggling to get this working and finding that your Window (for instance)
    /// has zero visual children, try running this method in the "_Loaded" event handler. 
    /// If you call this from a constructor (even after InitializeComponent), the visual 
    /// children won't be added to the VisualTree yet and it won't work properly.
    /// </summary>
    /// <param name="parent">some parent control like <see cref="System.Windows.Window"/></param>
    /// <returns>list of <see cref="IEnumerable{DependencyObject}"/></returns>
    public static IEnumerable<DependencyObject> FindUIElements(this DependencyObject parent)
    {
        if (parent == null)
            yield break;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject o = VisualTreeHelper.GetChild(parent, i);
            foreach (DependencyObject obj in FindUIElements(o))
            {
                if (obj == null)
                    continue;
                if (obj is UIElement ret)
                    yield return ret;
            }
        }
        yield return parent;
    }

    /// <summary>
    /// Find & return the first ancestor of a given type in the visual tree.
    /// </summary>
    public static T? FindAncestor<T>(this DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match)
                return match;

            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    /// <summary>
    /// Should be called on UI thread only.
    /// </summary>
    public static void HideAllVisualChildren<T>(this UIElementCollection coll) where T : UIElementCollection
    {
        // Casting the UIElementCollection into List
        List<FrameworkElement> lstElement = coll.Cast<FrameworkElement>().ToList();
        var lstControl = lstElement.OfType<Control>();
        foreach (Control control in lstControl)
        {
            if (control == null)
                continue;
            control.Visibility = System.Windows.Visibility.Hidden;
        }
    }

    /// <summary>
    /// Should be called on UI thread only.
    /// </summary>
    public static IEnumerable<Control> GetAllControls<T>(this UIElementCollection coll) where T : UIElementCollection
    {
        // Casting the UIElementCollection into List
        List<FrameworkElement> lstElement = coll.Cast<FrameworkElement>().ToList();
        var lstControl = lstElement.OfType<Control>();
        foreach (Control control in lstControl)
        {
            if (control == null)
                continue;
            yield return control;
        }
    }
    #endregion

    #region [General Helpers]
    public static string FormatTime(TimeSpan ts)
    {
        List<string> parts = new();
        if (ts.Days > 0) parts.Add($"{ts.Days}d");
        if (ts.Hours > 0) parts.Add($"{ts.Hours}h");
        if (ts.Minutes > 0) parts.Add($"{ts.Minutes}m");
        return parts.Count == 0 ? "0m" : string.Join(" ", parts);
    }

    public static bool IsSameBusinessWeek(DateTime date)
    {
        var today = DateTime.Today;
        int diff = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
        if (diff < 0) diff += 7;

        var monday = today.AddDays(-diff);
        var friday = monday.AddDays(4);

        return date >= monday && date <= friday;
    }

    public static bool IsSameSevenDayWeek(DateTime date)
    {
        var today = DateTime.Today;
        var sevenDaysAgo = today.AddDays(-6); // inclusive 7‑day window
        return date.Date >= sevenDaysAgo && date.Date <= today;
    }

    /// <summary>
    /// Tries to execute the given <paramref name="action"/> for a maximum of 
    /// <paramref name="max"/> time stepping by 1 additional second each iteration.
    /// </summary>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise</returns>
    public static bool TryForThisLongOrUntilSuccessful(Action action, TimeSpan max)
    {
        if (max <= TimeSpan.FromSeconds(1))
            max = TimeSpan.FromSeconds(2);

        bool success = false;

        foreach (var ts in Extensions.Range(TimeSpan.FromSeconds(1), max, TimeSpan.FromSeconds(1)))
        {
            try
            {
                action();
                success = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.WriteLine($"Trying again in {ts.ToReadableString()}…");
                Thread.Sleep(ts);
            }

            if (success)
                break; // Exit the loop if successful
        }
        return success;
    }

    /// <summary>
    /// Tries to execute the given <paramref name="action"/> for a maximum of
    /// <paramref name="max"/> time, stepping by 1 additional second each iteration.
    /// </summary>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static async Task<bool> TryForThisLongOrUntilSuccessfulAsync(Action action, TimeSpan max)
    {
        if (max <= TimeSpan.FromSeconds(1))
            max = TimeSpan.FromSeconds(2);

        bool success = false;

        foreach (var ts in Extensions.Range(TimeSpan.FromSeconds(1), max, TimeSpan.FromSeconds(1)))
        {
            try
            {
                action();
                success = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] {ex.Message}");
                Debug.WriteLine($"Trying again in {ts.ToReadableString()}…");

                await Task.Delay(ts).ConfigureAwait(false);
            }

            if (success)
                break; // Exit the loop if successful
        }
        return success;
    }
    #endregion
}
