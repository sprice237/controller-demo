using System;

namespace ControllerDemo.Common
{
    public static class DateTimeExtensions
    {
        public static DateTime Round(this DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }

        public static DateTime Floor(this DateTime dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            return new DateTime(dt.Ticks - delta, dt.Kind);
        }

        public static DateTime Ceiling(this DateTime dt, TimeSpan d)
        {
            var floor = dt.Floor(d);
            return new DateTime(floor.Ticks + d.Ticks, floor.Kind);
        }

        public static DateTimeOffset Floor(this DateTimeOffset dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            return new DateTimeOffset(dt.Ticks - delta, dt.Offset);
        }

        public static DateTimeOffset Ceiling(this DateTimeOffset dt, TimeSpan d)
        {
            var floor = dt.Floor(d);
            return new DateTimeOffset(floor.Ticks + d.Ticks, floor.Offset);
        }
    }
}