namespace Mocha.Core.Extensions;

public static class DateTimeOffsetExtensions
{
    public static ulong ToUnixTimeNanoseconds(this DateTimeOffset dateTimeOffset) =>
        (ulong)(dateTimeOffset - DateTimeOffset.UnixEpoch).Ticks * 100;
}
