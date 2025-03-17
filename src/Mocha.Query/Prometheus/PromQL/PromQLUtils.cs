// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Mocha.Query.Prometheus.PromQL;

public partial class PromQLUtils
{
    public static bool TryParseDuration(string? duration, out TimeSpan timeSpan)
    {
        if (string.IsNullOrWhiteSpace(duration))
        {
            timeSpan = TimeSpan.Zero;
            return true;
        }

        var m = FormatDurationRegex().Match(duration.ToLowerInvariant());
        if (!m.Success)
        {
            timeSpan = TimeSpan.Zero;
            return false;
        }

        var weeks = m.Groups["weeks"].Success ? int.Parse(m.Groups["weeks"].Value) : 0;
        var days = m.Groups["days"].Success ? int.Parse(m.Groups["days"].Value) : 0;
        var hours = m.Groups["hours"].Success ? int.Parse(m.Groups["hours"].Value) : 0;
        var minutes = m.Groups["minutes"].Success ? int.Parse(m.Groups["minutes"].Value) : 0;
        var seconds = m.Groups["seconds"].Success ? int.Parse(m.Groups["seconds"].Value) : 0;
        var milliseconds = m.Groups["milliseconds"].Success ? int.Parse(m.Groups["milliseconds"].Value) : 0;

        timeSpan = new TimeSpan(
            weeks * 7 + days,
            hours,
            minutes,
            seconds,
            milliseconds
        );
        return true;
    }

    [GeneratedRegex(
        @"^((?<weeks>\d+)w)?((?<days>\d+)d)?((?<hours>\d+)h)?((?<minutes>\d+)m)?((?<seconds>\d+)s)?((?<milliseconds>\d+)ms)?$",
        RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.RightToLeft |
        RegexOptions.CultureInvariant)]
    private static partial Regex FormatDurationRegex();
}
